using System.Collections;
using System.Collections.Generic;
using System.IO;
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


        public IReadOnlyList<HNRenderGraphNodeInfo> RenderStack => renderStack;
        
        
        [SerializeField]
        private List<HNRenderGraphNodeInfo> renderStack;


        public HNRenderGraph()
        {
            renderStack = new List<HNRenderGraphNodeInfo>();
        }

        public void AddToRenderStack(HNRenderGraphNodeInfo renderGraphNode)
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
