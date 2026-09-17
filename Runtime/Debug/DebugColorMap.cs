// <copyright file="DebugColorMap.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using UnityEngine;

namespace HN.HNRP
{
    /// <summary>
    /// 值 → 颜色的渐变映射：CPU 侧把 <see cref="DebugColorMapSettings"/> 烘焙成
    /// 一张 256×1 的 LUT 纹理，shader 只做一次归一化 + 一次采样。
    /// </summary>
    /// <remarks>
    /// 采用 LUT 而非 shader 内硬编码色带的原因：采样成本恒定、切换色带零变体、
    /// 且天然支持任意用户自定义控制点（见开发方案 ADR-035）。
    /// </remarks>
    public static class DebugColorMap
    {
        /// <summary>LUT 宽度（色带采样点数）。</summary>
        public const int LutWidth = 256;

        /// <summary>LUT 高度（固定 1）。</summary>
        public const int LutHeight = 1;

        /// <summary>
        /// 按配置生成 LUT 纹理（R8G8B8A8_UNorm、Point、Clamp）。
        /// </summary>
        /// <param name="settings">颜色映射配置。</param>
        /// <returns>新建的 LUT 纹理；调用方负责释放。</returns>
        public static Texture2D CreateLut(DebugColorMapSettings settings)
        {
            var texture = new Texture2D(LutWidth, LutHeight, TextureFormat.RGBA32, false, true)
            {
                name = "HNRPDebugColorMap",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            UpdateLut(texture, settings);
            return texture;
        }

        /// <summary>
        /// 把配置采样进已有的 LUT 纹理（原地更新，避免重建纹理对象）。
        /// </summary>
        /// <param name="texture">目标 LUT 纹理；为 <c>null</c> 时为空操作。</param>
        /// <param name="settings">颜色映射配置。</param>
        public static void UpdateLut(Texture2D texture, DebugColorMapSettings settings)
        {
            if (texture == null)
            {
                return;
            }

            var colors = new Color32[LutWidth];
            for (int i = 0; i < LutWidth; i++)
            {
                float t = LutWidth > 1 ? i / (float)(LutWidth - 1) : 0f;
                colors[i] = Evaluate(settings, t);
            }

            texture.SetPixels32(colors);
            texture.Apply(false, false);
        }

        /// <summary>
        /// 在归一化位置 <paramref name="t"/>（0..1）处求色带颜色。
        /// </summary>
        /// <param name="settings">颜色映射配置。</param>
        /// <param name="t">归一化位置。</param>
        /// <returns>该位置的颜色（alpha 恒为 1）。</returns>
        public static Color32 Evaluate(DebugColorMapSettings settings, float t)
        {
            t = Mathf.Clamp01(t);

            if (settings.Preset == DebugColorMapPreset.Custom
                && settings.Palette != null
                && settings.Palette.Length >= 2)
            {
                return EvaluatePalette(settings.Palette, t);
            }

            return EvaluatePreset(settings.Preset, t);
        }

        /// <summary>
        /// 在自定义控制点之间做分段线性插值。
        /// </summary>
        /// <param name="palette">控制点数组（≥2）。</param>
        /// <param name="t">归一化位置。</param>
        /// <returns>插值颜色。</returns>
        public static Color32 EvaluatePalette(Color[] palette, float t)
        {
            int last = palette.Length - 1;
            float scaled = Mathf.Clamp01(t) * last;
            int index = Mathf.Min((int)scaled, last - 1);
            float local = scaled - index;

            return Color.Lerp(palette[index], palette[index + 1], local);
        }

        /// <summary>
        /// 求内置色带在归一化位置 <paramref name="t"/> 处的颜色。
        /// </summary>
        /// <param name="preset">内置色带。</param>
        /// <param name="t">归一化位置（0..1）。</param>
        /// <returns>该位置的颜色。</returns>
        public static Color32 EvaluatePreset(DebugColorMapPreset preset, float t)
        {
            t = Mathf.Clamp01(t);

            switch (preset)
            {
                case DebugColorMapPreset.Jet:
                    return Jet(t);
                case DebugColorMapPreset.Turbo:
                    return Turbo(t);
                case DebugColorMapPreset.Viridis:
                    return Viridis(t);
                case DebugColorMapPreset.Heat:
                    return Heat(t);
                case DebugColorMapPreset.RedGreen:
                    return RedGreen(t);
                default:
                    return new Color32((byte)(t * 255f + 0.5f), (byte)(t * 255f + 0.5f), (byte)(t * 255f + 0.5f), 255);
            }
        }

        /// <summary>
        /// 把值区间映射到归一化位置。
        /// </summary>
        /// <param name="value">输入值。</param>
        /// <param name="rangeMin">映射下界。</param>
        /// <param name="rangeMax">映射上界。</param>
        /// <param name="wrap">越界处理：<c>true</c> 为 Wrap，否则 Clamp。</param>
        /// <returns>归一化位置（0..1）。</returns>
        public static float Normalize(float value, float rangeMin, float rangeMax, bool wrap)
        {
            float span = rangeMax - rangeMin;
            if (Mathf.Abs(span) < 1e-6f)
            {
                span = 1e-6f;
            }

            float t = (value - rangeMin) / span;
            if (wrap)
            {
                t -= Mathf.Floor(t);
            }

            return Mathf.Clamp01(t);
        }

        /// <summary>Jet 色带。</summary>
        private static Color32 Jet(float t)
        {
            float r = Mathf.Clamp01(1.5f - Mathf.Abs(4f * t - 3f));
            float g = Mathf.Clamp01(1.5f - Mathf.Abs(4f * t - 2f));
            float b = Mathf.Clamp01(1.5f - Mathf.Abs(4f * t - 1f));
            return ToColor32(r, g, b);
        }

        /// <summary>Turbo 色带的多项式近似。</summary>
        private static Color32 Turbo(float t)
        {
            float r = Mathf.Clamp01(0.13572138f + (4.61539260f + (-42.66032258f + (132.13108234f + (-152.94239396f + 59.28637943f * t) * t) * t) * t) * t);
            float g = Mathf.Clamp01(0.09140261f + (2.19418839f + (4.84296658f + (-14.18503333f + (4.27729857f + 2.82956604f * t) * t) * t) * t) * t);
            float b = Mathf.Clamp01(0.10667330f + (12.64194608f + (-60.58204836f + (110.36276771f + (-89.90310912f + 27.34824973f * t) * t) * t) * t) * t);
            return ToColor32(r, g, b);
        }

        /// <summary>Viridis 色带的多项式近似。</summary>
        private static Color32 Viridis(float t)
        {
            float r = Mathf.Clamp01(0.277727327f + (0.105093044f + (0.330861829f + (0.319365945f - 0.187824471f * t) * t) * t) * t);
            float g = Mathf.Clamp01(0.005407344f + (1.404613530f + (-1.888542140f + (0.162298635f + 0.321976847f * t) * t) * t) * t);
            float b = Mathf.Clamp01(0.334099805f + (0.589747582f + (-0.166087405f + (-0.577305554f + 0.270966903f * t) * t) * t) * t);
            return ToColor32(r, g, b);
        }

        /// <summary>绿→黄→红（heat）色带。</summary>
        private static Color32 Heat(float t)
        {
            float r = Mathf.Clamp01(t * 2f);
            float g = Mathf.Clamp01(t < 0.5f ? t * 2f : (1f - t) * 2f);
            return ToColor32(r, g, 0f);
        }

        /// <summary>红→绿对照色带。</summary>
        private static Color32 RedGreen(float t)
        {
            return ToColor32(1f - t, t, 0f);
        }

        /// <summary>把 0..1 的三分量转成 Color32。</summary>
        private static Color32 ToColor32(float r, float g, float b)
        {
            return new Color32(
                (byte)(Mathf.Clamp01(r) * 255f + 0.5f),
                (byte)(Mathf.Clamp01(g) * 255f + 0.5f),
                (byte)(Mathf.Clamp01(b) * 255f + 0.5f),
                255);
        }

        /// <summary>
        /// 内置色带的展示名（供 Editor 菜单使用）。
        /// </summary>
        /// <param name="preset">内置色带。</param>
        /// <returns>展示名。</returns>
        public static string GetPresetName(DebugColorMapPreset preset)
        {
            return Enum.GetName(typeof(DebugColorMapPreset), preset) ?? "Custom";
        }
    }
}
