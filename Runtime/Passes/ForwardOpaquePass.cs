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
    public class ForwardOpaquePass
    {
        public static void Record(RenderGraph renderGraph, JsonData paramsData, TextureHandle inputTexture)
        {
            Debug.Log("Record Forward Opaque pass.");

            ForwardOpaquePassParams param = paramsData.Obj as ForwardOpaquePassParams;
            Color defaultDrawColor = param.DefaultDrawColor;
            Material material = new Material(Shader.Find("Unlit/TestShader"));

            using(var builder = renderGraph.AddRenderPass<ForwardOpaquePassData>("Forward Opaque Pass", out var passData))
            {
                passData.defaultDrawColor = defaultDrawColor;
                passData.material = material;
                passData.inputTexture = builder.ReadTexture(inputTexture);
                passData.outputTexture = builder.UseColorBuffer(inputTexture, 0);

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

    }


    public class ForwardOpaquePassData : RenderPassData
    {
        public Material material;
        public Color defaultDrawColor;
        public TextureHandle inputTexture;
        public TextureHandle outputTexture;
    }


    [Serializable]
    [NodeInfo("Forward Opaque Pass", NodeInfo.NodeType.Renderer, "Pass/Forward Opaque Pass")]
    public class ForwardOpaquePassParams : NodeParams
    {
        [ColorInspector("Default Draw Color", false, false)]
        public Color DefaultDrawColor
        {
            get { return defaultDrawColor; }
            set { defaultDrawColor = value; }
        }


        [PortInfo("Color Target", PortInfo.Direction.Input, PortInfo.Capacity.Single)]
        public TexturePort InputColorTarget
        {
            get { return inputColorTarget; }
            set { inputColorTarget = value; }
        }


        [PortInfo("Color Target", PortInfo.Direction.Output, PortInfo.Capacity.Multi)]
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


        public override void SetupOutput(int nodeIndex)
        {
            outputColorTarget = new TexturePort(inputColorTarget.RefTextureName);
        }

        public override void AppendScript(ref string main, int nodeIndex)
        {
            string script =
$@"
#region ForwardOpaquePass_{nodeIndex}
            ForwardOpaquePass.Record(renderGraph, passParamsData[{nodeIndex}], {inputColorTarget.RefTextureName});
#endregion
";

            main += script;
        }
    }

}

