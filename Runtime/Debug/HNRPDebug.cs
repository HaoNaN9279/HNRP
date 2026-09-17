// <copyright file="HNRPDebug.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace HN.HNRP
{
    /// <summary>
    /// 渲染调试的公共 C# API 门面。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 调试配置是<b>进程级</b>的（<see cref="RenderDebugSettings"/>）：一旦开启，
    /// 运行时的所有相机都会显示叠层，SceneView 相机可单独关闭；反射 / 预览相机
    /// 永不参与。因此本类不接收 <see cref="Camera"/> 参数。
    /// </para>
    /// <para>
    /// 所有接口只写 CPU 帧缓存（<see cref="RenderDebugManager"/> 的注册表）与全局配置，
    /// 真正的 GPU 工作在帧内由 <see cref="DebugDisplayPass"/> 与各生产者 pass 完成；
    /// 因此这些调用没有同步开销，也不产生 GPU 回读。
    /// </para>
    /// <para>
    /// 调试未编译（无 <c>HNRP_DEBUG_DISPLAY</c> 且非 Editor）或
    /// <see cref="HNRenderPipelineGlobalSettings.SupportRuntimeDebugDisplay"/> 未开启时，
    /// 显示部分为空操作；配置写入仍然有效。
    /// </para>
    /// </remarks>
    public static class HNRPDebug
    {
        /// <summary>取当前全局配置的副本。</summary>
        /// <returns>全局配置副本。</returns>
        public static RenderDebugSettings GetSettings()
        {
            return RenderDebugManager.Settings;
        }

        /// <summary>整体覆盖全局配置。</summary>
        /// <param name="settings">要写入的配置。</param>
        public static void SetSettings(in RenderDebugSettings settings)
        {
            RenderDebugManager.Settings = settings;
        }

        /// <summary>渲染调试总开关。</summary>
        /// <param name="enabled">是否启用。</param>
        public static void SetEnabled(bool enabled)
        {
            RenderDebugManager.MutateSettings((ref RenderDebugSettings s) => s.Enabled = enabled);
        }

        /// <summary>当前是否已开启渲染调试。</summary>
        public static bool IsEnabled => RenderDebugManager.Settings.Enabled;

        /// <summary>SceneView 相机是否参与调试显示。</summary>
        /// <param name="enabled">是否启用。</param>
        public static void SetSceneViewEnabled(bool enabled)
        {
            RenderDebugManager.MutateSettings((ref RenderDebugSettings s) => s.AffectsSceneView = enabled);
        }

        /// <summary>整体设置三段式显示开关。</summary>
        /// <param name="mode">显示内容开关。</param>
        public static void SetDisplayMode(DebugDisplayMode mode)
        {
            RenderDebugManager.MutateSettings((ref RenderDebugSettings s) => s.DisplayMode = mode);
        }

        /// <summary>单独开关「数值」段。</summary>
        /// <param name="enabled">是否启用。</param>
        public static void SetSingleValuesEnabled(bool enabled)
        {
            SetSectionEnabled(DebugDisplayMode.SingleValues, enabled);
        }

        /// <summary>单独开关「每像素」段。</summary>
        /// <param name="enabled">是否启用。</param>
        public static void SetPerPixelEnabled(bool enabled)
        {
            SetSectionEnabled(DebugDisplayMode.PerPixel, enabled);
        }

        /// <summary>单独开关「预览」段。</summary>
        /// <param name="enabled">是否启用。</param>
        public static void SetTexturePreviewEnabled(bool enabled)
        {
            SetSectionEnabled(DebugDisplayMode.TexturePreview, enabled);
        }

        // ── 单值 ──

        /// <summary>注册或更新一个整数单值。</summary>
        /// <param name="group">
        /// 分组路径（<c>"/"</c> 分隔，可多级，如 <c>"Animation/Value"</c>）；空串表示不分组。
        /// </param>
        /// <param name="name">条目名称。</param>
        /// <param name="value">数值。</param>
        /// <param name="source">注册点（编译器自动填入调用方文件路径）。</param>
        public static void SetValue(
            string group, string name, int value, [CallerFilePath] string source = null)
        {
            RenderDebugManager.SetValue(
                group, name, DebugValueType.Int, new Vector4(value, 0f, 0f, 0f), 0,
                NormalizeSource(source));
        }

        /// <summary>注册或更新一个无符号整数单值。</summary>
        /// <param name="group">分组路径（<c>"/"</c> 分隔，可多级）；空串表示不分组。</param>
        /// <param name="name">条目名称。</param>
        /// <param name="value">数值。</param>
        /// <param name="source">注册点（编译器自动填入调用方文件路径）。</param>
        public static void SetValue(
            string group, string name, uint value, [CallerFilePath] string source = null)
        {
            RenderDebugManager.SetValue(
                group, name, DebugValueType.Uint, new Vector4(value, 0f, 0f, 0f), 0,
                NormalizeSource(source));
        }

        /// <summary>注册或更新一个浮点单值。</summary>
        /// <param name="group">分组路径（<c>"/"</c> 分隔，可多级）；空串表示不分组。</param>
        /// <param name="name">条目名称。</param>
        /// <param name="value">数值。</param>
        /// <param name="decimalDigits">显示位数（默认 3）。</param>
        /// <param name="source">注册点（编译器自动填入调用方文件路径）。</param>
        public static void SetValue(
            string group,
            string name,
            float value,
            int decimalDigits = 3,
            [CallerFilePath] string source = null)
        {
            RenderDebugManager.SetValue(
                group, name, DebugValueType.Float, new Vector4(value, 0f, 0f, 0f), decimalDigits,
                NormalizeSource(source));
        }

        /// <summary>注册或更新一个四分量浮点单值。</summary>
        /// <param name="group">分组路径（<c>"/"</c> 分隔，可多级）；空串表示不分组。</param>
        /// <param name="name">条目名称。</param>
        /// <param name="value">数值。</param>
        /// <param name="decimalDigits">显示位数（默认 3）。</param>
        /// <param name="source">注册点（编译器自动填入调用方文件路径）。</param>
        public static void SetValue(
            string group,
            string name,
            Vector4 value,
            int decimalDigits = 3,
            [CallerFilePath] string source = null)
        {
            RenderDebugManager.SetValue(
                group, name, DebugValueType.Float4, value, decimalDigits, NormalizeSource(source));
        }

        /// <summary>注册或更新一个三分量浮点单值。</summary>
        /// <param name="group">分组路径（<c>"/"</c> 分隔，可多级）；空串表示不分组。</param>
        /// <param name="name">条目名称。</param>
        /// <param name="value">数值。</param>
        /// <param name="decimalDigits">显示位数（默认 3）。</param>
        /// <param name="source">注册点（编译器自动填入调用方文件路径）。</param>
        public static void SetValue(
            string group,
            string name,
            Vector3 value,
            int decimalDigits = 3,
            [CallerFilePath] string source = null)
        {
            RenderDebugManager.SetValue(
                group, name, DebugValueType.Float3,
                new Vector4(value.x, value.y, value.z, 0f), decimalDigits, NormalizeSource(source));
        }

        /// <summary>注册或更新一个二分量浮点单值。</summary>
        /// <param name="group">分组路径（<c>"/"</c> 分隔，可多级）；空串表示不分组。</param>
        /// <param name="name">条目名称。</param>
        /// <param name="value">数值。</param>
        /// <param name="decimalDigits">显示位数（默认 3）。</param>
        /// <param name="source">注册点（编译器自动填入调用方文件路径）。</param>
        public static void SetValue(
            string group,
            string name,
            Vector2 value,
            int decimalDigits = 3,
            [CallerFilePath] string source = null)
        {
            RenderDebugManager.SetValue(
                group, name, DebugValueType.Float2,
                new Vector4(value.x, value.y, 0f, 0f), decimalDigits, NormalizeSource(source));
        }

        /// <summary>注册或更新一个布尔单值。</summary>
        /// <param name="group">分组路径（<c>"</c> 分隔，可多级）；空串表示不分组。</param>
        /// <param name="name">条目名称。</param>
        /// <param name="value">数值。</param>
        /// <param name="source">注册点（编译器自动填入调用方文件路径）。</param>
        public static void SetValue(
            string group, string name, bool value, [CallerFilePath] string source = null)
        {
            RenderDebugManager.SetValue(
                group, name, DebugValueType.Bool, new Vector4(value ? 1f : 0f, 0f, 0f, 0f), 0,
                NormalizeSource(source));
        }

        /// <summary>
        /// 把调用方文件全路径归一化为「注册点」名（文件名去扩展名）。
        /// </summary>
        /// <param name="callerFilePath">编译器填入的调用方文件路径。</param>
        /// <returns>注册点名；路径为空时返回 <c>null</c>。</returns>
        private static string NormalizeSource(string callerFilePath)
        {
            return string.IsNullOrEmpty(callerFilePath)
                ? null
                : Path.GetFileNameWithoutExtension(callerFilePath);
        }

        // ── 单值（不分组便捷重载，等价于 group 传空串） ──

        /// <summary>注册或更新一个不分组整数单值。</summary>
        /// <param name="name">条目名称。</param>
        /// <param name="value">数值。</param>
        /// <param name="source">注册点（编译器自动填入调用方文件路径）。</param>
        public static void SetValue(string name, int value, [CallerFilePath] string source = null)
        {
            SetValue(string.Empty, name, value, source);
        }

        /// <summary>注册或更新一个不分组无符号整数单值。</summary>
        /// <param name="name">条目名称。</param>
        /// <param name="value">数值。</param>
        /// <param name="source">注册点（编译器自动填入调用方文件路径）。</param>
        public static void SetValue(string name, uint value, [CallerFilePath] string source = null)
        {
            SetValue(string.Empty, name, value, source);
        }

        /// <summary>注册或更新一个不分组浮点单值。</summary>
        /// <param name="name">条目名称。</param>
        /// <param name="value">数值。</param>
        /// <param name="decimalDigits">显示位数（默认 3）。</param>
        /// <param name="source">注册点（编译器自动填入调用方文件路径）。</param>
        public static void SetValue(
            string name, float value, int decimalDigits = 3, [CallerFilePath] string source = null)
        {
            SetValue(string.Empty, name, value, decimalDigits, source);
        }

        /// <summary>注册或更新一个不分组四分量浮点单值。</summary>
        /// <param name="name">条目名称。</param>
        /// <param name="value">数值。</param>
        /// <param name="decimalDigits">显示位数（默认 3）。</param>
        /// <param name="source">注册点（编译器自动填入调用方文件路径）。</param>
        public static void SetValue(
            string name, Vector4 value, int decimalDigits = 3, [CallerFilePath] string source = null)
        {
            SetValue(string.Empty, name, value, decimalDigits, source);
        }

        /// <summary>注册或更新一个不分组三分量浮点单值。</summary>
        /// <param name="name">条目名称。</param>
        /// <param name="value">数值。</param>
        /// <param name="decimalDigits">显示位数（默认 3）。</param>
        /// <param name="source">注册点（编译器自动填入调用方文件路径）。</param>
        public static void SetValue(
            string name, Vector3 value, int decimalDigits = 3, [CallerFilePath] string source = null)
        {
            SetValue(string.Empty, name, value, decimalDigits, source);
        }

        /// <summary>注册或更新一个不分组二分量浮点单值。</summary>
        /// <param name="name">条目名称。</param>
        /// <param name="value">数值。</param>
        /// <param name="decimalDigits">显示位数（默认 3）。</param>
        /// <param name="source">注册点（编译器自动填入调用方文件路径）。</param>
        public static void SetValue(
            string name, Vector2 value, int decimalDigits = 3, [CallerFilePath] string source = null)
        {
            SetValue(string.Empty, name, value, decimalDigits, source);
        }

        /// <summary>注册或更新一个不分组布尔单值。</summary>
        /// <param name="name">条目名称。</param>
        /// <param name="value">数值。</param>
        /// <param name="source">注册点（编译器自动填入调用方文件路径）。</param>
        public static void SetValue(string name, bool value, [CallerFilePath] string source = null)
        {
            SetValue(string.Empty, name, value, source);
        }

        /// <summary>设置指定单值的浮点显示位数。</summary>
        /// <param name="label">显示标签。</param>
        /// <param name="decimalDigits">显示位数。</param>
        public static void SetFormat(string label, int decimalDigits)
        {
            RenderDebugManager.SetFormat(label, decimalDigits);
        }

        /// <summary>移除一个已注册的单值。</summary>
        /// <param name="label">显示标签。</param>
        public static void Clear(string label)
        {
            RenderDebugManager.Clear(label);
        }

        /// <summary>设置单个单值的显示开关。</summary>
        /// <param name="label">显示标签。</param>
        /// <param name="visible">是否显示。</param>
        /// <returns>成功修改返回 <c>true</c>。</returns>
        public static bool SetVisible(string label, bool visible)
        {
            return RenderDebugManager.SetVisible(label, visible);
        }

        /// <summary>设置某个注册点下全部单值的显示开关。</summary>
        /// <param name="source">注册点（调用方源文件名）。</param>
        /// <param name="visible">是否显示。</param>
        /// <returns>实际被修改的条目数。</returns>
        public static int SetSourceVisible(string source, bool visible)
        {
            return RenderDebugManager.SetSourceVisible(source, visible);
        }

        // ── 每像素 ──

        /// <summary>选择每像素调试通道。</summary>
        /// <remarks>
        /// 会同时套用该通道自带的颜色映射预设：各通道取值域差异极大
        ///（[0,1] 材质参数 / [-1,1] 法线 / 米级坐标 / 0..N 群集计数），
        /// 沿用上一通道的参数往往只能看到整屏纯色。
        /// </remarks>
        /// <param name="passName">
        /// 来源 pass 的注册显示名（<see cref="PassAttribute.DisplayName"/>，如 "Draw Object"）。
        /// </param>
        /// <param name="channelId">通道 ID。</param>
        public static void SetPerPixelChannel(string passName, int channelId)
        {
            RenderDebugManager.MutateSettings((ref RenderDebugSettings s) =>
            {
                s.PerPixelPassName = passName;
                s.PerPixelChannelId = channelId;
                s.DisplayMode |= DebugDisplayMode.PerPixel;

                if (PassDebugRegistry.TryGetChannelDefaultColorMap(
                    passName, channelId, out DebugColorMapSettings preset))
                {
                    s.ColorMap = preset;
                }
            });
        }

        /// <summary>整体设置每像素值的颜色渐变映射。</summary>
        /// <param name="settings">颜色映射配置。</param>
        public static void SetColorMap(in DebugColorMapSettings settings)
        {
            // 匿名方法不能捕获 in 参数，先复制到局部变量。
            DebugColorMapSettings colorMap = settings;
            RenderDebugManager.MutateSettings((ref RenderDebugSettings s) => s.ColorMap = colorMap);
        }

        /// <summary>设置颜色映射的范围。</summary>
        /// <param name="rangeMin">映射下界。</param>
        /// <param name="rangeMax">映射上界。</param>
        public static void SetColorRange(float rangeMin, float rangeMax)
        {
            RenderDebugManager.MutateSettings((ref RenderDebugSettings s) =>
            {
                s.ColorMap.RangeMin = rangeMin;
                s.ColorMap.RangeMax = rangeMax;
            });
        }

        /// <summary>设置颜色映射抽取的通道。</summary>
        /// <param name="channel">标量通道。</param>
        public static void SetColorChannel(DebugColorChannel channel)
        {
            RenderDebugManager.MutateSettings((ref RenderDebugSettings s) => s.ColorMap.Channel = channel);
        }

        /// <summary>设置颜色映射的覆盖强度。</summary>
        /// <param name="opacity">0..1；1 为完全覆盖原渲染颜色。</param>
        public static void SetColorOpacity(float opacity)
        {
            RenderDebugManager.MutateSettings(
                (ref RenderDebugSettings s) => s.ColorMap.Opacity = Mathf.Clamp01(opacity));
        }

        /// <summary>设置颜色映射使用的色带。</summary>
        /// <param name="preset">内置色带。</param>
        /// <param name="palette">自定义控制点；非空时同时把 preset 设为 <see cref="DebugColorMapPreset.Custom"/>。</param>
        public static void SetColorPalette(DebugColorMapPreset preset, Color[] palette = null)
        {
            RenderDebugManager.MutateSettings((ref RenderDebugSettings s) =>
            {
                if (palette != null && palette.Length >= 2)
                {
                    s.ColorMap.Palette = palette;
                    s.ColorMap.Preset = DebugColorMapPreset.Custom;
                }
                else
                {
                    s.ColorMap.Preset = preset;
                }
            });
        }

        // ── 纹理预览 ──

        /// <summary>设置纹理预览来源。</summary>
        /// <param name="passName">来源 pass 的实例名或注册显示名（如 "drawShadow" / "Draw Shadow"）。</param>
        /// <param name="slice">texture array 的 slice。</param>
        public static void PreviewTexture(string passName, int slice = 0)
        {
            RenderDebugManager.MutateSettings((ref RenderDebugSettings s) =>
            {
                s.PreviewPassName = passName;
                s.PreviewSlice = Mathf.Max(0, slice);
                s.DisplayMode |= DebugDisplayMode.TexturePreview;
            });
        }

        /// <summary>
        /// 单独开关某一段显示内容。
        /// </summary>
        private static void SetSectionEnabled(DebugDisplayMode section, bool enabled)
        {
            RenderDebugManager.MutateSettings((ref RenderDebugSettings s) =>
            {
                s.DisplayMode = enabled
                    ? s.DisplayMode | section
                    : s.DisplayMode & ~section;
            });
        }
    }
}
