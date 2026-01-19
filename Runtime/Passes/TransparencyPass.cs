using System;
using System.Collections;
using System.Collections.Generic;
using Codice.CM.SEIDInfo;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace HN.HNRP
{
    [Serializable]
    public class TransparencyPass : PassBase
    {
        public override void Record(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<TransparencyPassData>($"{name}({PassName})", out var passData))
            {
                var textureHandles = renderingData.GraphData.textureHandles;
                passData.colorTarget = builder.UseColorBuffer(textureHandles[colorTargetIndex], 0);
                if (textureHandles[depthTargetIndex].IsValid())
                {
                    passData.depthTarget = builder.UseDepthBuffer(textureHandles[depthTargetIndex], DepthAccess.Read);
                }
                RendererListDesc rendererListDesc = HNRenderPipelineUtils.GetTransparentRendererListDesc(ShaderPassNames.AllForwardNames, renderingData.CullingResults, renderingData.Camera, renderingLayerMask);
                passData.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(rendererListDesc));
                builder.AllowRendererListCulling(false);

                builder.SetRenderFunc(
                    (TransparencyPassData data, RenderGraphContext ctx) =>
                    {
                        ctx.cmd.DrawRendererList(data.rendererList);
                    }
                );
            }
        }

        public override void Cleanup()
        {
            
        }


        [SerializeField]
        public uint renderingLayerMask = 0x00000001;

        [SerializeField]
        public int colorTargetIndex = -1;

        [SerializeField]
        public int depthTargetIndex = -1;

        public const string PassName = "Transparency Pass";
        

        public class TransparencyPassData : PassData
        {
            public TextureHandle colorTarget;
            public TextureHandle depthTarget;
            public RendererListHandle rendererList;

        }
    
    }

}
