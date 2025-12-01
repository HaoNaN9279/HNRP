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
        public override void Record(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<BuiltinSkyPassData>($"{name}({PassName})", out var passData))
            {
                var textureHandles = renderingData.GraphData.textureHandles;
                passData.colorTarget = builder.UseColorBuffer(textureHandles[colorTargetIndex], 0);
                if (textureHandles[depthTargetIndex].IsValid())
                {
                    passData.depthTarget = builder.UseDepthBuffer(textureHandles[depthTargetIndex], DepthAccess.Read);
                }
                builder.AllowPassCulling(false);

                var camera = renderingData.Camera;
                builder.SetRenderFunc(
                    (BuiltinSkyPassData data, RenderGraphContext ctx) =>
                    {
                        RendererList rendererList = ctx.renderContext.CreateSkyboxRendererList(camera);
                        ctx.cmd.DrawRendererList(rendererList);
                    }
                );
            }
        }

        public override void EndRecord()
        {
            
        }


        [SerializeField]
        public int colorTargetIndex = -1;

        [SerializeField]
        public int depthTargetIndex = -1;

        public const string PassName = "Builtin Sky Pass";


        public class BuiltinSkyPassData : PassData
        {
            public TextureHandle colorTarget;
            public TextureHandle depthTarget;
        }
        

    }
}
