using System;
using System.Collections;
using System.Collections.Generic;
using HN.Graph;
using UnityEngine;

namespace HN.HNRP
{
    [Serializable]
    [HNRenderGraphNodeInfo("Draw Opaque Pass", HNRenderGraphNodeInfoAttribute.NodeType.Renderer, "Pass/Draw Opaque Pass")]
    public class DrawOpaquePass : RendererNode
    {
        [SerializeField]
        public Color defaultDrawColor = Color.cyan;


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
            Debug.Log("Draw Opaque Pass");


        }
    }

}

