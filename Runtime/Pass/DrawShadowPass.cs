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
    public sealed class DrawShadowPass : Pass, IGlobalShaderResource, IPassDebugPreviewProvider
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

        /// <summary>软阴影系数（写入 <c>shadowParams.y</c>）。</summary>
        [SerializeField]
        private float shadowSoft = 0f;

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

        /// <summary>方向光矩阵计算复用的近平面视锥角点缓冲（避免每帧堆分配）。</summary>
        private readonly Vector3[] nearCornersScratch = new Vector3[4];

        /// <summary>方向光矩阵计算复用的远平面视锥角点缓冲（避免每帧堆分配）。</summary>
        private readonly Vector3[] farCornersScratch = new Vector3[4];

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

            EnsureResources();
            UpdateCameraShadowSettings();
            CacheCameraMatrices();

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
        /// <param name="mipCount">可预览的 mip 级数（阴影图集无 mip，恒为 1）。</param>
        /// <returns>当前存在可预览图集时返回 <c>true</c>。</returns>
        public bool TryGetDebugPreview(out Texture texture, out int sliceCount, out int mipCount)
        {
            texture = shadowAtlas;
            sliceCount = allocatedSliceCount;
            mipCount = 1;
            return texture != null;
        }

        /// <inheritdoc />
        public override void Cleanup()
        {
            // 注意：pass 每相机复用，此处仅在模板重建 / 渲染器销毁时被调用。
            shadowAtlas?.Release();
            shadowAtlas = null;

            shadowLightDatasBuffer?.Release();
            shadowLightDatasBuffer = null;
            shadowMapDatasBuffer?.Release();
            shadowMapDatasBuffer = null;
            shadowCameraDatasBuffer?.Release();
            shadowCameraDatasBuffer = null;

            CoreUtils.Destroy(shadowClearMaterial);
            shadowClearMaterial = null;

            textureAllocator = null;
            if (residentMaps != null)
            {
                System.Array.Clear(residentMaps, 0, residentMaps.Length);
            }
            activeResidentMaps.Clear();
            allocatedResolution = 0;
            allocatedSliceCount = 0;
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

        // ── 资源 ──

        /// <summary>
        /// 确保持久资源（atlas、分配器、表缓冲）已按当前参数分配。
        /// </summary>
        private void EnsureResources()
        {
            if (residentMaps == null)
            {
                residentMaps = new ResidentMap[ResidentMapCapacity];
            }

            if (shadowAtlas == null || allocatedResolution != sliceResolution || allocatedSliceCount != sliceCount)
            {
                shadowAtlas?.Release();

                int resolution = Mathf.Max(512, Mathf.ClosestPowerOfTwo(sliceResolution));
                int slices = Mathf.Clamp(sliceCount, 1, 4);

                shadowAtlas = RTHandles.Alloc(
                    resolution,
                    resolution,
                    slices: slices,
                    depthBufferBits: DepthBits.Depth32,
                    colorFormat: GraphicsFormat.None,
                    filterMode: FilterMode.Bilinear,
                    wrapMode: TextureWrapMode.Clamp,
                    dimension: TextureDimension.Tex2DArray,
                    isShadowMap: true,
                    name: "HN Shadow Atlas");

                allocatedResolution = resolution;
                allocatedSliceCount = slices;

                textureAllocator = new TextureAllocator(resolution, 512, 4096, slices);
                System.Array.Clear(residentMaps, 0, residentMaps.Length);
                activeResidentMaps.Clear();

                // atlas 已重建，旧 slice / 偏移的搬移命令全部失效，避免残留命令访问无效区域。
                copyCommands.Clear();

                // 整图初始化为远深度（clearDepth=true, clearColor=false：atlas 无颜色目标）。
                // 命令延迟到 render func 内经 RenderGraph 的命令缓冲提交，避免在录制期
                // 私自申请 CommandBuffer 并即时提交到 ScriptableRenderContext。
                needsAtlasClear = true;
            }

            int maxLightCount = HNRenderPipelineAsset.MAX_DIRECTIONAL_LIGHT_ON_SCREEN
                                + HNRenderPipelineAsset.MAX_LOCAL_LIGHT_ON_SCREEN;

            if (shadowLightDatasArray == null || lightDataCapacity != maxLightCount)
            {
                lightDataCapacity = maxLightCount;
                shadowLightDatasArray = new ShadowLightData[maxLightCount];
                HNRenderPipelineUtils.ValidateComputeBuffer(
                    ref shadowLightDatasBuffer, maxLightCount, System.Runtime.InteropServices.Marshal.SizeOf<ShadowLightData>());
            }

            int maxMapSlots = (allocatedResolution / 512) * (allocatedResolution / 512) * allocatedSliceCount;
            if (shadowMapDatasArray == null || mapDataCapacity != maxMapSlots)
            {
                mapDataCapacity = maxMapSlots;
                shadowMapDatasArray = new ShadowMapData[maxMapSlots];
                HNRenderPipelineUtils.ValidateComputeBuffer(
                    ref shadowMapDatasBuffer, maxMapSlots, System.Runtime.InteropServices.Marshal.SizeOf<ShadowMapData>());
            }

            if (shadowCameraDatasArray == null)
            {
                shadowCameraDatasArray = new ShadowCameraData[MaxDirectionalShadowLights];
                HNRenderPipelineUtils.ValidateComputeBuffer(
                    ref shadowCameraDatasBuffer,
                    MaxDirectionalShadowLights,
                    System.Runtime.InteropServices.Marshal.SizeOf<ShadowCameraData>());
            }
        }

        // ── 选灯 ──

        /// <summary>
        /// 收集本帧参与阴影的光源：方向光 main light 优先，其余按强度降序；
        /// 方向光 ≤4、本地光 ≤32。
        /// </summary>
        private void BuildSelectedLights()
        {
            selectedLights.Clear();

            NativeArray<VisibleLight> visibleLights = cameraContext.VisibleLights;
            if (!visibleLights.IsCreated || visibleLights.Length == 0)
            {
                return;
            }

            int mainLightIndex = cameraContext.MainLightIndex;
            int maxLightCount = Mathf.Min(
                visibleLights.Length,
                HNRenderPipelineAsset.MAX_DIRECTIONAL_LIGHT_ON_SCREEN
                + HNRenderPipelineAsset.MAX_LOCAL_LIGHT_ON_SCREEN);

            int directionalCount = 0;
            int localCount = 0;

            // main light 优先。
            if (mainLightIndex >= 0 && mainLightIndex < maxLightCount
                && TryAddShadowLight(visibleLights, mainLightIndex, ref directionalCount, ref localCount))
            {
            }

            for (int i = 0; i < maxLightCount; i++)
            {
                if (i == mainLightIndex)
                {
                    continue;
                }

                TryAddShadowLight(visibleLights, i, ref directionalCount, ref localCount);
            }
        }

        private bool TryAddShadowLight(
            NativeArray<VisibleLight> visibleLights,
            int index,
            ref int directionalCount,
            ref int localCount)
        {
            VisibleLight visibleLight = visibleLights[index];
            Light light = visibleLight.light;
            if (light == null)
            {
                return false;
            }

            // 先按光源类型过滤（无需组件查询），跳过不支持阴影的类型。
            LightType type = visibleLight.lightType;
            if (type != LightType.Directional && type != LightType.Point && type != LightType.Spot)
            {
                return false;
            }

            if (!light.TryGetComponent(out HNAdditionalLightData additionalLightData)
                || !additionalLightData.EnableShadow)
            {
                return false;
            }

            if (type == LightType.Directional)
            {
                if (directionalCount >= MaxDirectionalShadowLights)
                {
                    return false;
                }
                directionalCount++;
            }
            else if (localCount >= MaxLocalShadowLights)
            {
                return false;
            }
            else
            {
                localCount++;
            }

            // 方向光可覆盖相机的级联设置（级数 / 分割 / 更新模式）；否则沿用相机设置。
            // 非方向光不参与级联，其 cascadeSettings 仅用于更新模式判定，取相机设置。
            bool isDirectional = type == LightType.Directional;
            ShadowCameraSettings cascadeSettings = frameShadowCameraSettings;
            if (isDirectional && additionalLightData.OverrideCameraShadowSettings)
            {
                cascadeSettings = additionalLightData.ShadowSettings;
                cascadeSettings.EnsureValid();
            }

            int cascadeCount = isDirectional
                ? Mathf.Clamp((int)cascadeSettings.CascadeCount, 1, MaxMapPerLight)
                : 0;

            // 分辨率先按有效级联级数收敛（级数越大，单张 map 允许的分辨率越低），
            // 再收敛到 atlas slice 分辨率以内
            ResolutionType clampedResolution = CascadeShadowUtils.ClampResolution(
                cascadeSettings.CascadeCount, additionalLightData.CascadeResolution);
            int resolution = HNRenderPipelineUtils.ClampShadowResolution(
                (int)clampedResolution, allocatedResolution);

            selectedLights.Add(new SelectedLight
            {
                lightIndex = index,
                lightType = type,
                resolution = resolution,
                cascadeCount = cascadeCount,
                cascadeSettings = cascadeSettings,
                cascadeHash = isDirectional
                    ? ComputeCascadeHash(cascadeCount, cascadeSettings.CascadeSplits)
                    : 0,
                // 方向光槽位（0 起）对应 _ShadowCameraDatas 下标；其他类型无意义。
                cameraDataIndex = isDirectional ? directionalCount - 1 : -1,
                lightLocalToWorld = visibleLight.localToWorldMatrix,
                lightRange = visibleLight.range,
                lightSpotAngle = visibleLight.spotAngle,
                lightInstanceId = light.GetInstanceID(),
            });

            return true;
        }

        // ── 驻留集 diff ──

        /// <summary>
        /// 同步驻留表与分配器：新增 map 分配、消失 map 释放、重分配结果回写。
        /// </summary>
        private void UpdateResidentMaps()
        {
            // 标记本帧仍需要的 map；缺失或分辨率变化的重新分配。
            for (int i = 0; i < selectedLights.Count; i++)
            {
                SelectedLight selected = selectedLights[i];
                int mapCount = GetMapCount(selected);
                for (int sub = 0; sub < mapCount; sub++)
                {
                    uint mapIndex = EncodeMapIndex(selected.lightIndex, sub);
                    if (mapIndex >= (uint)residentMaps.Length)
                    {
                        continue;
                    }

                    ResidentMap resident = residentMaps[mapIndex];
                    if (resident == null)
                    {
                        AllocateMap(mapIndex, selected);
                    }
                    else if (resident.resolution != selected.resolution)
                    {
                        textureAllocator.Release(mapIndex);
                        AllocateMap(mapIndex, selected);
                    }
                    else
                    {
                        resident.neededThisFrame = true;
                    }
                }
            }

            // 释放本帧不再需要的 map（只遍历活跃项，O(驻留数)）。
            for (int i = 0; i < activeResidentMaps.Count; )
            {
                uint mapIndex = activeResidentMaps[i];
                ResidentMap resident = residentMaps[mapIndex];
                if (resident != null && resident.neededThisFrame)
                {
                    resident.neededThisFrame = false;
                    i++;
                    continue;
                }

                textureAllocator.Release(mapIndex);
                residentMaps[mapIndex] = null;
                activeResidentMaps[i] = activeResidentMaps[activeResidentMaps.Count - 1];
                activeResidentMaps.RemoveAt(activeResidentMaps.Count - 1);
            }
        }

        private void AllocateMap(uint mapIndex, SelectedLight selected)
        {
            allocateResults.Clear();
            textureAllocator.Allocate(ref allocateResults, mapIndex, selected.resolution);

            // 处理被本次分配重分配（搬移）的既有 map：更新驻留位置。
            // Graphics.CopyTexture 不允许在同一 texture 的同一 element/mip 内拷贝（即使源与目标
            // 区域不同也会报「identical source and destination element」），而 atlas 通常只有
            // 单 slice，因此同 slice 内搬移无法拷贝，改为标记强制重绘，在新位置重建内容。
            // 仅跨 slice 搬移才用 CopyTexture 保留原内容。
            foreach (KeyValuePair<uint, TextureAllocatorResult> pair in allocateResults)
            {
                if (pair.Key == mapIndex || !pair.Value.IsReorg)
                {
                    continue;
                }

                if (!TryGetResidentMap(pair.Key, out ResidentMap moved))
                {
                    continue;
                }

                moved.allocation = pair.Value;

                if (pair.Value.OldSliceIndex == pair.Value.SliceIndex)
                {
                    // 位置未变的搬移是空操作，无需重绘；确已移动的强制下一帧重绘。
                    if (pair.Value.OldScaleOffset != pair.Value.ScaleOffset)
                    {
                        moved.hasSignature = false;
                        moved.lastUpdateFrame = -1;
                    }
                }
                else
                {
                    copyCommands.Add(new ShadowCopyCommand
                    {
                        fromSlice = pair.Value.OldSliceIndex,
                        fromScaleOffset = pair.Value.OldScaleOffset,
                        toSlice = pair.Value.SliceIndex,
                        toScaleOffset = pair.Value.ScaleOffset,
                    });
                }
            }

            if (allocateResults.TryGetValue(mapIndex, out TextureAllocatorResult result))
            {
                if (residentMaps[mapIndex] == null)
                {
                    activeResidentMaps.Add(mapIndex);
                }

                residentMaps[mapIndex] = new ResidentMap
                {
                    lightIndex = selected.lightIndex,
                    subIndex = (int)(mapIndex & 0xFu),
                    lightType = selected.lightType,
                    resolution = selected.resolution,
                    allocation = result,
                    hasSignature = false,
                    lastUpdateFrame = -1,
                    neededThisFrame = true,
                };
            }
            else
            {
                // 分配失败：清除旧记录，避免残留已释放的分配被后续读取。
                residentMaps[mapIndex] = null;
            }
        }

        private static int GetMapCount(SelectedLight selected)
        {
            switch (selected.lightType)
            {
                case LightType.Point:
                    return 6;
                case LightType.Spot:
                    return 1;
                default:
                    return Mathf.Clamp(selected.cascadeCount, 1, MaxMapPerLight);
            }
        }

        private static uint EncodeMapIndex(int lightIndex, int subIndex)
        {
            return ((uint)lightIndex << 4) | (uint)subIndex;
        }

        /// <summary>
        /// 打包光源类型 / cascade 级数 / 方向光槽位到单个 uint（供 shader 直读，避免逐像素分支与解码）。
        /// </summary>
        /// <param name="lightType">光源类型。</param>
        /// <param name="cascadeCount">方向光 cascade 级数（非方向光为 0）。</param>
        /// <returns>bit0..7=lightType，bit8..15=cascadeCount/returns>
        private static uint PackLightTypeAndCascadeCount(LightType lightType, int cascadeCount)
        {
            // 与 <see cref="LightData.lightType"/> 的 shader 约定一致：1=方向 2=点 3=聚光。
            uint type = lightType switch
            {
                LightType.Directional => 1u,
                LightType.Point => 2u,
                LightType.Spot => 3u,
                _ => 0u,
            };
            uint cascades = ((uint)Mathf.Clamp(cascadeCount, 0, 0xFF)) << 8;
            return type | cascades;
        }

        /// <summary>
        /// 按 mapIndex 取驻留记录；越界或未驻留时返回 <c>false</c>。
        /// </summary>
        private bool TryGetResidentMap(uint mapIndex, out ResidentMap resident)
        {
            if (residentMaps != null && mapIndex < (uint)residentMaps.Length)
            {
                resident = residentMaps[mapIndex];
                return resident != null;
            }

            resident = null;
            return false;
        }

        // ── 表与绘制命令 ──

        /// <summary>
        /// 填充 <c>ShadowLightData</c> / <c>ShadowMapData</c> 两张表，并生成本帧绘制命令。
        /// 并生成本帧绘制命令
        /// </summary>
        private void BuildTablesAndDrawCommands()
        {
            drawCommands.Clear();

            // 清除上一帧写入的 light 条目：光源关闭阴影 / 移出视野后，若残留
            // resolution>0 与旧 blockDatas，着色器会采样已释放的 atlas 槽。
            System.Array.Clear(shadowLightDatasArray, 0, shadowLightDatasArray.Length);

            // 每方向光一套 cascade 级数与分割：上传后 shader 按 light 的槽位索引取用。
            System.Array.Clear(shadowCameraDatasArray, 0, shadowCameraDatasArray.Length);

            for (int i = 0; i < selectedLights.Count; i++)
            {
                SelectedLight selected = selectedLights[i];
                int lightIndex = selected.lightIndex;
                if (lightIndex < 0 || lightIndex >= shadowLightDatasArray.Length)
                {
                    continue;
                }

                ShadowLightData lightData = default;
                lightData.lightIndex = lightIndex;
                lightData.resolution = selected.resolution;
                lightData.typeAndCascadeCount = PackLightTypeAndCascadeCount(
                    selected.lightType, selected.cascadeCount);

                // 方向光：记录本光源的 cascade 参数槽位并写入对应 splits。
                if (selected.lightType == LightType.Directional
                    && selected.cameraDataIndex >= 0
                    && selected.cameraDataIndex < shadowCameraDatasArray.Length)
                {
                    lightData.cameraDataIndex = selected.cameraDataIndex;
                    shadowCameraDatasArray[selected.cameraDataIndex] =
                        BuildShadowCameraData(selected.cascadeSettings, selected.cascadeCount);
                }

                int mapCount = GetMapCount(selected);
                bool allAllocated = true;
                for (int sub = 0; sub < mapCount; sub++)
                {
                    uint mapIndex = EncodeMapIndex(lightIndex, sub);
                    if (!TryGetResidentMap(mapIndex, out ResidentMap resident))
                    {
                        allAllocated = false;
                        continue;
                    }

                    uint field = EncodeField(resident.allocation);
                    if (sub < 4)
                    {
                        lightData.blockDatas0 |= field << (sub * 8);
                    }
                    else
                    {
                        lightData.blockDatas1 |= field << ((sub - 4) * 8);
                    }

                    // 先判定是否重绘，再只计算一次矩阵：结果同时供阴影表与绘制命令使用。
                    // 非重绘且已有有效缓存时跳过 ComputeMatrices（签名已覆盖所有影响矩阵的输入）。
                    bool shouldRedraw = ShouldRedrawMap(selected, resident, sub);
                    bool needMatrices = shouldRedraw
                        || !resident.hasMapData
                        || resident.mapData.shadowParams.x != shadowStrength
                        || resident.mapData.shadowParams.y != shadowSoft;

                    if (needMatrices)
                    {
                        if (ComputeMatrices(selected, sub, out Matrix4x4 view, out Matrix4x4 proj))
                        {
                            ShadowMapData mapData = default;
                            mapData.shadowParams = new Vector4(shadowStrength, shadowSoft, sub, 0f);
                            mapData.worldToShadow = GetShadowTransform(proj, view);
                            resident.mapData = mapData;
                            resident.hasMapData = true;

                            if (shouldRedraw)
                            {
                                drawCommands.Add(new ShadowDrawCommand
                                {
                                    view = view,
                                    proj = proj,
                                    allocation = resident.allocation,
                                });
                            }
                        }
                        else
                        {
                            resident.mapData = default;
                            resident.hasMapData = false;
                        }
                    }

                    if ((int)field < shadowMapDatasArray.Length)
                    {
                        shadowMapDatasArray[field] = resident.mapData;
                    }
                }

                // 任一 map 分配失败则禁用该 light 的阴影，避免着色器读到无效槽
                if (!allAllocated)
                {
                    lightData.resolution = 0;
                }

                shadowLightDatasArray[lightIndex] = lightData;
            }

            shadowGlobalParams._ShadowSliceResolution = allocatedResolution;

            // OnDemand 请求本帧已消费，清空。
            pendingOnDemandRequests.Clear();
        }

        /// <summary>
        /// 判定某张 map 本帧是否需要重绘（新增 / 参数变化 / 更新模式）。
        /// </summary>
        private bool ShouldRedrawMap(SelectedLight selected, ResidentMap resident, int sub)
        {
            LightSignature signature = ComputeSignature(selected, sub, resident.resolution);
            bool redraw;

            if (!resident.hasSignature || !resident.signature.Equals(signature))
            {
                redraw = true;
            }
            else
            {
                switch (selected.cascadeSettings.ShadowUpdateMode)
                {
                    case ShadowUpdateModeType.OnDemand:
                        redraw = pendingOnDemandRequests.Contains(selected.lightInstanceId);
                        break;

                    case ShadowUpdateModeType.Custom:
                        int interval = GetTimeSlice(selected.cascadeSettings, sub);
                        if (interval <= 0)
                        {
                            redraw = true;
                        }
                        else
                        {
                            // 引入稳定相位：同 interval 的不同 map（多灯 / 多 cascade / 多面）
                            // 错开到不同帧更新，避免在同一帧集中重绘造成卡顿尖峰。
                            int phase = ComputePhase(selected.lightInstanceId, sub, interval);
                            redraw = ((Time.frameCount + phase) % interval) == 0;
                        }
                        break;

                    default:
                        redraw = true;
                        break;
                }
            }

            // 刚分配的 map 一定重绘。
            if (resident.lastUpdateFrame < 0)
            {
                redraw = true;
            }

            if (redraw)
            {
                resident.signature = signature;
                resident.hasSignature = true;
                resident.lastUpdateFrame = Time.frameCount;
            }

            return redraw;
        }

        private LightSignature ComputeSignature(SelectedLight selected, int sub, int resolution)
        {
            LightSignature signature = default;
            signature.lightMatrix = selected.lightLocalToWorld;
            signature.lightRange = selected.lightRange;
            signature.spotAngle = selected.lightSpotAngle;
            signature.resolution = resolution;
            signature.subIndex = sub;

            // 方向光 cascade 依赖相机与级数 / 分割，任一变化都需重绘并重算矩阵。
            if (selected.lightType == LightType.Directional)
            {
                signature.cameraView = cachedCameraViewMatrix;
                signature.cameraProj = cachedCameraProjMatrix;
                signature.cascadeHash = selected.cascadeHash;
            }

            return signature;
        }

        /// <summary>
        /// 计算方向光 cascade 级数与分割的哈希，用于签名比较与缓存失效判断。
        /// </summary>
        private static int ComputeCascadeHash(int cascadeCount, List<float> splits)
        {
            int hash = cascadeCount * 397;
            if (splits != null)
            {
                for (int i = 0; i < splits.Count; i++)
                {
                    hash = (hash * 31) + splits[i].GetHashCode();
                }
            }

            return hash;
        }

        /// <summary>
        /// 计算 map 在 Custom 更新模式下的稳定帧相位，把同 interval 的 map 错开更新。
        /// </summary>
        /// <param name="lightInstanceId">光源实例 id。</param>
        /// <param name="sub">map 在光源内的子索引（cascade / 面）。</param>
        /// <param name="interval">更新帧间隔。</param>
        /// <returns>取值 <c>[0, interval)</c> 的相位。</returns>
        private static int ComputePhase(int lightInstanceId, int sub, int interval)
        {
            if (interval <= 1)
            {
                return 0;
            }

            int hash = (lightInstanceId * 73856093) ^ (sub * 19349663);
            return (hash & 0x7fffffff) % interval;
        }

        private int GetTimeSlice(ShadowCameraSettings settings, int sub)
        {
            List<int> slices = settings.CascadeTimeSlices;
            if (slices == null || slices.Count == 0)
            {
                return 0;
            }

            int index = Mathf.Clamp(sub, 0, slices.Count - 1);
            return slices[index];
        }

        private static uint EncodeField(TextureAllocatorResult allocation)
        {
            return ((uint)allocation.SliceIndex << 6) | (allocation.BlockId & 0x3Fu);
        }

        /// <summary>
        /// 由某方向光的级联设置构建上传给 shader 的相机阴影参数（各级 cascade 远边界）
        /// shader 侧据此与片段深度比较来选级
        /// </summary>
        private static ShadowCameraData BuildShadowCameraData(ShadowCameraSettings settings, int cascadeCount)
        {
            ShadowCameraData data = default;
            List<float> splits = settings.CascadeSplits;
            if (splits == null)
            {
                return data;
            }

            int count = Mathf.Clamp(cascadeCount, 1, splits.Count);
            for (int i = 0; i < 4; i++)
            {
                if (i < count && i < splits.Count)
                {
                    data.cascadeSplits0[i] = splits[i];
                }

                int index = i + 4;
                if (index < count && index < splits.Count)
                {
                    data.cascadeSplits1[i] = splits[index];
                }
            }

            return data;
        }

        /// <summary>
        /// 计算第 <paramref name="sub"/> 张 map 的视图 / 投影矩阵与裁剪数据。
        /// </summary>
        private bool ComputeMatrices(
            SelectedLight selected,
            int sub,
            out Matrix4x4 view,
            out Matrix4x4 proj)
        {
            view = Matrix4x4.identity;
            proj = Matrix4x4.identity;

            CullingResults cullingResults = cameraContext.CullingResults;
            switch (selected.lightType)
            {
                case LightType.Directional:
                    return ComputeDirectionalMatrices(selected, sub, out view, out proj);

                case LightType.Spot:
                    return cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(
                        selected.lightIndex, out view, out proj, out _);

                case LightType.Point:
                {
                    if (!cullingResults.ComputePointShadowMatricesAndCullingPrimitives(
                            selected.lightIndex, (CubemapFace)sub, PointLightFovBias,
                            out view, out proj, out _))
                    {
                        return false;
                    }

                    // 对齐 URP：翻转 view 的第三行，修正点光面与聚光面法线偏置不一致。
                    view.m10 = -view.m10;
                    view.m11 = -view.m11;
                    view.m12 = -view.m12;
                    view.m13 = -view.m13;
                    return true;
                }

                default:
                    return false;
            }
        }

        /// <summary>
        /// 手写方向光级联矩阵：用相机子视锥拟合球体，再绕光方向建正交投影。
        /// 使用世界空间 split，支持任意级数。
        /// 透视与正交相机共用同一split 语义 —<c>CalculateFrustumCorners</c>
        /// 对两种投影都在「距相机 <c>distance</c> 的平面」上取角点
        /// </summary>
        private bool ComputeDirectionalMatrices(
            SelectedLight selected,
            int cascadeIndex,
            out Matrix4x4 view,
            out Matrix4x4 proj)
        {
            view = Matrix4x4.identity;
            proj = Matrix4x4.identity;

            Camera camera = cameraContext.Camera;
            List<float> splits = selected.cascadeSettings.CascadeSplits;
            if (camera == null || splits == null || cascadeIndex >= selected.cascadeCount)
            {
                return false;
            }

            float near = cascadeIndex == 0
                ? camera.nearClipPlane
                : splits[cascadeIndex - 1];
            float far = splits[Mathf.Min(cascadeIndex, splits.Count - 1)];
            if (far <= near)
            {
                far = near + 1f;
            }

            Vector3[] nearCorners = nearCornersScratch;
            Vector3[] farCorners = farCornersScratch;
            camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), near, camera.stereoActiveEye, nearCorners);
            camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), far, camera.stereoActiveEye, farCorners);

            for (int i = 0; i < 4; i++)
            {
                nearCorners[i] = camera.transform.TransformPoint(nearCorners[i]);
                farCorners[i] = camera.transform.TransformPoint(farCorners[i]);
            }

            Vector3 center = Vector3.zero;
            for (int i = 0; i < 4; i++)
            {
                center += nearCorners[i] + farCorners[i];
            }
            center /= 8f;

            float radius = 0f;
            for (int i = 0; i < 4; i++)
            {
                radius = Mathf.Max(radius, (nearCorners[i] - center).magnitude);
                radius = Mathf.Max(radius, (farCorners[i] - center).magnitude);
            }
            radius = Mathf.Max(radius, 0.001f);

            Vector3 lightForward = selected.lightLocalToWorld.GetColumn(2);
            Quaternion lightRotation = Quaternion.LookRotation(lightForward, Vector3.up);
            Vector3 viewPosition = center - lightForward * (radius + shadowNearPlaneOffset);

            // Unity 视图空间沿 -Z 观察（OpenGL 约定），而 TRS(...).inverse 得到的是沿 +Z
            // 的灯光局部空间；必须左乘 Z 翻转，否则正交投影会打反 z 符号，
            // 使投影深度落在 [0,1] 之外——caster 被裁掉、采样恒返回「无阴影」。
            Matrix4x4 lightToWorld = Matrix4x4.TRS(viewPosition, lightRotation, Vector3.one);
            view = Matrix4x4.Scale(new Vector3(1f, 1f, -1f)) * lightToWorld.inverse;
            proj = Matrix4x4.Ortho(-radius, radius, -radius, radius, 0f, 2f * radius + shadowNearPlaneOffset);

            // 选级不依赖裁剪球：shader 用相splits 与片段视轴深度直接判定（Shadow.hlsl）
            return true;
        }

        /// <summary>
        /// 构建「世界 → 阴影图坐标」矩阵（含 xy 的 [0,1] remap，不含 atlas 偏移）。
        /// </summary>
        /// <remarks>
        /// 深度与绘制侧严格同源：绘制用 GL.GetGPUProjectionMatrix(proj, true)，
        /// 它包含平台的 z 映射（reversed-Z 平台 near=1 / far=0）与渲染到纹理所需的 Y 翻转；
        /// 采样用 GL.GetGPUProjectionMatrix(proj, false)，保留同一 z 映射但不做 Y 翻转，
        /// 使采样的 UV（左下原点）与渲染结果对齐。这里只补上裁剪空间 xy 的 [-1,1] → [0,1] 映射。
        /// </remarks>
        private static Matrix4x4 GetShadowTransform(Matrix4x4 proj, Matrix4x4 view)
        {
            Matrix4x4 worldToShadow = GL.GetGPUProjectionMatrix(proj, false) * view;
            Matrix4x4 textureScaleAndBias = Matrix4x4.identity;
            textureScaleAndBias.m00 = 0.5f;
            textureScaleAndBias.m11 = 0.5f;
            textureScaleAndBias.m03 = 0.5f;
            textureScaleAndBias.m13 = 0.5f;
            return textureScaleAndBias * worldToShadow;
        }

        /// <summary>
        /// 把光源投影矩阵转换为「渲染阴影图」用的 GPU 投影矩阵。
        /// </summary>
        /// <remarks>
        /// renderIntoTexture: true 会按平台追加 z 映射与 Y 翻转：D3D 上渲染到纹理必须翻转 Y
        /// </remarks>
        private static Matrix4x4 GetShadowDrawProjection(Matrix4x4 proj)
        {
            return GL.GetGPUProjectionMatrix(proj, true);
        }

        private void RenderShadows(RenderGraphContext ctx, RTHandle atlas)
        {
            CommandBuffer cmd = ctx.cmd;

            // 上传两张表。全局绑定（SetGlobalBuffer / SetGlobalTexture / PushGlobal）
            // 由 BindGlobalShaderResources 在绘制前统一完成。
            if (shadowLightDatasBuffer != null && shadowLightDatasArray != null)
            {
                cmd.SetBufferData(shadowLightDatasBuffer, shadowLightDatasArray);
            }

            if (shadowMapDatasBuffer != null && shadowMapDatasArray != null)
            {
                cmd.SetBufferData(shadowMapDatasBuffer, shadowMapDatasArray);
            }

            if (shadowCameraDatasBuffer != null && shadowCameraDatasArray != null)
            {
                cmd.SetBufferData(shadowCameraDatasBuffer, shadowCameraDatasArray);
            }

            // 0) atlas 重分配后整图清远深度（一次性，经 RenderGraph 命令缓冲提交）。
            if (needsAtlasClear)
            {
                for (int slice = 0; slice < allocatedSliceCount; slice++)
                {
                    cmd.SetRenderTarget((RenderTargetIdentifier)atlas, 0, CubemapFace.Unknown, slice);
                    cmd.ClearRenderTarget(true, false, Color.black);
                }

                needsAtlasClear = false;
            }

            EnsureClearMaterial();

            // 1) 重分配搬移：把被移动 map 的旧区域内容复制到新区域。
            for (int i = 0; i < copyCommands.Count; i++)
            {
                ShadowCopyCommand copy = copyCommands[i];
                int size = Mathf.RoundToInt(copy.toScaleOffset.x * allocatedResolution);
                int srcX = Mathf.RoundToInt(copy.fromScaleOffset.z * allocatedResolution);
                int srcY = Mathf.RoundToInt(copy.fromScaleOffset.w * allocatedResolution);
                int dstX = Mathf.RoundToInt(copy.toScaleOffset.z * allocatedResolution);
                int dstY = Mathf.RoundToInt(copy.toScaleOffset.w * allocatedResolution);

                cmd.CopyTexture(
                    (RenderTargetIdentifier)atlas, copy.fromSlice, 0, srcX, srcY, size, size,
                    (RenderTargetIdentifier)atlas, copy.toSlice, 0, dstX, dstY);
            }

            // 2) 重绘：局部清远深度后把投射者几何直接画进本 map 区域；
            //    未重绘的 map 内容跨帧保留。
            //    投射者绘制设置每帧只构建一次，逐 map 仅切换光视图 / 投影矩阵与 viewport。
            //    ScriptableRenderContext.DrawRenderers 为 context 级 API，读取已提交到
            //    context 的状态，故每次绘制前先 ExecuteCommandBuffer 刷新命令缓冲
            //    （与 URP 的 ShadowUtils.RenderShadowSlice 一致）。
            if (drawCommands.Count > 0 && cameraContext.Camera != null)
            {
                var sortingSettings = new SortingSettings(cameraContext.Camera)
                {
                    criteria = SortingCriteria.CommonOpaque,
                };
                var drawingSettings = new DrawingSettings(ShaderPassNames.ShadowCasterName, sortingSettings)
                {
                    perObjectData = PerObjectData.None,
                    enableInstancing = true,
                };
                var filteringSettings = new FilteringSettings(HNRenderQueue.AllOpaque, ~0);

                for (int i = 0; i < drawCommands.Count; i++)
                {
                    ShadowDrawCommand command = drawCommands[i];
                    int slice = command.allocation.SliceIndex;
                    float scale = command.allocation.ScaleOffset.x;
                    int resolution = Mathf.RoundToInt(scale * allocatedResolution);
                    int offsetX = Mathf.RoundToInt(command.allocation.ScaleOffset.z * allocatedResolution);
                    int offsetY = Mathf.RoundToInt(command.allocation.ScaleOffset.w * allocatedResolution);

                    // 绑定 atlas 的目标 slice + viewport，使清屏与投射者绘制只落在本 map 区域。
                    cmd.SetRenderTarget((RenderTargetIdentifier)atlas, 0, CubemapFace.Unknown, slice);
                    cmd.SetViewport(new Rect(offsetX, offsetY, resolution, resolution));

                    // 全屏三角形（ShadowClear）写远深度，受 viewport 限制只清本 map 区域。
                    if (shadowClearMaterial != null)
                    {
                        CoreUtils.DrawFullScreen(cmd, shadowClearMaterial, null, 0);
                    }

                    // 深度偏置抑制自阴影；光矩阵决定投射者落到区域内的位置。
                    cmd.SetGlobalDepthBias(1.0f, 2.5f);

                    // 注意：SetViewProjectionMatrices 只作用于引擎内置矩阵（剔除 / LOD），
                    // 本项目 shader 的 UNITY_MATRIX_V / VP 来自自定义 ShaderVariablesGlobal（b0，
                    // 按相机上传），因此投射者必须额外用 _ShadowViewProj 取光源矩阵。
                    // 投影统一走 GetShadowDrawProjection（含平台 z 映射与渲染到纹理的 Y 翻转），
                    // 保证写入深度与采样侧 GetShadowTransform 严格同源。
                    cmd.SetViewProjectionMatrices(command.view, GetShadowDrawProjection(command.proj));
                    shadowViewProjParams._ShadowViewProj =
                        GetShadowDrawProjection(command.proj) * command.view;

                    // 逐 map 推送：_ShadowViewProj 随 map 变化，必须与随后的 ExecuteCommandBuffer
                    // 一起提交，确保 DrawRenderers 读到本 map 的矩阵。
                    ConstantBuffer.PushGlobal(
                        cmd,
                        shadowViewProjParams,
                        PropertyIDs.shadowViewProjBuffer);

                    ctx.renderContext.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    // 直接把投射者画进当前绑定的 atlas 区域（取代引擎 DrawShadows）。
                    ctx.renderContext.DrawRenderers(
                        cameraContext.CullingResults, ref drawingSettings, ref filteringSettings);

                    cmd.DisableScissorRect();
                    cmd.SetGlobalDepthBias(0f, 0f);
                    ctx.renderContext.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                }
            }

            copyCommands.Clear();
        }

        private void EnsureClearMaterial()
        {
            if (shadowClearMaterial != null)
            {
                return;
            }

            Shader clearShader = cameraContext.RuntimeResources.shaderResources.ShadowClear;
            if (clearShader != null)
            {
                shadowClearMaterial = new Material(clearShader)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                };
            }
        }

        // ── 数据结构 ──

        private sealed class ResidentMap
        {
            public int lightIndex;
            public int subIndex;
            public LightType lightType;
            public int resolution;
            public TextureAllocatorResult allocation;

            /// <summary>上次重绘帧号；-1 表示刚分配尚未绘制。</summary>
            public int lastUpdateFrame;

            /// <summary>是否已记录参数签名。</summary>
            public bool hasSignature;

            /// <summary>光源 / 相机参数签名，用于检测是否需重绘。</summary>
            public LightSignature signature;

            /// <summary>本 map 最近一次计算的阴影表数据（矩阵派生），未重算时跨帧复用。</summary>
            public ShadowMapData mapData;

            /// <summary>是否已缓存有效的 <see cref="mapData"/>。</summary>
            public bool hasMapData;

            /// <summary>本帧是否仍被需要（O(驻留 释放遍历）/summary>
            public bool neededThisFrame;
        }

        private struct SelectedLight
        {
            public int lightIndex;
            public LightType lightType;
            public int resolution;

            /// <summary>该光源占用的 map 数（方向= 相机级联级数；否则由类型决定）/summary>
            public int cascadeCount;

            /// <summary>该光源生效的级联设置（方向光 override 时为光源自身设置，否则为相机设置）。</summary>
            public ShadowCameraSettings cascadeSettings;

            /// <summary>该光源级联级数与分割的哈希，用于签名比较。</summary>
            public int cascadeHash;

            /// <summary>方向光在 <c>_ShadowCameraDatas</c> 中的槽位；非方向光为 -1。</summary>
            public int cameraDataIndex;

            public Matrix4x4 lightLocalToWorld;
            public float lightRange;
            public float lightSpotAngle;
            public int lightInstanceId;
        }

        private struct ShadowDrawCommand
        {
            public Matrix4x4 view;
            public Matrix4x4 proj;
            public TextureAllocatorResult allocation;
        }

        private struct ShadowCopyCommand
        {
            public int fromSlice;
            public Vector4 fromScaleOffset;
            public int toSlice;
            public Vector4 toScaleOffset;
        }

        /// <summary>
        /// 光源与相机参数签名，用于检测阴影是否需要重绘。
        /// </summary>
        private struct LightSignature : System.IEquatable<LightSignature>
        {
            public Matrix4x4 lightMatrix;
            public float lightRange;
            public float spotAngle;
            public int resolution;
            public int subIndex;
            public Matrix4x4 cameraView;
            public Matrix4x4 cameraProj;
            public int cascadeHash;

            public bool Equals(LightSignature other)
            {
                return lightMatrix == other.lightMatrix
                    && lightRange == other.lightRange
                    && spotAngle == other.spotAngle
                    && resolution == other.resolution
                    && subIndex == other.subIndex
                    && cameraView == other.cameraView
                    && cameraProj == other.cameraProj
                    && cascadeHash == other.cascadeHash;
            }

            public override bool Equals(object obj)
            {
                return obj is LightSignature other && Equals(other);
            }

            public override int GetHashCode()
            {
                return subIndex ^ resolution;
            }
        }

        private sealed class DrawShadowPassData
        {
            public TextureHandle shadowMap;
            public ComputeBufferHandle lightDatasBuffer;
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
        }
    }

    /// <summary>
    /// 按 light 的阴影元数据（供 GPU 两级查表的第一级）。
    /// 布局须与 shader 侧一致。
    /// </summary>
    public struct ShadowLightData
    {
        /// <summary>该 light 在可见光列表中的索引（= buffer 下标）。</summary>
        public int lightIndex;

        /// <summary>该 light 单张 map 的分辨率；0 表示该 light 无阴影。</summary>
        public int resolution;

        /// <summary>map 0..3 的 atlas 位置，每张 8 位 = (slice &lt;&lt; 6) | blockId。</summary>
        public uint blockDatas0;

        /// <summary>map 4..7 的 atlas 位置。</summary>
        public uint blockDatas1;

        /// <summary>
        /// 打包的光源类型 / cascade 级数 / 方向光槽位：
        /// bit0..7=lightType，bit8..15=cascadeCount
        /// </summary>
        public uint typeAndCascadeCount;

        /// <summary>
        /// 方向光在 <c>_ShadowCameraDatas</c> 中的下标；非方向光无意义。
        /// </summary>
        public int cameraDataIndex;
    }

    /// <summary>
    /// 每方向光阴影参数（按方向光槽位索引）：方向光 cascade 各级的远边界（沿相机视轴深度）。
    /// 布局须与 shader 侧一致。
    /// </summary>
    public struct ShadowCameraData
    {
        /// <summary>cascade 0..3 的远边界（沿相机视轴的深度；0 表示无该级）。</summary>
        public Vector4 cascadeSplits0;

        /// <summary>cascade 4..7 的远边界（沿相机视轴的深度；0 表示无该级）。</summary>
        public Vector4 cascadeSplits1;
    }

    /// <summary>
    /// atlas 槽的阴影数据（供 GPU 两级查表的第二级）。buffer 下标atlas 位置字段
    /// 布局须与 shader 侧一致
    /// </summary>
    public struct ShadowMapData
    {
        /// <summary>x=strength, y=soft, z=cascadeIndex/faceIndex, w=预留。</summary>
        public Vector4 shadowParams;

        /// <summary>世界 → 阴影裁剪矩阵（含 [0,1] remap，不含 atlas offset）。</summary>
        public Matrix4x4 worldToShadow;
    }

    /// <summary>
    /// 阴影全局标量参数常量缓冲。字段布局须与 shader 侧 <c>_ShadowMapParamsBuffer</c> 一致。
    /// </summary>
    /// <remarks>
    /// GPU 常量缓冲要求绑定的字节数为 16 的倍数（<c>ComputeBufferType.Constant</c> 的
    /// stride 同样受此约束），故标量后补齐 3 个 float，合计 16 字节。
    /// 未来新增标量全局参数时占用 padding；新增行时需保持总量为 16 的倍数。
    /// </remarks>
    public struct ShadowGlobalParams
    {
        /// <summary>atlas 单 slice 分辨率。</summary>
        public float _ShadowSliceResolution;

        /// <summary>对齐占位（无数据语义）。</summary>
        public float padding0;

        /// <summary>对齐占位（无数据语义）。</summary>
        public float padding1;

        /// <summary>对齐占位（无数据语义）。</summary>
        public float padding2;
    }

    /// <summary>
    /// 光源视图投影矩阵常量缓冲。字段布局须与 shader 侧 <c>_ShadowViewProjBuffer</c> 一致。
    /// </summary>
    /// <remarks>
    /// 更新频率高于 <see cref="ShadowGlobalParams"/>（每张阴影 map 一次），故独立成缓冲。
    /// 单个 4x4 矩阵 = 64 字节，天然满足 16 字节对齐。
    /// </remarks>
    public struct ShadowViewProjParams
    {
        /// <summary>世界 → 光源裁剪矩阵（阴影投射 pass 用）。</summary>
        public Matrix4x4 _ShadowViewProj;
    }
}
