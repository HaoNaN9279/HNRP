using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.Rendering;

namespace HN.HNRP.Editor
{
    public abstract class HNRenderGraphBaseEditor : UnityEditor.Editor
    {
        protected SerializedProperty passesProperty;

        protected List<UnityEditor.Editor> editors = new List<UnityEditor.Editor>();


        protected abstract void DrawSettings();

        public override void OnInspectorGUI()
        {
            if (passesProperty == null)
            {
                OnEnable();
            }

            serializedObject.Update();
            UpdateEditorList();

            DrawRenderGraphSettings();
            DrawPassesList();
        }


        private void DrawRenderGraphSettings()
        {
            string renderGraphName = serializedObject.targetObject.name;
            string renderGraphType = serializedObject.targetObject.GetType().Name;
            EditorGUILayout.LabelField($"{renderGraphName}({renderGraphType})", EditorStyles.boldLabel);
            var shEvalModeProperty = serializedObject.FindProperty("shEvalMode");
            EditorGUILayout.PropertyField(shEvalModeProperty, new GUIContent("SH Evaluation Mode"));
            EditorGUILayout.Space();
            DrawSettings();
            EditorGUILayout.Space();
        }

        private void DrawPassesList()
        {
            if (passesProperty.arraySize == 0)
            {
                EditorGUILayout.LabelField("No passes defined.");
                return;
            }

            CoreEditorUtils.DrawSplitter();
            for (int i = 0; i < passesProperty.arraySize; i++)
            {
                SerializedProperty passProperty = passesProperty.GetArrayElementAtIndex(i);
                PassBaseEditor passEditor = editors[i] as PassBaseEditor;

                var passObject = passProperty.objectReferenceValue;
                if (passObject != null)
                {
                    bool hasChangedProperties = false;

                    string passName = passObject.name;
                    string title = $"{passName} ({passObject.GetType().Name})";
                    SerializedProperty isExpandedInInspectorProperty = passEditor.serializedObject.FindProperty("isExpandedInInspector");

                    EditorGUI.BeginChangeCheck();
                    
                    isExpandedInInspectorProperty.boolValue = CoreEditorUtils.DrawHeaderFoldout(EditorGUIUtility.TrTextContent(title), isExpandedInInspectorProperty.boolValue, false);
                    hasChangedProperties |= EditorGUI.EndChangeCheck();

                    if (isExpandedInInspectorProperty.boolValue)
                    {
                        EditorGUI.BeginChangeCheck();
                        passEditor.OnInspectorGUI();
                        hasChangedProperties |= EditorGUI.EndChangeCheck();
                        EditorGUILayout.Space();
                    }

                    if (hasChangedProperties)
                    {
                        passEditor.serializedObject.ApplyModifiedProperties();
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(target);
                    }
                }
                
                CoreEditorUtils.DrawSplitter();
            }
        }

        private void OnEnable()
        {
            passesProperty = serializedObject.FindProperty(nameof(HNRenderGraphBase.passes));

            UpdateEditorList();
        }

        private void UpdateEditorList()
        {
            ClearEditorList();
            for (int i = 0; i < passesProperty.arraySize; i++)
            {
                editors.Add(CreateEditor(passesProperty.GetArrayElementAtIndex(i).objectReferenceValue));
            }
        }

        private void ClearEditorList()
        {
            for (int i = editors.Count - 1; i >= 0; i--)
            {
                DestroyImmediate(editors[i]);
            }
            editors.Clear();
        }

    }
}
