using System;
using System.Collections;
using System.Collections.Generic;
using Codice.CM.SEIDInfo;
using HN.Graph;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public class TransparencyPass : RenderPass
    {
        private TransparencyPassParams param => nodeParams as TransparencyPassParams;

        private CommandBuffer cmd;
        private Material material;


        public TransparencyPass()
        {

        }

        public override void Initialize(NodeParams nodeParams)
        {
            base.Initialize(nodeParams);
        }

        public override void Setup(CommandBuffer cmd)
        {
            this.cmd = cmd;
            material = new Material(Shader.Find("Unlit/TestShader"));
        }

        public override void Record(RenderGraph renderGraph, Dictionary<string, TextureHandle> textureHandleDict)
        {
            Debug.Log("Record Transparency pass.");
            using(var builder = renderGraph.AddRenderPass<TransparencyPassData>("Transparency Pass", out var passData))
            {
                builder.SetRenderFunc(
                    (TransparencyPassData data, RenderGraphContext ctx) =>
                    {

                    }
                );
            }
        }

        public override void Dispose()
        {
            nodeParams = null;
            material = null;
        }
    }


    public class TransparencyPassData : RenderPassData
    {
        public Material material;
        public Color defaultDrawColor;
        public TextureHandle inputTexture;
        public TextureHandle outputTexture;
    }


    [Serializable]
    [NodeInfo("Transparency Pass", "_TransparencyPass", NodeInfo.NodeType.Renderer, "Pass/Transparency Pass")]
    public class TransparencyPassParams : NodeParams
    {
        [ColorInspector("Default Draw Color", false, false)]
        public Color DefaultDrawColor
        {
            get { return defaultDrawColor; }
            set { defaultDrawColor = value; }
        }


        [PortInfo("Color Target", "_InputColorTarget", PortInfo.Direction.Input, PortInfo.Capacity.Single)]
        public TexturePort InputColorTarget
        {
            get { return inputColorTarget; }
            set { inputColorTarget = value; }
        }

        [PortInfo("Color Target", "_OutputColorTarget", PortInfo.Direction.Output, PortInfo.Capacity.Multi)]
        public TexturePort OutputColorTarget
        {
            get { return outputColorTarget; }
            set { outputColorTarget = value; }
        }



        [SerializeField]
        private Color defaultDrawColor = Color.blue;

        [SerializeField]
        private TexturePort inputColorTarget;

        [SerializeField]
        private TexturePort outputColorTarget;


        protected override RenderPass GetRenderPass()
        {
            TransparencyPass pass = new TransparencyPass();
            pass.Initialize(this);
            return pass;
        }
    }


}
