using System.Collections;
using System.Collections.Generic;
using HN.Graph;
using UnityEngine;

namespace HN.HNRP
{
    [HNRenderGraphNodeInfo("Draw Transparent Pass", HNRenderGraphNodeInfoAttribute.NodeType.Renderer, "Pass/Draw Transparent Pass")]
    public class DrawTransparentPassInfo : RendererNodeInfo
    {
        [HNRenderGraphPortInfo("Test Input RT 0", HNRenderGraphPortInfoAttribute.Direction.Input, HNRenderGraphPortInfoAttribute.Capacity.Single)]
        public RenderTexture TestInputRT0 => ((DrawTransparentPassParams)param).testInputRT0;

        [HNRenderGraphPortInfo("Test Input RT 1", HNRenderGraphPortInfoAttribute.Direction.Input, HNRenderGraphPortInfoAttribute.Capacity.Single)]
        public RenderTexture TestInputRT1 => ((DrawTransparentPassParams)param).testInputRT1;

        [HNRenderGraphPortInfo("Test Output RT 0", HNRenderGraphPortInfoAttribute.Direction.Output, HNRenderGraphPortInfoAttribute.Capacity.Multi)]
        public RenderTexture TestOutputRT0 => ((DrawTransparentPassParams)param).testOutputRT0;


        public DrawTransparentPassInfo()
        {
            param = ScriptableObject.CreateInstance<DrawTransparentPassParams>();
        }
    }


    public class DrawTransparentPassParams : RendererNodeParams
    {
        public RenderTexture testInputRT0;

        public RenderTexture testInputRT1;

        public RenderTexture testOutputRT0;


        public void OnEnable()
        {
            name = "Draw Transparent Pass";
        }


        public override void Execute()
        {
            Debug.Log("Draw Transparent Pass");
        }
    }
}
