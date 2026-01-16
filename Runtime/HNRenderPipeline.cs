using System;
using System.Collections;
using System.Collections.Generic;
using PlasticPipe.PlasticProtocol.Messages;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public class HNRenderPipeline : RenderPipeline
    {
        public HNRenderPipeline(HNRenderPipelineAsset asset)
        {
            GraphicsSettings.lightsUseLinearIntensity = QualitySettings.activeColorSpace == ColorSpace.Linear;
            GraphicsSettings.lightsUseColorTemperature = true;
            GraphicsSettings.defaultRenderingLayerMask = defaultRenderingLayerMask;
            GraphicsSettings.useScriptableRenderPipelineBatching = true;
            sceneViewRenderRequests = new List<RenderRequest>();
            previewRenderRequests = new List<RenderRequest>();
            reflectionRenderRequests = new List<RenderRequest>();
            gameViewRenderRequests = new List<RenderRequest>();

            RTHandles.Initialize(Screen.width, Screen.height);
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            Render(context, new List<Camera>(cameras));
        }

        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            BeginContextRendering(context, cameras);
            
#if UNITY_EDITOR
            if(globalSettings == null || HNRenderPipelineGlobalSettings.Instance == null)
            {
                globalSettings = HNRenderPipelineGlobalSettings.Ensure();
                if(globalSettings == null)
                    return;
            }
#endif
            UpdateFrameCount();

            PrepareRenderRequests(context, cameras);

            ExecuteRenderRequests(sceneViewRenderRequests);
            ExecuteRenderRequests(previewRenderRequests);
            ExecuteRenderRequests(reflectionRenderRequests);
            ExecuteRenderRequests(gameViewRenderRequests);

            context.Submit();

            EndFrame();

            EndContextRendering(context, cameras);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Graphics.SetRenderTarget(null);

            CleanupRenderGraph();
        }


        private void UpdateFrameCount()
        {
            if (renderingData.FrameCount != Time.frameCount)
            {
                renderingData.FrameCount = Time.frameCount;
            }
        }

        private void PrepareRenderRequests(ScriptableRenderContext context, List<Camera> cameras)
        {
            sceneViewRenderRequests.Clear();
            previewRenderRequests.Clear();
            reflectionRenderRequests.Clear();
            gameViewRenderRequests.Clear();

            foreach (Camera camera in cameras)
            {
                var cameraData = camera.GetHNRPAdditionalCameraData();

                HNRenderGraphBase graphObject = GetRenderGraphObject(camera, cameraData);
                if(graphObject == null)
                    return;
                if(camera.cameraType == CameraType.Reflection)
                {
                    Debug.Log($"Reflection RenderPipeline:{graphObject.name}");
                }
                CommandBuffer cmd = CommandBufferPool.Get($"RenderRequest_{camera.name}_cmd");
                RenderTargetIdentifier targetId = camera.targetTexture ?? new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
                // RenderTargetIdentifier targetId = camera.targetTexture != null ? new RenderTargetIdentifier(camera.targetTexture) : BuiltinRenderTextureType.CameraTarget;
                if (camera.targetTexture != null)
                {
                    camera.targetTexture.IncrementUpdateCount();
                }

                renderingData.Camera = camera;
                renderingData.CameraData = camera.GetHNRPAdditionalCameraData();
                renderingData.Cmd = cmd;
                renderingData.TargetId = targetId;
                renderingData.runtimeResources = Asset.runtimeResources;
                renderingData.GraphObject = graphObject;
                renderingData.catchedReflectionProbeData = new CatchedReflectionProbeData(HNRenderPipelineAsset.MAX_REFLECTION_PROBES_ON_SCREEN);
                AddRenderRequests(camera, context, renderGraph, ref renderingData);
            }
        }

        private void ExecuteRenderRequests(List<RenderRequest> renderRequests)
        {
            foreach (var request in renderRequests)
            {
                request.RecordAndExecute();

                EndCameraRendering(request.Context, renderingData.Camera);
                request.Context.ExecuteCommandBuffer(renderingData.Cmd);
                request.Context.Submit();
                CommandBufferPool.Release(renderingData.Cmd);

                request.EndRecord();
            }

        }

        private void EndFrame()
        {
            renderGraph.EndFrame();
        }


        private void CleanupRenderGraph()
        {
            renderGraph.Cleanup();
            renderGraph = null;
        }

        private HNRenderGraphBase GetRenderGraphObject(Camera camera, HNAdditionalCameraData cameraData)
        {
            RenderGraphViewBlock block = null;
            if(camera.cameraType == CameraType.Game)
            {
                block = Asset.gameViewRenderGraphViewBlock;
            }
            else if(camera.cameraType == CameraType.Reflection)
            {
                block = Asset.reflectionRenderGraphViewBlock;
            }
            else if(camera.cameraType == CameraType.SceneView)
            {
                block = Asset.sceneViewRenderGraphViewBlock;
            }
            else if(camera.cameraType == CameraType.Preview)
            {
                block = Asset.previewRenderGraphViewBlock;
            }

            if(block == null)
            {
                return null;
            }
            return block.GetRenderGraphObject(cameraData.RenderGraphViewIndex);
        }

        private void AddRenderRequests(Camera camera, ScriptableRenderContext context, RenderGraph renderGraph, ref RenderingData renderingData)
        {
            var renderRequest = new RenderRequest(context, renderGraph, ref renderingData);
            
            if(camera.cameraType == CameraType.SceneView)
            {
                sceneViewRenderRequests.Add(renderRequest);
            }
            else if(camera.cameraType == CameraType.Preview)
            {
                previewRenderRequests.Add(renderRequest);
            }
            else if(camera.cameraType == CameraType.Reflection)
            {
                reflectionRenderRequests.Add(renderRequest);
            }
            else if(camera.cameraType == CameraType.Game)
            {
                gameViewRenderRequests.Add(renderRequest);
            }
        }


        public static HNRenderPipelineAsset Asset
        {
            get => GraphicsSettings.currentRenderPipeline as HNRenderPipelineAsset;
        }

        internal RenderGraph renderGraph = new RenderGraph("HNRP");

        public override RenderPipelineGlobalSettings defaultSettings => globalSettings;


        private HNRenderPipelineGlobalSettings globalSettings;

        //TODO: pool
        private List<RenderRequest> sceneViewRenderRequests;
        private List<RenderRequest> previewRenderRequests;
        private List<RenderRequest> reflectionRenderRequests;
        private List<RenderRequest> gameViewRenderRequests;

        private RenderingData renderingData = default;



        internal const int defaultRenderingLayerMask = 0x00000001;
    }

}
