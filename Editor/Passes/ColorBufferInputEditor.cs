using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace HN.HNRP.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ColorBufferInput))]
    public class ColorBufferInputEditor : PassBaseEditor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("textureScale"), new GUIContent("Texture Scale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("colorFormat"), new GUIContent("Color Format"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("clearBuffer"), new GUIContent("Clear Buffer"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("clearColor"), new GUIContent("Clear Color"));

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("colorTargetIndex"), new GUIContent("Color Target Index"));
            EditorGUI.EndDisabledGroup();
        }
    }
}
