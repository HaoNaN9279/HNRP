// <copyright file="RenderGraphAssetEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
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
    /// <item><see cref="ResourceDefinition"/> list — resource nodes (add, remove, reorder)</item>
    /// <item><see cref="ResourceConnection"/> list — resource↔pass edges (add, remove, edit)</item>
    /// </list>
    /// </summary>
    [CustomEditor(typeof(RenderGraphAsset))]
    public sealed class RenderGraphAssetEditor : UnityEditor.Editor
    {
        #region Fields

        private SerializedProperty m_PassesProp;
        private SerializedProperty m_ConnectionsProp;
        private SerializedProperty m_ResourcesProp;
        private SerializedProperty m_ResourceConnectionsProp;
        private SerializedProperty m_SettingsProp;

        private SerializedProperty m_SHEvalModeProp;
        private SerializedProperty m_AllowHDRProp;

        private ReorderableList m_PassesList;
        private ReorderableList m_ConnectionsList;
        private ReorderableList m_ResourcesList;
        private ReorderableList m_ResourceConnectionsList;

        private bool m_PassesExpanded = true;
        private bool m_ConnectionsExpanded = true;
        private bool m_ResourcesExpanded = true;
        private bool m_ResourceConnectionsExpanded = true;
        private bool m_SettingsExpanded = true;

        #endregion

        #region Unity Messages

        private void OnEnable()
        {
            m_PassesProp = serializedObject.FindProperty("m_Passes");
            m_ConnectionsProp = serializedObject.FindProperty("m_Connections");
            m_ResourcesProp = serializedObject.FindProperty("m_Resources");
            m_ResourceConnectionsProp = serializedObject.FindProperty("m_ResourceConnections");
            m_SettingsProp = serializedObject.FindProperty("m_Settings");

            if (m_SettingsProp != null)
            {
                m_SHEvalModeProp = m_SettingsProp.FindPropertyRelative(nameof(RenderGraphSettings.SHEvalMode));
                m_AllowHDRProp = m_SettingsProp.FindPropertyRelative(nameof(RenderGraphSettings.AllowHDR));
            }

            SetupPassesList();
            SetupConnectionsList();
            SetupResourcesList();
            SetupResourceConnectionsList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScriptField();
            DrawSettingsSection();
            DrawPassesSection();
            DrawConnectionsSection();
            DrawResourcesSection();
            DrawResourceConnectionsSection();

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

        private void DrawResourcesSection()
        {
            m_ResourcesExpanded = EditorGUILayout.Foldout(
                m_ResourcesExpanded,
                $"Resource Nodes ({m_ResourcesProp.arraySize})",
                toggleOnLabelClick: true);

            if (!m_ResourcesExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;
            m_ResourcesList.DoLayoutList();
            EditorGUI.indentLevel--;
        }

        private void DrawResourceConnectionsSection()
        {
            m_ResourceConnectionsExpanded = EditorGUILayout.Foldout(
                m_ResourceConnectionsExpanded,
                $"Resource Connections ({m_ResourceConnectionsProp.arraySize})",
                toggleOnLabelClick: true);

            if (!m_ResourceConnectionsExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;
            m_ResourceConnectionsList.DoLayoutList();
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

        #region ResourceDefinition List Setup

        private void SetupResourcesList()
        {
            m_ResourcesList = new ReorderableList(
                serializedObject, m_ResourcesProp,
                draggable: true,
                displayHeader: true,
                displayAddButton: true,
                displayRemoveButton: true)
            {
                drawHeaderCallback = DrawResourcesHeader,
                drawElementCallback = DrawResourceElement,
                elementHeightCallback = GetResourceElementHeight,
                onAddDropdownCallback = OnAddResourceDropdown,
            };
        }

        private static void DrawResourcesHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Resource Nodes");
        }

        private void DrawResourceElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty element = m_ResourcesProp.GetArrayElementAtIndex(index);
            if (element == null)
            {
                return;
            }

            var asset = (RenderGraphAsset)target;
            ResourceDefinition def = index < asset.Resources.Count ? asset.Resources[index] : null;
            if (def == null)
            {
                return;
            }

            SerializedProperty nameProp = element.FindPropertyRelative("ResourceName");

            float singleLine = EditorGUIUtility.singleLineHeight;
            float padding = 2f;
            float halfWidth = (rect.width - padding) / 2f;

            // Row 1: Name | Kind（类型标签由具体子类派生，非序列化字段）
            var row1NameRect = new Rect(rect.x, rect.y + padding, halfWidth, singleLine);
            var row1KindRect = new Rect(rect.x + halfWidth + padding, rect.y + padding, halfWidth, singleLine);

            EditorGUI.PropertyField(row1NameRect, nameProp, new GUIContent("Name"));
            EditorGUI.LabelField(row1KindRect, def.Kind.ToString());

            // Row 2: Preset dropdown
            var presetRect = new Rect(rect.x, rect.y + singleLine + padding, rect.width, singleLine);
            DrawPresetPopup(presetRect, index, def);
        }

        private float GetResourceElementHeight(int index)
        {
            return (EditorGUIUtility.singleLineHeight * 2) + 6f;
        }

        /// <summary>
        /// 绘制预设下拉菜单。选中后把预设参数值拷贝进当前定义（值复制）。
        /// </summary>
        private void DrawPresetPopup(Rect rect, int index, ResourceDefinition def)
        {
            IReadOnlyList<IResourcePreset> presets = def.Presets;
            if (presets == null || presets.Count == 0)
            {
                return;
            }

            var names = new string[presets.Count];
            for (int i = 0; i < presets.Count; i++)
            {
                names[i] = presets[i].Name;
            }

            int selected = EditorGUI.Popup(rect, "Preset", -1, names);
            if (selected < 0)
            {
                return;
            }

            var asset = (RenderGraphAsset)target;
            Undo.RecordObject(asset, "Apply Resource Preset");
            presets[selected].ApplyTo(asset.Resources[index]);
            EditorUtility.SetDirty(asset);
            serializedObject.Update();
        }

        /// <summary>
        /// Add 按钮下拉菜单：选择要新增的资源类型。
        /// </summary>
        private void OnAddResourceDropdown(Rect buttonRect, ReorderableList list)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Texture"), false,
                () => AddResource(() => new TextureResourceDefinition()));
            menu.AddItem(new GUIContent("Compute Buffer"), false,
                () => AddResource(() => new ComputeBufferResourceDefinition()));
            menu.AddItem(new GUIContent("Renderer List"), false,
                () => AddResource(() => new RendererListResourceDefinition()));
            menu.DropDown(buttonRect);
        }

        /// <summary>
        /// 通过工厂创建指定类型的资源定义并追加到列表末尾。
        /// </summary>
        private void AddResource(Func<ResourceDefinition> factory)
        {
            int index = m_ResourcesProp.arraySize;
            m_ResourcesProp.InsertArrayElementAtIndex(index);

            SerializedProperty element = m_ResourcesProp.GetArrayElementAtIndex(index);
            if (element != null)
            {
                element.managedReferenceValue = factory();
            }

            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region ResourceConnection List Setup

        private void SetupResourceConnectionsList()
        {
            m_ResourceConnectionsList = new ReorderableList(
                serializedObject, m_ResourceConnectionsProp,
                draggable: true,
                displayHeader: true,
                displayAddButton: true,
                displayRemoveButton: true)
            {
                drawHeaderCallback = DrawResourceConnectionsHeader,
                drawElementCallback = DrawResourceConnectionElement,
                elementHeightCallback = GetResourceConnectionElementHeight,
                onAddCallback = OnAddResourceConnection,
            };
        }

        private static void DrawResourceConnectionsHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Resource Connections");
        }

        private void DrawResourceConnectionElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty element = m_ResourceConnectionsProp.GetArrayElementAtIndex(index);
            if (element == null)
            {
                return;
            }

            SerializedProperty resourceProp = element.FindPropertyRelative("ResourceName");
            SerializedProperty passProp = element.FindPropertyRelative("PassName");
            SerializedProperty slotProp = element.FindPropertyRelative("SlotName");

            float singleLine = EditorGUIUtility.singleLineHeight;
            float padding = 2f;
            float labelWidth = 60f;
            float fieldWidth = (rect.width - (labelWidth * 2f) - (padding * 3f)) / 2f;

            float col1X = rect.x + labelWidth + padding;
            float col2X = col1X + fieldWidth + labelWidth + (padding * 2f);

            // Row 1: Resource (full width)
            var resourceLabelRect = new Rect(rect.x, rect.y + padding, labelWidth, singleLine);
            var resourceRect = new Rect(col1X, rect.y + padding, rect.width - labelWidth - padding, singleLine);

            EditorGUI.LabelField(resourceLabelRect, "Resource");
            EditorGUI.PropertyField(resourceRect, resourceProp, GUIContent.none);

            // Row 2: Pass + Slot
            var passLabelRect = new Rect(rect.x, rect.y + singleLine + padding, labelWidth, singleLine);
            var passRect = new Rect(col1X, rect.y + singleLine + padding, fieldWidth, singleLine);
            var slotRect = new Rect(col2X, rect.y + singleLine + padding, fieldWidth, singleLine);

            EditorGUI.LabelField(passLabelRect, "Pass");
            EditorGUI.PropertyField(passRect, passProp, GUIContent.none);
            EditorGUI.PropertyField(slotRect, slotProp, GUIContent.none);
        }

        private float GetResourceConnectionElementHeight(int index)
        {
            return (EditorGUIUtility.singleLineHeight * 2) + 6f;
        }

        private void OnAddResourceConnection(ReorderableList list)
        {
            int index = m_ResourceConnectionsProp.arraySize;
            m_ResourceConnectionsProp.InsertArrayElementAtIndex(index);

            // Initialize defaults.
            SerializedProperty element = m_ResourceConnectionsProp.GetArrayElementAtIndex(index);
            if (element != null)
            {
                element.FindPropertyRelative("ResourceName").stringValue = string.Empty;
                element.FindPropertyRelative("PassName").stringValue = string.Empty;
                element.FindPropertyRelative("SlotName").stringValue = string.Empty;
            }

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }
}
