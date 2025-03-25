using System;
using System.Collections;
using System.Collections.Generic;
using Codice.CM.SEIDInfo;
using HN.Graph;
using HN.Serialize;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public class TransparencyPass
    {
        public static void Record(RenderGraph renderGraph, JsonData paramsData, TextureHandle inputTexture)
        {
            Debug.Log("Record Transparency pass.");

            TransparencyPassParams param = paramsData.Obj as TransparencyPassParams;
            Color defaultDrawColor = param.DefaultDrawColor;
            Material material = new Material(Shader.Find("Unlit/TestShader"));

            using(var builder = renderGraph.AddRenderPass<TransparencyPassData>("Transparency Pass", out var passData))
            {
                passData.defaultDrawColor = defaultDrawColor;
                passData.material = material;
                passData.inputTexture = builder.ReadTexture(inputTexture);
                TextureHandle output = renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, true, true)
                    {
                        colorFormat = GraphicsFormat.R8G8B8A8_UNorm,
                        clearBuffer = true,
                        clearColor = Color.black,
                        name = "TransparencyOutput"
                    }
                );
                
                builder.SetRenderFunc(
                    (TransparencyPassData data, RenderGraphContext ctx) =>
                    {
                        var materialPropertyBlock = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                        materialPropertyBlock.SetColor("_DefaultDrawColor", data.defaultDrawColor);

                        CoreUtils.DrawFullScreen(ctx.cmd, data.material, materialPropertyBlock);
                    }
                );
            }
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
    [NodeInfo("Transparency Pass", NodeInfo.NodeType.Renderer, "Pass/Transparency Pass")]
    public class TransparencyPassParams : NodeParams
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
        private Color defaultDrawColor = Color.blue;

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
#region TransparencyPass_{nodeIndex}
            TransparencyPass.Record(renderGraph, passParamsData[{nodeIndex}], {inputColorTarget.RefTextureName});
#endregion
";

            main += script;
        }
    }


}
