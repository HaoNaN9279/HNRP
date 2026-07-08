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
    public class ColorBufferInput : PassBase
    {
        public override void OnCreate(HNRenderGraphBase hnRenderGraph, string passName)
        {
            base.OnCreate(hnRenderGraph, passName);
            
            colorTargetSlot = new TexturePassSlot(hnRenderGraph, PassSlotType.WriteOnly);
        }

        public override void Record(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            var graphObject = renderingData.GraphObject;
            TextureHandle outputColorTarget = renderGraph.CreateTexture(new TextureDesc(textureScale, true, false)
            {
                colorFormat = colorFormat,
                clearBuffer = clearBuffer,
                clearColor = clearColor,
                name = name
            });
            graphObject.RegistTextureHandle(outputColorTarget);
        }

        public override void Cleanup()
        {
            
        }


        [SerializeField]
        public Vector2 textureScale = Vector2.one;

        [SerializeField]
        public GraphicsFormat colorFormat = GraphicsFormat.R8G8B8A8_UNorm;

        [SerializeField]
        public bool clearBuffer = true;

        [SerializeField]
        public Color clearColor = Color.black;


        [SerializeField]
        public TexturePassSlot colorTargetSlot;


    }

}
