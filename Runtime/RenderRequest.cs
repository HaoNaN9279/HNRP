using System;
using System.Collections;
using System.Collections.Generic;
using HN.Serialize;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public class RenderRequest
    {
        internal ScriptableRenderContext context;
        internal CommandBuffer cmd;
        internal Camera camera;
        internal HNRenderGraph graphObject;

        private List<JsonData> passParamsData;
        private RenderGraph renderGraph;
        public RenderTargetIdentifier targetId;
        private int frameCount;


        public RenderRequest(
            ScriptableRenderContext context, 
            CommandBuffer cmd,
            Camera camera, 
            HNRenderGraph graphObject, 
            RenderGraph renderGraph, 
            RenderTargetIdentifier targetId,
            int frameCount
            )
        {
            this.context = context;
            this.cmd = cmd;
            this.camera = camera;
            this.graphObject = graphObject;
            this.passParamsData = graphObject.PassParamsData;
            this.renderGraph = renderGraph;
            this.targetId = targetId;
            this.frameCount = frameCount;
        }

        public void RecordAndExecute()
        {
            if(camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }

            context.SetupCameraProperties(camera);

            RecordPasses();

            camera.targetTexture = null;
        }


        private void RecordPasses()
        {
            if(graphObject == null)
            {
                Debug.LogError("RenderGraph is null.");
                return;
            }

            if(graphObject.Target == null)
            {
                Debug.LogError("RenderGraph.Target is null.");
                return;
            }

            graphObject.Target.Initialize(renderGraph, passParamsData, camera, targetId, frameCount);

            using(renderGraph.RecordAndExecute(new RenderGraphParameters
            {
                executionName = "execution_" + camera.name,
                currentFrameIndex = frameCount,
                rendererListCulling = true,
                scriptableRenderContext = context,
                commandBuffer = cmd
            }))
            {
                graphObject.Target.Execute();
            }
        }

    }
}
