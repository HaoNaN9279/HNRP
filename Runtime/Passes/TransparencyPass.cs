using System;
using System.Collections;
using System.Collections.Generic;
using Codice.CM.SEIDInfo;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    [Serializable]
    public class TransparencyPass : PassBase
    {
        [SerializeField]
        public Color defaultDrawColor = Color.blue;

        public int colorTargetIndex = -1;


        public override void Record(RenderGraph renderGraph, FrameData frameData, GraphObjectData graphObjectData, List<TextureHandle> textureHandles)
        {
            Debug.Log("Record Transparency pass.");
            
            Material material = new Material(Shader.Find("Unlit/TestShader"));

            using (var builder = renderGraph.AddRenderPass<TransparencyPassData>("Transparency Pass", out var passData))
            {
                passData.material = material;
                passData.defaultDrawColor = defaultDrawColor;
                passData.colorTarget = builder.WriteTexture(textureHandles[colorTargetIndex]);

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
        public Material material;
        public Color defaultDrawColor;
        public TextureHandle colorTarget;

    }

}
