using System;
using System.Collections;
using System.Collections.Generic;
using HN.Graph;
using UnityEngine;

namespace HN.HNRP
{
    public abstract class HNRenderGraphNodeInfo : IHNGraphNode
    {
        public HNRenderGraphNodeParams param;


        [SerializeField]
        public string name;


        public string GetName()
        {
            return name;
        }
    }


    public abstract class HNRenderGraphNodeParams : ScriptableObject
    {

    }
}
