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
        public HNRenderPipeline(HNRenderPipelineAsset asset)
        {
            GraphicsSettings.lightsUseLinearIntensity = QualitySettings.activeColorSpace == ColorSpace.Linear;
            GraphicsSettings.lightsUseColorTemperature = true;
            GraphicsSettings.defaultRenderingLayerMask = defaultRenderingLayerMask;
            GraphicsSettings.useScriptableRenderPipelineBatching = true;
            renderRequests = new List<RenderRequest>();

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

            ExecuteRenderRequests();

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
            renderRequests.Clear();

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
                renderRequests.Add(new RenderRequest(context, renderGraph, ref renderingData));
            }
        }

        private void ExecuteRenderRequests()
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


        public static HNRenderPipelineAsset Asset
        {
            get => GraphicsSettings.currentRenderPipeline as HNRenderPipelineAsset;
        }

        internal RenderGraph renderGraph = new RenderGraph("HNRP");

        public override RenderPipelineGlobalSettings defaultSettings => globalSettings;


        private HNRenderPipelineGlobalSettings globalSettings;

        //TODO: pool
        private List<RenderRequest> renderRequests;

        private RenderingData renderingData = default;



        internal const int defaultRenderingLayerMask = 0x00000001;
    }

}
