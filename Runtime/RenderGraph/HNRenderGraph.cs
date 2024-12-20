using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HN.Graph;
using HN.Serialize;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace HN.HNRP
{
    public class HNRenderGraph : HNGraphObject
    {
        public const string HNRenderGraphExtension = "hnrg";

        public IReadOnlyList<HNRenderGraphNode> RenderStack => renderStack;
        



        [SerializeField]
        private List<HNRenderGraphNode> renderStack;



        public void OnEnable()
        {
            renderStack = new List<HNRenderGraphNode>();    
        }
        
        public void AddToRenderStack(HNRenderGraphNode renderGraphNode)
        {
            if(renderGraphNode == null)
                return;

            renderStack.Add(renderGraphNode);
        }

        public void ClearRenderStack()
        {
            renderStack.Clear();
        }


#if UNITY_EDITOR
        public override void Serialize()
        {
            Json.Serialize(this, AssetPath);
            AssetDatabase.ImportAsset(AssetPath);
        }
#endif

    }
}
