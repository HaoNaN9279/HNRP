using System.Collections;
using System.Collections.Generic;
using HN.Graph;
using UnityEngine;

namespace HN.HNRP
{
    [HNRenderGraphNodeInfo("Render Output", HNRenderGraphNodeInfoAttribute.NodeType.Output, "Output/Render Output")]
    public class RenderOutput : OutputNode
    {
        [HNRenderGraphPortInfo("Test Input RT 0", HNRenderGraphPortInfoAttribute.Direction.Input, HNRenderGraphPortInfoAttribute.Capacity.Single)]
        public RenderTexture TestInputRT0 => testInputRT0;


        private RenderTexture testInputRT0;


    }
}

