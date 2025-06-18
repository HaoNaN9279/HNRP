using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public struct GraphObjectData
    {
        public Camera Camera;
        public HNRenderGraphBase GraphObject;
        public CommandBuffer Cmd;
        public RenderTargetIdentifier TargetId;
    }
}
