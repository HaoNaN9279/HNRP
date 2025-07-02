using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace HN.HNRP.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TransparencyPass))]
    public class TransparencyPassEditor : PassBaseEditor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultDrawColor"), new GUIContent("Default Draw Color"));
            
        }
    }
}
