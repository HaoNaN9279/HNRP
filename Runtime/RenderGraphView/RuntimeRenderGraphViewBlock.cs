using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HN.HNRP
{
    [Serializable]
    public class GameViewRenderGraphViewBlock : RenderGraphViewBlock
    {
        public GameViewRenderGraphViewBlock()
        {
            renderGraphViews = new RenderGraphView();
            CreateView(DefaultGameViewName);
        }

        public override RenderGraphViewType ViewType => RenderGraphViewType.MainGameView;

        public const string DefaultGameViewName = "MainGameView";
    }


    [Serializable]
    public class ReflectionRenderGraphViewBlock : RenderGraphViewBlock
    {
        public ReflectionRenderGraphViewBlock()
        {
            renderGraphViews = new RenderGraphView();
            CreateView(DefaultReflectionViewName);
        }

        public override RenderGraphViewType ViewType => RenderGraphViewType.Reflection;

        public const string DefaultReflectionViewName = "DefaultReflectionView";
    }
}
