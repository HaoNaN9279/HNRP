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
        void OnEnable()
        {
            shaderResourcesProperty = serializedObject.FindProperty("shaderResources");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(shaderResourcesProperty);
        }


        private SerializedProperty shaderResourcesProperty;
    }
}
