// <copyright file="DebugColorMapPreset.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

namespace HN.HNRP
{
    /// <summary>
    /// 值 → 颜色映射使用的内置渐变带。
    /// </summary>
    public enum DebugColorMapPreset
    {
        /// <summary>灰阶（黑 → 白）。</summary>
        Grayscale = 0,

        /// <summary>Jet（蓝 → 青 → 黄 → 红）。</summary>
        Jet = 1,

        /// <summary>Turbo（近似感知均匀的高对比色带）。</summary>
        Turbo = 2,

        /// <summary>Viridis（感知均匀，色盲友好）。</summary>
        Viridis = 3,

        /// <summary>绿 → 黄 → 红（heat）。</summary>
        Heat = 4,

        /// <summary>红 → 绿（错误 / 正确对照）。</summary>
        RedGreen = 5,

        /// <summary>使用 <c>DebugColorMapSettings.Palette</c> 自定义控制点。</summary>
        Custom = 6,
    }
}
