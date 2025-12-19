using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;

namespace HN.HNRP.Editor
{
    public static class HNRenderPipelineEditorUtils
    {
        public static void DrawRenderingLayerMask(SerializedProperty property, GUIContent style)
        {
            // if(property == null)
            //     return;
            
            Rect controlRect = EditorGUILayout.GetControlRect(true);
            int renderingLayer = property.intValue;

            string[] renderingLayerMaskNames = HNRenderPipelineGlobalSettings.Instance.RenderingLayerNames;
            int maskCount = (int)Mathf.Log(renderingLayer, 2) + 1;
            if (renderingLayerMaskNames.Length < maskCount && maskCount <= 32)
            {
                var newRenderingLayerMaskNames = new string[maskCount];
                for (int i = 0; i < maskCount; ++i)
                {
                    newRenderingLayerMaskNames[i] = i < renderingLayerMaskNames.Length ? renderingLayerMaskNames[i] : $"Unused Layer {i}";
                }
                renderingLayerMaskNames = newRenderingLayerMaskNames;

                EditorGUILayout.HelpBox($"One or more of the Rendering Layers is not defined in the Universal Global Settings asset.", MessageType.Warning);
            }

            EditorGUI.BeginProperty(controlRect, style, property);

            EditorGUI.BeginChangeCheck();
            renderingLayer = EditorGUI.MaskField(controlRect, style, renderingLayer, renderingLayerMaskNames);

            if (EditorGUI.EndChangeCheck())
                property.uintValue = (uint)renderingLayer;

            EditorGUI.EndProperty();
        }
    }
}
