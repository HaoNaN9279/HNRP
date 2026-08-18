using UnityEngine;
using UnityEditor;

namespace HN.HNRP.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HNAdditionalCameraData))]
    public class HNRenderPipelineAdditionalCameraDataEditor : UnityEditor.Editor
    {
        private SerializedProperty m_PipelineConfigOverride;
        private SerializedProperty m_Dithering;
        private SerializedProperty m_StopNaNs;
        private SerializedProperty m_AllowDynamicResolution;
        private SerializedProperty m_VolumeLayerMask;
        private SerializedProperty m_ClearDepth;

        private void OnEnable()
        {
            m_PipelineConfigOverride = serializedObject.FindProperty("pipelineConfigOverride");
            m_Dithering = serializedObject.FindProperty("dithering");
            m_StopNaNs = serializedObject.FindProperty("stopNaNs");
            m_AllowDynamicResolution = serializedObject.FindProperty("allowDynamicResolution");
            m_VolumeLayerMask = serializedObject.FindProperty("volumeLayerMask");
            m_ClearDepth = serializedObject.FindProperty("clearDepth");
        }

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // ── Pipeline Config Override ──
            DrawPipelineConfigOverrideField();

            EditorGUILayout.Space();

            // ── Rendering Settings ──
            EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_Dithering);
            EditorGUILayout.PropertyField(m_StopNaNs);
            EditorGUILayout.PropertyField(m_AllowDynamicResolution);
            EditorGUILayout.PropertyField(m_VolumeLayerMask);
            EditorGUILayout.PropertyField(m_ClearDepth);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPipelineConfigOverrideField()
        {
            EditorGUILayout.PropertyField(
                m_PipelineConfigOverride,
                new GUIContent("Pipeline Config Override"));
        }
    }
}
