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
            colorTargetIndex = hnRenderGraph.RegistAndGetTextureHandleIndex();
        }

        public override void Record(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            TextureHandle outputColorTarget = renderGraph.CreateTexture(new TextureDesc(textureScale, true, false)
            {
                colorFormat = colorFormat,
                clearBuffer = clearBuffer,
                clearColor = clearColor,
                name = name
            });
            renderingData.GraphData.textureHandles.Add(outputColorTarget);
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
        public int colorTargetIndex = -1;


    }

}
