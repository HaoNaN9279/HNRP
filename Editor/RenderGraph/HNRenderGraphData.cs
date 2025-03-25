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
using Codice.CM.Common.Tree;

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
            if(Graph.Initialize() == false)
                return;

            Debug.Log("Compile");      
            List<HNGraphNode> outputNodes = FindNodesWithType<RenderOutputParams>();
            if(outputNodes.Count == 0)
                return;
            
            List<HNGraphNode> nodes = PackNodesFromOutput(outputNodes[0]);
            nodes.Reverse();

            // 正向遍历节点
            for(int i = 0; i < nodes.Count; i++)
            {
                BuildRenderNodeConnection(i, nodes[i], nodes);
                CombineGeneratedScript(nodes[i], i);
            }

            Graph.GenerateScript();
            EditorUtility.SetDirty(Graph);
            AssetDatabase.SaveAssetIfDirty(Graph);
            AssetDatabase.Refresh();
        }


        private void BuildRenderNodeConnection(int nodeIndex, HNGraphNode node, List<HNGraphNode> nodes)
        {
            if(node == null)
                return;

            var nodeParamsData = node.NodeData;
            if(nodeParamsData == null)
                return;
            
            var nodeParams = nodeParamsData.Obj as NodeParams;
            if(nodeParams == null)
                return;

            Type nodeParamsType = nodeParams.GetType();
            foreach(var inputPortGuid in node.InputPortGuids)
            {
                var inputPort = GetPort(inputPortGuid);
                if(inputPort == null)
                    continue;

                string propertyName = inputPort.PropertyName;
                if(inputPort.EdgeGuids.Count > 0)
                {
                    var edge = GetEdge(inputPort.EdgeGuids[0]);
                    var refNode = GetNode(edge.GetOutputPort(this).OwnerNodeGuid);
                    var refNodeParams = refNode?.NodeData?.Obj;
                    var refPort = edge.GetOutputPort(this);
                    string refPortName = refPort.PropertyName;
                    if (refNodeParams == null)
                        continue;
                    Type refNodeParamsType = refNodeParams.GetType();
                    PropertyInfo refPorpertyInfo = refNodeParamsType.GetProperty(refPortName);
                    TexturePort refTexturePort = refPorpertyInfo.GetValue(refNodeParams) as TexturePort;
                    TexturePort texturePort = new TexturePort(refTexturePort.RefTextureName);
                    nodeParamsType.GetProperty(propertyName)?.SetValue(nodeParams, texturePort);
                }
            }
            nodeParams.SetupOutput(nodeIndex);
            Graph.AppendPassParams(new Serialize.JsonData(nodeParams));
        }

        private void CombineGeneratedScript(HNGraphNode node, int nodeIndex)
        {
            if(node == null)
                return;

            var nodeParamsData = node.NodeData;
            if(nodeParamsData == null)
                return;
            
            NodeParams nodeParams = nodeParamsData.Obj as NodeParams;
            if(nodeParams == null)
                return;

            nodeParams.AppendScript(ref Graph.GeneratedScript, nodeIndex);
        }
    }

}
