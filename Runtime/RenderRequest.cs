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
        internal Camera camera;
        internal HNRenderGraph graphObject;

        private List<NodeParams> renderStack;
        private RenderGraph renderGraph;
        private int frameCount;
        private List<RenderPass> passes;


        public RenderRequest(ScriptableRenderContext context, Camera camera, HNRenderGraph graphObject, RenderGraph renderGraph, int frameCount)
        {
            this.context = context;
            this.camera = camera;
            this.graphObject = graphObject;
            renderStack = graphObject.RenderStack;
            this.renderGraph = renderGraph;
            this.frameCount = frameCount;
            passes = new List<RenderPass>();
        }

        public void SetupPasses(CommandBuffer cmd)
        {
            if(context == null || camera == null || graphObject == null)
                return;

            CleanPasses();
            foreach(var param in renderStack)
            {
                if(param == null)
                    continue;
                
                var pass = param.RenderPass;
                if(pass == null)
                    continue;
                
                passes.Add(pass);
                pass.Setup(cmd);
            }
        }

        public void RecordPasses()
        {
            Dictionary<string, TextureHandle> textureHandleDict = new Dictionary<string, TextureHandle>();

            foreach(var pass in passes)
            {
                pass.Record(renderGraph, textureHandleDict);
            }
            // Debug.Log("Record Transparency pass.");
            // using(var builder = renderGraph.AddRenderPass<TransparencyPassData>("Transparency Pass", out var passData))
            // {
            //     builder.SetRenderFunc(
            //         (TransparencyPassData data, RenderGraphContext ctx) =>
            //         {

            //         }
            //     );
            // }
        }

        private void CleanPasses()
        {
            passes.Clear();
        }
    }
}
