using System.Collections;
using System.Collections.Generic;
using HN.Graph;
using UnityEngine;

namespace HN.HNRP
{
    [HNRenderGraphNodeInfo("Render Output", HNRenderGraphNodeInfo.NodeType.Output, "Output/Render Output")]
    public class RenderOutput : OutputNode
    {
        [HNRenderGraphPortInfo("Test Input RT 0", HNRenderGraphPortInfo.Direction.Input, HNRenderGraphPortInfo.Capacity.Single)]
        public RenderTexture TestInputRT0 => testInputRT0;


        private RenderTexture testInputRT0;


    }
}

