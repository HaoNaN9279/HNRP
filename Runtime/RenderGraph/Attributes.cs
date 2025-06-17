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


    public class PortInputInfo : HNGraphPortInfo
    {
        public PortInputInfo(string slotName, Capacity capacity)
             : base(slotName, Orientation.Horizontal, Direction.Input, capacity)
        {

        }
    }

    public class PortOutputInfo : HNGraphPortInfo
    {
        public PortOutputInfo(string slotName, Capacity capacity)
             : base(slotName, Orientation.Horizontal, Direction.Output, capacity)
        {

        }
    }
    


    public abstract class InspectableInfo : HNGraphInspectableInfo
    {

    }
}
