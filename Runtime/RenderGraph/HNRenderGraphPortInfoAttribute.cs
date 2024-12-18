using System.Collections;
using System.Collections.Generic;
using HN.Graph;
using UnityEngine;

namespace HN.HNRP
{
    public class HNRenderGraphPortInfoAttribute : HNGraphPortInfoAttribute
    {
        public HNRenderGraphPortInfoAttribute(string slotName, Direction direction, Capacity capacity)
             : base(slotName, Orientation.Horizontal, direction, capacity)
        {

        }
    }
}
