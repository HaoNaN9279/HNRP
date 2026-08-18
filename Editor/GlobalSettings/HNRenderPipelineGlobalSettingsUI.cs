using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;

namespace HN.HNRP.Editor
{
    using CED = CoreEditorDrawer<SerializedHNRenderPipelineGlobalSettings>;

    internal class HNRenderPipelineGlobalSettingsUI
    {
        internal static readonly CED.IDrawer RenderingLayerNamesSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.renderingLayersLabel, contextAction: pos => OnContentClickRenderingLayerNames(pos, serialized))),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawRenderingLayerNames),
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );

        internal static readonly CED.IDrawer RuntimeResourcesSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.runtimeResourcesLabel)),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawRuntimeResources),
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );

        internal static readonly CED.IDrawer EditorResourcesSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.editorResourcesLabel)),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawEditorResources),
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );


        internal static void DrawRenderingLayerNames(SerializedHNRenderPipelineGlobalSettings serialized, UnityEditor.Editor owner)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                using (var changed = new EditorGUI.ChangeCheckScope())
                {
                    serialized.renderingLayerNameList.DoLayoutList();

                    if(changed.changed)
                    {
                        serialized.serializedObject?.ApplyModifiedProperties();
                        if(serialized.serializedObject?.targetObject is HNRenderPipelineGlobalSettings hnrpGlobalSettings)
                            hnrpGlobalSettings.UpdateRenderingLayerNames();
                    }
                }
            }
        }

        internal static void OnContentClickRenderingLayerNames(Vector2 position, SerializedHNRenderPipelineGlobalSettings serialized)
        {
            var menu = new GenericMenu();
            menu.AddItem(CoreEditorStyles.resetButtonLabel, false, () =>
            {
                var globalSettings = (serialized.serializedObject.targetObject as HNRenderPipelineGlobalSettings);
                globalSettings.ResetRenderingLayerNames();
            });
            menu.DropDown(new Rect(position, Vector2.zero));
        }

        internal static void DrawRuntimeResources(SerializedHNRenderPipelineGlobalSettings serialized, UnityEditor.Editor owner)
        {
            using(new EditorGUI.IndentLevelScope())
            {
                serialized.runtimeResourcesEditor.OnInspectorGUI();
            }
        }

        internal static void DrawEditorResources(SerializedHNRenderPipelineGlobalSettings serialized, UnityEditor.Editor owner)
        {
            using(new EditorGUI.IndentLevelScope())
            {
                serialized.editorResourcesEditor.OnInspectorGUI();
            }
        }


        public static readonly CED.IDrawer Inspector = CED.Group(
            RenderingLayerNamesSection,
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            RuntimeResourcesSection,
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            EditorResourcesSection,
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );


        internal class Styles
        {
            public static readonly GUIContent renderingLayersLabel = EditorGUIUtility.TrTextContent("Rendering Layers", "The list of rendering layer names.");
            public static readonly GUIContent runtimeResourcesLabel = EditorGUIUtility.TrTextContent("Runtime Resources", "Runtime Resources");
            public static readonly GUIContent editorResourcesLabel = EditorGUIUtility.TrTextContent("Editor Resources", "Editor Resources");
        }
    }
}
