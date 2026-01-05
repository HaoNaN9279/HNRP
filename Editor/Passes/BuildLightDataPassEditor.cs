using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace HN.HNRP.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BuildLightDataPass))]
    public class BuildLightDataPassEditor : PassBaseEditor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lightDatasBufferIndex"), new GUIContent("LightDatas Buffer Index"));
            EditorGUI.EndDisabledGroup();
        }
    }
}
