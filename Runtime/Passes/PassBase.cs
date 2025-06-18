using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace HN.HNRP
{
    public abstract class PassBase : ScriptableObject
    {
        [SerializeReference]
        protected HNRenderGraphBase hnRenderGraph;


        public virtual void Initialize(HNRenderGraphBase hnRenderGraph, string passName)
        {
            this.hnRenderGraph = hnRenderGraph;
            this.name = passName;
        }
        
        public abstract void Record(RenderGraph renderGraph, FrameData frameData, GraphObjectData graphObjectData, List<TextureHandle> textureHandles);
    }
}
