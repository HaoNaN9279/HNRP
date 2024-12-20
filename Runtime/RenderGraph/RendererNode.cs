using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HN.HNRP
{
    [Serializable]
    public abstract class RendererNode : HNRenderGraphNode
    {
        public abstract void Execute();
    }
}
