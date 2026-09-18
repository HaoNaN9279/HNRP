// <copyright file="DrawShadowPass.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    /// <summary>
    /// 绘制所有光源阴影到一张 <see cref="Texture2DArray"/> 阴影图集。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 每盏启用阴影的光源在 atlas 中占据一张或多张 map（方向光 = cascade、点光 = 6 面、
    /// 聚光 = 1 张）。位置由 <see cref="TextureAllocator"/> 分配，结果写入
    /// <c>_ShadowLightDatas</c> / <c>_ShadowMapDatas</c> 两张结构化缓冲供着色器两级查表。
    /// </para>
    /// <para>
    /// pass 由相机的 <see cref="HNAdditionalCameraData"/> 持有并随相机跨帧复用，
    /// 故 atlas / 分配器 / 驻留表可跨帧保留。
    /// </para>
    /// </remarks>
    [Pass(PassNameConst)]
    public sealed partial class DrawShadowPass : Pass, IGlobalShaderResource, IPassDebugPreviewProvider
    {
        /// <summary>
        /// 用于注册与识别的常量 pass 名。
        /// </summary>
        public const string PassNameConst = "Draw Shadow";

        // ── 常量 ──

        /// <summary>每盏光最多占用的 map 数（方向光 cascade / 点光面）。</summary>
        public const int MaxMapPerLight = 8;

        /// <summary>参与阴影渲染的方向光上限。</summary>
        private const int MaxDirectionalShadowLights = 4;

        /// <summary>
        /// 方向光阴影级联上限，与 <see cref="CascadeCountType"/> 的最大值一致（8）。
        /// </summary>
        private const int MaxDirectionalShadowCascades = 8;

        /// <summary>参与阴影渲染的本地光（点 / 聚光）上限。</summary>
        private const int MaxLocalShadowLights = 32;

        /// <summary>
        /// 驻留 map 数组容量：按最大光源数编码的全部 (lightIndex, sub) 槽位
        /// （<see cref="EncodeMapIndex"/> 每 light 占 16 位空间）。
        /// </summary>
        private const int ResidentMapCapacity =
            (HNRenderPipelineAsset.MAX_DIRECTIONAL_LIGHT_ON_SCREEN
             + HNRenderPipelineAsset.MAX_LOCAL_LIGHT_ON_SCREEN) << 4;

        /// <summary>点光阴影面的 fov 偏置（度），用于避免面与面之间的裂缝。</summary>
        private const float PointLightFovBias = 2.0f;

        /// <summary>
        /// 矩图最大 mip 层级：最小 brick 为 512 = 2^9，故最高一级 mip 恰为 1 像素；
        /// 同时约束 <see cref="HNAdditionalLightData.PenumbraRadius"/> 的取值范围。
        /// </summary>
        public const int MaxShadowMipLevel = 9;

        /// <summary>
        /// 某分辨率 map 的矩图最大 mip 层级（受 <see cref="MaxShadowMipLevel"/> 限制）。
        /// </summary>
        /// <param name="resolution">map level 0 的分辨率（2 的幂）。</param>
        /// <returns>可用的最大 mip 层级；非法分辨率返回 <c>0</c>。</returns>
        public static int GetMaxMomentMipLevel(int resolution)
        {
            if (resolution <= 0)
            {
                return 0;
            }

            int level = Mathf.FloorToInt(Mathf.Log(resolution, 2f));
            return Mathf.Clamp(level, 0, MaxShadowMipLevel);
        }

        // ── 可配置参数 ──

        /// <summary>
        /// atlas 单张 slice 的分辨率（正方形边长）。默认 4096。
        /// </summary>
        [SerializeField]
        private int sliceResolution = 4096;

        /// <summary>
        /// atlas 的 slice 数量（最多 4）。默认 1。
        /// </summary>
        [SerializeField]
        private int sliceCount = 1;

        /// <summary>
        /// 方向光阴影近平面偏移，用于避免投影面附近自阴影。
        /// </summary>
        [SerializeField]
        private float shadowNearPlaneOffset = 0.1f;

        /// <summary>阴影强度（写入 <c>shadowParams.x</c>）。</summary>
        [SerializeField]
        private float shadowStrength = 1f;

        [SerializeField]
        private float depthBias = 1.0f;

        [SerializeField]
        private float slopBias = 2.5f;

        /// <summary>
        /// 软阴影总开关：关闭后即使有 light 选择 Soft，也维持硬阴影路径。
        /// </summary>
        [SerializeField]
        private bool softShadowEnabled = true;

        /// <summary>VSSM 最小方差（抑制自阴影 / 漏光）。</summary>
        [SerializeField]
        private float minVariance = 0.0001f;

        /// <summary>VSSM 漏光偏置（0–1）。</summary>
        [SerializeField, Range(0f, 1f)]
        private float lightLeakBias = 0.05f;

        /// <summary>矩图手工比较的接收者深度偏置（线性归一化单位）。</summary>
        [SerializeField]
        private float momentDepthBias = 0.002f;

        /// <summary>VSSM blocker search 采样 mip 层级。</summary>
        [SerializeField, Range(0f, 4f)]
        private float blockerSearchMip = 1f;

        /// <summary>获取或设置 atlas 单 slice 分辨率。</summary>
        public int SliceResolution
        {
            get => sliceResolution;
            set => sliceResolution = value;
        }

        /// <summary>获取或设置 atlas slice 数量。</summary>
        public int SliceCount
        {
            get => sliceCount;
            set => sliceCount = value;
        }

        // ── Slot ──

        /// <summary>光照数据缓冲输入 slot（连接 <see cref="BuildLightDataPass"/>，只读）。</summary>
        public ComputeBufferSlot LightDatasBufferSlot { get; private set; }

        /// <summary>阴影图集输出 slot（连接下游 <see cref="DrawObjectPass"/> 的 ShadowMap）。</summary>
        public TextureSlot ShadowMapOutputSlot { get; private set; }

        // ── 每帧状态 ──

        private CameraContext cameraContext;

        // ── 持久资源 ──

        private RTHandle shadowAtlas;
        private int allocatedResolution;
        private int allocatedSliceCount;

        /// <summary>当前 atlas 是否为矩图（软阴影）格式。</summary>
        private bool allocatedSoftMode;
        private TextureAllocator textureAllocator;

        /// <summary>atlas 重分配后需要在下次 render func 内经 RenderGraph 命令缓冲整图清空。</summary>
        private bool needsAtlasClear;

        private ComputeBuffer shadowLightDatasBuffer;
        private ComputeBuffer shadowMapDatasBuffer;

        /// <summary>每方向光阴影参数（<c>_ShadowCameraDatas</c>，按方向光槽位索引；shader 按深度 + splits 选级）</summary>
        private ComputeBuffer shadowCameraDatasBuffer;

        private ShadowLightData[] shadowLightDatasArray;
        private ShadowMapData[] shadowMapDatasArray;

        /// <summary>每方向光阴影参数与上传缓冲一一对应的数组（按方向光槽位索引）</summary>
        private ShadowCameraData[] shadowCameraDatasArray;

        private int lightDataCapacity;
        private int mapDataCapacity;

        /// <summary>
        /// 驻留 map，按 <see cref="EncodeMapIndex"/> 结果直接索引；<c>null</c> 表示未驻留。
        /// 数组化替代字典，消除每帧每 map 的哈希开销。
        /// </summary>
        private ResidentMap[] residentMaps;

        /// <summary>当前驻留的 mapIndex 列表，供释放遍历只处理活跃项（O(驻留数)）。</summary>
        private readonly List<uint> activeResidentMaps = new List<uint>();

        /// <summary>本帧选中的光源（暂存）。</summary>
        private readonly List<SelectedLight> selectedLights = new List<SelectedLight>();

        /// <summary>本帧要绘制的阴影 slice（暂存）。</summary>
        private readonly List<ShadowDrawCommand> drawCommands = new List<ShadowDrawCommand>();

        /// <summary>本帧因 atlas 重分配需要搬移的 map（暂存）。</summary>
        private readonly List<ShadowCopyCommand> copyCommands = new List<ShadowCopyCommand>();

        /// <summary>本帧跨 slice 搬移的软 map 需重建 mip 的区域（暂存）。</summary>
        private readonly List<ShadowMipUpdateCommand> mipUpdateCommands = new List<ShadowMipUpdateCommand>();

        /// <summary>本帧所有需更新 mip 的软 map（重绘 + 跨 slice 搬移，暂存）。</summary>
        private readonly List<ShadowMipUpdateCommand> mipDirtyMaps = new List<ShadowMipUpdateCommand>();

        /// <summary>矩图 mip 生成的区域描述数组（逐帧复用，按需扩容）。</summary>
        private ShadowMipRegion[] mipRegionsArray;

        /// <summary>矩图 mip 生成的区域描述缓冲（上传给 compute shader）。</summary>
        private ComputeBuffer mipRegionsBuffer;

        /// <summary>本帧是否启用矩图（软阴影）路径。</summary>
        private bool frameSoftShadowMode;

        /// <summary>方向光矩阵计算复用的近平面视锥角点缓冲（避免每帧堆分配）。</summary>
        private readonly Vector3[] nearCornersScratch = new Vector3[4];

        /// <summary>方向光矩阵计算复用的远平面视锥角点缓冲（避免每帧堆分配）。</summary>
        private readonly Vector3[] farCornersScratch = new Vector3[4];

        /// <summary>方向光分组结果复用列表（避免每帧堆分配）。</summary>
        private readonly List<CascadeGroup> cascadeGroupsScratch =
            new List<CascadeGroup>(ShadowCameraSettings.MaxGroupCount);

        /// <summary>本帧各组的引擎剔除结果（按组下标索引），由组内各级共享。</summary>
        private readonly ShadowSplitData[] groupSplitDataScratch =
            new ShadowSplitData[ShadowCameraSettings.MaxGroupCount];

        /// <summary>本帧各组剔除结果是否已成功取得；失败时该组本帧不绘制投射者。</summary>
        private readonly bool[] groupSplitDataReady = new bool[ShadowCameraSettings.MaxGroupCount];

        /// <summary>本帧相机视图矩阵（每帧缓存一次，供各 map 签名比较复用）。</summary>
        private Matrix4x4 cachedCameraViewMatrix;

        /// <summary>本帧相机 GPU 投影矩阵（每帧缓存一次）。</summary>
        private Matrix4x4 cachedCameraProjMatrix;

        /// <summary>局部清空阴影图区域用的材质（懒创建）。</summary>
        private Material shadowClearMaterial;

        /// <summary>OnDemand 更新请求（key = light 实例 id）。</summary>
        private static readonly HashSet<int> pendingOnDemandRequests = new HashSet<int>();

        /// <summary>分配器结果输出（暂存，避免分配）。</summary>
        private Dictionary<uint, TextureAllocatorResult> allocateResults =
            new Dictionary<uint, TextureAllocatorResult>();

        /// <summary>
        /// 阴影全局标量参数（<c>_ShadowMapParamsBuffer</c>）。每帧写入一次。
        /// </summary>
        private ShadowGlobalParams shadowGlobalParams;

        /// <summary>
        /// 光源视图投影矩阵（<c>_ShadowViewProjBuffer</c>）。
        /// </summary>
        private ShadowViewProjParams shadowViewProjParams;

        private ShadowCameraSettings frameShadowCameraSettings;

        /// <summary>
        /// 无参构造仅供参数缓存 Pass 的反序列化使用。
        /// </summary>
        public DrawShadowPass()
            : base(string.Empty)
        {
        }

        /// <summary>
        /// 初始化 <see cref="DrawShadowPass"/> 的新实例。
        /// </summary>
        /// <param name="passName">本 pass 的实例名。</param>
        public DrawShadowPass(string passName)
            : base(passName)
        {
        }

        // ── 生命周期 ──

        /// <inheritdoc />
        public override void SetupSlots()
        {
            LightDatasBufferSlot = new ComputeBufferSlot("lightDatasBuffer", SlotDirection.Input);
            RegisterSlot(LightDatasBufferSlot);
            ShadowMapOutputSlot = new TextureSlot("ShadowMap", SlotDirection.Output);
            RegisterSlot(ShadowMapOutputSlot);
        }

        /// <inheritdoc />
        public override void PreRecord(RenderGraphAsset template, CameraContext context)
        {
            cameraContext = context;
        }

        /// <inheritdoc />
        public override void Record(RenderGraph renderGraph)
        {
            if (cameraContext == null || !cameraContext.HasCullingResults || cameraContext.Camera == null)
            {
                return;
            }

            UpdateCameraShadowSettings();
            CacheCameraMatrices();

            // 先判定本帧是否启用软阴影（决定 atlas 格式），再分配资源。
            DetectSoftShadowMode();
            EnsureResources();

            BuildSelectedLights();
            UpdateResidentMaps();
            BuildTablesAndDrawCommands();

            RTHandle atlas = shadowAtlas;
            TextureHandle atlasHandle = renderGraph.ImportTexture(atlas);

            using var builder = renderGraph.AddRenderPass<DrawShadowPassData>(PassName, out var passData);
            builder.AllowPassCulling(false);

            passData.shadowMap = builder.WriteTexture(atlasHandle);

            if (LightDatasBufferSlot.IsConnected)
            {
                passData.lightDatasBuffer = builder.ReadComputeBuffer(LightDatasBufferSlot.ReadHandle());
            }

            if (ShadowMapOutputSlot != null)
            {
                ShadowMapOutputSlot.SetHandle(atlasHandle);
            }

            builder.SetRenderFunc((DrawShadowPassData data, RenderGraphContext ctx) =>
            {
                if (!IsEnabled)
                {
                    return;
                }

                RenderShadows(ctx, atlas);
            });
        }

        /// <summary>
        /// 读取本相机挂载的级联阴影设置并缓存到帧状态
        /// 相机未挂 <see cref="HNAdditionalCameraData"/> 时（如烘焙反射临时相机）使用默认设置
        /// </summary>
        private void UpdateCameraShadowSettings()
        {
            HNAdditionalCameraData cameraData = cameraContext.AdditionalCameraData;
            frameShadowCameraSettings = cameraData != null
                ? cameraData.ShadowSettings
                : ShadowCameraSettings.Default;

            frameShadowCameraSettings.EnsureValid();
        }

        /// <summary>
        /// 缓存本帧相机视图 / GPU 投影矩阵，供方向map 的签名比较复用，
        /// 避免每张 map 重复计算 <c>GL.GetGPUProjectionMatrix</c>
        /// </summary>
        private void CacheCameraMatrices()
        {
            Camera camera = cameraContext.Camera;
            cachedCameraViewMatrix = camera.worldToCameraMatrix;
            cachedCameraProjMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
        }

        /// <summary>
        /// 把持久阴影图集暴露给渲染调试的纹理预览（支持按 slice 查看单张阴影 map）。
        /// </summary>
        /// <param name="texture">阴影图集；尚未分配时为 <c>null</c>。</param>
        /// <param name="sliceCount">图集内的 map 数量（texture array 的 slice 数）。</param>
        /// <param name="mipCount">可预览的 mip 级数（硬阴影图集无 mip；矩图有完整 mip 链）。</param>
        /// <returns>当前存在可预览图集时返回 <c>true</c>。</returns>
        public bool TryGetDebugPreview(out Texture texture, out int sliceCount, out int mipCount)
        {
            texture = shadowAtlas;
            sliceCount = allocatedSliceCount;
            mipCount = allocatedSoftMode
                ? Mathf.FloorToInt(Mathf.Log(allocatedResolution, 2f)) + 1
                : 1;
            return texture != null;
        }

        /// <inheritdoc />
        public override void Cleanup()
        {
            // 注意：pass 每相机复用，此处仅在模板重建 / 渲染器销毁时被调用。
            ReleaseAtlas();

            shadowLightDatasBuffer?.Release();
            shadowLightDatasBuffer = null;
            shadowMapDatasBuffer?.Release();
            shadowMapDatasBuffer = null;
            shadowCameraDatasBuffer?.Release();
            shadowCameraDatasBuffer = null;
            mipRegionsBuffer?.Release();
            mipRegionsBuffer = null;

            CoreUtils.Destroy(shadowClearMaterial);
            shadowClearMaterial = null;

            textureAllocator = null;
            if (residentMaps != null)
            {
                System.Array.Clear(residentMaps, 0, residentMaps.Length);
            }
            activeResidentMaps.Clear();
            mipUpdateCommands.Clear();
            allocatedResolution = 0;
            allocatedSliceCount = 0;
            allocatedSoftMode = false;
            frameSoftShadowMode = false;
            cameraContext = null;
        }

        /// <inheritdoc />
        /// <remarks>
        /// 阴影图集产出有效时，开启 <c>SHADOW_MAP</c> keyword，并绑定两张结构化表、
        /// 图集纹理与阴影参数常量缓冲；否则关闭该 keyword。
        /// <c>SCREEN_SPACE_SHADOW_MAP</c> 目前没有生产者，一并关闭以避免残留。
        /// </remarks>
        public void BindGlobalShaderResources(CommandBuffer cmd)
        {
            bool available = IsEnabled
                && ShadowMapOutputSlot != null
                && ShadowMapOutputSlot.HasHandle
                && shadowAtlas != null;

            // 屏幕空间阴影暂无生产者，恒关闭，避免全局 keyword 残留。
            cmd.DisableShaderKeyword(GlobalKeywords.screenSpaceShadowMap);

            if (!available)
            {
                cmd.DisableShaderKeyword(GlobalKeywords.shadowMap);
                return;
            }

            cmd.EnableShaderKeyword(GlobalKeywords.shadowMap);

            if (shadowLightDatasBuffer != null)
            {
                cmd.SetGlobalBuffer(PropertyIDs.shadowLightDatas, shadowLightDatasBuffer);
            }

            if (shadowMapDatasBuffer != null)
            {
                cmd.SetGlobalBuffer(PropertyIDs.shadowMapDatas, shadowMapDatasBuffer);
            }

            if (shadowCameraDatasBuffer != null)
            {
                cmd.SetGlobalBuffer(PropertyIDs.shadowCameraDatas, shadowCameraDatasBuffer);
            }

            cmd.SetGlobalTexture(
                PropertyIDs.shadowMapArray,
                (RenderTargetIdentifier)shadowAtlas);
            ConstantBuffer.PushGlobal(
                cmd,
                shadowGlobalParams,
                PropertyIDs.shadowMapParamsBuffer);
        }

        /// <summary>
        /// 请求某盏灯在下一次 <see cref="DrawShadowPass"/> 执行时重绘其阴影（
        /// 用于 <see cref="ShadowUpdateModeType.OnDemand"/>）。
        /// </summary>
        /// <param name="light">要请求重绘的灯。</param>
        public static void RequestShadowUpdate(Light light)
        {
            if (light != null)
            {
                pendingOnDemandRequests.Add(light.GetInstanceID());
            }
        }
        // ── Property IDs ──

        /// <summary>本 pass 使用的 shader 属性标识。</summary>
        public static class PropertyIDs
        {
            /// <summary>阴影图集（Texture2DArray）。值：<c>_ShadowMapArray</c>。</summary>
            public static readonly int shadowMapArray = Shader.PropertyToID("_ShadowMapArray");

            /// <summary>按 light 的阴影元数据（StructuredBuffer）。值：<c>_ShadowLightDatas</c>。</summary>
            public static readonly int shadowLightDatas = Shader.PropertyToID("_ShadowLightDatas");

            /// <summary>按 atlas 槽的阴影数据（StructuredBuffer）。值：<c>_ShadowMapDatas</c>。</summary>
            public static readonly int shadowMapDatas = Shader.PropertyToID("_ShadowMapDatas");

            /// <summary>每方向光阴影参数（StructuredBuffer&lt;ShadowCameraData&gt;，按方向光槽位索引）。
            /// 值：<c>_ShadowCameraDatas</c>。</summary>
            public static readonly int shadowCameraDatas = Shader.PropertyToID("_ShadowCameraDatas");

            /// <summary>阴影全局标量常量缓冲（_ShadowSliceResolution）。
            /// 值：<c>_ShadowMapParamsBuffer</c>。</summary>
            public static readonly int shadowMapParamsBuffer = Shader.PropertyToID("_ShadowMapParamsBuffer");

            /// <summary>光源视图投影矩阵常量缓冲（_ShadowViewProj，逐 map 更新）。
            /// 值：<c>_ShadowViewProjBuffer</c>。</summary>
            public static readonly int shadowViewProjBuffer = Shader.PropertyToID("_ShadowViewProjBuffer");

            // ── 矩图 mip 生成（ShadowMipmapCS）──

            /// <summary>矩图 mip 生成：源 mip 纹理。</summary>
            public static readonly int mipSource = Shader.PropertyToID("_SourceMip");

            /// <summary>矩图 mip 生成：目标 mip 纹理。</summary>
            public static readonly int mipDest = Shader.PropertyToID("_DestMip");

            /// <summary>矩图 mip 生成：本级待更新区域数组（StructuredBuffer）。</summary>
            public static readonly int mipRegions = Shader.PropertyToID("_Regions");

            /// <summary>矩图 mip 生成：本级区域数量。</summary>
            public static readonly int mipRegionCount = Shader.PropertyToID("_RegionCount");
        }
    }
}
