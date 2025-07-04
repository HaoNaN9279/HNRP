using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace HN.HNRP.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(EditorWireOverlayPass))]
    public class EditorWireOverlayPassEditor : PassBaseEditor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("colorTargetIndex"), new GUIContent("Color Target Index"));
            EditorGUI.EndDisabledGroup();
        }
    }
}
