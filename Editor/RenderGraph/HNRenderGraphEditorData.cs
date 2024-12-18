using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HN.Graph;
using HN.Graph.Editor;
using UnityEngine;
using UnityEditor;

namespace HN.HNRP.Editor
{
    [Serializable]
    public class HNRenderGraphEditorData : HNGraphEditorData
    {
        public HNRenderGraph Graph => GraphData as HNRenderGraph;

        public HNRenderGraphNodeInspector NodeInspector => nodeInspector;


        [SerializeField]
        private HNRenderGraphNodeInspector nodeInspector;


        public HNRenderGraphEditorData()
        {
            
        }

        public override void Initialize(HNGraphObject graphData)
        {
            base.Initialize(graphData);
        }

        public override void SaveAsset()
        {
            Compile();
            base.SaveAsset();

            Debug.Log("Render Stack Count:" + Graph.RenderStack.Count);
            for(int i = 0; i < Graph.RenderStack.Count; i++)
            {
                Debug.Log(Graph.RenderStack[i]);
            }
        }

        public void Compile()
        {            
            if(Graph == null)
                return;

            CleanRenderNode();

            List<HNGraphNode> outputNodes = FindNodesWithType<RenderOutputInfo>();
            if(outputNodes.Count == 0)
                return;
            
            List<HNGraphNode> nodes = PackNodesFromOutput(outputNodes[0]);
            for(int i = nodes.Count - 1; i >= 0; i--)
            {
                PushRenderNode(nodes[i]);
            }
        }


        private void PushRenderNode(HNGraphNode node)
        {
            var rendererNode = node?.NodeViewData as HNRenderGraphNodeInfo;
            if(rendererNode == null)
                return;
            
            Graph.AddToRenderStack(rendererNode);
        }

        private void CleanRenderNode() => Graph.ClearRenderStack();
    }


    public class HNRenderGraphNewAction : HNGraphNewAction<HNRenderGraph>
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            CreateGraphData();
            Serialize(pathName);
        }


        [MenuItem("Assets/Create/Rendering/HN Render Graph")]
        public static void CreateRenderGraph()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                ScriptableObject.CreateInstance<HNRenderGraphNewAction>(),
                string.Format("New HN Render Graph.{0}", HNRenderGraph.HNRenderGraphExtension),
                null,
                null);
        }

    }
}
