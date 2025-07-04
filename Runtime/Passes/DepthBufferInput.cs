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
        [SerializeField]
        public Vector2 textureScale = Vector2.one;

        [SerializeField]
        public DepthBits depthBits = DepthBits.Depth32;

        [SerializeField]
        public bool clearBuffer = true;

        [SerializeField]
        public int depthTargetIndex = -1;


        public override void Initialize(HNRenderGraphBase hnRenderGraph, string passName)
        {
            base.Initialize(hnRenderGraph, passName);
            depthTargetIndex = hnRenderGraph.RegistAndGetTextureHandleIndex();
        }

        public override void Record(RenderGraph renderGraph, FrameData frameData, GraphObjectData graphObjectData, List<TextureHandle> textureHandles)
        {
            TextureHandle outputDepthTarget = renderGraph.CreateTexture(new TextureDesc(textureScale, true, false)
            {
                depthBufferBits = depthBits,
                clearBuffer = clearBuffer,
                name = name
            });
            textureHandles.Add(outputDepthTarget);
        }
    }
}
