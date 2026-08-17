// <copyright file="RenderGraphAssetEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using HN.HNRP;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="RenderGraphAsset"/> using IMGUI.
    /// Provides editing for:
    /// <list type="bullet">
    /// <item><see cref="RenderGraphSettings"/> (SHEvalMode, AllowHDR)</item>
    /// <item><see cref="PassDefinition"/> list (add, remove, reorder)</item>
    /// <item><see cref="SlotConnection"/> list (add, remove, edit source/target names)</item>
    /// </list>
    /// </summary>
    [CustomEditor(typeof(RenderGraphAsset))]
    public sealed class RenderGraphAssetEditor : UnityEditor.Editor
    {
        #region Fields

        private SerializedProperty m_PassesProp;
        private SerializedProperty m_ConnectionsProp;
        private SerializedProperty m_SettingsProp;

        private SerializedProperty m_SHEvalModeProp;
        private SerializedProperty m_AllowHDRProp;

        private ReorderableList m_PassesList;
        private ReorderableList m_ConnectionsList;

        private bool m_PassesExpanded = true;
        private bool m_ConnectionsExpanded = true;
        private bool m_SettingsExpanded = true;

        #endregion

        #region Unity Messages

        private void OnEnable()
        {
            m_PassesProp = serializedObject.FindProperty("m_Passes");
            m_ConnectionsProp = serializedObject.FindProperty("m_Connections");
            m_SettingsProp = serializedObject.FindProperty("m_Settings");

            if (m_SettingsProp != null)
            {
                m_SHEvalModeProp = m_SettingsProp.FindPropertyRelative(nameof(RenderGraphSettings.SHEvalMode));
                m_AllowHDRProp = m_SettingsProp.FindPropertyRelative(nameof(RenderGraphSettings.AllowHDR));
            }

            SetupPassesList();
            SetupConnectionsList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScriptField();
            DrawSettingsSection();
            DrawPassesSection();
            DrawConnectionsSection();

            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Sections

        private static void DrawScriptField()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                if (Selection.activeObject is ScriptableObject so)
                {
                    EditorGUILayout.ObjectField(
                        "Script",
                        MonoScript.FromScriptableObject(so),
                        typeof(MonoScript),
                        allowSceneObjects: false);
                }
            }
        }

        private void DrawSettingsSection()
        {
            m_SettingsExpanded = EditorGUILayout.Foldout(
                m_SettingsExpanded,
                "Render Graph Settings",
                toggleOnLabelClick: true);

            if (!m_SettingsExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;

            if (m_SHEvalModeProp != null)
            {
                EditorGUILayout.PropertyField(m_SHEvalModeProp,
                    new GUIContent("SH Eval Mode",
                        "Spherical harmonics evaluation mode: PerVertex, Mixed, or PerPixel."));
            }

            if (m_AllowHDRProp != null)
            {
                EditorGUILayout.PropertyField(m_AllowHDRProp,
                    new GUIContent("Allow HDR",
                        "When enabled, the render graph may allocate HDR render targets."));
            }

            EditorGUI.indentLevel--;
        }

        private void DrawPassesSection()
        {
            m_PassesExpanded = EditorGUILayout.Foldout(
                m_PassesExpanded,
                $"Pass Definitions ({m_PassesProp.arraySize})",
                toggleOnLabelClick: true);

            if (!m_PassesExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;
            m_PassesList.DoLayoutList();
            EditorGUI.indentLevel--;
        }

        private void DrawConnectionsSection()
        {
            m_ConnectionsExpanded = EditorGUILayout.Foldout(
                m_ConnectionsExpanded,
                $"Slot Connections ({m_ConnectionsProp.arraySize})",
                toggleOnLabelClick: true);

            if (!m_ConnectionsExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;
            m_ConnectionsList.DoLayoutList();
            EditorGUI.indentLevel--;
        }

        #endregion

        #region PassDefinition List Setup

        private void SetupPassesList()
        {
            m_PassesList = new ReorderableList(
                serializedObject, m_PassesProp,
                draggable: true,
                displayHeader: true,
                displayAddButton: true,
                displayRemoveButton: true)
            {
                drawHeaderCallback = DrawPassesHeader,
                drawElementCallback = DrawPassElement,
                elementHeightCallback = GetPassElementHeight,
                onAddCallback = OnAddPass,
            };
        }

        private static void DrawPassesHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Passes");
        }

        private void DrawPassElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty element = m_PassesProp.GetArrayElementAtIndex(index);
            if (element == null)
            {
                return;
            }

            SerializedProperty passTypeProp = element.FindPropertyRelative("m_PassType");
            SerializedProperty instanceNameProp = element.FindPropertyRelative("m_InstanceName");
            SerializedProperty configProp = element.FindPropertyRelative("m_Config");

            float singleLine = EditorGUIUtility.singleLineHeight;
            float padding = 2f;
            float halfWidth = (rect.width - padding) / 2f;

            // Row 1: Pass Type | Instance Name
            var row1TypeRect = new Rect(rect.x, rect.y + padding, halfWidth, singleLine);
            var row1NameRect = new Rect(rect.x + halfWidth + padding, rect.y + padding, halfWidth, singleLine);

            EditorGUI.PropertyField(row1TypeRect, passTypeProp, new GUIContent("Type"));
            EditorGUI.PropertyField(row1NameRect, instanceNameProp, new GUIContent("Name"));

            // Row 2: Pass Config (object reference)
            var configRect = new Rect(rect.x, rect.y + singleLine + padding,
                rect.width, singleLine);
            EditorGUI.PropertyField(configRect, configProp, new GUIContent("Config"));
        }

        private float GetPassElementHeight(int index)
        {
            return (EditorGUIUtility.singleLineHeight * 2) + 6f;
        }

        private void OnAddPass(ReorderableList list)
        {
            int index = m_PassesProp.arraySize;
            m_PassesProp.InsertArrayElementAtIndex(index);

            // Initialize defaults.
            SerializedProperty element = m_PassesProp.GetArrayElementAtIndex(index);
            if (element != null)
            {
                element.FindPropertyRelative("m_PassType").stringValue = string.Empty;
                element.FindPropertyRelative("m_InstanceName").stringValue = string.Empty;
                element.FindPropertyRelative("m_Config").objectReferenceValue = null;
            }

            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region SlotConnection List Setup

        private void SetupConnectionsList()
        {
            m_ConnectionsList = new ReorderableList(
                serializedObject, m_ConnectionsProp,
                draggable: true,
                displayHeader: true,
                displayAddButton: true,
                displayRemoveButton: true)
            {
                drawHeaderCallback = DrawConnectionsHeader,
                drawElementCallback = DrawConnectionElement,
                elementHeightCallback = GetConnectionElementHeight,
                onAddCallback = OnAddConnection,
            };
        }

        private static void DrawConnectionsHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Slot Connections");
        }

        private void DrawConnectionElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty element = m_ConnectionsProp.GetArrayElementAtIndex(index);
            if (element == null)
            {
                return;
            }

            SerializedProperty sourcePassProp = element.FindPropertyRelative("m_SourcePass");
            SerializedProperty sourceSlotProp = element.FindPropertyRelative("m_SourceSlot");
            SerializedProperty targetPassProp = element.FindPropertyRelative("m_TargetPass");
            SerializedProperty targetSlotProp = element.FindPropertyRelative("m_TargetSlot");

            float singleLine = EditorGUIUtility.singleLineHeight;
            float padding = 2f;
            float labelWidth = 42f;
            float fieldWidth = (rect.width - (labelWidth * 2f) - (padding * 3f)) / 2f;

            float col1X = rect.x + labelWidth + padding;
            float col2X = col1X + fieldWidth + labelWidth + (padding * 2f);

            // Row 1: Source
            var sourceLabelRect = new Rect(rect.x, rect.y + padding, labelWidth, singleLine);
            var sourcePassRect = new Rect(col1X, rect.y + padding, fieldWidth, singleLine);
            var sourceSlotRect = new Rect(col2X, rect.y + padding, fieldWidth, singleLine);

            EditorGUI.LabelField(sourceLabelRect, "Source");
            EditorGUI.PropertyField(sourcePassRect, sourcePassProp, GUIContent.none);
            EditorGUI.PropertyField(sourceSlotRect, sourceSlotProp, GUIContent.none);

            // Row 2: Target
            var targetLabelRect = new Rect(rect.x, rect.y + singleLine + padding, labelWidth, singleLine);
            var targetPassRect = new Rect(col1X, rect.y + singleLine + padding, fieldWidth, singleLine);
            var targetSlotRect = new Rect(col2X, rect.y + singleLine + padding, fieldWidth, singleLine);

            EditorGUI.LabelField(targetLabelRect, "Target");
            EditorGUI.PropertyField(targetPassRect, targetPassProp, GUIContent.none);
            EditorGUI.PropertyField(targetSlotRect, targetSlotProp, GUIContent.none);
        }

        private float GetConnectionElementHeight(int index)
        {
            return (EditorGUIUtility.singleLineHeight * 2) + 6f;
        }

        private void OnAddConnection(ReorderableList list)
        {
            int index = m_ConnectionsProp.arraySize;
            m_ConnectionsProp.InsertArrayElementAtIndex(index);

            // Initialize defaults.
            SerializedProperty element = m_ConnectionsProp.GetArrayElementAtIndex(index);
            if (element != null)
            {
                element.FindPropertyRelative("m_SourcePass").stringValue = string.Empty;
                element.FindPropertyRelative("m_SourceSlot").stringValue = string.Empty;
                element.FindPropertyRelative("m_TargetPass").stringValue = string.Empty;
                element.FindPropertyRelative("m_TargetSlot").stringValue = string.Empty;
            }

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }
}
