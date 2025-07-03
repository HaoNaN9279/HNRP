using System;
using System.Collections;
using System.Collections.Generic;
using HN.Serialize;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public class RenderRequest
    {
        public ScriptableRenderContext Context => context;
        public GraphObjectData GraphObjectData => graphObjectData;

        private ScriptableRenderContext context;
        private RenderGraph renderGraph;
        private FrameData frameData;
        private GraphObjectData graphObjectData;


        public RenderRequest(
            ScriptableRenderContext context,
            RenderGraph renderGraph,
            FrameData frameData,
            GraphObjectData graphObjectData
            )
        {
            this.context = context;
            this.renderGraph = renderGraph;
            this.frameData = frameData;
            this.graphObjectData = graphObjectData;
        }

        public void RecordAndExecute()
        {
            RTHandles.SetReferenceSize(graphObjectData.Camera.pixelWidth, graphObjectData.Camera.pixelHeight);

            if (graphObjectData.Camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(graphObjectData.Camera);
            }

            context.SetupCameraProperties(graphObjectData.Camera);

            if (!TryCull())
            {
                Debug.LogError("Culling failed for camera: " + graphObjectData.Camera.name);
                return;
            }

            RecordPasses();

            // graphObjectData.Camera.targetTexture = null;
        }


        private void RecordPasses()
        {
            if (graphObjectData.GraphObject == null)
            {
                Debug.LogError("RenderGraph is null.");
                return;
            }

            graphObjectData.GraphObject.UpdateData(renderGraph, frameData, graphObjectData);

            using (renderGraph.RecordAndExecute(new RenderGraphParameters
            {
                executionName = "execution_" + graphObjectData.Camera.name,
                currentFrameIndex = frameData.FrameCount,
                rendererListCulling = true,
                scriptableRenderContext = context,
                commandBuffer = graphObjectData.Cmd
            }))
            {
                Debug.Log("Record And Execute: " + this);

                List<TextureHandle> textureHandles = new List<TextureHandle>();

                graphObjectData.GraphObject.RecordRenderGraph(textureHandles);
            }
        }

        private bool TryCull()
        {
            if (graphObjectData.Camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters))
            {
                frameData.CullingResults = context.Cull(ref cullingParameters);
                return true;
            }
            return false;
        }
        
    }
}
