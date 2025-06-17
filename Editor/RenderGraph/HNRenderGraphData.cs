using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HN.Graph;
using HN.Graph.Editor;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

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
            {
                Debug.LogError("HNRenderGraph is null.");
                return;
            }

            Debug.Log("Start Compile"); 

            Graph.ClearData();   

            List<HNGraphNode> outputNodes = FindNodesWithType<RenderOutput>();
            if(outputNodes.Count == 0)
                return;
            
            List<HNGraphNode> nodes = PackNodesFromOutput(outputNodes[0]);
            nodes.Reverse();

            // 正向遍历节点
            for(int i = 0; i < nodes.Count; i++)
            {

                BuildPassParams(nodes[i]);
            }

            
        }


        private void BuildPassParams(HNGraphNode node)
        {
            Debug.Log(node);
            if (node == null)
                return;

            var nodePassData = node.NodeData;
            if (nodePassData == null)
                return;

            var nodePass = nodePassData.Obj as Pass;
            if (nodePass == null)
                return;

            Type nodePassType = nodePass.GetType();
            foreach (var inputPortGuid in node.InputPortGuids)
            {
                var inputPort = GetPort(inputPortGuid);
                if (inputPort == null)
                    continue;

                string fieldName = inputPort.FieldName;
                if (inputPort.EdgeGuids.Count > 0)
                {
                    var edge = GetEdge(inputPort.EdgeGuids[0]);
                    var refPort = edge.GetOutputPort(this);
                    var refNode = GetNode(refPort.OwnerNodeGuid);
                    var refNodePass = refNode?.NodeData?.Obj;
                    string refFieldName = refPort.FieldName;
                    if (refNodePass == null)
                        continue;
                    Type refNodeParamsType = refNodePass.GetType();
                    FieldInfo refFieldInfo = refNodeParamsType.GetField(refFieldName);
                    int refTexturePort = (int)refFieldInfo.GetValue(refNodePass);
                    nodePassType.GetField(fieldName)?.SetValue(nodePass, refTexturePort);
                }
            }

            Graph.AddPass(nodePass);
            // Debug.Log("Added Pass: " + nodePass + "  Type: " + nodePass.GetType());
        }
        
    }

}
