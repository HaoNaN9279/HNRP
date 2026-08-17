using System.Collections.Generic;
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

        private int m_SelectedConfigIndex;

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
            var configs = GetAvailableConfigs();
            var displayNames = BuildDisplayNames(configs);

            var currentConfig = m_PipelineConfigOverride.objectReferenceValue as CameraPipelineConfig;
            m_SelectedConfigIndex = FindCurrentConfigIndex(configs, currentConfig);

            var newIndex = EditorGUILayout.Popup(
                "Pipeline Config Override",
                m_SelectedConfigIndex,
                displayNames);

            if (newIndex != m_SelectedConfigIndex)
            {
                ApplyConfigSelection(configs, newIndex);
            }
        }

        /// <summary>
        /// Gets the available <see cref="CameraPipelineConfig"/> list from
        /// <see cref="HNRenderPipelineGlobalSettings.Instance"/>.
        /// Returns an empty list if the settings are not available.
        /// </summary>
        private static List<CameraPipelineConfig> GetAvailableConfigs()
        {
            var settings = HNRenderPipelineGlobalSettings.Instance;
            if (settings != null)
            {
                return settings.CameraPipelineConfigs;
            }

            return new List<CameraPipelineConfig>();
        }

        /// <summary>
        /// Builds the display name array for the dropdown popup.
        /// First entry is always "None" (index 0).
        /// </summary>
        private static string[] BuildDisplayNames(List<CameraPipelineConfig> configs)
        {
            var names = new string[configs.Count + 1];
            names[0] = "None";

            for (var i = 0; i < configs.Count; i++)
            {
                var config = configs[i];
                names[i + 1] = config != null ? config.name : "(Missing)";
            }

            return names;
        }

        /// <summary>
        /// Finds the index of <paramref name="currentConfig"/> in <paramref name="configs"/>.
        /// Returns 0 ("None") if the config is null or not found in the list.
        /// </summary>
        private static int FindCurrentConfigIndex(List<CameraPipelineConfig> configs, CameraPipelineConfig currentConfig)
        {
            if (currentConfig == null)
            {
                return 0;
            }

            var foundIndex = configs.IndexOf(currentConfig);
            return foundIndex >= 0 ? foundIndex + 1 : 0;
        }

        /// <summary>
        /// Applies the selected config to the serialized property.
        /// Index 0 sets the override to null ("None").
        /// </summary>
        private void ApplyConfigSelection(List<CameraPipelineConfig> configs, int selectedIndex)
        {
            if (selectedIndex == 0)
            {
                m_PipelineConfigOverride.objectReferenceValue = null;
            }
            else
            {
                var configIndex = selectedIndex - 1;

                if (configIndex >= 0 && configIndex < configs.Count)
                {
                    m_PipelineConfigOverride.objectReferenceValue = configs[configIndex];
                }
            }
        }
    }
}
