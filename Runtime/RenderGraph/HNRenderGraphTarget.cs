using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HN.Graph;
using HN.Serialize;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public abstract class HNRenderGraphTarget
    {
        protected RenderGraph renderGraph;
        protected List<JsonData> passParamsData;
        protected Camera camera;
        protected RenderTargetIdentifier targetId;
        protected int frameCount;


        public void Initialize(
            RenderGraph renderGraph, 
            List<JsonData> passParamsData,
            Camera camera,
            RenderTargetIdentifier targetId,
            int frameCount
            )
        {
            this.renderGraph = renderGraph;
            this.passParamsData = passParamsData;
            this.camera = camera;
            this.targetId = targetId;
            this.frameCount = frameCount;
        }

        public abstract void Execute();
    }
}
