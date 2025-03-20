using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public class HNRenderPipeline : RenderPipeline
    {
        public static HNRenderPipelineAsset Asset
        {
            get => GraphicsSettings.currentRenderPipeline as HNRenderPipelineAsset;
        }

        internal RenderGraph renderGraph = new RenderGraph("HNRP");

        public override RenderPipelineGlobalSettings defaultSettings => globalSettings;


        private HNRenderPipelineGlobalSettings globalSettings;

        //TODO: pool
        private List<RenderRequest> renderRequests;

        private int frameCount;


        public HNRenderPipeline(HNRenderPipelineAsset asset)
        {
            GraphicsSettings.lightsUseLinearIntensity = QualitySettings.activeColorSpace == ColorSpace.Linear;
            GraphicsSettings.lightsUseColorTemperature = true;
            GraphicsSettings.defaultRenderingLayerMask = defaultRenderingLayerMask;
            GraphicsSettings.useScriptableRenderPipelineBatching = asset.useSRPBatcher;
            renderRequests = new List<RenderRequest>();
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

            ExecuteRenderRequests(context);

            EndFrame();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CleanupRenderGraph();
        }


        private void UpdateFrameCount()
        {
            if(frameCount != Time.frameCount)
            {
                frameCount = Time.frameCount;
            }
        }

        private void PrepareRenderRequests(ScriptableRenderContext context, List<Camera> cameras)
        {
            renderRequests.Clear();

            foreach (Camera camera in cameras)
            {
                var cameraData = camera.GetHNRPAdditionalCameraData();

                HNRenderGraph graphObject = null;
                if(camera.cameraType == CameraType.Game)
                {
                    graphObject = Asset.runtimeRenderGraphViews.GetRenderGraphObject(cameraData.RenderGraphViewIndex);
                }
                else
                {
                    var renderGraphView = Asset.editorRenderGraphViews;
                    if(camera.cameraType == CameraType.SceneView)
                    {
                        graphObject = renderGraphView.GetRenderGraphObject("SceneView");
                    }
                    else if(camera.cameraType == CameraType.Preview)
                    {
                        graphObject = renderGraphView.GetRenderGraphObject("Preview");
                    }
                    else if(camera.cameraType == CameraType.Reflection)
                    {
                        graphObject = renderGraphView.GetRenderGraphObject("Reflection");
                    }
                }
                
                if(graphObject == null)
                    return;

                renderRequests.Add(new RenderRequest(context, camera, graphObject, renderGraph, frameCount));
            }
        }

        private void ExecuteRenderRequests(ScriptableRenderContext context)
        {
            foreach(var request in renderRequests)
            {
                var cmd = CommandBufferPool.Get($"{request.camera.name}.cmd");
                request.SetupPasses(cmd);
            }
            
            using(renderGraph.RecordAndExecute(new RenderGraphParameters
            {
                executionName = "test",
                currentFrameIndex = frameCount,
                rendererListCulling = true,
                scriptableRenderContext = context,
                commandBuffer = CommandBufferPool.Get("test.cmd")
            }))
            {
                foreach(var request in renderRequests)
                {
                    request.RecordPasses();
                }
            }
            
        }

        private void EndFrame()
        {
            renderGraph.EndFrame();
        }


        void CleanupRenderGraph()
        {
            renderGraph.Cleanup();
            renderGraph = null;
        }


        internal const int defaultRenderingLayerMask = 0x00000001;
    }
}
