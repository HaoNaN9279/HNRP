// <copyright file="DrawObjectPass.cs" company="HN">
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
    /// Generic object-drawing pass that creates a <see cref="RendererList"/> from
    /// the camera's culling results and draws it using
    /// <c>ctx.cmd.DrawRendererList</c>.
    /// New <see cref="Pass"/>-based replacement for the legacy
    /// <see cref="DrawObjectPass"/> (<c>PassBase</c>).
    /// </summary>
    /// <remarks>
    /// <para>Outputs:</para>
    /// <list type="bullet">
    ///   <item><b>ColorTarget</b> — the color buffer written by draw calls.</item>
    ///   <item><b>DepthTarget</b> — the depth buffer written by draw calls.</item>
    /// </list>
    /// <para>Inputs (optional — gated via <c>IsConnected</c>):</para>
    /// <list type="bullet">
    ///   <item><b>LightDatas</b> — compute buffer with light data for shader access.</item>
    ///   <item><b>ReflectionProbeAtlas</b> — reflection probe cubemap atlas texture.</item>
    ///   <item><b>ProbeMask</b> — cluster culling reflection probe mask buffer.</item>
    ///   <item><b>ProbeDatas</b> — cluster culling reflection probe data buffer.</item>
    ///   <item><b>LightMask</b> — cluster culling light mask buffer.</item>
    /// </list>
    /// </remarks>
    [Pass(PassNameConst)]
    public sealed class DrawObjectPass : Pass
    {
        /// <summary>
        /// The constant pass name string used for registration and identification.
        /// </summary>
        public const string PassNameConst = "Draw Object";

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
        /// Available after <see cref="Pass.SetupSlots"/> is called.
        /// </summary>
        public TextureSlot? ColorTargetSlot { get; private set; }

        /// <summary>
        /// Gets the output depth target slot.
        /// Available after <see cref="Pass.SetupSlots"/> is called.
        /// </summary>
        public TextureSlot? DepthTargetSlot { get; private set; }

        /// <summary>
        /// Gets the input light data compute buffer slot.
        /// Available after <see cref="Pass.SetupSlots"/> is called.
        /// </summary>
        public ComputeBufferSlot? LightDatasSlot { get; private set; }

        /// <summary>
        /// Gets the input reflection probe atlas texture slot.
        /// Available after <see cref="Pass.SetupSlots"/> is called.
        /// </summary>
        public TextureSlot? ReflectionProbeAtlasSlot { get; private set; }

        /// <summary>
        /// Gets the input cluster culling reflection probe mask buffer slot.
        /// Available after <see cref="Pass.SetupSlots"/> is called.
        /// </summary>
        public ComputeBufferSlot? ProbeMaskSlot { get; private set; }

        /// <summary>
        /// Gets the input cluster culling reflection probe data buffer slot.
        /// Available after <see cref="Pass.SetupSlots"/> is called.
        /// </summary>
        public ComputeBufferSlot? ProbeDatasSlot { get; private set; }

        /// <summary>
        /// Gets the input cluster culling light mask buffer slot.
        /// Available after <see cref="Pass.SetupSlots"/> is called.
        /// </summary>
        public ComputeBufferSlot? LightMaskSlot { get; private set; }

        // ── Camera context ──

        private CameraContext? cameraContext;

        // ── Constructor ──

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawObjectPass"/> class.
        /// </summary>
        /// <param name="passName">
        /// The instance name of this pass. Must be non-null and unique within the render graph.
        /// </param>
        public DrawObjectPass(string passName)
            : base(passName)
        {
        }

        // ── Lifecycle ──

        /// <inheritdoc />
        public override void SetupSlots()
        {
            ColorTargetSlot = new TextureSlot("ColorTarget", SlotDirection.Output);
            DepthTargetSlot = new TextureSlot("DepthTarget", SlotDirection.Output);
            LightDatasSlot = new ComputeBufferSlot("LightDatas", SlotDirection.Input);
            ReflectionProbeAtlasSlot = new TextureSlot("ReflectionProbeAtlas", SlotDirection.Input);
            ProbeMaskSlot = new ComputeBufferSlot("ProbeMask", SlotDirection.Input);
            ProbeDatasSlot = new ComputeBufferSlot("ProbeDatas", SlotDirection.Input);
            LightMaskSlot = new ComputeBufferSlot("LightMask", SlotDirection.Input);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Stores the camera context so the renderer list can be built from
        /// <c>CullingResults</c>, <c>Camera</c>, and <c>RuntimeResources</c>
        /// during <see cref="Record"/>.
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

            using var builder = renderGraph.AddRenderPass<DrawObjectPassData>(
                PassName, out var passData);

            builder.AllowRendererListCulling(false);

            // ── Output slots: create and register color / depth targets ──
            // Explicit size so window resizes allocate a correctly-sized target.
            var colorDesc = new TextureDesc(
                cameraContext.Camera.pixelWidth,
                cameraContext.Camera.pixelHeight,
                false, false)
            {
                colorFormat = GraphicsFormat.R8G8B8A8_UNorm,
                clearBuffer = false,
                name = $"{PassName}_ColorTarget",
            };

            var depthDesc = new TextureDesc(
                cameraContext.Camera.pixelWidth,
                cameraContext.Camera.pixelHeight,
                false, false)
            {
                depthBufferBits = DepthBits.Depth32,
                clearBuffer = false,
                name = $"{PassName}_DepthTarget",
            };

            TextureHandle colorTarget = renderGraph.CreateTexture(colorDesc);
            TextureHandle depthTarget = renderGraph.CreateTexture(depthDesc);

            passData.colorTarget = builder.UseColorBuffer(colorTarget, 0);
            passData.depthTarget = builder.UseDepthBuffer(depthTarget, DepthAccess.ReadWrite);

            ColorTargetSlot.SetHandle(colorTarget);
            DepthTargetSlot.SetHandle(depthTarget);

            // ── Input slot: light data buffer ──

            if (LightDatasSlot?.IsConnected == true)
            {
                passData.lightDatasBuffer = builder.ReadComputeBuffer(
                    (ComputeBufferHandle)LightDatasSlot.ReadHandle()!);
            }

            // ── Input slots: reflection probe atlas + cluster culling buffers ──

            if (ReflectionProbeAtlasSlot?.IsConnected == true
                && ProbeMaskSlot?.IsConnected == true
                && ProbeDatasSlot?.IsConnected == true)
            {
                passData.reflectionProbeAtlas = builder.ReadTexture(
                    (TextureHandle)ReflectionProbeAtlasSlot.ReadHandle()!);
                passData.probeMaskBuffer = builder.ReadComputeBuffer(
                    (ComputeBufferHandle)ProbeMaskSlot.ReadHandle()!);
                passData.probeDatasBuffer = builder.ReadComputeBuffer(
                    (ComputeBufferHandle)ProbeDatasSlot.ReadHandle()!);
            }

            // ── Input slot: cluster culling light mask buffer ──

            if (LightMaskSlot?.IsConnected == true)
            {
                passData.lightMaskBuffer = builder.ReadComputeBuffer(
                    (ComputeBufferHandle)LightMaskSlot.ReadHandle()!);
            }

            // ── Renderer list: same logic as old DrawObjectPass ──

            RendererListDesc rendererListDesc = HNRenderPipelineUtils.GetOpaqueRendererListDesc(
                ShaderPassNames.AllForwardNames,
                cameraContext.CullingResults,
                cameraContext.Camera,
                RenderingLayerMask);

            passData.rendererList = builder.UseRendererList(
                renderGraph.CreateRendererList(rendererListDesc));

            // ── Render function ──

            builder.SetRenderFunc(
                (DrawObjectPassData data, RenderGraphContext ctx) =>
                {
                    // Reflection probe shader keyword + globals
                    if (ReflectionProbeAtlasSlot?.IsConnected == true
                        && ProbeMaskSlot?.IsConnected == true
                        && ProbeDatasSlot?.IsConnected == true)
                    {
                        ctx.cmd.EnableShaderKeyword(
                            GlobalKeywords.clusterCullingReflectionProbe);
                        ctx.cmd.SetGlobalTexture(
                            ClusterCullingReflectionProbePass.PropertyIDs.reflectionProbeAtlas,
                            data.reflectionProbeAtlas);
                        ctx.cmd.SetGlobalBuffer(
                            ClusterCullingReflectionProbePass.PropertyIDs.clusterCullingReflectionProbeMaskBuffer,
                            data.probeMaskBuffer);
                        ctx.cmd.SetGlobalBuffer(
                            ClusterCullingReflectionProbePass.PropertyIDs.clusterCullingReflectionProbeDatasBuffer,
                            data.probeDatasBuffer);
                    }

                    // Cluster culling light shader keyword + globals
                    if (LightMaskSlot?.IsConnected == true)
                    {
                        ctx.cmd.EnableShaderKeyword(
                            GlobalKeywords.clusterCullingLight);
                        ctx.cmd.SetGlobalBuffer(
                            ClusterCullingLightPass.PropertyIDs.clusterCullingLightMaskBuffer,
                            data.lightMaskBuffer);
                    }

                    // Light data buffer (always set if available — gated by slot
                    // connectivity at record time)
                    ctx.cmd.SetGlobalBuffer(
                        BuildLightDataPass.PropertyIDs.LightDatasBuffer,
                        data.lightDatasBuffer);

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
        /// Render graph pass data container for <see cref="DrawObjectPass"/>.
        /// </summary>
        private sealed class DrawObjectPassData
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
            /// The light data compute buffer handle.
            /// </summary>
            public ComputeBufferHandle lightDatasBuffer;

            /// <summary>
            /// The reflection probe atlas texture handle.
            /// </summary>
            public TextureHandle reflectionProbeAtlas;

            /// <summary>
            /// The cluster culling reflection probe mask buffer handle.
            /// </summary>
            public ComputeBufferHandle probeMaskBuffer;

            /// <summary>
            /// The cluster culling reflection probe data buffer handle.
            /// </summary>
            public ComputeBufferHandle probeDatasBuffer;

            /// <summary>
            /// The cluster culling light mask buffer handle.
            /// </summary>
            public ComputeBufferHandle lightMaskBuffer;

            /// <summary>
            /// The opaque renderer list handle.
            /// </summary>
            public RendererListHandle rendererList;
        }
    }
}
