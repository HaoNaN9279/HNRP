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
    public class RenderOutput
    {
        public static void Record(RenderGraph renderGraph, TextureHandle inputTexture)
        {
            Debug.Log("Render Output");

            Material singleBlitMat = new Material(Shader.Find("Unlit/TestShader"));
            
            using(var builder = renderGraph.AddRenderPass<RenderOutputData>("Render Output", out var passData))
            {
                passData.inputTexture = builder.ReadTexture(inputTexture);
                passData.singleBlitMat = singleBlitMat;
                builder.SetRenderFunc(
                    (RenderOutputData data, RenderGraphContext ctx) =>
                    {
                        CoreUtils.DrawFullScreen(ctx.cmd, data.singleBlitMat);
                    }
                );
            }
        }
    }


    public class RenderOutputData : RenderPassData
    {
        public TextureHandle inputTexture;
        public Material singleBlitMat;
    }


    [Serializable]
    [NodeInfo("Render Output", NodeInfo.NodeType.Output, "Output/Render Output")]
    public class RenderOutputParams : NodeParams
    {
        [PortInfo("Color Target", PortInfo.Direction.Input, PortInfo.Capacity.Single)]
        public TexturePort InputColorTarget
        {
            get => inputColorTarget;
            set => inputColorTarget = value;
        }

        [SerializeField]
        private TexturePort inputColorTarget;


        public override void SetupOutput(int nodeIndex)
        {
        }

        public override void AppendScript(ref string main, int nodeIndex)
        {
            string script =
$@"
#region RenderOutput_{nodeIndex}
            RenderOutput.Record(renderGraph, {inputColorTarget.RefTextureName});
#endregion
";

            main += script;
        }
    }


}

