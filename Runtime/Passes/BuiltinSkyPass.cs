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
        public override void OnCreate(HNRenderGraphBase hnRenderGraph, string passName)
        {
            base.OnCreate(hnRenderGraph, passName);

            colorTargetSlot = new TexturePassSlot(hnRenderGraph, PassSlotType.ReadWrite);
            depthTargetSlot = new TexturePassSlot(hnRenderGraph, PassSlotType.ReadWrite);
        }

        public override void Record(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            if(!colorTargetSlot.IsConnected || !depthTargetSlot.IsConnected)
            {
                return;
            }

            using (var builder = renderGraph.AddRenderPass<BuiltinSkyPassData>($"{name}({PassName})", out var passData))
            {
                var graphObject = renderingData.GraphObject;
                passData.colorTarget = builder.UseColorBuffer(graphObject.GetTextureHandle(colorTargetSlot), 0);
                var depthTargetHandle = graphObject.GetTextureHandle(depthTargetSlot);
                if (depthTargetHandle.IsValid())
                {
                    passData.depthTarget = builder.UseDepthBuffer(depthTargetHandle, DepthAccess.Read);
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

        public override void Cleanup()
        {
            
        }


        [SerializeField]
        public TexturePassSlot colorTargetSlot;

        [SerializeField]
        public TexturePassSlot depthTargetSlot;

        public const string PassName = "Builtin Sky Pass";


        public class BuiltinSkyPassData : PassData
        {
            public TextureHandle colorTarget;
            public TextureHandle depthTarget;
        }
        

    }
}
