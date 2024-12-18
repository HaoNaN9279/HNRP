using System;
using System.Collections;
using System.Collections.Generic;
using HN.HNRP;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace HN.Graph.Editor
{
    public class HNRenderGraphNodeInspectorView : HNGraphFloatingPanelView
    {
        public HNRenderGraphNodeInspectorView(HNGraphView graphView, IHNGraphFloatingPanel floatingPanelData) : base(graphView, floatingPanelData)
        {
            graphView.OnSelectionChanged += RefreshInspector;
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public void RefreshInspector(List<ISelectable> selection)
        {
            scrollView.Clear();

            if(selection == null || selection.Count == 0)
                return;
            
            var graphNodeView = selection[0] as HNGraphNodeView;
            var renderGraphNode = graphNodeView?.NodeData?.NodeViewData as HNRenderGraphNodeInfo;
            if(renderGraphNode == null)
                return;

            var editor = UnityEditor.Editor.CreateEditor(renderGraphNode.param);
            scrollView.Add(GetDefaultInspector(editor));

            MarkDirtyRepaint();
        }

        public VisualElement GetDefaultInspector(UnityEditor.Editor editor)
        {
            IMGUIContainer container = new IMGUIContainer(() =>
            {
                editor.OnInspectorGUI();
            });

            return container;
        }

        public override void Dispose()
        {
            graphView.OnSelectionChanged -= RefreshInspector;
        }
    }
}
