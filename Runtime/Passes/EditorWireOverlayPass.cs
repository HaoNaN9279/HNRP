using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    /// <summary>
    /// Draws the Editor wire overlay (gizmos, selection outlines, etc.) into the color target.
    /// New <see cref="Pass"/>-based replacement for the legacy
    /// <see cref="EditorWireOverlayPass"/> (<c>PassBase</c>).
    /// </summary>
    /// <remarks>
    /// <para>Outputs:</para>
    /// <list type="bullet">
    ///   <item><b>ColorTarget</b> — the color buffer into which the wire overlay is drawn.</item>
    /// </list>
    /// <para>
    /// This pass is only active in the Unity Editor and only for Scene View cameras.
    /// The entire <see cref="Record"/> implementation is wrapped in <c>#if UNITY_EDITOR</c>.
    /// The render function calls <c>ctx.renderContext.DrawWireOverlay(camera)</c>,
    /// matching the legacy <see cref="EditorWireOverlayPass"/> behavior.
    /// </para>
    /// </remarks>
    [Pass(PassNameConst)]
    public sealed class EditorWireOverlayPass : Pass
    {
        /// <summary>
        /// The constant pass name string used for registration and identification.
        /// Matches the legacy <see cref="EditorWireOverlayPass.PassName"/> pattern.
        /// </summary>
        public const string PassNameConst = "Editor Wire Overlay";

        // ── Slots ──

        /// <summary>
        /// Gets the output color target slot.
        /// Available after <see cref="SetupSlots"/> is called.
        /// </summary>
        public TextureSlot? ColorTargetSlot { get; private set; }

        // ── Camera context ──

        private CameraContext? cameraContext;

        // ── Constructor ──

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorWireOverlayPass"/> class.
        /// </summary>
        /// <param name="passName">
        /// The instance name of this pass. Must be non-null and unique within the render graph.
        /// </param>
        public EditorWireOverlayPass(string passName)
            : base(passName)
        {
        }

        // ── Lifecycle ──

        /// <inheritdoc />
        public override void SetupSlots()
        {
            ColorTargetSlot = new TextureSlot("ColorTarget", SlotDirection.Output);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Stores the camera context so the camera can be accessed during
        /// <see cref="Record"/> for the <c>DrawWireOverlay</c> call.
        /// </remarks>
        public override void Initialize(CameraContext context)
        {
            cameraContext = context;
        }

        /// <inheritdoc />
        /// <remarks>
        /// <para>
        /// Creates a color texture and writes it via <c>builder.UseColorBuffer</c>,
        /// then sets a render function that draws the Editor wire overlay using
        /// <c>ctx.renderContext.DrawWireOverlay(camera)</c> — identical logic to
        /// the legacy <see cref="EditorWireOverlayPass.Record"/>.
        /// </para>
        /// <para>
        /// Only active for Scene View cameras. The entire implementation is
        /// wrapped in <c>#if UNITY_EDITOR</c> so it is stripped from player builds.
        /// </para>
        /// </remarks>
        public override void Record(RenderGraph renderGraph)
        {
#if UNITY_EDITOR
            if (ColorTargetSlot == null)
            {
                return;
            }

            if (cameraContext == null)
            {
                return;
            }

            Camera camera = cameraContext.Camera;
            if (camera.cameraType != CameraType.SceneView)
            {
                return;
            }

            using var builder = renderGraph.AddRenderPass<EditorWireOverlayPassData>(
                PassName, out var passData);

            builder.AllowPassCulling(false);

            // ── Output slot: create and register color target ──

            var colorDesc = new TextureDesc(Vector2.one, true, false)
            {
                colorFormat = GraphicsFormat.R8G8B8A8_UNorm,
                clearBuffer = false,
                name = $"{PassName}_ColorTarget",
            };

            TextureHandle colorTarget = renderGraph.CreateTexture(colorDesc);

            passData.colorTarget = builder.UseColorBuffer(colorTarget, 0);

            ColorTargetSlot.CreateHandle();

            // ── Render function: draw wire overlay (same logic as legacy EditorWireOverlayPass) ──

            builder.SetRenderFunc(
                (EditorWireOverlayPassData data, RenderGraphContext ctx) =>
                {
                    ctx.renderContext.ExecuteCommandBuffer(ctx.cmd);
                    ctx.cmd.Clear();
                    ctx.renderContext.DrawWireOverlay(camera);
                });
#endif
        }

        /// <inheritdoc />
        public override void Cleanup()
        {
            // No disposable resources held by this pass.
        }

        // ── Pass data ──

        /// <summary>
        /// Render graph pass data container for <see cref="EditorWireOverlayPass"/>.
        /// </summary>
        private sealed class EditorWireOverlayPassData
        {
            /// <summary>
            /// The color target texture handle.
            /// </summary>
            public TextureHandle colorTarget;
        }
    }
}
