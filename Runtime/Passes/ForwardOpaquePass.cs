using System;
using System.Collections;
using System.Collections.Generic;
using HN.Serialize;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace HN.HNRP
{
    public class ForwardOpaquePass
    {
        public static TextureHandle Record(RenderGraph renderGraph, JsonData paramsData)
        {
            Debug.Log("Record Forward Opaque pass.");

            using(var builder = renderGraph.AddRenderPass<ForwardOpaquePassData>("Forward Opaque Pass", out var passData))
            {
                ForwardOpaquePassParams param = paramsData.Obj as ForwardOpaquePassParams;
                passData.defaultDrawColor = param.DefaultDrawColor;
                passData.material = new Material(Shader.Find("Unlit/TestShader"));
                TextureHandle colorTarget = renderGraph.CreateTexture(new TextureDesc(Vector2.one, false, false)
                {
                    colorFormat = GraphicsFormat.R8G8B8A8_UNorm, clearBuffer = true, clearColor = Color.white, name = "ColorTarget123"
                });
                passData.colorTarget = builder.WriteTexture(colorTarget);

                builder.SetRenderFunc(
                    (ForwardOpaquePassData data, RenderGraphContext ctx) =>
                    {
                        CoreUtils.SetRenderTarget(ctx.cmd, data.colorTarget);
                        
                        var materialPropertyBlock = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                        materialPropertyBlock.SetColor("_DefaultDrawColor", data.defaultDrawColor);

                        CoreUtils.DrawFullScreen(ctx.cmd, data.material, materialPropertyBlock);
                    }
                );

                return colorTarget;
            }
        }

    }


    public class ForwardOpaquePassData : RenderPassData
    {
        public Material material;
        public Color defaultDrawColor;
        public TextureHandle colorTarget;
        public RendererListHandle rendererList;
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
        public string InputColorTarget
        {
            get { return inputColorTarget; }
            set { inputColorTarget = value; }
        }


        [PortInfo("Color Target", PortInfo.Direction.Output, PortInfo.Capacity.Multi)]
        public string OutputColorTarget
        {
            get { return outputColorTarget; }
            set { outputColorTarget = value; }
        }


        [SerializeField]
        private Color defaultDrawColor = Color.cyan;
        
        [SerializeField]
        private string inputColorTarget;
        
        [SerializeField]
        private string outputColorTarget;


        public override void SetupOutput(int nodeIndex)
        {
            outputColorTarget = $"_ForwardOpaquePassParams_{nodeIndex}_ColorTarget";
        }

        public override void AppendScript(ref string main, int nodeIndex)
        {
            string script =
$@"
#region ForwardOpaquePass_{nodeIndex}
            TextureHandle _ForwardOpaquePassParams_{nodeIndex}_ColorTarget = ForwardOpaquePass.Record(renderGraph, passParamsData[{nodeIndex}]);
#endregion
";

            main += script;
        }
    }

}

