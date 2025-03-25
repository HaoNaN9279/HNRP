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
        internal Camera camera;
        internal HNRenderGraph graphObject;

        private List<JsonData> passParamsData;
        private RenderGraph renderGraph;
        private int frameCount;

        private System.Type classType;
        private System.Reflection.MethodInfo method;


        public RenderRequest(ScriptableRenderContext context, Camera camera, HNRenderGraph graphObject, RenderGraph renderGraph, int frameCount)
        {
            this.context = context;
            this.camera = camera;
            this.graphObject = graphObject;
            this.passParamsData = graphObject.PassParamsData;
            this.renderGraph = renderGraph;
            this.frameCount = frameCount;
        }

        public void RecordPasses()
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

                method.Invoke(null, new object[]{renderGraph, passParamsData});
            }
        }

    }
}
