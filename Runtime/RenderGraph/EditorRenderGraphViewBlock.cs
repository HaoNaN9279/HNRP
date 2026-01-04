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
            renderGraphViews = new RenderGraphView();
            CreateView(defaultSceneViewName);
        }

        public override HNRenderGraphBase GetRenderGraphObject()
        {
            //TODO:不同渲染模式下切换不同的scene view graph
            return renderGraphViews[defaultSceneViewName];
        }


        public override RenderGraphViewType ViewType => RenderGraphViewType.SceneView;

        private const string defaultSceneViewName = "DefaultSceneView";
    }


    [Serializable]
    public class PreviewRenderGraphViewBlock : RenderGraphViewBlock
    {
        public PreviewRenderGraphViewBlock()
        {
            renderGraphViews = new RenderGraphView();
            CreateView(defaultPreviewViewName);
        }

        public override HNRenderGraphBase GetRenderGraphObject()
        {
            //TODO:切换不同的preview graph
            return renderGraphViews[defaultPreviewViewName];
        }


        public override RenderGraphViewType ViewType => RenderGraphViewType.Preview;

        private const string defaultPreviewViewName = "DefaultPreviewView";
    }
}
