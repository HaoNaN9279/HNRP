using System.Collections;
using System.Collections.Generic;
using HN.Graph;
using UnityEngine;

namespace HN.HNRP
{
    public class HNRenderGraphNodeInfoAttribute : HNGraphNodeInfoAttribute
    {
        public NodeType Type => type;
        private NodeType type;

        public HNRenderGraphNodeInfoAttribute(string nodeTitle, NodeType nodeType, string menuItem = "") : base(nodeTitle, menuItem)
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
}
