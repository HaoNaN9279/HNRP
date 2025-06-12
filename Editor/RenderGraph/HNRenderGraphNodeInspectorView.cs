using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HN.HNRP;
using HN.HNRP.Editor;
using HN.Serialize;
using Unity.Properties;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace HN.Graph.Editor
{
    public class HNRenderGraphNodeInspectorView : HNGraphFloatingPanelView
    {
        private static readonly string nodeInspectorPanelTree = "Elements/NodeInspector";
        private static readonly string nodeInspectorPanelStyle = "Elements/NodeInspector";


        private VisualElement nodeSettingsContainer;
        private VisualElement graphSettingsContainer;


        public HNRenderGraphNodeInspectorView(HNGraphView graphView, IHNGraphFloatingPanel floatingPanelData) : base(graphView, floatingPanelData)
        {
            HNRenderGraphNodeInspector inspectorData = floatingPanelData as HNRenderGraphNodeInspector;

            var tpl = Resources.Load<VisualTreeAsset>(nodeInspectorPanelTree);
            styleSheets.Add(Resources.Load<StyleSheet>(nodeInspectorPanelStyle));
            var nodeInspector = tpl.CloneTree();
            nodeInspector.AddToClassList("nodeInspector");
            nodeSettingsContainer = nodeInspector.Q("NodeSettingsContainer");
            graphSettingsContainer = nodeInspector.Q("GraphSettingsContainer");
            root.Add(nodeInspector);

            graphView.OnSelectionChanged += RefreshInspector;
            RefreshInspector(graphView.selection);
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public void RefreshInspector(List<ISelectable> selections)
        {
            nodeSettingsContainer.Clear();

            foreach (var selection in selections)
            {
                VisualElement panel = new VisualElement();
                panel.name = "panel";

                HNGraphNodeView nodeView = selection as HNGraphNodeView;
                if (nodeView == null)
                    continue;

                HNGraphNode nodeData = nodeView.NodeData;
                NodeParams nodeParams = nodeData?.NodeData?.Obj as NodeParams;
                if (nodeParams == null)
                    continue;

                Label label = new Label(nodeParams.GetType().Name);
                label.name = "label";
                panel.Add(label);

                VisualElement divideLine = new VisualElement();
                divideLine.name = "divideLine";
                panel.Add(divideLine);

                BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var propertyFields = HNGraphUtilsEditor.DrawProperties(nodeData, bindingFlags);
                panel.Add(propertyFields);

                nodeSettingsContainer.Add(panel);
            }
            
            MarkDirtyRepaint();
        }

        public override void Dispose()
        {
            graphView.OnSelectionChanged -= RefreshInspector;
        }
    }
}
