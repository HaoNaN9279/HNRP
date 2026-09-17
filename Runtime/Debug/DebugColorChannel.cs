// <copyright file="DebugColorChannel.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

namespace HN.HNRP
{
    /// <summary>
    /// 每像素值 → 颜色映射时，从值向量中抽取哪一个标量。
    /// </summary>
    /// <remarks>
    /// 取值必须与 HLSL 侧 <c>HN_DebugSelectChannel</c> 的 switch 分支一致。
    /// </remarks>
    public enum DebugColorChannel
    {
        /// <summary>取 X 分量。</summary>
        Red = 0,

        /// <summary>取 Y 分量。</summary>
        Green = 1,

        /// <summary>取 Z 分量。</summary>
        Blue = 2,

        /// <summary>取 W 分量。</summary>
        Alpha = 3,

        /// <summary>亮度（Rec.709 加权和）。</summary>
        Luminance = 4,

        /// <summary>向量长度。</summary>
        Magnitude = 5,
    }
}
