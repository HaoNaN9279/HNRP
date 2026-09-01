// <copyright file="RenderGraphAssetEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditorInternal;
using UnityEngine;
using HN.HNRP;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="RenderGraphAsset"/> using IMGUI.
    /// Passes and resources are displayed as read-only foldouts that inline-edit
    /// their parameters directly on the panel. Each foldout header carries a preset
    /// menu (three-dot button) on its right, listing that definition type's presets.
    /// No add / remove controls are exposed — the graph structure is fixed.
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

        private ReorderableList m_ConnectionsList;

        private readonly List<bool> m_PassFoldouts = new();

        private bool m_PassesExpanded = true;
        private bool m_ConnectionsExpanded = true;
        private bool m_SettingsExpanded = true;

        /// <summary>
        /// Set when a preset is applied. Serialized properties are re-initialized on
        /// the next repaint so the freshly-copied preset values show up without the
        /// stale <see cref="SerializedProperty"/> snapshot overwriting them on apply.
        /// </summary>
        private bool m_RefreshPending;

        #endregion

        #region Unity Messages

        private void OnEnable()
        {
            InitProperties();
            SetupConnectionsList();
        }

        public override void OnInspectorGUI()
        {
            if (m_RefreshPending)
            {
                InitProperties();
                SetupConnectionsList();
                m_RefreshPending = false;
            }

            serializedObject.Update();

            DrawScriptField();
            DrawSettingsSection();
            DrawPassesSection();
            DrawConnectionsSection();

            if (!m_RefreshPending)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        #endregion

        #region Property Init

        private void InitProperties()
        {
            m_PassesProp = serializedObject.FindProperty("m_Passes");
            m_ConnectionsProp = serializedObject.FindProperty("m_Connections");
            m_SettingsProp = serializedObject.FindProperty("m_Settings");

            if (m_SettingsProp != null)
            {
                m_SHEvalModeProp = m_SettingsProp.FindPropertyRelative(nameof(RenderGraphSettings.SHEvalMode));
                m_AllowHDRProp = m_SettingsProp.FindPropertyRelative(nameof(RenderGraphSettings.AllowHDR));
            }
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

            EnsureFoldoutCount(m_PassFoldouts, m_PassesProp.arraySize);

            for (int i = 0; i < m_PassesProp.arraySize; i++)
            {
                DrawPassFoldout(i);
                if (m_RefreshPending)
                {
                    EditorGUI.indentLevel--;
                    return;
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawPassFoldout(int index)
        {
            SerializedProperty element = m_PassesProp.GetArrayElementAtIndex(index);
            if (element == null)
            {
                return;
            }

            var asset = (RenderGraphAsset)target;
            Pass pass = index < asset.Passes.Count ? asset.Passes[index] : null;

            string instanceName = pass != null ? pass.PassName : string.Empty;
            string typeName = pass != null ? pass.GetType().Name : "?";

            string title = string.IsNullOrEmpty(instanceName) ? $"Pass {index}" : instanceName;

            EnsureFoldoutCount(m_PassFoldouts, m_PassesProp.arraySize);

            PassEditor editor = pass != null ? PassEditorRegistry.GetEditor(pass.GetType()) : null;

            Action<Vector2> contextAction = (editor != null && editor.Presets.Count > 0)
                ? (pos => ShowPassPresetMenu(pos, editor, pass))
                : null;

            m_PassFoldouts[index] = CoreEditorUtils.DrawHeaderFoldout(
                new GUIContent($"{title} ({typeName})"),
                m_PassFoldouts[index],
                contextAction: contextAction);

            if (!m_PassFoldouts[index])
            {
                return;
            }

            EditorGUI.indentLevel++;
            editor?.DrawPassGUI(element, pass);
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

        #region Preset Menu

        /// <summary>
        /// 弹出 Pass 预设菜单：列出该 Pass 类型编辑器中定义的所有预设，选择后套用。
        /// </summary>
        private void ShowPassPresetMenu(Vector2 position, PassEditor editor, Pass pass)
        {
            IReadOnlyList<IPassPreset> presets = editor?.Presets;
            if (presets == null || presets.Count == 0)
            {
                return;
            }

            var menu = new GenericMenu();
            for (int i = 0; i < presets.Count; i++)
            {
                int presetIndex = i;
                menu.AddItem(new GUIContent(presets[i].Name), false,
                    () => ApplyPassPreset(editor, pass, presetIndex));
            }

            menu.DropDown(new Rect(position, Vector2.zero));
        }

        private void ApplyPassPreset(PassEditor editor, Pass pass, int presetIndex)
        {
            IReadOnlyList<IPassPreset> presets = editor?.Presets;
            if (presets == null || pass == null || presetIndex < 0 || presetIndex >= presets.Count)
            {
                return;
            }

            var asset = (RenderGraphAsset)target;
            Undo.RecordObject(asset, "Apply Pass Preset");
            presets[presetIndex].ApplyTo(pass);
            EditorUtility.SetDirty(asset);
            m_RefreshPending = true;
        }

        #endregion

        #region Serialized Reference Fields

        /// <summary>
        /// 绘制一个 <c>[SerializeReference]</c> 引用对象的全部序列化字段
        /// （限制在引用对象范围内，避免越界到兄弟字段）。
        /// </summary>
        private static void DrawSerializedReferenceFields(SerializedProperty referenceProp)
        {
            SerializedProperty iterator = referenceProp.Copy();
            SerializedProperty end = iterator.GetEndProperty();

            if (iterator.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(iterator, end))
                    {
                        break;
                    }

                    EditorGUILayout.PropertyField(iterator, true);
                }
                while (iterator.NextVisible(false));
            }
        } 

        #endregion

        #region SlotConnection List Setup (read-only)

        private void SetupConnectionsList()
        {
            m_ConnectionsList = new ReorderableList(
                serializedObject, m_ConnectionsProp,
                draggable: false,
                displayHeader: true,
                displayAddButton: false,
                displayRemoveButton: false)
            {
                drawHeaderCallback = DrawConnectionsHeader,
                drawElementCallback = DrawConnectionElement,
                elementHeightCallback = GetConnectionElementHeight,
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

        #endregion

        #region Helpers

        private static void EnsureFoldoutCount(List<bool> foldouts, int count)
        {
            while (foldouts.Count < count)
            {
                foldouts.Add(false);
            }
        }

        #endregion
    }
}
