using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace HN.HNRP.Editor
{
    [CustomEditor(typeof(HNRenderPipelineRuntimeResources))]
    public class HNRenderPipelineRuntimeResourcesEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 必须 Update/Apply：否则面板上的修改只写入 SerializedProperty 而未回写对象，
            // 关闭/重导入后引用丢失（这正是 compute shader 引用「赋值后仍为 null」的根因）。
            serializedObject.Update();

            if (GUILayout.Button(new GUIContent("Reload All Resources")))
            {
                ReloadAllResources(target as HNRenderPipelineRuntimeResources);
                serializedObject.Update();
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("shaderResources"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("clusterCullingLightCS"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("clusterCullingReflectionProbeCS"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("screenSpaceShadowMaskCS"));

            serializedObject.ApplyModifiedProperties();
        }


        private void ReloadAllResources(RenderPipelineResources resources)
        {
            if (resources == null)
                return;

            ResourceReloader.ReloadAllNullIn(resources, HNRenderPipelineGlobalSettings.HNRenderPipelinePath);

            // ReloadAllNullIn 直接经反射写字段，不标记脏；显式标记并落盘，
            // 保证重新导入 / 构建时引用不丢。
            EditorUtility.SetDirty(resources);
            AssetDatabase.SaveAssets();
        }
    }
}
