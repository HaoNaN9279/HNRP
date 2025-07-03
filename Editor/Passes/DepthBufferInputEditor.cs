using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HN.HNRP.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(DepthBufferInput))]
    public class DepthBufferInputEditor : PassBaseEditor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("textureScale"), new GUIContent("Texture Scale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("depthBits"), new GUIContent("Depth Bits"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("clearBuffer"), new GUIContent("Clear Buffer"));
        }
    }
}
