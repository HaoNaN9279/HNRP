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
    public class ForwardOpaquePass : PassBase
    {
        [SerializeField]
        public uint renderingLayerMask =  0x00000001;
        public RendererListHandle rendererList;

        public int colorTargetIndex = -1;
        public int depthTargetIndex = -1;
        public ShaderTagId[] PassNames;


        void OnEnable()
        {
            PassNames = new[] { ShaderPassNames.ForwardName };
            
        }

        public override void Record(RenderGraph renderGraph, FrameData frameData, GraphObjectData graphObjectData, List<TextureHandle> textureHandles)
        {
            Debug.Log("Record Forward Opaque pass.");

            using (var builder = renderGraph.AddRenderPass<ForwardOpaquePassData>("Forward Opaque Pass", out var passData))
            {
                passData.colorTarget = builder.UseColorBuffer(textureHandles[colorTargetIndex], 0);
                passData.depthTarget = builder.UseDepthBuffer(textureHandles[depthTargetIndex], DepthAccess.ReadWrite);
                RendererListDesc rendererListDesc = GetOpaqueRendererListDesc(frameData, graphObjectData, renderingLayerMask);
                passData.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(rendererListDesc));
                builder.AllowRendererListCulling(false);

                builder.SetRenderFunc(
                    (ForwardOpaquePassData data, RenderGraphContext ctx) =>
                    {
                        ctx.cmd.DrawRendererList(data.rendererList);
                    }
                );

            }
        }
        

        private RendererListDesc GetOpaqueRendererListDesc(FrameData frameData, GraphObjectData graphObjectData, uint renderingLayerMask)
        {
            var desc = new RendererListDesc(PassNames, frameData.CullingResults, graphObjectData.Camera)
            {
                renderingLayerMask = renderingLayerMask,
				rendererConfiguration = 0,
                renderQueueRange = HNRenderQueue.AllOpaque,
                sortingCriteria = SortingCriteria.CommonOpaque,
                stateBlock = null,
                overrideMaterial = null,
                excludeObjectMotionVectors = false,
            };

            return desc;
        }

    }


    public class ForwardOpaquePassData : PassData
    {
        public TextureHandle colorTarget;
        public TextureHandle depthTarget;
        public RendererListHandle rendererList;

    }

}

