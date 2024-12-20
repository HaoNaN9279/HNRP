using System.Collections;
using System.Collections.Generic;
using HN.Graph;
using UnityEngine;

namespace HN.HNRP
{
    [SerializeField]
    [HNRenderGraphNodeInfo("Draw Transparent Pass", HNRenderGraphNodeInfoAttribute.NodeType.Renderer, "Pass/Draw Transparent Pass")]
    public class DrawTransparentPass : RendererNode
    {
        [HNRenderGraphPortInfo("Test Input RT 0", HNRenderGraphPortInfoAttribute.Direction.Input, HNRenderGraphPortInfoAttribute.Capacity.Single)]
        public RenderTexture TestInputRT0 => testInputRT0;

        [HNRenderGraphPortInfo("Test Input RT 1", HNRenderGraphPortInfoAttribute.Direction.Input, HNRenderGraphPortInfoAttribute.Capacity.Single)]
        public RenderTexture TestInputRT1 => testInputRT1;

        [HNRenderGraphPortInfo("Test Output RT 0", HNRenderGraphPortInfoAttribute.Direction.Output, HNRenderGraphPortInfoAttribute.Capacity.Multi)]
        public RenderTexture TestOutputRT0 => testOutputRT0;


        private RenderTexture testInputRT0;

        private RenderTexture testInputRT1;

        private RenderTexture testOutputRT0;



        public override void Execute()
        {
            Debug.Log("Draw Transparent Pass");
        }
    }
}
