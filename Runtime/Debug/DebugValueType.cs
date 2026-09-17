// <copyright file="DebugValueType.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

namespace HN.HNRP
{
    /// <summary>
    /// debug 值的类型编码。
    /// 取值与 HLSL 侧 <c>DebugValue.hlsl</c> 的 <c>DV_*</c> 常量一一对应，
    /// 亦沿用 CoreRP <c>ShaderDebugPrint.hlsl</c> 的类型表。
    /// </summary>
    /// <remarks>
    /// 编码写入值缓冲条目头（<c>DebugValueEntry.header</c>）低 7 位。
    /// 刻意保持为普通枚举：项目已知 Mono 在「枚举作为泛型类型实参」时会触发
    /// native crash，因此传向 shader 前一律转为 <see cref="int"/>。
    /// </remarks>
    public enum DebugValueType
    {
        /// <summary>无值（槽位空闲）。</summary>
        None = 0,

        /// <summary>单分量无符号整数。</summary>
        Uint = 1,

        /// <summary>单分量有符号整数。</summary>
        Int = 2,

        /// <summary>单分量浮点。</summary>
        Float = 3,

        /// <summary>二分量无符号整数。</summary>
        Uint2 = 4,

        /// <summary>二分量有符号整数。</summary>
        Int2 = 5,

        /// <summary>二分量浮点。</summary>
        Float2 = 6,

        /// <summary>三分量无符号整数。</summary>
        Uint3 = 7,

        /// <summary>三分量有符号整数。</summary>
        Int3 = 8,

        /// <summary>三分量浮点。</summary>
        Float3 = 9,

        /// <summary>四分量无符号整数。</summary>
        Uint4 = 10,

        /// <summary>四分量有符号整数。</summary>
        Int4 = 11,

        /// <summary>四分量浮点。</summary>
        Float4 = 12,

        /// <summary>布尔（0/1）。</summary>
        Bool = 13,
    }
}
