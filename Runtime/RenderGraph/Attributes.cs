using System.Collections;
using System.Collections.Generic;
using HN.Graph;
using UnityEngine;

namespace HN.HNRP
{
    public class HNRenderGraphNodeInfo : HNGraphNodeInfo
    {
        public NodeType Type => type;
        private NodeType type;

        public HNRenderGraphNodeInfo(string nodeTitle, NodeType nodeType, string menuItem = "") : base(nodeTitle, menuItem)
        {
            this.type = nodeType;
        }


        public enum NodeType
        {
            Renderer,
            Output,
            RenderTarget,
            Behaviour
        }
    }


    public class HNRenderGraphPortInfo : HNGraphPortInfo
    {
        public HNRenderGraphPortInfo(string slotName, Direction direction, Capacity capacity)
             : base(slotName, Orientation.Horizontal, direction, capacity)
        {

        }
    }


    public abstract class HNRenderGraphInspectableInfo : HNGraphInspectableInfo
    {

    }
}
