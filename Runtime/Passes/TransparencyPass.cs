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
        [SerializeField]
        public uint renderingLayerMask = 0x00000001;

        [SerializeField]
        public int colorTargetIndex = -1;

        [SerializeField]
        public int depthTargetIndex = -1;
        
        public RendererListHandle rendererList;


        public override void Record(RenderGraph renderGraph, FrameData frameData, GraphObjectData graphObjectData, List<TextureHandle> textureHandles)
        {
            Debug.Log("Record Transparency pass.");
            using (var builder = renderGraph.AddRenderPass<TransparencyPassData>("Transparency Pass", out var passData))
            {
                passData.colorTarget = builder.UseColorBuffer(textureHandles[colorTargetIndex], 0);
                if (textureHandles[depthTargetIndex].IsValid())
                {
                    passData.depthTarget = builder.UseDepthBuffer(textureHandles[depthTargetIndex], DepthAccess.Read);
                }
                RendererListDesc rendererListDesc = HNRenderPipelineUtils.GetTransparentRendererListDesc(ShaderPassNames.AllForwardNames, frameData.CullingResults, graphObjectData.Camera, renderingLayerMask);
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


        public class TransparencyPassData : PassData
        {
            public TextureHandle colorTarget;
            public TextureHandle depthTarget;
            public RendererListHandle rendererList;

        }
    }

}
