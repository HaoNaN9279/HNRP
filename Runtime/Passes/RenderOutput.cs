using System;
using System.Collections;
using System.Collections.Generic;
using HN.Graph;
using HN.Serialize;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public class RenderOutput : RenderPass
    {
        private RenderOutputParams param => nodeParams as RenderOutputParams;

        private CommandBuffer cmd;


        public RenderOutput()
        {

        }

        public override void Initialize(NodeParams nodeParams)
        {
            base.Initialize(nodeParams);
        }

        public override void Setup(CommandBuffer cmd)
        {
            this.cmd = cmd;
        }

        public override void Record(RenderGraph renderGraph, Dictionary<string, TextureHandle> textureHandleDict)
        {
            // Debug.Log("Render Output");
            using(var builder = renderGraph.AddRenderPass<RenderOutputData>("Render Output", out var passData))
            {
                builder.SetRenderFunc(
                    (RenderOutputData data, RenderGraphContext ctx) =>
                    {
                        Debug.Log("output:" + param.InputColorTarget.RefTextureName);
                        cmd.SetRenderTarget(textureHandleDict[param.InputColorTarget.RefTextureName]);
                    }
                );
            }
        }

        public override void Dispose()
        {
            
        }
    }


    public class RenderOutputData : RenderPassData
    {

    }


    [Serializable]
    [NodeInfo("Render Output", "_RenderOutput", NodeInfo.NodeType.Output, "Output/Render Output")]
    public class RenderOutputParams : NodeParams
    {
        [PortInfo("Color Target", "_InputColorTarget", PortInfo.Direction.Input, PortInfo.Capacity.Single)]
        public TexturePort InputColorTarget
        {
            get => inputColorTarget;
            set => inputColorTarget = value;
        }

        [SerializeField]
        private TexturePort inputColorTarget;

        protected override RenderPass GetRenderPass()
        {
            RenderOutput pass = new RenderOutput();
            pass.Initialize(this);
            return pass;
        }
    }


}

