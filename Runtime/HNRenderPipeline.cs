using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering;
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

        private FrameData frameData;


        public HNRenderPipeline(HNRenderPipelineAsset asset)
        {
            GraphicsSettings.lightsUseLinearIntensity = QualitySettings.activeColorSpace == ColorSpace.Linear;
            GraphicsSettings.lightsUseColorTemperature = true;
            GraphicsSettings.defaultRenderingLayerMask = defaultRenderingLayerMask;
            GraphicsSettings.useScriptableRenderPipelineBatching = asset.useSRPBatcher;
            renderRequests = new List<RenderRequest>();

            RTHandles.Initialize(Screen.width, Screen.height);

            // Blitter.Initialize(Shader.Find("Unlit/SingleBlitShader"), Shader.Find("Unlit/SingleBlitShader"));
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
            UpdateFrameData();

            PrepareRenderRequests(context, cameras);

            ExecuteRenderRequests();

            context.Submit();

            EndFrame();

            EndContextRendering(context, cameras);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Graphics.SetRenderTarget(null);
            Blitter.Cleanup();

            CleanupRenderGraph();
        }


        private void UpdateFrameData()
        {
            UpdateFrameCount();
        }

        private void UpdateFrameCount()
        {
            if (frameData.FrameCount != Time.frameCount)
            {
                frameData.FrameCount = Time.frameCount;
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

                CommandBuffer cmd = CommandBufferPool.Get($"RenderRequest_{camera.name}_cmd");
                RenderTargetIdentifier targetId = camera.targetTexture ?? new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
                if(camera.targetTexture != null)
                {
                    camera.targetTexture.IncrementUpdateCount();
                }

                GraphObjectData graphObjectData = new GraphObjectData
                {
                    Camera = camera,
                    GraphObject = graphObject,
                    Cmd = cmd,
                    TargetId = targetId
                };
                renderRequests.Add(new RenderRequest(context, renderGraph, frameData, graphObjectData));
            }
        }

        private void ExecuteRenderRequests()
        {
            RTHandles.SetReferenceSize(Screen.width, Screen.height);

            foreach(var request in renderRequests)
            {
                request.GraphObjectData.Cmd.ClearRenderTarget(true, true, Color.gray);
                request.RecordAndExecute();

                EndCameraRendering(request.Context, request.GraphObjectData.Camera);
                request.Context.ExecuteCommandBuffer(request.GraphObjectData.Cmd);
                CommandBufferPool.Release(request.GraphObjectData.Cmd);
                request.Context.Submit();
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


        internal const int defaultRenderingLayerMask = 0x00000001;
    }

}
