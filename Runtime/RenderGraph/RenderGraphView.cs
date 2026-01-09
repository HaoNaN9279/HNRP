using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HN.HNRP
{
    [Serializable]
    public class RenderGraphViews : HNDictionary<string, HNRenderGraphBase>
    {

    }


    [Serializable]
    public class SceneViewRenderGraphViews : RenderGraphViews
    {
        
    }


    [Serializable]
    public class PreviewRenderGraphViews : RenderGraphViews
    {
        
    }


    [Serializable]
    public class GameViewRenderGraphViews : RenderGraphViews
    {
        
    }


    [Serializable]
    public class ReflectionRenderGraphViews : RenderGraphViews
    {
        
    }
}
