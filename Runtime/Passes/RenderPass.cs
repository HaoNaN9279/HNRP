using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public abstract class RenderPass : IRenderPass
    {
        protected NodeParams nodeParams;


        public RenderPass()
        {
            
        }


        public virtual void Initialize(NodeParams nodeParams)
        {
            this.nodeParams = nodeParams;
        }

        public abstract void Setup(CommandBuffer cmd);

        public abstract void Record(RenderGraph renderGraph, Dictionary<string, TextureHandle> textureHandleDict);

        public abstract void Dispose();
    }


    public interface IRenderPass : IDisposable
    {
        public void Setup(CommandBuffer cmd);
        public void Record(RenderGraph renderGraph, Dictionary<string, TextureHandle> textureHandleDict);
    }
}
