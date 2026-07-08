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
        /// <summary>
        /// Pass资源创建时回调
        /// 创建、加载Pass所需的资源
        /// 只会在Editor下调用
        /// </summary>
        /// <param name="hnRenderGraph"></param>
        /// <param name="passName"></param>
        public virtual void OnCreate(HNRenderGraphBase hnRenderGraph, string passName)
        {
            this.hnRenderGraph = hnRenderGraph;
            this.name = passName;
        }

        /// <summary>
        /// Record Pass
        /// </summary>
        /// <param name="renderGraph"></param>
        /// <param name="renderingData"></param>
        public abstract void Record(RenderGraph renderGraph, ref RenderingData renderingData);
        
        /// <summary>
        /// 渲染管线卸载时回调
        /// Pass资源销毁时回调
        /// 卸载渲染管线是清理Pass中创建的资源
        /// </summary>
        public abstract void Cleanup();


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
