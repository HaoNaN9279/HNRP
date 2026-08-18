using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    /// <summary>
    /// Creates and outputs a depth buffer texture for the render graph.
    /// This is the new Pass-based replacement for the legacy
    /// <see cref="DepthBufferInput"/> (<c>PassBase</c>).
    /// </summary>
    /// <remarks>
    /// The output depth buffer can be consumed by any downstream pass
    /// (e.g., Forward Opaque, Transparency) that declares a matching
    /// input depth slot.
    /// </remarks>
    [Pass("Depth Buffer Input")]
    public sealed class DepthBufferInputPass : Pass
    {
        private TextureSlot? depthTargetSlot;

        /// <summary>
        /// Gets the output depth target slot declared by this pass.
        /// Available after <see cref="SetupSlots"/> is called.
        /// </summary>
        public TextureSlot? DepthTargetSlot => depthTargetSlot;

        /// <summary>
        /// Gets or sets the depth buffer bit count.
        /// Default is <see cref="DepthBits.Depth32"/>.
        /// </summary>
        public DepthBits DepthBits { get; set; } = DepthBits.Depth32;

        /// <summary>
        /// Gets or sets a value indicating whether the depth buffer is cleared
        /// each frame. Default is <c>true</c>.
        /// </summary>
        public bool ClearBuffer { get; set; } = true;

        /// <summary>
        /// Gets or sets the texture scale applied to the output depth texture.
        /// Default is <c>(1, 1)</c> — full resolution.
        /// </summary>
        public Vector2 TextureScale { get; set; } = Vector2.one;

        /// <summary>
        /// Initializes a new instance of the <see cref="DepthBufferInputPass"/> class.
        /// </summary>
        /// <param name="passName">
        /// The name of this pass. Default is "Depth Buffer Input".
        /// </param>
        public DepthBufferInputPass(string passName = "Depth Buffer Input")
            : base(passName)
        {
        }

        /// <inheritdoc />
        public override void SetupSlots()
        {
            depthTargetSlot = new TextureSlot("DepthTarget", SlotDirection.Output);
            RegisterSlot(depthTargetSlot);
        }

        /// <inheritdoc />
        public override void Initialize(CameraContext context)
        {
            // No resources to load — depth buffer is created in Record.
        }

        /// <inheritdoc />
        public override void Record(RenderGraph renderGraph)
        {
            if (depthTargetSlot == null)
            {
                return;
            }

            var depthDesc = new TextureDesc(TextureScale, false, false)
            {
                depthBufferBits = DepthBits,
                clearBuffer = ClearBuffer,
                name = PassName,
            };

            var depthTarget = renderGraph.CreateTexture(depthDesc);
            depthTargetSlot.SetHandle(depthTarget);

            using var builder = renderGraph.AddRenderPass<DepthBufferInputData>(PassName, out var passData);
            passData.depthTarget = builder.UseDepthBuffer(depthTarget, DepthAccess.ReadWrite);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc(
                (DepthBufferInputData data, RenderGraphContext ctx) =>
                {
                    // No render commands — only resource creation and clearing.
                });
        }

        /// <summary>
        /// Render graph pass data for <see cref="DepthBufferInputPass"/>.
        /// </summary>
        private sealed class DepthBufferInputData
        {
            /// <summary>
            /// The depth buffer created and written to by this pass.
            /// </summary>
            public TextureHandle depthTarget;
        }
    }
}
