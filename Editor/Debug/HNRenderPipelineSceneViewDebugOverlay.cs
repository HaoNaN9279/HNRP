// <copyright file="HNRenderPipelineSceneViewDebugOverlay.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// SceneView 内的渲染调试面板（唯一的调试 UI）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 采用 Unity 2022.3 的 <see cref="Overlay"/> 框架（SceneView 自带 Tools / Grid
    /// 等叠加层用的同一套机制）：可拖动标题栏移动、可折叠、可自由停靠 / 浮动，
    /// GUI 事件由框架正常派发。
    /// </para>
    /// <para>
    /// <b>配置是进程级的</b>（<see cref="RenderDebugManager.Settings"/>）：开关一开，
    /// 运行时所有相机都显示叠层；SceneView 相机有独立开关；反射 / 预览相机永不参与。
    /// </para>
    /// <para>
    /// 面板按<b>数值 / 每像素 / 预览</b>三段组织，每段有自己的启用开关，
    /// 未启用时该段参数为只读（<see cref="EditorGUI.DisabledScope"/>）。
    /// 低频参数（文本区位置、字号、预览边长 / mip、色带越界）不在此暴露，
    /// 统一放在 <see cref="HNRenderPipelineGlobalSettings.DebugDefaults"/>。
    /// </para>
    /// </remarks>
    [Overlay(
        typeof(SceneView),
        OverlayId,
        "HNRP Debug",
        defaultDisplay = true,
        defaultLayout = Layout.Panel,
        defaultDockZone = DockZone.Floating)]
    internal sealed class HNRenderPipelineSceneViewDebugOverlay : Overlay
    {
        /// <summary>叠加层标识（供 <see cref="SceneView.TryGetOverlay"/> 查找）。</summary>
        public const string OverlayId = "hnrp-sceneview-debug";

        /// <summary>面板宽度（像素）。</summary>
        private const float ContentWidth = 286f;

        /// <summary>
        /// 面板固定尺寸。
        /// </summary>
        /// <remarks>
        /// 高度取三段全部展开时的需要值；面板因此不可调整大小，
        /// 不会出现「拖动后无法恢复」的状态。
        /// </remarks>
        private static readonly Vector2 FixedSize = new Vector2(ContentWidth, 430f);

        /// <summary>可预览 pass 的能力缓存。</summary>
        private static List<PassDebugInfo> passInfos;

        /// <summary>色带预览纹理（惰性创建并缓存）。</summary>
        private static Texture2D colorMapPreview;

        /// <summary>色带预览纹理对应的参数签名。</summary>
        private static int colorMapPreviewSignature = int.MinValue;

        /// <summary>「数值」段下标。</summary>
        private const int SectionSingleValues = 0;

        /// <summary>「每像素」段下标。</summary>
        private const int SectionPerPixel = 1;

        /// <summary>「预览」段下标。</summary>
        private const int SectionPreview = 2;

        /// <summary>各段的折叠状态（数值 / 每像素 / 预览）。</summary>
        private static readonly bool[] sectionExpanded = { false, true, false };

        /// <inheritdoc />
        public override void OnCreated()
        {
            // floating 是只读属性（内部 set），改由 defaultDockZone = Floating 与 Undock()
            // 让叠加层以浮动面板出现；位置避开 SceneView 顶部工具栏。
            Undock();
            floatingPosition = new Vector2(12f, 96f);

            // 固定尺寸：min == max 让面板不可调整大小。
            //
            // 悬浮 Overlay 一旦被用户拖动过尺寸，Unity 会把该尺寸持久化到布局里，
            // 之后再也不会回到贴合内容的大小（收起全部折叠段也会留一片空白），
            // 表现为「调整过大小之后无法恢复」。而按内容实时回贴会与容器的布局
            // 约束形成正反馈（尺寸被压小 → 测得更小 → 继续压小），实测会把面板
            // 压到只剩标题栏。因此这里直接锁死尺寸：高度取全部折叠段展开时的值。
            minSize = FixedSize;
            maxSize = FixedSize;
        }

        /// <inheritdoc />
        public override VisualElement CreatePanelContent()
        {
            var root = new IMGUIContainer(DrawContent);
            root.style.minWidth = ContentWidth;
            return root;
        }

        /// <summary>
        /// 显示 / 隐藏 SceneView 调试面板。
        /// </summary>
        [MenuItem("HNRP/Debug/Toggle SceneView Debug Overlay", priority = 102)]
        private static void ToggleOverlay()
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                return;
            }

            if (sceneView.TryGetOverlay(OverlayId, out Overlay overlay))
            {
                overlay.displayed = !overlay.displayed;
                if (overlay.displayed && !overlay.floating)
                {
                    overlay.Undock();
                }

                return;
            }

            SceneView.AddOverlayToActiveView(new HNRenderPipelineSceneViewDebugOverlay());
        }

        // ── 内容 ──

        /// <summary>
        /// 绘制面板内容。
        /// </summary>
        private void DrawContent()
        {
            // 常态化为固定尺寸：清掉历史上被拖动过的持久化尺寸。
            if (size != FixedSize)
            {
                size = FixedSize;
            }

            RenderDebugSettings settings = RenderDebugManager.Settings;

            EditorGUILayout.BeginHorizontal();
            bool enabled = EditorGUILayout.ToggleLeft("Debug", settings.Enabled, GUILayout.Width(64f));

            using (new EditorGUI.DisabledScope(!enabled && !settings.Enabled))
            {
                bool affectsSceneView = EditorGUILayout.ToggleLeft(
                    "SceneView", settings.AffectsSceneView, GUILayout.Width(96f));
                if (affectsSceneView != settings.AffectsSceneView)
                {
                    settings.AffectsSceneView = affectsSceneView;
                }
            }

            EditorGUILayout.EndHorizontal();

            if (enabled != settings.Enabled)
            {
                settings.Enabled = enabled;
            }

            using (new EditorGUI.DisabledScope(!settings.Enabled))
            {
                DrawSingleValuesSection(ref settings);
                DrawPerPixelSection(ref settings);
                DrawPreviewSection(ref settings);
            }

            RenderDebugManager.Settings = settings;
        }

        /// <summary>
        /// 「数值」段：单值注册表概览。
        /// </summary>
        private void DrawSingleValuesSection(ref RenderDebugSettings settings)
        {
            bool on;
            bool expanded = DrawSectionHeader(
                SectionSingleValues, "数值", DebugDisplayMode.SingleValues, ref settings, out on);

            if (!expanded)
            {
                return;
            }

            using (new EditorGUI.DisabledScope(!on))
            {
                EditorGUILayout.LabelField(
                    $"已注册 {RenderDebugManager.EntryCount} 条 / 显示 {RenderDebugManager.VisibleEntryCount} 条",
                    EditorStyles.miniLabel);

                EditorGUILayout.LabelField(
                    "逐条显示开关见 GlobalSettings > Debug", EditorStyles.miniLabel);
            }
        }

        /// <summary>
        /// 「每像素」段：通道选择与颜色映射。
        /// </summary>
        private void DrawPerPixelSection(ref RenderDebugSettings settings)
        {
            bool on;
            bool expanded = DrawSectionHeader(
                SectionPerPixel, "每像素", DebugDisplayMode.PerPixel, ref settings, out on);

            if (!expanded)
            {
                return;
            }

            using (new EditorGUI.DisabledScope(!on))
            {
                EnsurePassInfos();

                var passNames = new List<string>();
                foreach (PassDebugInfo info in passInfos)
                {
                    passNames.Add(info.PassName);
                }

                if (passNames.Count == 0)
                {
                    EditorGUILayout.LabelField("无自描述每像素通道", EditorStyles.miniLabel);
                    return;
                }

                string previousPassName = settings.PerPixelPassName;
                int passIndex = Mathf.Max(0, passNames.IndexOf(previousPassName));
                passIndex = EditorGUILayout.Popup("来源", passIndex, passNames.ToArray());
                settings.PerPixelPassName = passNames[passIndex];

                PassDebugInfo passInfo = passInfos[passIndex];
                var channelNames = new List<string>();
                foreach (DebugChannelDescriptor descriptor in passInfo.Channels)
                {
                    channelNames.Add(descriptor.Name);
                }

                int channelIndex = 0;
                for (int i = 0; i < passInfo.Channels.Count; i++)
                {
                    if (passInfo.Channels[i].ChannelId == settings.PerPixelChannelId)
                    {
                        channelIndex = i;
                        break;
                    }
                }

                channelIndex = EditorGUILayout.Popup("通道", channelIndex, channelNames.ToArray());
                DebugChannelDescriptor channel = passInfo.Channels[Mathf.Max(0, channelIndex)];

                // 切换通道时套用该通道的颜色映射预设：各通道取值域差异极大
                //（[0,1] 材质参数 / [-1,1] 法线 / 米级坐标 / 0..N 群集计数）。
                bool channelChanged = channel.ChannelId != settings.PerPixelChannelId
                    || settings.PerPixelPassName != previousPassName;

                settings.PerPixelChannelId = channel.ChannelId;

                if (channelChanged)
                {
                    settings.ColorMap = channel.DefaultColorMap;
                    colorMapPreviewSignature = int.MinValue;
                }

                settings.ColorMap.Enabled = true;

                EditorGUILayout.BeginHorizontal();
                settings.ColorMap.RangeMin = EditorGUILayout.FloatField(
                    "范围", settings.ColorMap.RangeMin, GUILayout.Width(ContentWidth * 0.5f));
                settings.ColorMap.RangeMax = EditorGUILayout.FloatField(
                    settings.ColorMap.RangeMax, GUILayout.Width(ContentWidth * 0.35f));
                EditorGUILayout.EndHorizontal();

                settings.ColorMap.Channel = (DebugColorChannel)EditorGUILayout.EnumPopup(
                    "抽取", settings.ColorMap.Channel);
                settings.ColorMap.Preset = (DebugColorMapPreset)EditorGUILayout.EnumPopup(
                    "色带", settings.ColorMap.Preset);
                settings.ColorMap.Opacity = EditorGUILayout.Slider(
                    "强度", settings.ColorMap.Opacity, 0f, 1f);

                DrawColorMapPreview(settings.ColorMap);

                if (GUILayout.Button("应用该通道预设"))
                {
                    settings.ColorMap = channel.DefaultColorMap;
                    colorMapPreviewSignature = int.MinValue;
                }
            }
        }

        /// <summary>
        /// 「预览」段：pass 内部渲染目标的角落预览。
        /// </summary>
        private void DrawPreviewSection(ref RenderDebugSettings settings)
        {
            bool on;
            bool expanded = DrawSectionHeader(
                SectionPreview, "预览", DebugDisplayMode.TexturePreview, ref settings, out on);

            if (!expanded)
            {
                return;
            }

            using (new EditorGUI.DisabledScope(!on))
            {
                var keys = new List<string>();
                var labels = new List<string>();
                var sliceCounts = new List<int>();

                CollectPreviewSources(keys, labels, sliceCounts);

                if (keys.Count == 0)
                {
                    EditorGUILayout.LabelField("无可预览的 pass 纹理", EditorStyles.miniLabel);
                    return;
                }

                int index = Mathf.Max(0, keys.IndexOf(settings.PreviewPassName));
                index = EditorGUILayout.Popup("来源", index, labels.ToArray());
                settings.PreviewPassName = keys[index];

                settings.PreviewSlice = EditorGUILayout.IntSlider(
                    "slice", settings.PreviewSlice, 0, Mathf.Max(0, sliceCounts[index] - 1));
            }
        }

        // ── 辅助 ──

        /// <summary>
        /// 绘制段标题：可折叠 + 右侧启用开关。
        /// </summary>
        /// <param name="index">段下标（折叠状态按此记忆）。</param>
        /// <param name="title">段标题。</param>
        /// <param name="section">对应的显示开关位。</param>
        /// <param name="settings">待修改的配置。</param>
        /// <param name="enabled">输出：该段是否启用。</param>
        /// <returns>该段是否展开。</returns>
        private static bool DrawSectionHeader(
            int index,
            string title,
            DebugDisplayMode section,
            ref RenderDebugSettings settings,
            out bool enabled)
        {
            EditorGUILayout.Space(3f);

            enabled = (settings.DisplayMode & section) != 0;

            EditorGUILayout.BeginHorizontal();
            sectionExpanded[index] = EditorGUILayout.Foldout(
                sectionExpanded[index], title, true, EditorStyles.foldoutHeader);
            GUILayout.FlexibleSpace();

            bool next = EditorGUILayout.ToggleLeft(GUIContent.none, enabled, GUILayout.Width(22f));
            EditorGUILayout.EndHorizontal();

            if (next != enabled)
            {
                settings.DisplayMode = next
                    ? settings.DisplayMode | section
                    : settings.DisplayMode & ~section;
                enabled = next;
            }

            return sectionExpanded[index];
        }

        /// <summary>
        /// 收集可预览的 pass 纹理来源。
        /// </summary>
        /// <remarks>
        /// 优先枚举相机当前实际的 pass 实例（预览按实例绑定）；相机尚未渲染过
        ///（脚本重载后 pass 列表为空）时回退为按注册表列出声明了预览能力的类型 ——
        /// 预览解析器同时支持实例名与注册显示名。
        /// </remarks>
        private static void CollectPreviewSources(
            List<string> keys, List<string> labels, List<int> sliceCounts)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            Camera camera = sceneView != null ? sceneView.camera : null;
            HNAdditionalCameraData cameraData = camera != null
                ? camera.GetComponent<HNAdditionalCameraData>()
                : null;

            if (cameraData != null)
            {
                foreach (Pass pass in cameraData.GetRuntimePasses())
                {
                    if (pass is IPassDebugPreviewProvider provider
                        && provider.TryGetDebugPreview(out Texture texture, out int slices, out _)
                        && texture != null)
                    {
                        keys.Add(pass.PassName);
                        labels.Add($"{pass.PassName} ({texture.dimension})");
                        sliceCounts.Add(Mathf.Max(1, slices));
                    }
                }
            }

            if (keys.Count > 0)
            {
                return;
            }

            foreach (string passName in PassRegistry.GetAllPassNames())
            {
                System.Type passType = PassRegistry.GetPassType(passName);
                if (passType != null && typeof(IPassDebugPreviewProvider).IsAssignableFrom(passType))
                {
                    keys.Add(passName);
                    labels.Add(passName);
                    sliceCounts.Add(1);
                }
            }
        }

        /// <summary>
        /// 绘制色带预览条。
        /// </summary>
        private static void DrawColorMapPreview(in DebugColorMapSettings settings)
        {
            int signature = settings.ComputeSignature();
            if (colorMapPreview == null || colorMapPreviewSignature != signature)
            {
                if (colorMapPreview != null)
                {
                    Object.DestroyImmediate(colorMapPreview);
                }

                colorMapPreview = DebugColorMap.CreateLut(settings);
                colorMapPreviewSignature = signature;
            }

            Rect rect = GUILayoutUtility.GetRect(48f, 14f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawPreviewTexture(rect, colorMapPreview);
        }

        /// <summary>
        /// 惰性收集 pass 调试能力（通道是编译期固定信息，扫描一次即可）。
        /// </summary>
        private static void EnsurePassInfos()
        {
            if (passInfos != null && passInfos.Count > 0)
            {
                return;
            }

            PassRegistry.RegisterAll();
            passInfos = PassDebugRegistry.EnumerateAll();
        }
    }
}
