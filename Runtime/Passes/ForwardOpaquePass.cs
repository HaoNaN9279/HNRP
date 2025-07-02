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
        public ShaderTagId[] PassNames;

        [SerializeField]
        public uint layerMask =  0x00000001;

        public int colorTargetIndex = -1;

        public RendererListHandle rendererList;


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
                RendererListDesc rendererListDesc = GetForwardOpaqueRendererListDesc(frameData, graphObjectData, layerMask);
                passData.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(rendererListDesc));
                builder.AllowRendererListCulling(false);

                builder.SetRenderFunc(
                    (ForwardOpaquePassData data, RenderGraphContext ctx) =>
                    {
                        CoreUtils.DrawRendererList(ctx.renderContext, graphObjectData.Cmd, data.rendererList);
                    }
                );

            }
        }
        

        private RendererListDesc GetForwardOpaqueRendererListDesc(FrameData frameData, GraphObjectData graphObjectData, uint layerMask)
        {
            var desc = new RendererListDesc(PassNames, frameData.CullingResults, graphObjectData.Camera)
            {
                renderingLayerMask = layerMask,
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
        public RendererListHandle rendererList;

    }

}

