// <copyright file="DebugColorMapSettings.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using UnityEngine;

namespace HN.HNRP
{
    /// <summary>
    /// 每像素值 → 颜色的渐变映射配置（用户可配置）。
    /// </summary>
    /// <remarks>
    /// 由 <see cref="DebugColorMap"/> 在 CPU 侧烘焙为 256×1 的 LUT 纹理；
    /// shader 仅做一次归一化 + 一次纹理采样，不产生任何 shader 变体。
    /// </remarks>
    [Serializable]
    public struct DebugColorMapSettings
    {
        /// <summary>是否启用颜色映射；关闭时保持原渲染颜色。</summary>
        [Tooltip("是否启用每像素值的颜色映射")]
        public bool Enabled;

        /// <summary>映射下界（低于该值的像素取色带起点）。</summary>
        [Tooltip("映射下界")]
        public float RangeMin;

        /// <summary>映射上界（高于该值的像素取色带终点）。</summary>
        [Tooltip("映射上界")]
        public float RangeMax;

        /// <summary>从值向量中抽取的标量通道。</summary>
        [Tooltip("从值向量中抽取的通道")]
        public DebugColorChannel Channel;

        /// <summary>越界处理：<c>false</c> = Clamp，<c>true</c> = Wrap（重复色带）。</summary>
        [Tooltip("越界处理：false = Clamp，true = Wrap")]
        public bool Wrap;

        /// <summary>覆盖强度 0..1；1 = 完全覆盖原渲染颜色，0 = 不覆盖。</summary>
        [Range(0f, 1f)]
        [Tooltip("覆盖强度：0 = 不覆盖，1 = 完全覆盖")]
        public float Opacity;

        /// <summary>使用的内置色带；为 <see cref="DebugColorMapPreset.Custom"/> 时用 <see cref="Palette"/>。</summary>
        [Tooltip("内置色带")]
        public DebugColorMapPreset Preset;

        /// <summary>自定义渐变控制点（≥2 个）；仅在 <see cref="Preset"/> 为 Custom 时生效。</summary>
        [Tooltip("自定义渐变控制点（≥2 个）")]
        public Color[] Palette;

        /// <summary>
        /// 文档化默认配置：灰阶、[0,1]、取 X 分量、Clamp、完全覆盖。
        /// </summary>
        public static DebugColorMapSettings Default
        {
            get
            {
                return new DebugColorMapSettings
                {
                    Enabled = true,
                    RangeMin = 0f,
                    RangeMax = 1f,
                    Channel = DebugColorChannel.Red,
                    Wrap = false,
                    Opacity = 1f,
                    Preset = DebugColorMapPreset.Grayscale,
                    Palette = null,
                };
            }
        }

        /// <summary>
        /// 构造一个完整的颜色映射配置（通道预设用）。
        /// </summary>
        /// <param name="rangeMin">映射下界。</param>
        /// <param name="rangeMax">映射上界。</param>
        /// <param name="channel">抽取的标量通道。</param>
        /// <param name="preset">色带。</param>
        /// <param name="opacity">覆盖强度。</param>
        /// <returns>配置实例。</returns>
        public static DebugColorMapSettings Create(
            float rangeMin,
            float rangeMax,
            DebugColorChannel channel,
            DebugColorMapPreset preset,
            float opacity = 1f)
        {
            return new DebugColorMapSettings
            {
                Enabled = true,
                RangeMin = rangeMin,
                RangeMax = rangeMax,
                Channel = channel,
                Wrap = false,
                Opacity = opacity,
                Preset = preset,
                Palette = null,
            };
        }

        /// <summary>
        /// 生成参数签名，用于判断 LUT 是否需要重建。
        /// </summary>
        /// <returns>可比较的哈希值；参数未变时返回相同值。</returns>
        public int ComputeSignature()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + Preset.GetHashCode();
                hash = (hash * 31) + Channel.GetHashCode();

                if (Palette != null)
                {
                    hash = (hash * 31) + Palette.Length;
                    for (int i = 0; i < Palette.Length; i++)
                    {
                        Color c = Palette[i];
                        hash = (hash * 31) + c.GetHashCode();
                    }
                }

                return hash;
            }
        }
    }
}
