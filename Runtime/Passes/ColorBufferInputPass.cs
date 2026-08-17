using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    /// <summary>
    /// Creates a color render texture to serve as the color target for downstream passes.
    /// This is the entry point for the color buffer in the render graph — it allocates
    /// a transient color texture that subsequent passes (e.g. opaque, sky, transparency)
    /// render into.
    /// </summary>
    /// <remarks>
    /// <para><b>New Pass system</b> (ADR-002, ADR-011):
    /// Inherits from <see cref="Pass"/> instead of the legacy <see cref="PassBase"/>.
    /// Uses name-based <see cref="TextureSlot"/> output for downstream connections
    /// instead of index-based slot registration.
    /// </para>
    /// <para>
    /// Configurable parameters (texture scale, format, clear color) are exposed as
    /// public properties and applied by <c>PassConfigBase.ApplyToPass</c> before
    /// <see cref="Initialize"/> is called.
    /// </para>
    /// </remarks>
    [Pass("Color Buffer Input")]
    public sealed class ColorBufferInputPass : Pass
    {
        // ── Configurable parameters ──
        // Applied externally via PassConfigBase.ApplyToPass before Initialize.

        /// <summary>
        /// Gets or sets the scale factor for the output texture,
        /// relative to the camera's pixel dimensions.
        /// Default is <c>(1, 1)</c> (full resolution).
        /// </summary>
        public Vector2 TextureScale { get; set; } = Vector2.one;

        /// <summary>
        /// Gets or sets the color format of the output texture.
        /// Default is <see cref="GraphicsFormat.R8G8B8A8_UNorm"/>.
        /// </summary>
        public GraphicsFormat ColorFormat { get; set; } = GraphicsFormat.R8G8B8A8_UNorm;

        /// <summary>
        /// Gets or sets a value indicating whether the output texture
        /// should be cleared before rendering.
        /// Default is <c>true</c>.
        /// </summary>
        public bool ClearBuffer { get; set; } = true;

        /// <summary>
        /// Gets or sets the clear color used when <see cref="ClearBuffer"/> is <c>true</c>.
        /// Default is <see cref="Color.black"/>.
        /// </summary>
        public Color ClearColor { get; set; } = Color.black;

        // ── Slot ──

        /// <summary>
        /// Gets the output texture slot that holds the created color target handle.
        /// Downstream passes connect their color target input slots to this output
        /// to receive the allocated color texture.
        /// </summary>
        public TextureSlot ColorTargetSlot { get; private set; }

        // ── Constructor ──

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorBufferInputPass"/> class.
        /// </summary>
        /// <param name="passName">
        /// The instance name of this pass. Must be non-null and unique within the render graph.
        /// </param>
        public ColorBufferInputPass(string passName)
            : base(passName)
        {
        }

        // ── Lifecycle ──

        /// <inheritdoc />
        public override void SetupSlots()
        {
            ColorTargetSlot = new TextureSlot("colorTargetSlot", SlotDirection.Output);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Config parameters are applied externally via <c>PassConfigBase.ApplyToPass</c>
        /// before this method is called. <paramref name="context"/> provides camera-specific
        /// data (e.g. pixel dimensions) that may be used for texture scaling in future iterations.
        /// </remarks>
        public override void Initialize(CameraContext context)
        {
            // Config parameters (TextureScale, ColorFormat, ClearBuffer, ClearColor)
            // are set externally via PassConfigBase.ApplyToPass before Initialize.
            // CameraContext is available if future logic needs to derive texture scale
            // from camera dimensions or other per-frame data.
        }

        /// <inheritdoc />
        /// <remarks>
        /// Creates a transient color texture via <c>renderGraph.CreateTexture</c> and
        /// registers it as the color target using <c>builder.UseColorBuffer</c>.
        /// The render function is a no-op — the texture creation is the side effect.
        /// </remarks>
        public override void Record(RenderGraph renderGraph)
        {
            using (var builder = renderGraph.AddRenderPass<ColorBufferInputPassData>(
                PassName, out var passData))
            {
                TextureHandle outputColorTarget = renderGraph.CreateTexture(
                    new TextureDesc(TextureScale, true, false)
                    {
                        colorFormat = ColorFormat,
                        clearBuffer = ClearBuffer,
                        clearColor = ClearColor,
                        name = PassName,
                    });

                passData.colorTarget = builder.UseColorBuffer(outputColorTarget, 0);

                // Create a handle on the output slot so downstream passes can read it.
                ColorTargetSlot.CreateHandle();

                builder.SetRenderFunc(
                    (ColorBufferInputPassData data, RenderGraphContext ctx) =>
                    {
                        // Texture creation is handled by renderGraph.CreateTexture above.
                        // The render graph allocates the transient texture automatically
                        // during execution; no rendering work is needed here.
                    });
            }
        }

        /// <inheritdoc />
        public override void Cleanup()
        {
            // No disposable resources held by this pass.
        }

        // ── Pass data ──

        /// <summary>
        /// Render graph pass data container for <see cref="ColorBufferInputPass"/>.
        /// Holds the color target handle populated by <c>builder.UseColorBuffer</c>.
        /// </summary>
        public class ColorBufferInputPassData
        {
            /// <summary>
            /// The color target texture handle.
            /// Populated by <c>builder.UseColorBuffer</c> during <see cref="Record"/>.
            /// </summary>
            public TextureHandle colorTarget;
        }
    }
}
