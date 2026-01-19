using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PlasticGui.WorkspaceWindow;

namespace HN.HNRP.Editor
{
    [CustomEditor(typeof(HNRenderPipelineRuntimeResources))]
    public class HNRenderPipelineRuntimeResourcesEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            shaderResourcesProperty = serializedObject.FindProperty("shaderResources");
            EditorGUILayout.PropertyField(shaderResourcesProperty);
        }


        private SerializedProperty shaderResourcesProperty;
    }
}
