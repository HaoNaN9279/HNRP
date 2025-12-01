using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace HN.HNRP
{
    public abstract class PassBase : ScriptableObject
    {
        public virtual void Initialize(HNRenderGraphBase hnRenderGraph, string passName)
        {
            this.hnRenderGraph = hnRenderGraph;
            this.name = passName;
        }

        public abstract void Record(RenderGraph renderGraph, ref RenderingData renderingData);
        public abstract void EndRecord();


        public bool IsEnable
        {
            get { return isEnable; }
            set { isEnable = value; }
        }


        [SerializeReference]
        protected HNRenderGraphBase hnRenderGraph;

        [SerializeField]
        protected bool isEnable = true;

#if UNITY_EDITOR
        [SerializeField]
        protected bool isExpandedInInspector = false;
#endif
    }
}
