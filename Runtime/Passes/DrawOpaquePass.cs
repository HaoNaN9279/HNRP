using System;
using System.Collections;
using System.Collections.Generic;
using HN.Graph;
using PlasticPipe.PlasticProtocol.Messages;
using UnityEngine;

namespace HN.HNRP
{
    [Serializable]
    [HNRenderGraphNodeInfo("Draw Opaque Pass", HNRenderGraphNodeInfo.NodeType.Renderer, "Pass/Draw Opaque Pass")]
    public class DrawOpaquePass : RendererNode
    {
        [SerializeField][ColorInspector("Default Draw Color", false, false)]
        public Color DefaultDrawColor
        {
            get { return defaultDrawColor; }
            set { defaultDrawColor = value; }
        }


        [HNRenderGraphPortInfo("Test Input RT 0", HNRenderGraphPortInfo.Direction.Input, HNRenderGraphPortInfo.Capacity.Single)]
        public RenderTexture TestInputRT0 => testInputRT0;

        [HNRenderGraphPortInfo("Test Input RT 1", HNRenderGraphPortInfo.Direction.Input, HNRenderGraphPortInfo.Capacity.Single)]
        public RenderTexture TestInputRT1 => testInputRT1;

        [HNRenderGraphPortInfo("Test Output RT 0", HNRenderGraphPortInfo.Direction.Output, HNRenderGraphPortInfo.Capacity.Multi)]
        public RenderTexture TestOutputRT0 => testOutputRT0;


        [SerializeField]
        private Color defaultDrawColor = Color.cyan;

        [SerializeReference]
        private RenderTexture testInputRT0;

        [SerializeReference]
        private RenderTexture testInputRT1;

        [SerializeReference]
        private RenderTexture testOutputRT0;


        public override void Execute()
        {
            Debug.Log("Draw Opaque Pass");


        }

    }

}

