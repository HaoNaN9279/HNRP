using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    [Serializable]
    public class DepthBufferInput : PassBase
    {
        public override void Initialize(HNRenderGraphBase hnRenderGraph, string passName)
        {
            base.Initialize(hnRenderGraph, passName);
            depthTargetIndex = hnRenderGraph.RegistAndGetTextureHandleIndex();
        }

        public override void Record(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            TextureHandle outputDepthTarget = renderGraph.CreateTexture(new TextureDesc(textureScale, true, false)
            {
                depthBufferBits = depthBits,
                clearBuffer = clearBuffer,
                name = name
            });
            renderingData.GraphData.textureHandles.Add(outputDepthTarget);
        }

        public override void EndRecord()
        {
            
        }

        public override void Dispose()
        {
            
        }


        [SerializeField]
        public Vector2 textureScale = Vector2.one;

        [SerializeField]
        public DepthBits depthBits = DepthBits.Depth32;

        [SerializeField]
        public bool clearBuffer = true;

        [SerializeField]
        public int depthTargetIndex = -1;


    }
}
