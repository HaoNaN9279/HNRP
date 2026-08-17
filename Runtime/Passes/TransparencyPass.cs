using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace HN.HNRP
{
    /// <summary>
    /// Renders transparent geometry into the color and depth targets using forward rendering.
    /// New <see cref="Pass"/>-based replacement for the legacy
    /// <see cref="TransparencyPass"/> (<c>PassBase</c>).
    /// </summary>
    /// <remarks>
    /// <para>Outputs:</para>
    /// <list type="bullet">
    ///   <item><b>ColorTarget</b> — the color buffer written by transparent draw calls.</item>
    ///   <item><b>DepthTarget</b> — the depth buffer read/written by transparent draw calls.</item>
    /// </list>
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
            ColorTargetSlot = new TextureSlot("ColorTarget", SlotDirection.Output);
            DepthTargetSlot = new TextureSlot("DepthTarget", SlotDirection.Output);
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

            using var builder = renderGraph.AddRenderPass<TransparencyPassData>(
                PassName, out var passData);

            builder.AllowRendererListCulling(false);

            // ── Output slots: create and register color / depth targets ──

            var colorDesc = new TextureDesc(Vector2.one, true, false)
            {
                colorFormat = GraphicsFormat.R8G8B8A8_UNorm,
                clearBuffer = false,
                name = $"{PassName}_ColorTarget",
            };

            var depthDesc = new TextureDesc(Vector2.one, true, false)
            {
                depthBufferBits = DepthBits.Depth32,
                clearBuffer = false,
                name = $"{PassName}_DepthTarget",
            };

            TextureHandle colorTarget = renderGraph.CreateTexture(colorDesc);
            TextureHandle depthTarget = renderGraph.CreateTexture(depthDesc);

            passData.colorTarget = builder.UseColorBuffer(colorTarget, 0);
            passData.depthTarget = builder.UseDepthBuffer(depthTarget, DepthAccess.ReadWrite);

            ColorTargetSlot.CreateHandle();
            DepthTargetSlot.CreateHandle();

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
