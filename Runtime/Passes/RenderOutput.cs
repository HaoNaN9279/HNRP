using System.Collections;
using System.Collections.Generic;
using HN.Graph;
using UnityEngine;

namespace HN.HNRP
{
    [HNRenderGraphNodeInfo("Render Output", HNRenderGraphNodeInfoAttribute.NodeType.Output, "Output/Render Output")]
    public class RenderOutputInfo : OutputNodeInfo
    {
        [HNRenderGraphPortInfo("Test Input RT 0", HNRenderGraphPortInfoAttribute.Direction.Input, HNRenderGraphPortInfoAttribute.Capacity.Single)]
        public RenderTexture TestInputRT0 => ((RenderOutputParams)param).testInputRT0;


        public RenderOutputInfo()
        {
            param = ScriptableObject.CreateInstance<RenderOutputParams>();
        }
    }


    public class RenderOutputParams : OutputNodeParams
    {
        public RenderTexture testInputRT0;


        public void OnEnable()
        {
            name = "Output";
        }
    }
}

