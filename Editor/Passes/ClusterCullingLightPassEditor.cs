using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HN.HNRP.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ClusterCullingLightPass))]
    public class ClusterCullingLightPassEditor : PassBaseEditor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("clusterCullingLightCS"), new GUIContent("Cluster Culling Light Compute Shader"));
        }
    }
}
