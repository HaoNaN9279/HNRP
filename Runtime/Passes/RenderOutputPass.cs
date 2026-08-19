// <copyright file="RenderOutputPass.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    /// <summary>
    /// Outputs the final rendered color to the camera backbuffer via a blit.
    /// New <see cref="Pass"/>-based replacement for the legacy
    /// <see cref="RenderOutput"/> (<c>PassBase</c>).
    /// </summary>
    /// <remarks>
    /// This pass consumes a color texture from an upstream output slot
    /// (e.g. <c>ColorBufferInput</c>) and blits it to the camera target
    /// using the <see cref="Blitter"/> utility.
    /// </remarks>
    [Pass("Render Output")]
    public sealed class RenderOutputPass : Pass
    {
        private TextureSlot? colorTargetSlot;
        private CameraContext? cameraContext;

        /// <summary>
        /// Gets the color target input slot declared by this pass.
        /// Available after <see cref="SetupSlots"/> is called.
        /// </summary>
        public TextureSlot? ColorTargetSlot => colorTargetSlot;

        /// <summary>
        /// Gets or sets a value indicating whether the output should be
        /// vertically flipped. Default is <c>false</c>.
        /// </summary>
        public bool Flip { get; set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderOutputPass"/> class.
        /// </summary>
        /// <param name="passName">
        /// The name of this pass. Default is "Render Output".
        /// </param>
        public RenderOutputPass(string passName = "Render Output")
            : base(passName)
        {
        }

        /// <inheritdoc />
        public override void SetupSlots()
        {
            colorTargetSlot = new TextureSlot("ColorTarget", SlotDirection.Input);
            RegisterSlot(colorTargetSlot);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Stores the camera context for use during <see cref="Record"/>.
        /// The <see cref="Blitter"/> static utility class is used for the
        /// final blit — its initialization is handled by the pipeline setup.
        /// </remarks>
        public override void Initialize(CameraContext context)
        {
            cameraContext = context;
        }

        /// <inheritdoc />
        public override void Record(RenderGraph renderGraph)
        {
            if (colorTargetSlot == null || !colorTargetSlot.IsConnected)
            {
                return;
            }

            if (cameraContext == null)
            {
                return;
            }

            var backBuffer = renderGraph.ImportBackbuffer(
                BuiltinRenderTextureType.CameraTarget);

            using var builder = renderGraph.AddRenderPass<RenderOutputData>(
                PassName, out var passData);
            builder.AllowPassCulling(false);

            passData.inputTexture = builder.ReadTexture(
                colorTargetSlot.ReadHandle());
            passData.backBuffer = builder.UseColorBuffer(backBuffer, 0);
            // Game cameras render into the backbuffer, whose UV origin differs
            // from the render-graph texture — flip the blit. SceneView/Preview
            // render into their own targets and must not flip.
            passData.flip = Flip
                || cameraContext.Camera.cameraType == CameraType.Game;

            builder.SetRenderFunc(
                (RenderOutputData data, RenderGraphContext ctx) =>
                {
                    var propertyBlock =
                        ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                    var scaleBias = data.flip
                        ? new Vector4(1.0f, -1.0f, 0.0f, 1.0f)
                        : new Vector4(1.0f, 1.0f, 0.0f, 0.0f);
                    Blitter.BlitCameraTexture(
                        ctx.cmd,
                        propertyBlock,
                        data.inputTexture,
                        data.backBuffer,
                        scaleBias,
                        0,
                        true);
                });
        }

        /// <summary>
        /// Render graph pass data for <see cref="RenderOutputPass"/>.
        /// </summary>
        private sealed class RenderOutputData
        {
            /// <summary>
            /// The input color texture to blit to the backbuffer.
            /// </summary>
            public TextureHandle inputTexture;

            /// <summary>
            /// The camera backbuffer that receives the blit output.
            /// </summary>
            public TextureHandle backBuffer;

            /// <summary>
            /// Whether to vertically flip the output.
            /// </summary>
            public bool flip;
        }
    }
}
