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
        public override void OnCreate(HNRenderGraphBase hnRenderGraph, string passName)
        {
            base.OnCreate(hnRenderGraph, passName);
            
            depthTargetSlot = new TexturePassSlot(hnRenderGraph, PassSlotType.WriteOnly);
        }

        public override void Record(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            var graphObject = renderingData.GraphObject;
            TextureHandle outputDepthTarget = renderGraph.CreateTexture(new TextureDesc(textureScale, true, false)
            {
                depthBufferBits = depthBits,
                clearBuffer = clearBuffer,
                name = name
            });
            graphObject.RegistTextureHandle(outputDepthTarget);
        }

        public override void Cleanup()
        {
            
        }


        [SerializeField]
        public Vector2 textureScale = Vector2.one;

        [SerializeField]
        public DepthBits depthBits = DepthBits.Depth32;

        [SerializeField]
        public bool clearBuffer = true;

        [SerializeField]
        public TexturePassSlot depthTargetSlot;


    }
}
