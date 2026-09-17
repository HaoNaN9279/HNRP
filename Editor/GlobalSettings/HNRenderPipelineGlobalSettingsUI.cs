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

        internal static readonly CED.IDrawer DebugSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.debugLabel)),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawDebug),
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

        internal static void DrawDebug(SerializedHNRenderPipelineGlobalSettings serialized, UnityEditor.Editor owner)
        {
            using (new EditorGUI.IndentLevelScope())
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                SerializedProperty support = serialized.serializedObject?.FindProperty("SupportRuntimeDebugDisplay");
                if (support != null)
                {
                    EditorGUILayout.PropertyField(support, Styles.supportRuntimeDebugLabel);
                    EditorGUILayout.HelpBox(
                        "渲染调试在 Player 构建中的第二层开关。\n" +
                        "编译期需定义 HNRP_DEBUG_DISPLAY 才会把调试代码纳入构建；\n" +
                        "该开关再决定运行时是否真的允许调试显示生效。\n" +
                        "Editor 内无需定义宏，也不受该开关限制。",
                        MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "未找到 SupportRuntimeDebugDisplay 序列化字段。", MessageType.Warning);
                }

                // 低频的调试布局参数：不进 SceneView 浮动面板，统一在此配置。
                SerializedProperty defaults = serialized.serializedObject?.FindProperty("DebugDefaults");
                if (defaults != null)
                {
                    EditorGUILayout.Space(2f);
                    EditorGUILayout.PropertyField(defaults, Styles.debugDefaultsLabel, includeChildren: true);
                    EditorGUILayout.HelpBox(
                        "文本区位置 / 字号 / 预览边长 / 预览 mip / 色带越界处理。\n" +
                        "这些是设一次就很少再改的低频参数，SceneView 上的调试面板不再暴露它们。",
                        MessageType.None);
                }

                DrawRegisteredValues(serialized);

                if (changed.changed)
                {
                    serialized.serializedObject?.ApplyModifiedProperties();
                }
            }
        }


        /// <summary>值列表的滚动位置。</summary>
        private static Vector2 registeredValuesScroll;

        /// <summary>值列表的展开状态。</summary>
        private static bool registeredValuesExpanded = true;

        /// <summary>各分组节点的展开状态（按分组路径索引）。</summary>
        private static readonly Dictionary<string, bool> valueGroupExpanded = new();

        /// <summary>
        /// 按注册点分组列出当前已注册的全部单值，并提供逐条 / 整组的显示开关。
        /// </summary>
        /// <remarks>
        /// 列表内容来自运行时的 <see cref="RenderDebugManager"/> 注册表（每次 OnGUI 重新取），
        /// 显示开关写入 <c>HiddenDebugValues</c> 以便跨脚本重载 / 会话保留。
        /// </remarks>
        internal static void DrawRegisteredValues(SerializedHNRenderPipelineGlobalSettings serialized)
        {
            EditorGUILayout.Space(4f);
            registeredValuesExpanded = EditorGUILayout.Foldout(
                registeredValuesExpanded,
                $"Registered Debug Values ({RenderDebugManager.EntryCount})",
                true);

            if (!registeredValuesExpanded)
            {
                return;
            }

            IReadOnlyList<DebugDisplayEntry> entries = RenderDebugManager.GetEntries();
            if (entries.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "暂无已注册的单值。在运行时代码里调用 "
                    + "HNRPDebug.SetValue(\"分组/子分组\", \"名称\", value) 注册。",
                    MessageType.None);
                return;
            }

            SerializedProperty hidden = serialized.serializedObject?.FindProperty("HiddenDebugValues");

            DebugValueGroupNode root = BuildValueTree(entries);

            registeredValuesScroll = EditorGUILayout.BeginScrollView(
                registeredValuesScroll, GUILayout.MaxHeight(300f));

            DrawValueGroupNode(root, hidden);

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 按分组路径构建值列表树。
        /// </summary>
        /// <param name="entries">当前注册条目。</param>
        /// <returns>树根（根自身不显示，只承载顶层分组与未分组值）。</returns>
        private static DebugValueGroupNode BuildValueTree(IReadOnlyList<DebugDisplayEntry> entries)
        {
            var root = new DebugValueGroupNode { Name = string.Empty, Path = string.Empty };

            for (int i = 0; i < entries.Count; i++)
            {
                DebugDisplayEntry entry = entries[i];
                DebugValueGroupNode node = root;
                string path = string.Empty;

                string[] segments = entry.GroupSegments;
                for (int level = 0; level < segments.Length; level++)
                {
                    path = path.Length == 0 ? segments[level] : $"{path}/{segments[level]}";

                    DebugValueGroupNode child = null;
                    for (int c = 0; c < node.Children.Count; c++)
                    {
                        if (node.Children[c].Name == segments[level])
                        {
                            child = node.Children[c];
                            break;
                        }
                    }

                    if (child == null)
                    {
                        child = new DebugValueGroupNode { Name = segments[level], Path = path };
                        node.Children.Add(child);
                    }

                    node = child;
                }

                node.Values.Add(entry);
            }

            return root;
        }

        /// <summary>
        /// 递归绘制分组节点：分组标题行带「全开 / 全关」，值行前面的 Toggle 控制单条显示。
        /// </summary>
        private static void DrawValueGroupNode(DebugValueGroupNode node, SerializedProperty hidden)
        {
            for (int i = 0; i < node.Children.Count; i++)
            {
                DebugValueGroupNode child = node.Children[i];
                int descendantCount = CountValues(child);

                if (!valueGroupExpanded.TryGetValue(child.Path, out bool expanded))
                {
                    expanded = true;
                    valueGroupExpanded[child.Path] = true;
                }

                EditorGUILayout.BeginHorizontal();
                expanded = EditorGUILayout.Foldout(
                    expanded, $"{child.Name} ({descendantCount})", true, EditorStyles.foldoutHeader);
                valueGroupExpanded[child.Path] = expanded;

                if (GUILayout.Button("全开", GUILayout.Width(44f)))
                {
                    RenderDebugManager.SetGroupVisible(child.Path, true);
                    SyncHiddenValues(hidden);
                }

                if (GUILayout.Button("全关", GUILayout.Width(44f)))
                {
                    RenderDebugManager.SetGroupVisible(child.Path, false);
                    SyncHiddenValues(hidden);
                }

                EditorGUILayout.EndHorizontal();

                if (!expanded)
                {
                    continue;
                }

                using (new EditorGUI.IndentLevelScope())
                {
                    DrawValueGroupNode(child, hidden);
                }
            }

            for (int i = 0; i < node.Values.Count; i++)
            {
                DebugDisplayEntry entry = node.Values[i];

                EditorGUILayout.BeginHorizontal();
                bool visible = EditorGUILayout.ToggleLeft(entry.Name, entry.Visible, GUILayout.Width(200f));
                EditorGUILayout.LabelField(
                    entry.Type.ToString(), EditorStyles.miniLabel, GUILayout.Width(56f));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(entry.Source, EditorStyles.miniLabel, GUILayout.Width(120f));
                EditorGUILayout.EndHorizontal();

                if (visible != entry.Visible)
                {
                    RenderDebugManager.SetVisible(entry.Label, visible);
                    SyncHiddenValues(hidden);
                }
            }
        }

        /// <summary>统计一个分组节点（含子分组）下的值数量。</summary>
        private static int CountValues(DebugValueGroupNode node)
        {
            int count = node.Values.Count;
            for (int i = 0; i < node.Children.Count; i++)
            {
                count += CountValues(node.Children[i]);
            }

            return count;
        }

        /// <summary>值列表的分组节点。</summary>
        private sealed class DebugValueGroupNode
        {
            public string Name;
            public string Path;
            public readonly List<DebugValueGroupNode> Children = new();
            public readonly List<DebugDisplayEntry> Values = new();
        }

        /// <summary>
        /// 把当前「不可见」的标签集合写回 GlobalSettings，使其跨会话保留。
        /// </summary>
        private static void SyncHiddenValues(SerializedProperty hidden)
        {
            if (hidden == null)
            {
                return;
            }

            hidden.ClearArray();

            IReadOnlyList<DebugDisplayEntry> entries = RenderDebugManager.GetEntries();
            int index = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Visible)
                {
                    continue;
                }

                hidden.InsertArrayElementAtIndex(index);
                hidden.GetArrayElementAtIndex(index).stringValue = entries[i].Label;
                index++;
            }

            hidden.serializedObject?.ApplyModifiedProperties();
        }

        public static readonly CED.IDrawer Inspector = CED.Group(
            RenderingLayerNamesSection,
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            RuntimeResourcesSection,
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            EditorResourcesSection,
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            DebugSection,
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );


        internal class Styles
        {
            public static readonly GUIContent renderingLayersLabel = EditorGUIUtility.TrTextContent("Rendering Layers", "The list of rendering layer names.");
            public static readonly GUIContent runtimeResourcesLabel = EditorGUIUtility.TrTextContent("Runtime Resources", "Runtime Resources");
            public static readonly GUIContent editorResourcesLabel = EditorGUIUtility.TrTextContent("Editor Resources", "Editor Resources");
            public static readonly GUIContent debugLabel = EditorGUIUtility.TrTextContent("Debug", "Rendering debug display settings.");
            public static readonly GUIContent supportRuntimeDebugLabel = EditorGUIUtility.TrTextContent(
                "Support Runtime Debug Display",
                "Allow the rendering debug display to be enabled at runtime in non-Editor players.");
            public static readonly GUIContent debugDefaultsLabel = EditorGUIUtility.TrTextContent(
                "Debug Defaults",
                "Low-frequency rendering debug layout defaults (text overlay rect, font scale, preview size/mip, color map wrap).");
        }
    }
}
