using System;
using System.Collections;
using System.Collections.Generic;
using HN.Serialize;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public class ForwardOpaquePass : RenderPass
    {
        private ForwardOpaquePassParams param => nodeParams as ForwardOpaquePassParams;

        private CommandBuffer cmd;
        private Material material;


        public ForwardOpaquePass()
        {

        }

        public override void Initialize(NodeParams nodeParams)
        {
            base.Initialize(nodeParams);

            // outputTexture.RefTexturePortName = param.InputColorTarget.Name;
            // this.inputTexture = inputTexture;
            // this.outputTexture = outputTexture;
        }

        public override void Setup(CommandBuffer cmd)
        {
            this.cmd = cmd;
            material = new Material(Shader.Find("Unlit/TestShader"));
        }

        public override void Record(RenderGraph renderGraph, Dictionary<string, TextureHandle> textureHandleDict)
        {
            Debug.Log("Record Forward Opaque pass.");
            using(var builder = renderGraph.AddRenderPass<ForwardOpaquePassData>("Forward Opaque Pass", out var passData))
            {
                passData.defaultDrawColor = param.DefaultDrawColor;
                passData.material = material;

                TextureHandle output = renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, true, true)
                    {
                        colorFormat = GraphicsFormat.R8G8B8A8_UNorm,
                        clearBuffer = true,
                        clearColor = Color.black,
                        name = param.OutputColorTarget.Name
                    }
                );
                
                Debug.Log("forward opaque:" + param.OutputColorTarget.Name);
                textureHandleDict[param.OutputColorTarget.Name] = output;
                passData.outputTexture = builder.UseColorBuffer(output, 0);

                builder.SetRenderFunc(
                    (ForwardOpaquePassData data, RenderGraphContext ctx) =>
                    {
                        var materialPropertyBlock = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                        materialPropertyBlock.SetColor("_DefaultDrawColor", data.defaultDrawColor);

                        CoreUtils.DrawFullScreen(ctx.cmd, data.material, materialPropertyBlock);
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


    public class ForwardOpaquePassData : RenderPassData
    {
        public Material material;
        public Color defaultDrawColor;
        public TextureHandle inputTexture;
        public TextureHandle outputTexture;
    }


    [Serializable]
    [NodeInfo("Forward Opaque Pass", "_ForwardOpaquePass", NodeInfo.NodeType.Renderer, "Pass/Forward Opaque Pass")]
    public class ForwardOpaquePassParams : NodeParams
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
        private Color defaultDrawColor = Color.cyan;
        
        [SerializeField]
        private TexturePort inputColorTarget;
        
        [SerializeField]
        private TexturePort outputColorTarget;


        protected override RenderPass GetRenderPass()
        {
            ForwardOpaquePass pass = new ForwardOpaquePass();
            pass.Initialize(this);
            return pass;
        }
    }

}

