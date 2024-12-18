using System.Collections;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public class CameraRenderer
    {
        private HNRenderPipelineAsset asset;
        private HNRenderPipelineGlobalSettings globalSettings;
        private ScriptableRenderContext context;
        private Camera camera;
        private HNRenderPipelineAdditionalCameraData cameraData;


        public CameraRenderer(HNRenderPipelineAsset asset, HNRenderPipelineGlobalSettings globalSettings)
        {
            this.asset = asset;
            this.globalSettings = globalSettings;
        }

        public void RenderCamera(ScriptableRenderContext context, Camera camera)
        {
            this.context = context;
            this.camera = camera;
            this.cameraData = camera.GetComponent<HNRenderPipelineAdditionalCameraData>();

            HNRenderGraph renderGraph = null;
            if(camera.cameraType == CameraType.Game)
                renderGraph = asset.runtimeRenderGraphViews.GetRenderGraph(cameraData.RenderGraphViewIndex);
            else
            {
                var renderGraphView = asset.editorRenderGraphViews;
                if(camera.cameraType == CameraType.SceneView)
                {
                    renderGraph = renderGraphView.GetRenderGraph("SceneView");
                }
                else if(camera.cameraType == CameraType.Preview)
                {
                    renderGraph = renderGraphView.GetRenderGraph("Preview");
                }
                else if(camera.cameraType == CameraType.Reflection)
                {
                    renderGraph = renderGraphView.GetRenderGraph("Reflection");
                }
            }
            
            if(renderGraph == null)
                return;
            Debug.Log(renderGraph.RenderStack[0]);
            foreach(var node in renderGraph.RenderStack)
            {
                Debug.Log(node);
            }
        }

        public void Dispose()
        {

        }


    }
}
