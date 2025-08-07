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
        [SerializeField]
        public int colorTargetIndex = -1;

        [SerializeField]
        public int depthTargetIndex = -1;

        public RendererListHandle rendererList;


        public override void Record(RenderGraph renderGraph, RenderingData renderingData, List<TextureHandle> textureHandles)
        {
            using (var builder = renderGraph.AddRenderPass<BuiltinSkyPassData>($"{name}({PassName})", out var passData))
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
                        RendererList rendererList = ctx.renderContext.CreateSkyboxRendererList(renderingData.Camera);
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
        

        public const string PassName = "Builtin Sky Pass";
    }
}
