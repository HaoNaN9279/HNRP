using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace HN.HNRP.Editor
{
    [CustomEditor(typeof(HNRenderPipelineRuntimeResources))]
    public class HNRenderPipelineRuntimeResourcesEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if(GUILayout.Button(new GUIContent("Reload All Resources")))
            {
                ReloadAllResources(target as HNRenderPipelineRuntimeResources);
            }
            shaderResourcesProperty = serializedObject.FindProperty("shaderResources");
            EditorGUILayout.PropertyField(shaderResourcesProperty);
        }


        private void ReloadAllResources(RenderPipelineResources resources)
        {
            if(resources == null)
                return;
                
            ResourceReloader.ReloadAllNullIn(resources, HNRenderPipelineGlobalSettings.HNRenderPipelinePath);
        }


        private SerializedProperty shaderResourcesProperty;
    }
}
