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
    [Serializable]
    [NodeInfo("Forward Opaque Pass", NodeInfo.NodeType.Renderer, "Pass/Forward Opaque Pass")]
    public class ForwardOpaquePass : Pass
    {
        [SerializeField]
        public Material material;

        [SerializeField]
        [ColorInspector("Default Draw Color", false, false)]
        public Color defaultDrawColor = Color.cyan;

        [SerializeField]
        [PortInputInfo("Color Target", PortInputInfo.Capacity.Single)]
        public int inputColorTargetIndex = -1;

        [SerializeField]
        [PortOutputInfo("Color Target", PortOutputInfo.Capacity.Multi)]
        public int outputColorTargetIndex = -1;

        [SerializeField]
        public RendererListHandle rendererList;


        public override void Setup(HNRenderGraph renderGraph)
        {
            Debug.Log("Forward Opaque pass Setup.");

            outputColorTargetIndex = inputColorTargetIndex;
            material = material ?? new Material(Shader.Find("Unlit/TestShader"));
        }

        public override void Record(RenderGraph renderGraph, FrameData frameData, GraphObjectData graphObjectData, List<TextureHandle> textureHandles)
        {
            Debug.Log("Record Forward Opaque pass.");

            using (var builder = renderGraph.AddRenderPass<ForwardOpaquePassData>("Forward Opaque Pass", out var passData))
            {
                passData.material = material;
                passData.defaultDrawColor = defaultDrawColor;

                passData.colorTarget = builder.WriteTexture(textureHandles[outputColorTargetIndex]);
                builder.SetRenderFunc(
                    (ForwardOpaquePassData data, RenderGraphContext ctx) =>
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


    public class ForwardOpaquePassData
    {
        public Material material;
        public Color defaultDrawColor;
        public TextureHandle colorTarget;
        public RendererListHandle rendererList;

    }

}

