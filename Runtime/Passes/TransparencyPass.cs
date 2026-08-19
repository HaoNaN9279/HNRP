using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RendererUtils;

namespace HN.HNRP
{
    /// <summary>
    /// Renders transparent geometry into the color and depth targets using forward rendering.
    /// New <see cref="Pass"/>-based replacement for the legacy
    /// <see cref="TransparencyPass"/> (<c>PassBase</c>).
    /// </summary>
    /// <remarks>
    /// <para>Inputs (connected from upstream, e.g. <c>ForwardOpaquePass</c>):</para>
    /// <list type="bullet">
    ///   <item><b>ColorTarget</b> — the color buffer written by transparent draw calls.</item>
    ///   <item><b>DepthTarget</b> — the depth buffer read/written by transparent draw calls.</item>
    /// </list>
    /// <para>
    /// Uses the shared texture model: the color/depth targets are allocated by the
    /// upstream chain head pass and this pass renders into the same buffers.
    /// </para>
    /// </remarks>
    [Pass("Transparency")]
    public sealed class TransparencyPass : Pass
    {
        // ── Configurable parameters ──

        /// <summary>
        /// Gets or sets the rendering layer mask used for culling.
        /// Only renderers on matching layers are drawn.
        /// Default is <c>0x00000001</c> (layer 0).
        /// </summary>
        public uint RenderingLayerMask { get; set; } = 0x00000001;

        // ── Slots ──

        /// <summary>
        /// Gets the output color target slot.
        /// Available after <see cref="SetupSlots"/> is called.
        /// </summary>
        public TextureSlot? ColorTargetSlot { get; private set; }

        /// <summary>
        /// Gets the output depth target slot.
        /// Available after <see cref="SetupSlots"/> is called.
        /// </summary>
        public TextureSlot? DepthTargetSlot { get; private set; }

        // ── Camera context ──

        private CameraContext? cameraContext;

        // ── Constructor ──

        /// <summary>
        /// Initializes a new instance of the <see cref="TransparencyPass"/> class.
        /// </summary>
        /// <param name="passName">
        /// The instance name of this pass. Must be non-null and unique within the render graph.
        /// </param>
        public TransparencyPass(string passName)
            : base(passName)
        {
        }

        // ── Lifecycle ──

        /// <inheritdoc />
        public override void SetupSlots()
        {
            ColorTargetSlot = new TextureSlot("ColorTarget", SlotDirection.Input);
            RegisterSlot(ColorTargetSlot);
            DepthTargetSlot = new TextureSlot("DepthTarget", SlotDirection.Input);
            RegisterSlot(DepthTargetSlot);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Stores the camera context so the renderer list can be built from
        /// <c>CullingResults</c> and <c>Camera</c> during <see cref="Record"/>.
        /// </remarks>
        public override void Initialize(CameraContext context)
        {
            cameraContext = context;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Reads the upstream color and depth targets (shared texture model — allocated
        /// by <c>ForwardOpaquePass</c>) and draws the transparent renderer list into them.
        /// </remarks>
        public override void Record(RenderGraph renderGraph)
        {
            if (ColorTargetSlot == null || DepthTargetSlot == null)
            {
                return;
            }

            if (cameraContext == null)
            {
                return;
            }

            if (!ColorTargetSlot.IsConnected || !DepthTargetSlot.IsConnected)
            {
                return;
            }

            using var builder = renderGraph.AddRenderPass<TransparencyPassData>(
                PassName, out var passData);

            builder.AllowRendererListCulling(false);

            // ── Input slots: use upstream color / depth targets (shared texture model) ──

            TextureHandle colorTarget = ColorTargetSlot.ReadHandle();
            TextureHandle depthTarget = DepthTargetSlot.ReadHandle();

            passData.colorTarget = builder.UseColorBuffer(colorTarget, 0);
            passData.depthTarget = builder.UseDepthBuffer(depthTarget, DepthAccess.ReadWrite);

            // ── Renderer list: transparent objects ──

            RendererListDesc rendererListDesc = HNRenderPipelineUtils.GetTransparentRendererListDesc(
                ShaderPassNames.AllForwardNames,
                cameraContext.CullingResults,
                cameraContext.Camera,
                RenderingLayerMask);

            passData.rendererList = builder.UseRendererList(
                renderGraph.CreateRendererList(rendererListDesc));

            // ── Render function ──

            builder.SetRenderFunc(
                (TransparencyPassData data, RenderGraphContext ctx) =>
                {
                    ctx.cmd.DrawRendererList(data.rendererList);
                });
        }

        /// <inheritdoc />
        public override void Cleanup()
        {
            // No disposable resources held by this pass.
        }

        // ── Pass data ──

        /// <summary>
        /// Render graph pass data container for <see cref="TransparencyPass"/>.
        /// </summary>
        private sealed class TransparencyPassData
        {
            /// <summary>
            /// The color target texture handle.
            /// </summary>
            public TextureHandle colorTarget;

            /// <summary>
            /// The depth target texture handle.
            /// </summary>
            public TextureHandle depthTarget;

            /// <summary>
            /// The transparent renderer list handle.
            /// </summary>
            public RendererListHandle rendererList;
        }
    }
}
