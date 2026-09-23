// <copyright file="ScreenSpaceShadowMaskPass.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    /// <summary>
    /// 屏幕空间阴影 mask pass：用 ComputeShader 把当前视锥内<b>所有投影光源</b>
    /// （方向光 / 点光 / 聚光）的可见性逐像素合并绘制到一张单通道 mask 上。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>输入：</b>
    /// <list type="bullet">
    ///   <item>深度缓冲 —— 经内容 slot 消费 <see cref="DepthOnlyPass"/> /
    ///     <see cref="DepthNormalPass"/> 的深度输出（RG 托管，显式绑定给 compute）；</item>
    ///   <item>阴影图集与三张阴影表 —— 经显式依赖边发现 <see cref="IShadowMapProvider"/>
    ///     （<see cref="DrawShadowPass"/>），用其既有全局绑定知识（Persistent 资源）。</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>输出：</b>单通道纹理（<c>R8_UNorm</c>，UAV 随机写入）经
    /// <see cref="ShadowMaskOutputSlot"/> 发布，供 <see cref="ForwardPass"/> 采样。
    /// 值语义：1 = 完全受光，0 = 被任一投影光源完全遮挡；多光源按相乘合并。
    /// </para>
    /// <para>
    /// <b>预检（ADR-021）：</b>深度未连接 / 无句柄、compute shader 缺失、
    /// 阴影生产者本帧未产出时，在 <c>AddRenderPass</c> 之前直接返回，
    /// 不产生渲染图 pass；下游以 <c>IsConnected &amp;&amp; HasHandle</c> 守卫输入句柄，
    /// 不满足时回退逐光源 atlas 阴影。
    /// </para>
    /// </remarks>
    [Pass(PassNameConst)]
    public sealed class ScreenSpaceShadowMaskPass : Pass
    {
        /// <summary>
        /// 用于注册与识别的常量 pass 名。
        /// </summary>
        public const string PassNameConst = "Screen Space Shadow Mask";

        /// <summary>compute shader 的 kernel 名。</summary>
        private const string KernelName = "ScreenSpaceShadowMaskCS";

        // ── 可配置参数 ──

        /// <summary>
        /// 获取或设置输出 mask 纹理分配参数（全分辨率、单通道、UAV 随机写入）。
        /// </summary>
        public TextureResourceParams ShadowMaskParams
        {
            get => shadowMaskParams;
            set => shadowMaskParams = value;
        }

        [SerializeField]
        private TextureResourceParams shadowMaskParams;

        // ── Slot ──

        /// <summary>
        /// 深度目标输入 slot（来自 <see cref="DepthOnlyPass"/> / <see cref="DepthNormalPass"/>）。
        /// <see cref="Pass.SetupSlots"/> 调用后可用。
        /// </summary>
        public TextureSlot DepthTargetSlot { get; private set; }

        /// <summary>
        /// 阴影 mask 输出 slot（接 <see cref="ForwardPass"/> 的
        /// <c>ScreenSpaceShadowMask</c> 输入）。<see cref="Pass.SetupSlots"/> 调用后可用。
        /// </summary>
        public TextureSlot ShadowMaskOutputSlot { get; private set; }

        // ── 相机上下文 ──

        private CameraContext cameraContext;

        /// <summary>本帧解析出的阴影生产者（复用列表避免渲染循环分配）。</summary>
        private readonly List<IShadowMapProvider> shadowMapProviders = new();

        // ── 构造函数 ──

        /// <summary>
        /// 初始化 <see cref="ScreenSpaceShadowMaskPass"/> 的新实例。
        /// </summary>
        /// <remarks>
        /// 无参构造仅供 <see cref="RenderGraphAsset"/> 上参数缓存 Pass 的
        /// <c>[SerializeReference]</c> 反序列化使用；实例名随后由序列化数据填充。
        /// </remarks>
        public ScreenSpaceShadowMaskPass()
            : base(string.Empty)
        {
            shadowMaskParams = CreateDefaultMaskParams();
        }

        /// <summary>
        /// 初始化 <see cref="ScreenSpaceShadowMaskPass"/> 的新实例。
        /// </summary>
        /// <param name="passName">本 pass 的实例名。必须非 null 且在渲染图内唯一。</param>
        public ScreenSpaceShadowMaskPass(string passName)
            : base(passName)
        {
            shadowMaskParams = CreateDefaultMaskParams();
        }

        // ── 生命周期 ──

        /// <inheritdoc />
        public override void SetupSlots()
        {
            DepthTargetSlot = new TextureSlot("DepthTarget", SlotDirection.Input);
            RegisterSlot(DepthTargetSlot);

            ShadowMaskOutputSlot = new TextureSlot("ShadowMaskOutput", SlotDirection.Output);
            RegisterSlot(ShadowMaskOutputSlot);
        }

        /// <inheritdoc />
        /// <remarks>保存相机上下文，使 <see cref="Record"/> 期间能取 compute shader 与光源数。</remarks>
        public override void PreRecord(RenderGraphAsset template, CameraContext context)
        {
            cameraContext = context;
        }

        /// <inheritdoc />
        public override void Record(RenderGraph renderGraph)
        {
            // ── 预检：必须在 AddRenderPass 之前完成（ADR-021）──
            if (DepthTargetSlot == null || ShadowMaskOutputSlot == null || cameraContext == null)
            {
                return;
            }

            Camera camera = cameraContext.Camera;
            if (camera == null || !cameraContext.HasCullingResults)
            {
                return;
            }

            // 深度必须由上游预渲染 pass 提供；未连接 / 无句柄时本 pass 无法工作。
            if (!DepthTargetSlot.IsConnected || !DepthTargetSlot.HasHandle)
            {
                return;
            }

            // 阴影生产者（DrawShadowPass）本帧未产出时跳过，避免读到陈旧缓冲。
            IShadowMapProvider provider = ResolveShadowMapProvider();
            if (provider == null)
            {
                return;
            }

            ComputeShader computeShader = cameraContext.RuntimeResources.shaderResources.screenSpaceShadowMaskCS;
            if (computeShader == null)
            {
                Debug.LogError(
                    "Screen Space Shadow Mask Compute Shader 为 null。 " +
                    "请确保已在 HNRenderPipelineRuntimeResources 中赋值。");
                return;
            }

            TextureHandle depthHandle = DepthTargetSlot.ReadHandle();
            if (!depthHandle.IsValid())
            {
                return;
            }

            // ── 输出 mask：RG 托管 transient 纹理（UAV 随机写入）──
            TextureResourceParams maskParams = shadowMaskParams;
            maskParams.ColorFormat = SystemInfo.GetCompatibleFormat(
                maskParams.ColorFormat, FormatUsage.Render);
            TextureHandle maskHandle = renderGraph.CreateTexture(
                maskParams.CreateDesc("Screen Space Shadow Mask", camera));
            if (!maskHandle.IsValid())
            {
                return;
            }

            // 发布输出句柄，供下游 ForwardPass 采样。
            ShadowMaskOutputSlot.SetHandle(maskHandle);

            int maxLightCount = HNRenderPipelineAsset.MAX_DIRECTIONAL_LIGHT_ON_SCREEN
                                + HNRenderPipelineAsset.MAX_LOCAL_LIGHT_ON_SCREEN;
            int lightCount = Mathf.Min(cameraContext.VisibleLights.Length, maxLightCount);

            int threadGroupsX = (camera.pixelWidth + 7) / 8;
            int threadGroupsY = (camera.pixelHeight + 7) / 8;

            using var builder = renderGraph.AddRenderPass<ScreenSpaceShadowMaskPassData>(
                PassName, out var passData);
            builder.AllowPassCulling(false);

            passData.depthTexture = builder.ReadTexture(depthHandle);
            passData.maskTexture = builder.WriteTexture(maskHandle);
            passData.computeShader = computeShader;
            passData.kernel = computeShader.FindKernel(KernelName);
            passData.shadowMapProvider = provider;
            passData.lightCount = lightCount;
            passData.threadGroupsX = threadGroupsX;
            passData.threadGroupsY = threadGroupsY;

            builder.SetRenderFunc(
                (ScreenSpaceShadowMaskPassData data, RenderGraphContext ctx) =>
                {
                    if (!IsEnabled)
                    {
                        return;
                    }

                    CommandBuffer cmd = ctx.cmd;

                    // 本 pass 是 compute 消费者：只需要阴影资源绑定，不需要任何 raster
                    // 全局 keyword。绑定前归零，避免残留上一渲染单元的生产者 keyword；
                    // 绑定后再次归零，撤销生产者绑定路径顺带开启的 keyword（如 SHADOW_MAP），
                    // 保证本 pass 不向后续绘制遗留 raster 分支状态 —— keyword 的最终决策
                    // 归各 raster 消费者（ForwardPass）在自身绘制前负责。
                    GlobalKeywords.DisableProducerKeywords(cmd);

                    // 阴影图集 / 三张表 / 阴影标量常量缓冲：由生产者自描述绑定
                    // （Persistent 资源，不经 RenderGraph 句柄）。
                    data.shadowMapProvider.BindGlobalShaderResources(cmd);

                    // 深度（RG transient）与输出 mask（UAV）逐 pass 显式绑定。
                    cmd.SetComputeTextureParam(
                        data.computeShader,
                        data.kernel,
                        PropertyIDs.depthTexture,
                        (RenderTargetIdentifier)(RTHandle)data.depthTexture);
                    cmd.SetComputeTextureParam(
                        data.computeShader,
                        data.kernel,
                        PropertyIDs.screenSpaceShadowMask,
                        (RenderTargetIdentifier)(RTHandle)data.maskTexture);
                    cmd.SetComputeIntParam(
                        data.computeShader,
                        PropertyIDs.lightCount,
                        data.lightCount);

                    cmd.DispatchCompute(
                        data.computeShader,
                        data.kernel,
                        data.threadGroupsX,
                        data.threadGroupsY,
                        1);
                });
        }

        /// <inheritdoc />
        public override void Cleanup()
        {
            // 输出 mask 为 transient，由 RenderGraph 托管；compute shader 为共享资源。
        }

        // ── 辅助 ──

        /// <summary>
        /// 从显式依赖的上游 pass 中解析已产出阴影图集的
        /// <see cref="IShadowMapProvider"/>。
        /// </summary>
        /// <returns>可用生产者；无可用者时为 <c>null</c>。</returns>
        private IShadowMapProvider ResolveShadowMapProvider()
        {
            ConnectedProducerQuery.CollectOwners(this, shadowMapProviders);

            for (int i = 0; i < shadowMapProviders.Count; i++)
            {
                if (shadowMapProviders[i].ShadowMapProduced)
                {
                    return shadowMapProviders[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 构造默认 mask 分配参数：全分辨率、单通道 <c>R8_UNorm</c>、双线性、Clamp、
        /// 允许 UAV 随机写入、不预清空（compute 会写满全屏）。
        /// </summary>
        private static TextureResourceParams CreateDefaultMaskParams()
        {
            TextureResourceParams parameters = TextureResourceParams.CreateDefault();
            parameters.ColorFormat = GraphicsFormat.R8_UNorm;
            parameters.FilterMode = FilterMode.Bilinear;
            parameters.WrapMode = TextureWrapMode.Clamp;
            parameters.EnableRandomWrite = true;
            parameters.ClearBuffer = false;
            return parameters;
        }

        // ── Pass data ──

        /// <summary>
        /// <see cref="ScreenSpaceShadowMaskPass"/> 的渲染图 pass 数据容器。
        /// </summary>
        private sealed class ScreenSpaceShadowMaskPassData
        {
            /// <summary>深度目标纹理句柄（RG transient，作为 compute 输入采样）。</summary>
            public TextureHandle depthTexture;

            /// <summary>输出 mask 纹理句柄（RG transient，作为 compute UAV）。</summary>
            public TextureHandle maskTexture;

            /// <summary>屏幕空间阴影 compute shader。</summary>
            public ComputeShader computeShader;

            /// <summary>kernel 索引。</summary>
            public int kernel;

            /// <summary>阴影图集 / 表生产者。</summary>
            public IShadowMapProvider shadowMapProvider;

            /// <summary>本帧参与求值的可见光源数量。</summary>
            public int lightCount;

            /// <summary>X 方向线程组数。</summary>
            public int threadGroupsX;

            /// <summary>Y 方向线程组数。</summary>
            public int threadGroupsY;
        }

        // ── Property IDs ──

        /// <summary>本 pass 与其消费方使用的 shader 属性标识。</summary>
        public static class PropertyIDs
        {
            /// <summary>深度输入纹理。值：<c>_SSShadowDepth</c>。</summary>
            public static readonly int depthTexture = Shader.PropertyToID("_SSShadowDepth");

            /// <summary>输出 / 采样 mask 纹理。值：<c>_ScreenSpaceShadowMask</c>。</summary>
            public static readonly int screenSpaceShadowMask =
                Shader.PropertyToID("_ScreenSpaceShadowMask");

            /// <summary>参与求值的可见光源数量。值：<c>_SSShadowLightCount</c>。</summary>
            public static readonly int lightCount = Shader.PropertyToID("_SSShadowLightCount");
        }
    }
}
