// <copyright file="DepthOnlyPass.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace HN.HNRP
{
    /// <summary>
    /// 深度预渲染 pass：用 shader 的 <c>DepthOnly</c> LightMode 把全部不透明物体的
    /// 深度绘制到深度目标纹理，不产生任何颜色输出。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 深度目标优先消费已连接的 <see cref="DepthTargetSlot"/>；未连接或句柄无效时
    /// 按 <see cref="DepthTargetParams"/> 自建（默认相机全分辨率 32 位深度）。
    /// 解析出的句柄经 <see cref="DepthTargetOutputSlot"/> 发布，供下游 pass 链式使用。
    /// </para>
    /// </remarks>
    [Pass(PassNameConst)]
    public sealed class DepthOnlyPass : Pass
    {
        /// <summary>
        /// 用于注册与识别的常量 pass 名。
        /// </summary>
        public const string PassNameConst = "Depth Only";

        // ── 可配置参数 ──

        /// <summary>
        /// 获取或设置深度目标分配参数。输入未连接 / 无效时以本参数自建深度目标。
        /// </summary>
        public TextureResourceParams DepthTargetParams
        {
            get => depthTargetParams;
            set => depthTargetParams = value;
        }

        /// <summary>
        /// 获取或设置渲染器列表分配参数。默认不透明队列。
        /// </summary>
        public RendererListParams RendererListParams
        {
            get => rendererListParams;
            set => rendererListParams = value;
        }

        /// <summary>
        /// 获取或设置本地分配渲染器列表时使用的渲染层掩码。
        /// 默认为 <c>0x00000001</c>（第 0 层）。
        /// </summary>
        public uint RenderingLayerMask
        {
            get => rendererListParams.RenderingLayerMask;
            set => rendererListParams.RenderingLayerMask = value;
        }

        // ── Slot ──

        /// <summary>
        /// 深度目标输入 slot。<see cref="Pass.SetupSlots"/> 调用后可用。
        /// </summary>
        public TextureSlot DepthTargetSlot { get; private set; }

        /// <summary>
        /// 深度目标输出 slot（解析后 <see cref="DepthTargetSlot"/> 句柄的透传，
        /// 供下游 pass 链式连接）。<see cref="Pass.SetupSlots"/> 调用后可用。
        /// </summary>
        public TextureSlot DepthTargetOutputSlot { get; private set; }

        /// <summary>
        /// <see cref="DepthTargetSlot"/> 输入未连接 / 无效时本地分配深度目标所用的参数。
        /// 默认：全分辨率 32 位深度。
        /// </summary>
        [SerializeField]
        private TextureResourceParams depthTargetParams;

        /// <summary>
        /// 本地创建渲染器列表所用的参数。默认：不透明队列、层掩码 <c>0x00000001</c>。
        /// </summary>
        [SerializeField]
        private RendererListParams rendererListParams;

        // ── 相机上下文 ──

        private CameraContext cameraContext;

        // ── 构造函数 ──

        /// <summary>
        /// 初始化 <see cref="DepthOnlyPass"/> 的新实例。
        /// </summary>
        /// <remarks>
        /// 无参构造仅供 <see cref="RenderGraphAsset"/> 上参数缓存 Pass 的
        /// <c>[SerializeReference]</c> 反序列化使用；实例名随后由序列化数据填充。
        /// 参数默认值与带名构造保持一致。
        /// </remarks>
        public DepthOnlyPass()
            : base(string.Empty)
        {
            depthTargetParams = CreateDefaultDepthParams();
            rendererListParams = RendererListParams.CreateDefault();
        }

        /// <summary>
        /// 初始化 <see cref="DepthOnlyPass"/> 的新实例。
        /// </summary>
        /// <param name="passName">
        /// 本 pass 的实例名。必须非 null 且在渲染图内唯一。
        /// </param>
        public DepthOnlyPass(string passName)
            : base(passName)
        {
            depthTargetParams = CreateDefaultDepthParams();
            rendererListParams = RendererListParams.CreateDefault();
        }

        // ── 生命周期 ──

        /// <inheritdoc />
        public override void SetupSlots()
        {
            DepthTargetSlot = new TextureSlot("DepthTarget", SlotDirection.Input);
            RegisterSlot(DepthTargetSlot);

            DepthTargetOutputSlot = new TextureSlot("DepthTargetOutput", SlotDirection.Output);
            RegisterSlot(DepthTargetOutputSlot);
        }

        /// <inheritdoc />
        /// <remarks>
        /// 保存相机上下文，使 <see cref="Record"/> 期间能构建渲染器列表。
        /// </remarks>
        public override void PreRecord(RenderGraphAsset template, CameraContext context)
        {
            cameraContext = context;
        }

        /// <inheritdoc />
        public override void Record(RenderGraph renderGraph)
        {
            // ── 预检：必须在 AddRenderPass 之前完成 ──
            if (DepthTargetSlot == null || cameraContext == null)
            {
                return;
            }

            Camera camera = cameraContext.Camera;
            if (camera == null)
            {
                return;
            }

            // 输入已连接且句柄有效时消费连接输入；否则按本 pass 参数本地分配。
            bool useInputDepth = DepthTargetSlot.IsConnected && DepthTargetSlot.HasHandle;
            TextureHandle depthTarget = useInputDepth
                ? DepthTargetSlot.ReadHandle()
                : renderGraph.CreateTexture(
                    depthTargetParams.CreateDesc("Depth Buffer", camera));

            RendererListHandle rendererList = CreateRendererList(renderGraph);

            if (!depthTarget.IsValid() || !rendererList.IsValid())
            {
                return;
            }

            // 把解析出的深度句柄透传到输出 slot，供下游链式连接。
            if (DepthTargetOutputSlot != null)
            {
                DepthTargetOutputSlot.SetHandle(depthTarget);
            }

            using var builder = renderGraph.AddRenderPass<DepthOnlyPassData>(
                PassName, out var passData);

            builder.AllowRendererListCulling(false);

            passData.depthTarget = builder.UseDepthBuffer(depthTarget, DepthAccess.ReadWrite);
            passData.rendererList = builder.UseRendererList(rendererList);
            passData.viewMatrix = camera.worldToCameraMatrix;
            passData.projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);

            builder.SetRenderFunc(
                (DepthOnlyPassData data, RenderGraphContext ctx) =>
                {
                    if (!IsEnabled)
                    {
                        return;
                    }

                    ctx.cmd.SetViewProjectionMatrices(data.viewMatrix, data.projMatrix);
                    ctx.cmd.DrawRendererList(data.rendererList);
                });
        }

        /// <inheritdoc />
        public override void Cleanup()
        {
            // 本 pass 不持有可释放资源。
        }

        // ── 辅助 ──

        /// <summary>
        /// 构造深度目标默认分配参数：全分辨率、32 位深度。
        /// </summary>
        private static TextureResourceParams CreateDefaultDepthParams()
        {
            TextureResourceParams parameters = TextureResourceParams.CreateDefault();
            parameters.DepthBits = DepthBits.Depth32;
            return parameters;
        }

        /// <summary>
        /// 从 <see cref="RendererListParams"/> 本地创建渲染器列表。
        /// 裁剪结果不可用时返回默认（无效）句柄。
        /// </summary>
        /// <param name="renderGraph">要在其中创建列表的渲染图。</param>
        /// <returns>创建的渲染器列表句柄，或默认句柄。</returns>
        private RendererListHandle CreateRendererList(RenderGraph renderGraph)
        {
            if (!cameraContext.HasCullingResults)
            {
                return default;
            }

            RendererListDesc desc = rendererListParams.CreateDesc(
                ShaderPassNames.AllDepthOnlyNames,
                cameraContext.CullingResults,
                cameraContext.Camera);
            return renderGraph.CreateRendererList(desc);
        }

        // ── Pass data ──

        /// <summary>
        /// <see cref="DepthOnlyPass"/> 的渲染图 pass 数据容器。
        /// </summary>
        private sealed class DepthOnlyPassData
        {
            /// <summary>
            /// 深度目标纹理句柄。
            /// </summary>
            public TextureHandle depthTarget;

            /// <summary>
            /// 渲染器列表句柄。
            /// </summary>
            public RendererListHandle rendererList;

            /// <summary>
            /// 本 pass 相机的视图矩阵。
            /// </summary>
            public Matrix4x4 viewMatrix;

            /// <summary>
            /// 本 pass 相机的 GPU 投影矩阵。
            /// </summary>
            public Matrix4x4 projMatrix;
        }
    }
}
