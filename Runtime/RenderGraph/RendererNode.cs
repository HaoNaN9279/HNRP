using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HN.HNRP
{
    public abstract class RendererNodeInfo : HNRenderGraphNodeInfo
    {

    }


    public abstract class RendererNodeParams : HNRenderGraphNodeParams
    {
        public abstract void Execute();
    }
}
