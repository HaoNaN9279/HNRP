using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    /// <summary>
    /// HNRP custom render pipeline implementation.
    /// Uses <see cref="CameraRenderer"/> per camera with a shared <see cref="RenderGraph"/>
    /// instance. Each camera selects its own <see cref="CameraPipelineConfig"/> via the
    /// priority chain: <c>pipelineConfigOverride ?? defaultXxxConfig ?? null</c>.
    /// </summary>
    public class HNRenderPipeline : RenderPipeline
    {
        /// <summary>
        /// Gets the pipeline asset that owns this pipeline instance.
        /// Unlike the static <see cref="Asset"/> property (which reads from
        /// <see cref="GraphicsSettings.currentRenderPipeline"/>), this instance
        /// reference is set at construction time and works in EditMode tests.
        /// </summary>
        public HNRenderPipelineAsset InstanceAsset { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HNRenderPipeline"/> class.
        /// </summary>
        /// <param name="asset">The pipeline asset providing configuration.</param>
        public HNRenderPipeline(HNRenderPipelineAsset asset)
        {
            InstanceAsset = asset;

            GraphicsSettings.lightsUseLinearIntensity = QualitySettings.activeColorSpace == ColorSpace.Linear;
            GraphicsSettings.lightsUseColorTemperature = true;
            GraphicsSettings.defaultRenderingLayerMask = defaultRenderingLayerMask;
            GraphicsSettings.useScriptableRenderPipelineBatching = true;

            try
            {
                Blitter.Initialize(
                    asset.runtimeResources.shaderResources.Blit,
                    asset.runtimeResources.shaderResources.BlitColorAndDepth);
            }
            catch (Exception)
            {
                // Blitter may already be initialized (e.g. by a previous test pipeline).
                // This is safe to ignore.
            }

            RTHandles.Initialize(Screen.width, Screen.height);
        }

        /// <inheritdoc />
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            Render(context, new List<Camera>(cameras));
        }

        /// <inheritdoc />
        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            BeginContextRendering(context, cameras);

#if UNITY_EDITOR
            if (globalSettings == null || HNRenderPipelineGlobalSettings.Instance == null)
            {
                globalSettings = HNRenderPipelineGlobalSettings.Ensure();
                if (globalSettings == null)
                    return;
            }
#endif

            foreach (Camera camera in cameras)
            {
                var cameraData = camera.GetHNRPAdditionalCameraData();

                // ── Select CameraPipelineConfig ──
                CameraPipelineConfig pipelineConfig = SelectPipelineConfig(camera, cameraData);
                if (pipelineConfig == null || pipelineConfig.RenderGraph == null)
                    continue;

                // ── Per-camera setup ──
                RTHandles.SetReferenceSize(camera.pixelWidth, camera.pixelHeight);

                if (camera.targetTexture != null)
                {
                    camera.targetTexture.IncrementUpdateCount();
                }

#if UNITY_EDITOR
                if (camera.cameraType == CameraType.SceneView)
                {
                    ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
                }
#endif

                SetupCameraProperties(context, camera);

                // ── Create CameraContext ──
                var cameraContext = new CameraContext(camera, context)
                {
                    TargetId = camera.targetTexture != null
                        ? new RenderTargetIdentifier(camera.targetTexture)
                        : new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget),
                    RuntimeResources = InstanceAsset.runtimeResources,
                };

                // ── Cull ──
                if (camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParams))
                {
                    cameraContext.CullingResults = context.Cull(ref cullingParams);
                }

                // ── Create CameraRenderer, build from template, render ──
                var cameraRenderer = new CameraRenderer(cameraContext);
                cameraRenderer.Build(pipelineConfig.RenderGraph);

                BeginCameraRendering(context, camera);
                cameraRenderer.Render(renderGraph, context);
                EndCameraRendering(context, camera);

                // ── Cleanup per-camera context ──
                cameraContext.Dispose();
            }

            // ── Execute all recorded render graph passes ──
            renderGraph.EndFrame();

            context.Submit();

            EndContextRendering(context, cameras);
        }

        /// <summary>
        /// Selects the <see cref="CameraPipelineConfig"/> for a camera using the priority chain:
        /// <c>pipelineConfigOverride ?? defaultXxxConfig ?? null</c>.
        /// </summary>
        /// <param name="camera">The camera being rendered.</param>
        /// <param name="cameraData">The camera's additional data component.</param>
        /// <returns>
        /// The selected <see cref="CameraPipelineConfig"/>, or <c>null</c> if no config
        /// is available (camera will be skipped).
        /// </returns>
        public CameraPipelineConfig SelectPipelineConfig(Camera camera, HNAdditionalCameraData cameraData)
        {
            // Step 1: Check per-camera override
            CameraPipelineConfig config = cameraData.PipelineConfigOverride;
            if (config != null)
                return config;

            // Step 2: Fall back to default config based on camera type
            return GetDefaultConfigForCameraType(camera.cameraType);
        }

        /// <summary>
        /// Gets the default <see cref="CameraPipelineConfig"/> from <see cref="HNRenderPipelineAsset"/>
        /// based on the camera type.
        /// </summary>
        /// <param name="cameraType">The camera's type.</param>
        /// <returns>The default config for the given camera type, or <c>null</c>.</returns>
        private CameraPipelineConfig GetDefaultConfigForCameraType(CameraType cameraType)
        {
            return cameraType switch
            {
                CameraType.Game => InstanceAsset.DefaultGameCameraConfig,
#if UNITY_EDITOR
                CameraType.SceneView => InstanceAsset.DefaultSceneViewCameraConfig,
                CameraType.Preview => InstanceAsset.DefaultPreviewCameraConfig,
#endif
                CameraType.Reflection => InstanceAsset.DefaultReflectionCameraConfig,
                _ => null,
            };
        }

        /// <summary>
        /// Sets up per-frame camera properties (VP matrix, etc.) on the render context.
        /// </summary>
        /// <param name="context">The scriptable render context.</param>
        /// <param name="camera">The camera to set up.</param>
        private static void SetupCameraProperties(ScriptableRenderContext context, Camera camera)
        {
            var cmd = CommandBufferPool.Get("CameraSetup");
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);

            context.SetupCameraProperties(camera);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Blitter.Cleanup();

            Graphics.SetRenderTarget(null);

            renderGraph.Cleanup();
            renderGraph = null;

            ConstantBuffer.ReleaseAll();
        }

        /// <summary>
        /// Gets the current pipeline asset. Convenience accessor.
        /// </summary>
        public static HNRenderPipelineAsset Asset
        {
            get => GraphicsSettings.currentRenderPipeline as HNRenderPipelineAsset;
        }

        /// <summary>
        /// The shared <see cref="RenderGraph"/> instance used across all cameras.
        /// All cameras record their passes into this graph; <see cref="RenderGraph.EndFrame"/>
        /// compiles and executes them together.
        /// </summary>
        internal RenderGraph renderGraph = new RenderGraph("HNRP");

        /// <inheritdoc />
        public override RenderPipelineGlobalSettings defaultSettings => globalSettings;

        private HNRenderPipelineGlobalSettings globalSettings;

        internal const int defaultRenderingLayerMask = 0x00000001;
    }
}
