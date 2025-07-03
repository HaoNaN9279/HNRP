using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace HN.HNRP
{
    [Serializable]
    public class BuiltinSkyPass : PassBase
    {
        public int colorTargetIndex = -1;
        public int depthTargetIndex = -1;
        public RendererListHandle rendererList;


        public override void Record(RenderGraph renderGraph, FrameData frameData, GraphObjectData graphObjectData, List<TextureHandle> textureHandles)
        {
            Debug.Log("Builtin Sky Pass.");

            using (var builder = renderGraph.AddRenderPass<BuiltinSkyPassData>("Builtin Sky Pass", out var passData))
            {
                passData.colorTarget = builder.UseColorBuffer(textureHandles[colorTargetIndex], 0);
                if (textureHandles[depthTargetIndex].IsValid())
                {
                    passData.depthTarget = builder.UseDepthBuffer(textureHandles[depthTargetIndex], DepthAccess.Read);
                }
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(
                    (BuiltinSkyPassData data, RenderGraphContext ctx) =>
                    {
                        RendererList rendererList = ctx.renderContext.CreateSkyboxRendererList(graphObjectData.Camera);
                        ctx.cmd.DrawRendererList(rendererList);
                    }
                );
            }
        }


        public class BuiltinSkyPassData : PassData
        {
        public TextureHandle colorTarget;
        public TextureHandle depthTarget;
        public RendererListHandle rendererList;
        }
    }
}
