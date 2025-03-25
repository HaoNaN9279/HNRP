using System.Collections;
using System.Collections.Generic;
using HN.Graph;
using UnityEngine;

namespace HN.HNRP
{
    public class NodeInfo : HNGraphNodeInfo
    {
        public NodeType Type => type;

        protected NodeType type;

        public NodeInfo(string nodeTitle, NodeType nodeType, string menuItem = "") : base(nodeTitle, menuItem)
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


    public class PortInfo : HNGraphPortInfo
    {
        public PortInfo(string slotName, Direction direction, Capacity capacity)
             : base(slotName, Orientation.Horizontal, direction, capacity)
        {

        }
    }


    public abstract class InspectableInfo : HNGraphInspectableInfo
    {

    }
}
