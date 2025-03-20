using System.Collections;
using System.Collections.Generic;
using System.IO;
using HN.Graph.Editor;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

namespace HN.HNRP.Editor
{
    public class HNRenderGraphEditorWindow : HNGraphEditorWindow
    {
        private HNRenderGraphNodeInspector nodeInspector;
        private HNGraphFloatingPanelView nodeInspectorView;


        public override void CreateSearchWindowProvider()
        {
            SearchWindowProvider = ScriptableObject.CreateInstance<HNGraphSearchWindowProvider>();
            SearchWindowProvider.GraphNodeInfoAttributeType = typeof(NodeInfo);
        }

        public override void AdditionalToolButton(Toolbar toolbar)
        {
            var inspectorToggle = new ToolbarToggle();
            inspectorToggle.text = "Inspector";
            inspectorToggle.RegisterCallback<ChangeEvent<bool>>(OnInspectorToggle);
            toolbar.Add(inspectorToggle);
        }


        protected override bool LoadGraphData(string path)
        {
            if(string.IsNullOrEmpty(path))
                return false;

            graphData = Activator.CreateInstance<HNRenderGraphData>();
            graphData.Initialize(path);
            if(graphData == null)
                return false;

            graphData.Deserialize();
            return true;
        }


        private void OnInspectorToggle(ChangeEvent<bool> env)
        {
            HNRenderGraphData editorData = GraphData as HNRenderGraphData;
            if(editorData == null)
                return;
            
            if(env.newValue == true)
            {
                nodeInspector = editorData.NodeInspector;
                if(!nodeInspector.IsSaved())
                    nodeInspector.Initialize();
                
                nodeInspectorView = new HNRenderGraphNodeInspectorView(GraphView, nodeInspector);
                nodeInspectorView.Initialize();
                GraphView.DrawFloatingPanelView(nodeInspectorView);
            }
            else
            {
                GraphView.CloseFloatingPanel(typeof(HNRenderGraphNodeInspector));
            }
        }

    }
}
