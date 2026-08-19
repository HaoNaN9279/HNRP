// <copyright file="BuiltinSkyPass.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;


namespace HN.HNRP
{
    /// <summary>
    /// Renders the skybox into the color and depth targets.
    /// New <see cref="Pass"/>-based replacement for the legacy
    /// <see cref="BuiltinSkyPass"/> (<c>PassBase</c>).
    /// </summary>
    /// <remarks>
    /// <para>Inputs (connected from upstream, e.g. <c>ForwardOpaquePass</c>):</para>
    /// <list type="bullet">
    ///   <item><b>ColorTarget</b> — the color buffer into which the skybox is rendered.</item>
    ///   <item><b>DepthTarget</b> — the depth buffer used for skybox depth writes.</item>
    /// </list>
    /// <para>
    /// Uses the shared texture model: the color/depth targets are allocated by the
    /// upstream chain head pass and this pass renders into the same buffers.
    /// The render function calls <c>ctx.renderContext.CreateSkyboxRendererList</c>
    /// followed by <c>ctx.cmd.DrawRendererList</c>, which renders the Unity skybox
    /// material assigned to the active camera. This is the same logic as the
    /// legacy <see cref="BuiltinSkyPass"/>.
    /// </para>
    /// </remarks>
    [Pass(PassNameConst)]
    public sealed class BuiltinSkyPass : Pass
    {
        /// <summary>
        /// The constant pass name string used for registration and identification.
        /// Matches the legacy <see cref="BuiltinSkyPass.PassName"/>.
        /// </summary>
        public const string PassNameConst = "Builtin Sky";

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
        /// Initializes a new instance of the <see cref="BuiltinSkyPass"/> class.
        /// </summary>
        /// <param name="passName">
        /// The instance name of this pass. Must be non-null and unique within the render graph.
        /// </param>
        public BuiltinSkyPass(string passName)
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
        /// Stores the camera context so the skybox renderer list can be built
        /// from <c>Camera</c> during <see cref="Record"/>.
        /// </remarks>
        public override void Initialize(CameraContext context)
        {
            cameraContext = context;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Reads the upstream color and depth targets (shared texture model — allocated
        /// by <c>ForwardOpaquePass</c>) and sets a render function that draws the
        /// skybox using <c>ctx.renderContext.CreateSkyboxRendererList</c> — identical
        /// logic to the legacy <see cref="BuiltinSkyPass.Record"/>.
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

            using var builder = renderGraph.AddRenderPass<BuiltinSkyPassData>(
                PassName, out var passData);

            builder.AllowPassCulling(false);

            // ── Input slots: use upstream color / depth targets (shared texture model) ──

            TextureHandle colorTarget = ColorTargetSlot.ReadHandle();
            TextureHandle depthTarget = DepthTargetSlot.ReadHandle();

            passData.colorTarget = builder.UseColorBuffer(colorTarget, 0);
            passData.depthTarget = builder.UseDepthBuffer(depthTarget, DepthAccess.ReadWrite);

            // ── Render function: draw skybox (same logic as legacy BuiltinSkyPass) ──

            var camera = cameraContext.Camera;
            builder.SetRenderFunc(
                (BuiltinSkyPassData data, RenderGraphContext ctx) =>
                {
                    UnityEngine.Rendering.RendererList rendererList = ctx.renderContext.CreateSkyboxRendererList(camera);
                    ctx.cmd.DrawRendererList(rendererList);
                });
        }

        /// <inheritdoc />
        public override void Cleanup()
        {
            // No disposable resources held by this pass.
        }

        // ── Pass data ──

        /// <summary>
        /// Render graph pass data container for <see cref="BuiltinSkyPass"/>.
        /// </summary>
        private sealed class BuiltinSkyPassData
        {
            /// <summary>
            /// The color target texture handle.
            /// </summary>
            public TextureHandle colorTarget;

            /// <summary>
            /// The depth target texture handle.
            /// </summary>
            public TextureHandle depthTarget;
        }
    }
}
