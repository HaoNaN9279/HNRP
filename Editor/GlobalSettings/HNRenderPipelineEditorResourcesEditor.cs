using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

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
            if(GUILayout.Button(new GUIContent("Reload All Resources")))
            {
                ReloadAllResources(target as HNRenderPipelineEditorResources);
            }
            EditorGUILayout.PropertyField(shaderResourcesProperty);
            EditorGUILayout.PropertyField(materialResourcesProperty);
        }


        private void ReloadAllResources(RenderPipelineResources resources)
        {
            if(resources == null)
                return;
                
            ResourceReloader.ReloadAllNullIn(resources, HNRenderPipelineGlobalSettings.HNRenderPipelinePath);
        }


        private SerializedProperty shaderResourcesProperty;
        private SerializedProperty materialResourcesProperty;
    }
}
