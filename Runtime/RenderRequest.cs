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

        private System.Type classType;
        private System.Reflection.MethodInfo method;


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
                return;
            }

            if(classType == null)
            {
                classType = Type.GetType("HN.HNRP.Generated." + graphObject.ScriptName);
            }
            if(classType == null)
            {
                Debug.LogWarning($"class {graphObject.ScriptName} not found.");
                return;
            }

            if(method == null)
            {
                method = classType.GetMethod(
                    graphObject.MethodName, 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
                    );

                using(renderGraph.RecordAndExecute(new RenderGraphParameters
                {
                    executionName = "test",
                    currentFrameIndex = frameCount,
                    rendererListCulling = true,
                    scriptableRenderContext = context,
                    commandBuffer = cmd
                }))
                {
                    method.Invoke(null, new object[]{renderGraph, passParamsData, targetId});
                }

            }
        }

    }
}
