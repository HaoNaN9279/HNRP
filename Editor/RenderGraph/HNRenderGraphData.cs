using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HN.Graph;
using HN.Graph.Editor;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace HN.HNRP.Editor
{
    [Serializable]
    public class HNRenderGraphData : HNGraphData
    {
        

        public HNRenderGraph Graph => GraphObject as HNRenderGraph;
        // public IReadOnlyDictionary<string, HNRenderGraphNode> NodeDataDict => nodeDataDict;
        public HNRenderGraphNodeInspector NodeInspector => nodeInspector;



        // [SerializeField]
        // private SerializableRenderGraphNode nodeDataDict;

        [SerializeField]
        private HNRenderGraphNodeInspector nodeInspector;


        public HNRenderGraphData()
        {
            GraphEditorAssemblyName = "HN.HNRP.Editor";
            GraphRuntimeAssemblyName = "HN.HNRP";
            GraphNodeDataNamespace = "HN.HNRP";
        }

        public override void UpdateGraphObject(ref HNGraphObject graphObject)
        {
            if(graphObject == null)
                return;

            
        }

        // public void AddNodeData(Type nodeDataType, string nodeGuid)
        // {
        //     var nodeDataRaw = Activator.CreateInstance(nodeDataType);
        //     var nodeData = (HNRenderGraphNode)nodeDataRaw;
        //     if(nodeData == null)
        //     {
        //         Debug.LogError($"Node data {nodeDataType} did not create sucessfully.");
        //         return;
        //     }

        //     if(nodeDataDict.ContainsKey(nodeGuid))
        //         return;

        //     nodeDataDict.Add(nodeGuid, nodeData);
        // }

        // public HNRenderGraphNode GetNodeData(string nodeGuid)
        // {
        //     if(!nodeDataDict.ContainsKey(nodeGuid))
        //         return null;
            
        //     return nodeDataDict[nodeGuid];
        // }

        // public void RemoveNodeData(string nodeGuid)
        // {
        //     if(!nodeDataDict.ContainsKey(nodeGuid))
        //         return;

        //     nodeDataDict.Remove(nodeGuid);
        // }

        public override void Initialize(string assetPath)
        {
            base.Initialize(assetPath);
            Deserialize();
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
            // if(Graph == null)
            //     return;

            // CleanRenderNode();

            // List<HNGraphNode> outputNodes = FindNodesWithType<RenderOutput>();
            // if(outputNodes.Count == 0)
            //     return;
            
            // List<HNGraphNode> nodes = PackNodesFromOutput(outputNodes[0]);
            // for(int i = nodes.Count - 1; i >= 0; i--)
            // {
            //     PushRenderNode(nodes[i]);
            // }
        }

        // public override void AddNode(HNGraphNode node)
        // {
        //     base.AddNode(node);
        //     AddNodeData(node.NodeDataType, node.Guid);
        // }

        // public override void RemoveNode(HNGraphNode node)
        // {
        //     base.RemoveNode(node);
        //     RemoveNodeData(node.Guid);
        // }


        // private void PushRenderNode(HNGraphNode node)
        // {
        //     var rendererNode = GetNodeData(node.Guid);
        //     if(rendererNode == null)
        //         return;
            
        //     Graph.AddToRenderStack(rendererNode);
        // }

        private void CleanRenderNode() => Graph.ClearRenderStack();

    }



    [Serializable]
    public class SerializableRenderGraphNode : SerializableDictionary<string, HNRenderGraphNode> {}
}
