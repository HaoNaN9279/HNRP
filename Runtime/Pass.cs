using System.Collections;
using System.Collections.Generic;
using HN.Serialize;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace HN.HNRP
{
    public abstract class Pass : JsonObject
    {
        public abstract void Setup(HNRenderGraph renderGraph);
        public abstract void Record(RenderGraph renderGraph, FrameData frameData, GraphObjectData graphObjectData, List<TextureHandle> textureHandles);
    }
}
