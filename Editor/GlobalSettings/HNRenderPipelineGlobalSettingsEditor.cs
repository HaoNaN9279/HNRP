using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HN.HNRP.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HNRenderPipelineGlobalSettings))]
    public class HNRenderPipelineGlobalSettingsEditor : UnityEditor.Editor
    {
        void OnEnable()
        {
            serializedGlobalSettings = new SerializedHNRenderPipelineGlobalSettings(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            var serialized = serializedGlobalSettings;

            serialized.serializedObject.Update();
            
            HNRenderPipelineGlobalSettingsUI.Inspector.Draw(serialized, this);

            serialized.serializedObject.ApplyModifiedProperties();
        }


        private SerializedHNRenderPipelineGlobalSettings serializedGlobalSettings;
    }
}
