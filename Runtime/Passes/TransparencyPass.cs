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
    [Serializable]
    [NodeInfo("Transparency Pass", NodeInfo.NodeType.Renderer, "Pass/Transparency Pass")]
    public class TransparencyPass : Pass
    {
        [SerializeField]
        public Material material;

        [SerializeField]
        [ColorInspector("Default Draw Color", false, false)]
        public Color defaultDrawColor = Color.blue;

        [SerializeField]
        [PortInputInfo("Color Target", PortInputInfo.Capacity.Single)]
        public int inputColorTargetIndex = -1;

        [SerializeField]
        [PortOutputInfo("Color Target", PortOutputInfo.Capacity.Multi)]
        public int outputColorTargetIndex = -1;


        public override void Setup(HNRenderGraph renderGraph)
        {
            Debug.Log("Transparency pass Setup.");

            outputColorTargetIndex = inputColorTargetIndex;
            material = material ?? new Material(Shader.Find("Unlit/TestShader"));
        }

        public override void Record(RenderGraph renderGraph, FrameData frameData, GraphObjectData graphObjectData, List<TextureHandle> textureHandles)
        {
            Debug.Log("Record Transparency pass.");

            using (var builder = renderGraph.AddRenderPass<TransparencyPassData>("Transparency Pass", out var passData))
            {
                passData.material = material;
                passData.defaultDrawColor = defaultDrawColor;
                passData.colorTarget = builder.WriteTexture(textureHandles[outputColorTargetIndex]);

                builder.SetRenderFunc(
                    (TransparencyPassData data, RenderGraphContext ctx) =>
                    {
                        CoreUtils.SetRenderTarget(ctx.cmd, data.colorTarget);
                        
                        var materialPropertyBlock = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                        materialPropertyBlock.SetColor("_DefaultDrawColor", data.defaultDrawColor);

                        CoreUtils.DrawFullScreen(ctx.cmd, data.material, materialPropertyBlock);
                    }
                );
            }
        }

    }


    public class TransparencyPassData : PassData
    {
        public Material material = new Material(Shader.Find("Unlit/TestShader"));
        public Color defaultDrawColor = Color.blue;
        public TextureHandle colorTarget;

    }

}
