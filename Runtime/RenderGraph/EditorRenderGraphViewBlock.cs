using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HN.HNRP
{
    [Serializable]
    public class SceneViewRenderGraphViewBlock : RenderGraphViewBlock
    {
        public SceneViewRenderGraphViewBlock()
        {
            renderGraphViews = new SceneViewRenderGraphViews();
            CreateView(defaultSceneViewName);
        }


        public override RenderGraphViewType ViewType => RenderGraphViewType.SceneView;

        private const string defaultSceneViewName = "DefaultSceneView";
    }


    [Serializable]
    public class PreviewRenderGraphViewBlock : RenderGraphViewBlock
    {
        public PreviewRenderGraphViewBlock()
        {
            renderGraphViews = new PreviewRenderGraphViews();
            CreateView(defaultPreviewViewName);
        }


        public override RenderGraphViewType ViewType => RenderGraphViewType.Preview;

        private const string defaultPreviewViewName = "DefaultPreviewView";
    }
}
