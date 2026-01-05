using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace HN.HNRP.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ForwardPlusLightCullingPass))]
    public class ForwardPlusLightCullingPassEditor : PassBaseEditor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("forwardPlusZBinsBufferIndex"), new GUIContent("Forward Plus ZBins Buffer Index"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("forwardPlusTileMasksBufferIndex"), new GUIContent("Forward Plus Tile Masks Buffer Index"));
            EditorGUI.EndDisabledGroup();
        }
    }
}
