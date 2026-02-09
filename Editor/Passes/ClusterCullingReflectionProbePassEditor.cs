using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HN.HNRP.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ClusterCullingReflectionProbePass))]
    public class ClusterCullingReflectionProbePassEditor : PassBaseEditor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("clusterCullingReflectionProbeCS"), new GUIContent("Cluster Culling Reflection Probe Compute Shader"));
        }
    }
}
