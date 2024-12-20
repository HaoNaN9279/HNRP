using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HN.HNRP;
using HN.HNRP.Editor;
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
            if(graphNodeView == null)
                return;
            string nodeGuid = graphNodeView.NodeData.Guid;
            
            SerializedObject renderGraphSerializedObject = new SerializedObject(graphView.GraphEditorData);
            scrollView.Add(GetDefaultInspector(renderGraphSerializedObject, nodeGuid));

            MarkDirtyRepaint();
        }

        public VisualElement GetDefaultInspector(SerializedObject serializedObject, string nodeGuid)
        {
            // var nodeDataDictProperty = serializedObject.FindProperty("nodeDataDict");
            // var nodeDataGuidListProperty = nodeDataDictProperty.FindPropertyRelative("keys");
            // int index = -1;
            // for(int i = 0; i < nodeDataGuidListProperty.arraySize; i++)
            // {
            //     var nodeGuidProperty = nodeDataGuidListProperty.GetArrayElementAtIndex(i);
            //     Debug.Log(nodeGuidProperty.stringValue);
            //     if(nodeGuidProperty.stringValue == nodeGuid)
            //     {
            //         index = i;
            //         break;
            //     }
            // }
            // var nodeDataListProperty = nodeDataDictProperty.FindPropertyRelative("values");
            // var nodeDataProperty = nodeDataListProperty.GetArrayElementAtIndex(index);

            IMGUIContainer container = new IMGUIContainer(() =>
            {
                // if(index == -1)
                //     return;
                
                // serializedObject.Update();

                // var iterator = serializedObject.GetIterator();
                // iterator.NextVisible(false);
                // do
                // {
                //     EditorGUILayout.PropertyField(iterator);
                // }while(iterator.NextVisible(false));
                // serializedObject.ApplyModifiedProperties();
            });

            return container;
        }

        public override void Dispose()
        {
            graphView.OnSelectionChanged -= RefreshInspector;
        }
    }
}
