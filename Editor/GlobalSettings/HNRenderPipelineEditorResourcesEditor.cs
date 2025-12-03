using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HN.HNRP.Editor
{
    [CustomEditor(typeof(HNRenderPipelineEditorResources))]
    public class HNRenderPipelineEditorResourcesEditor : UnityEditor.Editor
    {
        void OnEnable()
        {
            shaderResourcesProperty = serializedObject.FindProperty("shaderResources");
            materialResourcesProperty = serializedObject.FindProperty("materialResources");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(shaderResourcesProperty);
            EditorGUILayout.PropertyField(materialResourcesProperty);
        }


        private SerializedProperty shaderResourcesProperty;
        private SerializedProperty materialResourcesProperty;
    }
}
