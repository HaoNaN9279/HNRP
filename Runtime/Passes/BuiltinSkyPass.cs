// <copyright file="BuiltinSkyPass.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace HN.HNRP
{
    /// <summary>
    /// Renders the skybox into the color and depth targets.
    /// New <see cref="Pass"/>-based replacement for the legacy
    /// <see cref="BuiltinSkyPass"/> (<c>PassBase</c>).
    /// </summary>
    /// <remarks>
    /// <para>Outputs:</para>
    /// <list type="bullet">
    ///   <item><b>ColorTarget</b> — the color buffer into which the skybox is rendered.</item>
    ///   <item><b>DepthTarget</b> — the depth buffer used for skybox depth writes.</item>
    /// </list>
    /// <para>
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
            ColorTargetSlot = new TextureSlot("ColorTarget", SlotDirection.Output);
            DepthTargetSlot = new TextureSlot("DepthTarget", SlotDirection.Output);
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
        /// Creates color and depth textures, writes them via
        /// <c>builder.UseColorBuffer</c> and <c>builder.UseDepthBuffer</c>,
        /// and sets a render function that draws the skybox using
        /// <c>ctx.renderContext.CreateSkyboxRendererList</c> — identical
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

            using var builder = renderGraph.AddRenderPass<BuiltinSkyPassData>(
                PassName, out var passData);

            builder.AllowPassCulling(false);

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

            // ── Render function: draw skybox (same logic as legacy BuiltinSkyPass) ──

            var camera = cameraContext.Camera;
            builder.SetRenderFunc(
                (BuiltinSkyPassData data, RenderGraphContext ctx) =>
                {
                    RendererList rendererList = ctx.renderContext.CreateSkyboxRendererList(camera);
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
