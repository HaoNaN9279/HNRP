using System.Collections;
using System.Collections.Generic;
using HN.Graph;
using UnityEngine;

namespace HN.HNRP
{
    [HNRenderGraphNodeInfo("Draw Opaque Pass", HNRenderGraphNodeInfoAttribute.NodeType.Renderer, "Pass/Draw Opaque Pass")]
    public class DrawOpaquePassInfo : RendererNodeInfo
    {
        [HNRenderGraphPortInfo("Test Input RT 0", HNRenderGraphPortInfoAttribute.Direction.Input, HNRenderGraphPortInfoAttribute.Capacity.Single)]
        public RenderTexture TestInputRT0 => ((DrawOpaquePassParams)param).testInputRT0;

        [HNRenderGraphPortInfo("Test Input RT 1", HNRenderGraphPortInfoAttribute.Direction.Input, HNRenderGraphPortInfoAttribute.Capacity.Single)]
        public RenderTexture TestInputRT1 => ((DrawOpaquePassParams)param).testInputRT1;

        [HNRenderGraphPortInfo("Test Output RT 0", HNRenderGraphPortInfoAttribute.Direction.Output, HNRenderGraphPortInfoAttribute.Capacity.Multi)]
        public RenderTexture TestOutputRT0 => ((DrawOpaquePassParams)param).testOutputRT0;


        public DrawOpaquePassInfo()
        {
            param = ScriptableObject.CreateInstance<DrawOpaquePassParams>();
        }
    }


    public class DrawOpaquePassParams : RendererNodeParams
    {
        [SerializeField]
        public Color defaultDrawColor = Color.cyan;

        public RenderTexture testInputRT0;

        public RenderTexture testInputRT1;

        public RenderTexture testOutputRT0;


        public void OnEnable()
        {
            name = "Draw Opaque Pass";
        }

        public override void Execute()
        {
            Debug.Log("Draw Opaque Pass");


        }
    }

}

