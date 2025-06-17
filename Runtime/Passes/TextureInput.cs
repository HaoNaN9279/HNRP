using System;
using System.Collections;
using System.Collections.Generic;
using HN.Graph;
using HN.Serialize;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    [Serializable]
    [NodeInfo("Texture Input", NodeInfo.NodeType.RenderTarget, "Render Target/Texture Input")]
    public class TextureInput : Pass
    {
        [SerializeField]
        [PortOutputInfo("Color Target", PortOutputInfo.Capacity.Single)]
        public int outputColorTargetIndex = -1;


        public override void Setup(HNRenderGraph renderGraph)
        {
            Debug.Log("Texture Input Setup.");

            int index = renderGraph.AddTextureHandle(new TextureHandle());
            outputColorTargetIndex = index;
        }

        public override void Record(RenderGraph renderGraph, FrameData frameData, GraphObjectData graphObjectData, List<TextureHandle> textureHandles)
        {
            Debug.Log("Texture Input");

            TextureHandle outputColorTarget = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            {
                colorFormat = GraphicsFormat.R8G8B8A8_UNorm,
                clearBuffer = true,
                clearColor = Color.red,
                name = "ColorTarget"
            });

            textureHandles[outputColorTargetIndex] = outputColorTarget;
        }

    }

}
