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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("renderingLayerMask"), new GUIContent("Rendering Layer Mask"));
        }
    }
}
