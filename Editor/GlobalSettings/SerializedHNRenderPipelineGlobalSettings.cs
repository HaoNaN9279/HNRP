using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Rendering;
using UnityEditorInternal;
using UnityEditor;

namespace HN.HNRP.Editor
{
    public class SerializedHNRenderPipelineGlobalSettings : ISerializedRenderPipelineGlobalSettings
    {
        public SerializedHNRenderPipelineGlobalSettings(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            foreach(var currentSetting in serializedObject.targetObjects)
            {
                if(currentSetting is HNRenderPipelineGlobalSettings hnrpSettings)
                    serializedSettings.Add(hnrpSettings);
                else
                    throw new System.Exception($"Target object has an invalid object, objects must be of type {typeof(HNRenderPipelineGlobalSettings)}");
            }

            renderingLayerNames = serializedObject.FindProperty("renderingLayerNames");

            renderingLayerNameList = new ReorderableList(serializedObject, renderingLayerNames, false, false, true, true)
            {
                drawElementCallback = OnDrawElement,
                onCanRemoveCallback = (ReorderableList list) => list.IsSelected(list.count - 1) && !list.IsSelected(0),
                onCanAddCallback = (ReorderableList list) => list.count < 32,
                onAddCallback = OnAddElement,
            };

            cameraPipelineConfigs = serializedObject.FindProperty("m_CameraPipelineConfigs");

            cameraPipelineConfigsList = new ReorderableList(serializedObject, cameraPipelineConfigs, true, true, true, true)
            {
                drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, "Camera Pipeline Configs");
                },
                drawElementCallback = OnDrawCameraPipelineConfigElement,
                onAddDropdownCallback = OnAddCameraPipelineConfigDropdown,
                onRemoveCallback = OnRemoveCameraPipelineConfig,
            };

            var runtimeResources = serializedObject.FindProperty("hnRenderPipelineRuntimeResources")?.objectReferenceValue as HNRenderPipelineRuntimeResources;
            UnityEditor.Editor.CreateCachedEditor(runtimeResources, null, ref runtimeResourcesEditor);

            var editorResources = serializedObject.FindProperty("hnRenderPipelineEditorResources")?.objectReferenceValue as HNRenderPipelineEditorResources;
            UnityEditor.Editor.CreateCachedEditor(editorResources, null, ref editorResourcesEditor);
        }


        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect.y += 2.5f;
            SerializedProperty element = renderingLayerNameList.serializedProperty.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(rect, element, EditorGUIUtility.TrTextContent($"Layer {index}"), true);
            if(element.stringValue == "")
            {
                element.stringValue = GetDefaultLayerName(index);
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void OnAddElement(ReorderableList list)
        {
            int index = list.count;
            list.serializedProperty.arraySize = list.count + 1;
            list.serializedProperty.GetArrayElementAtIndex(index).stringValue = GetDefaultLayerName(index);
        }

        private string GetDefaultLayerName(int index)
        {
            return index == 0 ? "Default" : $"Layer {index}";
        }

        private void OnDrawCameraPipelineConfigElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect.y += 2.5f;
            SerializedProperty element = cameraPipelineConfigsList.serializedProperty.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                element,
                GUIContent.none);
        }

        private void OnAddCameraPipelineConfigDropdown(Rect buttonRect, ReorderableList list)
        {
            var menu = new GenericMenu();

            var guids = AssetDatabase.FindAssets("t:CameraPipelineConfig");
            if (guids.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("No CameraPipelineConfig assets found"));
            }
            else
            {
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var config = AssetDatabase.LoadAssetAtPath<CameraPipelineConfig>(path);
                    menu.AddItem(
                        new GUIContent(config.name),
                        false,
                        () => OnSelectCameraPipelineConfig(config));
                }
            }

            menu.DropDown(buttonRect);
        }

        private void OnSelectCameraPipelineConfig(CameraPipelineConfig config)
        {
            int index = cameraPipelineConfigs.arraySize;
            cameraPipelineConfigs.InsertArrayElementAtIndex(index);
            var element = cameraPipelineConfigs.GetArrayElementAtIndex(index);
            element.objectReferenceValue = config;
            serializedObject.ApplyModifiedProperties();
        }

        private void OnRemoveCameraPipelineConfig(ReorderableList list)
        {
            int index = list.index;
            if (index >= 0 && index < cameraPipelineConfigs.arraySize)
            {
                cameraPipelineConfigs.DeleteArrayElementAtIndex(index);
                serializedObject.ApplyModifiedProperties();
            }
        }


        public SerializedObject serializedObject { get; }
        public SerializedProperty shaderVariantLogLevel { get; }
        public SerializedProperty exportShaderVariants { get; }

        public SerializedProperty renderingLayerNames;
        public ReorderableList renderingLayerNameList;

        public SerializedProperty cameraPipelineConfigs;
        public ReorderableList cameraPipelineConfigsList;

        public UnityEditor.Editor runtimeResourcesEditor;
        public UnityEditor.Editor editorResourcesEditor;


        private List<HNRenderPipelineGlobalSettings> serializedSettings = new List<HNRenderPipelineGlobalSettings>();
    }
}
