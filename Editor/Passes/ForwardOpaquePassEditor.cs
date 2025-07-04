using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace HN.HNRP.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ForwardOpaquePass))]
    public class ForwardOpaquePassEditor : PassBaseEditor
    {
        public override void OnInspectorGUI()
        {
            uint layer = serializedObject.FindProperty("renderingLayerMask").uintValue;
            layer = (uint)EditorGUI.MaskField(EditorGUILayout.GetControlRect(), "Rendering Layer Mask", (int)layer, HNRenderPipelineGlobalSettings.Instance.PrefixedRenderingLayerNames);

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("colorTargetIndex"), new GUIContent("Color Target Index"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("depthTargetIndex"), new GUIContent("Depth Target Index"));
            EditorGUI.EndDisabledGroup();
        }
    }
}
