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
    public class ForwardOpaquePass : PassBase
    {
        [SerializeField]
        public Material material;

        [SerializeField]
        public Color defaultDrawColor = Color.cyan;

        [SerializeField]
        public int colorTargetIndex = -1;


        [SerializeField]
        public RendererListHandle rendererList;


        public override void Record(RenderGraph renderGraph, FrameData frameData, GraphObjectData graphObjectData, List<TextureHandle> textureHandles)
        {
            Debug.Log("Record Forward Opaque pass.");
            
            material = material ?? new Material(Shader.Find("Unlit/TestShader"));

            using (var builder = renderGraph.AddRenderPass<ForwardOpaquePassData>("Forward Opaque Pass", out var passData))
            {
                passData.material = material;
                passData.defaultDrawColor = defaultDrawColor;

                passData.colorTarget = builder.WriteTexture(textureHandles[colorTargetIndex]);
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


    public class ForwardOpaquePassData : PassData
    {
        public Material material;
        public Color defaultDrawColor;
        public TextureHandle colorTarget;
        public RendererListHandle rendererList;

    }

}

