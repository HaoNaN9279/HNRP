using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace HN.HNRP
{
    public static class HNRenderPipelineUtils
    {
        public static RendererListDesc GetOpaqueRendererListDesc(ShaderTagId[] passNames, CullingResults cullingResults, Camera camera, uint renderingLayerMask)
        {
            var desc = new RendererListDesc(passNames, cullingResults, camera)
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

        public static RendererListDesc GetTransparentRendererListDesc(ShaderTagId[] passNames, CullingResults cullingResults, Camera camera, uint renderingLayerMask)
        {
            var desc = new RendererListDesc(passNames, cullingResults, camera)
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
}
