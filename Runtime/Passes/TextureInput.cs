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
    public class TextureInput : PassBase
    {
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


        public override void Initialize(HNRenderGraphBase hnRenderGraph, string passName)
        {
            base.Initialize(hnRenderGraph, passName);
            colorTargetIndex = hnRenderGraph.RegistAndGetTextureHandleIndex();
        }

        public override void Record(RenderGraph renderGraph, FrameData frameData, GraphObjectData graphObjectData, List<TextureHandle> textureHandles)
        {
            Debug.Log("Texture Input");

            TextureHandle outputColorTarget = renderGraph.CreateTexture(new TextureDesc(textureScale, true, true)
            {
                colorFormat = colorFormat,
                clearBuffer = clearBuffer,
                clearColor = clearColor,
                name = name
            });
            textureHandles.Add(outputColorTarget);
        }

    }

}
