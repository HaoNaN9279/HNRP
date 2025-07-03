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
        public RendererListHandle rendererList;

        public int colorTargetIndex = -1;
        public ShaderTagId[] PassNames;


        void OnEnable()
        {
            PassNames = new[] { ShaderPassNames.ForwardName };

        }

        public override void Record(RenderGraph renderGraph, FrameData frameData, GraphObjectData graphObjectData, List<TextureHandle> textureHandles)
        {
            Debug.Log("Record Transparency pass.");
            using (var builder = renderGraph.AddRenderPass<TransparencyPassData>("Transparency Pass", out var passData))
            {
                passData.colorTarget = builder.UseColorBuffer(textureHandles[colorTargetIndex], 0);
                RendererListDesc rendererListDesc = GetTransparentRendererListDesc(frameData, graphObjectData, renderingLayerMask);
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
        

        private RendererListDesc GetTransparentRendererListDesc(FrameData frameData, GraphObjectData graphObjectData, uint renderingLayerMask)
        {
            var desc = new RendererListDesc(PassNames, frameData.CullingResults, graphObjectData.Camera)
            {
                renderingLayerMask = renderingLayerMask,
				rendererConfiguration = 0,
                renderQueueRange = HNRenderQueue.Transparent,
                sortingCriteria = SortingCriteria.CommonTransparent,
                stateBlock = null,
                overrideMaterial = null,
                excludeObjectMotionVectors = false,
            };

            return desc;
        }

    }


    public class TransparencyPassData : PassData
    {
        public TextureHandle colorTarget;
        public RendererListHandle rendererList;

    }

}
