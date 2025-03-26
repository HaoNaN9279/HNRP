using System;
using System.Collections;
using System.Collections.Generic;
using HN.Graph;
using HN.Serialize;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public class RenderOutput
    {
        public static void Record(RenderGraph renderGraph, TextureHandle inputTexture, TextureHandle backBuffer)
        {
            Debug.Log("Render Output");
            
            using(var builder = renderGraph.AddRenderPass<RenderOutputData>("Render Output", out var passData))
            {
                Debug.Log("Render Output 1");
                builder.AllowPassCulling(false);
                passData.singleBlitMat = new Material(Shader.Find("Unlit/SingleBlitShader"));
                passData.inputTexture = builder.ReadTexture(inputTexture);
                passData.renderTarget = builder.WriteTexture(backBuffer);
                builder.SetRenderFunc(
                    (RenderOutputData data, RenderGraphContext ctx) =>
                    {
                        Debug.Log("Render Output 2");
                        ctx.cmd.SetRenderTarget(backBuffer);

                        var materialPropertyBlock = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                        materialPropertyBlock.SetTexture("_MainTex", data.inputTexture);
                        CoreUtils.DrawFullScreen(ctx.cmd, data.singleBlitMat, materialPropertyBlock);
                    }
                );
            }
        }
    }


    public class RenderOutputData : RenderPassData
    {
        public TextureHandle inputTexture;
        public TextureHandle renderTarget;
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
            RenderOutput.Record(renderGraph, {inputColorTarget.RefTextureName}, backBuffer);
#endregion
";

            main += script;
        }
    }


}

