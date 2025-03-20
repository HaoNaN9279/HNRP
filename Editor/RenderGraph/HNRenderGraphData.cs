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
    public class HNRenderGraphData : HNGraphData
    {
        

        public HNRenderGraph Graph
        {
            get{ return GraphObject as HNRenderGraph; }
            set{ GraphObject = value; }
        }
        
        public HNRenderGraphNodeInspector NodeInspector => nodeInspector;




        [SerializeField]
        private HNRenderGraphNodeInspector nodeInspector;


        public HNRenderGraphData()
        {
            GraphEditorAssemblyName = "HN.HNRP.Editor";
            GraphRuntimeAssemblyName = "HN.HNRP";
            GraphNodeDataNamespace = "HN.HNRP";
        }

        public override void Initialize(string assetPath)
        {
            base.Initialize(assetPath);

            Deserialize();
            GetGraphObject<HNRenderGraph>(assetPath);
        }

        public override void SaveAsset()
        {
            Compile();
            base.SaveAsset();
        }

        public void Compile()
        {            
            if(Graph == null)
                return;

            Graph.ClearRenderStack();
            
            List<HNGraphNode> outputNodes = FindNodesWithType<RenderOutputParams>();
            if(outputNodes.Count == 0)
                return;
            
            List<HNGraphNode> nodes = PackNodesFromOutput(outputNodes[0]);
            // nodes.RemoveAt(0);

            int count = 0;
            for(int i = nodes.Count - 1; i >= 0; i--)
            {
                count++;
                PushRenderNodeParams(count, nodes[i], nodes);
            }

            EditorUtility.SetDirty(Graph);
            AssetDatabase.SaveAssetIfDirty(Graph);
        }


        private void PushRenderNodeParams(int count, HNGraphNode node, List<HNGraphNode> nodes)
        {
            if(node == null)
                return;

            var nodeParamsData = node.NodeData;
            if(nodeParamsData == null)
                return;
            
            var nodeParams = nodeParamsData.Obj;
            if(nodeParams == null)
                return;

            Type nodeParamsType = nodeParams.GetType();

            foreach(var inputPortGuid in node.InputPortGuids)
            {
                var inputPort = GetNodePort(inputPortGuid);
                if(inputPort == null)
                    continue;

                int nodeIndex = nodes.IndexOf(node);
                string nodeName = node.NodeDataTypeName;
                string propertyName = inputPort.PropertyName;
                string portFullName = nodeIndex.ToString() + "_" + nodeName + "." + propertyName;

                string refPortFullName = "";
                if(inputPort.EdgeGuids.Count > 0)
                {
                    var edge = GetEdge(inputPort.EdgeGuids[0]);
                    var refBaseNode = GetBaseNode(edge.GetOutputPort(this).OwnerNodeGuid);
                    var refBasePort = edge.GetOutputPort(this);
                    string refNodeName = "";
                    string refPortName = "";
                    if(refBaseNode is HNGraphNode)
                    {
                        refNodeName = (refBaseNode as HNGraphNode).NodeDataTypeName;
                        refPortName = (refBasePort as HNGraphNodePort).PropertyName;
                        int refNodeIndex = nodes.IndexOf(refBaseNode as HNGraphNode);
                        refPortFullName = refNodeIndex.ToString() + "_" + refNodeName + "." + refPortName;
                    }
                    else if(refBaseNode is HNGraphRelayNode)
                    {
                        var refRelayNode = refBaseNode as HNGraphRelayNode;
                        var refRelayNodeInputPort = GetRelayNodePort(refRelayNode.InputPortGuid);
                        var refNodePort = GetNodePort(refRelayNodeInputPort.RefPortGuid);
                        var refNode = GetNode(refNodePort.OwnerNodeGuid);
                        refNodeName = refNode.NodeDataTypeName;
                        refPortName = refNodePort.PropertyName;
                        int refNodeIndex = nodes.IndexOf(refNode);
                        refPortFullName = refNodeIndex.ToString() + "_" + refNodeName + "." + refPortName;
                    }
                }
                
                // Debug.Log(portFullName + "  " + refPortFullName);
                TexturePort texturePort = new TexturePort()
                {
                    Name = portFullName,
                    RefTextureName = refPortFullName
                };

                nodeParamsType.GetProperty(propertyName)?.SetValue(nodeParams, texturePort);
            }
            
            nodeParamsData.Serialize();
            Graph.AddToRenderStack(nodeParamsData);
        }


    }

}
