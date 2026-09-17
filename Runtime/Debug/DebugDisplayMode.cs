// <copyright file="DebugDisplayMode.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;

namespace HN.HNRP
{
    /// <summary>
    /// 调试叠层的显示内容开关（可组合）。
    /// </summary>
    [Flags]
    public enum DebugDisplayMode
    {
        /// <summary>全部关闭。</summary>
        None = 0,

        /// <summary>屏幕角落的单值文本列表。</summary>
        SingleValues = 1 << 0,

        /// <summary>每像素值的颜色映射（由生产者 pass 直接混入其颜色输出）。</summary>
        PerPixel = 1 << 1,

        /// <summary>角落纹理预览（支持 texture array 指定 slice）。</summary>
        TexturePreview = 1 << 2,

        /// <summary>全部开启（默认）。</summary>
        All = SingleValues | PerPixel | TexturePreview,
    }
}
