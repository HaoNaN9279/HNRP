// <copyright file="DebugValueKind.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

namespace HN.HNRP
{
    /// <summary>
    /// debug 通道的取值粒度，用于菜单分组与 API 校验。
    /// </summary>
    public enum DebugValueKind
    {
        /// <summary>全屏唯一值（如光源数量、探针槽位），走单值文本通道。</summary>
        SingleValue = 0,

        /// <summary>逐像素值（如 albedo、世界法线），走每像素颜色映射通道。</summary>
        PerPixel = 1,
    }
}
