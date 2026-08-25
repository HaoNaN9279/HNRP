// <copyright file="DrawObjectPass.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RendererUtils;

namespace HN.HNRP
{
    /// <summary>
    /// Generic parameterized object-drawing pass.
    /// All resources — color / depth targets, light data buffers, reflection probe
    /// data, and the renderer list — are supplied as <see cref="ResourceNode"/>
    /// inputs. The pass never allocates its own resources.
    /// </summary>
    /// <remarks>
    /// <para><b>Inputs (all optional except <see cref="ColorTargetSlot"/> /
    /// <see cref="DepthTargetSlot"/> / <see cref="RendererListSlot"/>):</b></para>
    /// <list type="bullet">
    ///   <item><b>ColorTarget</b> — the color buffer written by draw calls.</item>
    ///   <item><b>DepthTarget</b> — the depth buffer written by draw calls.</item>
    ///   <item><b>LightDatas</b> — compute buffer with light data for shader access.</item>
    ///   <item><b>ReflectionProbeAtlas</b> — reflection probe cubemap atlas texture.</item>
    ///   <item><b>ProbeMask</b> — cluster culling reflection probe mask buffer.</item>
    ///   <item><b>ProbeDatas</b> — cluster culling reflection probe data buffer.</item>
    ///   <item><b>LightMask</b> — cluster culling light mask buffer.</item>
    ///   <item><b>RendererList</b> — the renderer list to draw.</item>
    /// </list>
    /// <para>
    /// When <see cref="SetLightGlobals"/> is <c>true</c> the render function also
    /// binds the probe / light / light-data shader globals (replacing the old
    /// Forward Opaque pass behavior). When <c>false</c> only
    /// <c>DrawRendererList</c> is emitted (e.g. preview graphs without cluster data).
    /// </para>
    /// <para>
    /// <b>Outputs (pass-through for downstream chaining):</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><b>ColorTargetOutput</b> — pass-through of the input color target
    ///   so downstream passes can connect without a separate resource node.</item>
    ///   <item><b>DepthTargetOutput</b> — pass-through of the input depth target
    ///   so downstream passes can connect without a separate resource node.</item>
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
        /// <remarks>
        /// Retained for config compatibility. The actual renderer list comes from
        /// the <see cref="RendererListSlot"/> input (whose resource definition
        /// carries its own layer mask).
        /// </remarks>
        public uint RenderingLayerMask { get; set; } = 0x00000001;

        /// <summary>
        /// Gets or sets a value indicating whether the render function should set
        /// the probe / light / light-datas shader globals before drawing.
        /// Default is <c>true</c>. Set to <c>false</c> for graphs that have no
        /// cluster culling data (e.g. preview).
        /// </summary>
        public bool SetLightGlobals { get; set; } = true;

        // ── Slots ──

        /// <summary>
        /// Gets the input color target slot.
        /// Available after <see cref="Pass.SetupSlots"/> is called.
        /// </summary>
        public TextureSlot? ColorTargetSlot { get; private set; }

        /// <summary>
        /// Gets the input depth target slot.
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

        /// <summary>
        /// Gets the input renderer list slot.
        /// Available after <see cref="Pass.SetupSlots"/> is called.
        /// </summary>
        public RendererListSlot? RendererListSlot { get; private set; }

        /// <summary>
        /// Gets the output color target slot (pass-through of the input
        /// <see cref="ColorTargetSlot"/> handle for downstream chaining).
        /// Available after <see cref="Pass.SetupSlots"/> is called.
        /// </summary>
        public TextureSlot? ColorTargetOutputSlot { get; private set; }

        /// <summary>
        /// Gets the output depth target slot (pass-through of the input
        /// <see cref="DepthTargetSlot"/> handle for downstream chaining).
        /// Available after <see cref="Pass.SetupSlots"/> is called.
        /// </summary>
        public TextureSlot? DepthTargetOutputSlot { get; private set; }

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
            ColorTargetSlot = new TextureSlot("ColorTarget", SlotDirection.Input);
            RegisterSlot(ColorTargetSlot);
            DepthTargetSlot = new TextureSlot("DepthTarget", SlotDirection.Input);
            RegisterSlot(DepthTargetSlot);
            LightDatasSlot = new ComputeBufferSlot("LightDatas", SlotDirection.Input);
            RegisterSlot(LightDatasSlot);
            ReflectionProbeAtlasSlot = new TextureSlot("ReflectionProbeAtlas", SlotDirection.Input);
            RegisterSlot(ReflectionProbeAtlasSlot);
            ProbeMaskSlot = new ComputeBufferSlot("ProbeMask", SlotDirection.Input);
            RegisterSlot(ProbeMaskSlot);
            ProbeDatasSlot = new ComputeBufferSlot("ProbeDatas", SlotDirection.Input);
            RegisterSlot(ProbeDatasSlot);
            LightMaskSlot = new ComputeBufferSlot("LightMask", SlotDirection.Input);
            RegisterSlot(LightMaskSlot);
            RendererListSlot = new RendererListSlot("RendererList", SlotDirection.Input);
            RegisterSlot(RendererListSlot);

            ColorTargetOutputSlot = new TextureSlot("ColorTargetOutput", SlotDirection.Output);
            RegisterSlot(ColorTargetOutputSlot);
            DepthTargetOutputSlot = new TextureSlot("DepthTargetOutput", SlotDirection.Output);
            RegisterSlot(DepthTargetOutputSlot);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Stores the camera context so renderer list / lighting globals can be
        /// resolved during <see cref="Record"/>.
        /// </remarks>
        public override void Initialize(CameraContext context)
        {
            cameraContext = context;
        }

        /// <inheritdoc />
        public override void Record(RenderGraph renderGraph)
        {
            if (ColorTargetSlot == null || DepthTargetSlot == null || RendererListSlot == null)
            {
                return;
            }

            if (cameraContext == null)
            {
                return;
            }

            // ── Required inputs: color / depth targets + renderer list ──

            if (!ColorTargetSlot.IsConnected || !DepthTargetSlot.IsConnected || !RendererListSlot.IsConnected)
            {
                return;
            }

            TextureHandle colorTarget = ColorTargetSlot.ReadHandle();
            if (!colorTarget.IsValid())
            {
                return;
            }

            TextureHandle depthTarget = DepthTargetSlot.ReadHandle();
            if (!depthTarget.IsValid())
            {
                return;
            }

            RendererListHandle rendererList = RendererListSlot.ReadHandle();
            if (!rendererList.IsValid())
            {
                return;
            }

            // Pass-through the input color / depth handles to the output slots so
            // downstream passes can chain from this pass's outputs.
            if (ColorTargetOutputSlot != null)
            {
                ColorTargetOutputSlot.SetHandle(colorTarget);
            }

            if (DepthTargetOutputSlot != null)
            {
                DepthTargetOutputSlot.SetHandle(depthTarget);
            }

            using var builder = renderGraph.AddRenderPass<DrawObjectPassData>(
                PassName, out var passData);

            builder.AllowRendererListCulling(false);

            passData.colorTarget = builder.UseColorBuffer(colorTarget, 0);
            passData.depthTarget = builder.UseDepthBuffer(depthTarget, DepthAccess.ReadWrite);

            // ── Optional inputs: gated independently on connectivity ──

            bool hasLightDatas = LightDatasSlot?.IsConnected == true;
            if (hasLightDatas)
            {
                passData.lightDatasBuffer = builder.ReadComputeBuffer(
                    LightDatasSlot.ReadHandle());
            }

            bool hasReflectionProbeAtlas = ReflectionProbeAtlasSlot?.IsConnected == true;
            if (hasReflectionProbeAtlas)
            {
                passData.reflectionProbeAtlas = builder.ReadTexture(
                    ReflectionProbeAtlasSlot.ReadHandle());
            }

            bool hasProbeMask = ProbeMaskSlot?.IsConnected == true;
            if (hasProbeMask)
            {
                passData.probeMaskBuffer = builder.ReadComputeBuffer(
                    ProbeMaskSlot.ReadHandle());
            }

            bool hasProbeDatas = ProbeDatasSlot?.IsConnected == true;
            if (hasProbeDatas)
            {
                passData.probeDatasBuffer = builder.ReadComputeBuffer(
                    ProbeDatasSlot.ReadHandle());
            }

            bool hasLightMask = LightMaskSlot?.IsConnected == true;
            if (hasLightMask)
            {
                passData.lightMaskBuffer = builder.ReadComputeBuffer(
                    LightMaskSlot.ReadHandle());
            }

            // ── Renderer list: read from the resource node input ──

            passData.rendererList = builder.UseRendererList(rendererList);

            // ── Render function ──
            // The probe keyword requires all three probe slots connected at record
            // time (mirrors the original Forward Opaque behavior).

            bool setLightGlobals = SetLightGlobals;
            bool enableProbeKeyword = hasReflectionProbeAtlas && hasProbeMask && hasProbeDatas;

            // Explicit camera matrices for this pass. SetupCameraProperties on the
            // ScriptableRenderContext only stores the LAST camera's matrix as the
            // active global state, so passes that render offscreen cameras (e.g.
            // realtime probe faces) must set the matrices per pass — otherwise every
            // draw would use the main camera's view.
            // All passes in HNRP render through RenderGraph which always renders to
            // render textures internally, so renderIntoTexture is always true.
            Camera passCamera = cameraContext?.Camera;
            passData.viewMatrix = passCamera != null ? passCamera.worldToCameraMatrix : Matrix4x4.identity;
            passData.projMatrix = passCamera != null
                ? GL.GetGPUProjectionMatrix(passCamera.projectionMatrix, true)
                : Matrix4x4.identity;

            builder.SetRenderFunc(
                (DrawObjectPassData data, RenderGraphContext ctx) =>
                {
                    ctx.cmd.SetViewProjectionMatrices(data.viewMatrix, data.projMatrix);

                    if (setLightGlobals)
                    {
                        // Reflection probe shader keyword + globals (all three
                        // probe slots must be connected).
                        if (enableProbeKeyword)
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
                        else
                        {
                            ctx.cmd.DisableShaderKeyword(GlobalKeywords.clusterCullingReflectionProbe);
                        }

                        // Cluster culling light shader keyword + globals
                        if (hasLightMask)
                        {
                            ctx.cmd.EnableShaderKeyword(
                                GlobalKeywords.clusterCullingLight);
                            ctx.cmd.SetGlobalBuffer(
                                ClusterCullingLightPass.PropertyIDs.clusterCullingLightMaskBuffer,
                                data.lightMaskBuffer);
                        }

                        // Light data buffer (set only when the slot is connected —
                        // avoids binding an invalid handle when there is no light pass)
                        if (hasLightDatas)
                        {
                            ctx.cmd.SetGlobalBuffer(
                                BuildLightDataPass.PropertyIDs.LightDatasBuffer,
                                data.lightDatasBuffer);
                        }
                    }

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
            /// The renderer list handle.
            /// </summary>
            public RendererListHandle rendererList;

            /// <summary>
            /// The view matrix for this pass's camera.
            /// </summary>
            public Matrix4x4 viewMatrix;

            /// <summary>
            /// The GPU projection matrix for this pass's camera.
            /// </summary>
            public Matrix4x4 projMatrix;
        }
    }
}
