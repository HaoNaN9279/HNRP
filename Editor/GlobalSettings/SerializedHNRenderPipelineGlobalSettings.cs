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


        public SerializedObject serializedObject { get; }
        public SerializedProperty shaderVariantLogLevel { get; }
        public SerializedProperty exportShaderVariants { get; }

        public SerializedProperty renderingLayerNames;
        public ReorderableList renderingLayerNameList;

        public UnityEditor.Editor runtimeResourcesEditor;
        public UnityEditor.Editor editorResourcesEditor;


        private List<HNRenderPipelineGlobalSettings> serializedSettings = new List<HNRenderPipelineGlobalSettings>();
    }
}
