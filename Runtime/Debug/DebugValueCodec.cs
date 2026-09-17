// <copyright file="DebugValueCodec.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using UnityEngine;

namespace HN.HNRP
{
    /// <summary>
    /// 单值缓冲条目的 CPU 侧编解码。
    /// 与 HLSL <c>DebugValue.hlsl</c> 的 <c>DebugValueEntry</c> 布局一一对应。
    /// </summary>
    /// <remarks>
    /// 条目布局（stride 24 字节，6 个 uint）：
    /// <code>
    /// [0] header  = type | hasTag&lt;&lt;7 | labelCharCount&lt;&lt;8 | decimalDigits&lt;&lt;16
    /// [1] tag     = 标签前 4 字符的 ASCII 打包（保留字段，当前恒为 0）
    /// [2..5]      = 值载荷（按类型 asuint）
    /// </code>
    /// </remarks>
    public static class DebugValueCodec
    {
        /// <summary>每个条目的 stride（字节）。</summary>
        public const int EntryStride = 24;

        /// <summary>每个条目的 uint 数。</summary>
        public const int UintsPerEntry = EntryStride / sizeof(uint);

        /// <summary>类型字段位掩码（低 7 位）。</summary>
        public const uint TypeMask = 0x7Fu;

        /// <summary>「有标签」标记位。</summary>
        public const uint HasTagFlag = 1u << 7;

        /// <summary>标签字符数所在位偏移。</summary>
        public const int LabelCharCountShift = 8;

        /// <summary>标签字符数位掩码（8 位，最大 255）。</summary>
        public const uint LabelCharCountMask = 0xFFu;

        /// <summary>浮点显示位数所在位偏移。</summary>
        public const int DecimalDigitsShift = 16;

        /// <summary>浮点显示位数位掩码（4 位，0..15）。</summary>
        public const uint DecimalDigitsMask = 0xFu;

        /// <summary>分组缩进层级所在位偏移（4 位，0..15）。</summary>
        public const int IndentShift = 20;

        /// <summary>分组缩进层级位掩码。</summary>
        public const uint IndentMask = 0xFu;

        /// <summary>
        /// 「本行是分组标题行」标记位。
        /// 分组标题只画名称 + 冒号，不画数值。
        /// </summary>
        public const uint GroupHeaderFlag = 1u << 24;

        /// <summary>
        /// 取类型的分量数量（标量 1、向量 2/3/4）。
        /// </summary>
        /// <param name="type">值类型。</param>
        /// <returns>分量数量；<see cref="DebugValueType.None"/> 返回 0。</returns>
        public static int GetComponentCount(DebugValueType type)
        {
            switch (type)
            {
                case DebugValueType.Uint:
                case DebugValueType.Int:
                case DebugValueType.Float:
                case DebugValueType.Bool:
                    return 1;
                case DebugValueType.Uint2:
                case DebugValueType.Int2:
                case DebugValueType.Float2:
                    return 2;
                case DebugValueType.Uint3:
                case DebugValueType.Int3:
                case DebugValueType.Float3:
                    return 3;
                case DebugValueType.Uint4:
                case DebugValueType.Int4:
                case DebugValueType.Float4:
                    return 4;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// 判断类型是否为浮点族。
        /// </summary>
        /// <param name="type">值类型。</param>
        /// <returns>浮点族返回 <c>true</c>。</returns>
        public static bool IsFloat(DebugValueType type)
        {
            return type == DebugValueType.Float
                || type == DebugValueType.Float2
                || type == DebugValueType.Float3
                || type == DebugValueType.Float4;
        }

        /// <summary>
        /// 判断类型是否为无符号整数族。
        /// </summary>
        /// <param name="type">值类型。</param>
        /// <returns>无符号整数族返回 <c>true</c>。</returns>
        public static bool IsUnsignedInteger(DebugValueType type)
        {
            return type == DebugValueType.Uint
                || type == DebugValueType.Uint2
                || type == DebugValueType.Uint3
                || type == DebugValueType.Uint4;
        }

        /// <summary>
        /// 打包条目头。
        /// </summary>
        /// <param name="type">值类型。</param>
        /// <param name="labelCharCount">标签字符数（0..255，超出被裁剪）。</param>
        /// <param name="decimalDigits">浮点显示位数（0..15，超出被裁剪）。</param>
        /// <param name="indent">分组缩进层级（0..15，超出被裁剪）。</param>
        /// <param name="isGroupHeader">本行是否为分组标题行。</param>
        /// <returns>打包后的 header 值。</returns>
        public static uint PackHeader(
            DebugValueType type,
            int labelCharCount,
            int decimalDigits,
            int indent = 0,
            bool isGroupHeader = false)
        {
            uint count = (uint)Mathf.Clamp(labelCharCount, 0, (int)LabelCharCountMask);
            uint digits = (uint)Mathf.Clamp(decimalDigits, 0, (int)DecimalDigitsMask);
            uint nesting = (uint)Mathf.Clamp(indent, 0, (int)IndentMask);

            uint header = (uint)type
                | (count << LabelCharCountShift)
                | (digits << DecimalDigitsShift)
                | (nesting << IndentShift);

            if (isGroupHeader)
            {
                header |= GroupHeaderFlag;
            }

            return header;
        }

        /// <summary>
        /// 从 header 解出分组缩进层级。
        /// </summary>
        /// <param name="header">条目头。</param>
        /// <returns>缩进层级。</returns>
        public static int UnpackIndent(uint header)
        {
            return (int)((header >> IndentShift) & IndentMask);
        }

        /// <summary>
        /// 判断该行是否为分组标题行。
        /// </summary>
        /// <param name="header">条目头。</param>
        /// <returns>是分组标题行时返回 <c>true</c>。</returns>
        public static bool UnpackIsGroupHeader(uint header)
        {
            return (header & GroupHeaderFlag) != 0;
        }

        /// <summary>
        /// 从 header 解出值类型。
        /// </summary>
        /// <param name="header">条目头。</param>
        /// <returns>值类型。</returns>
        public static DebugValueType UnpackType(uint header)
        {
            return (DebugValueType)(header & TypeMask);
        }

        /// <summary>
        /// 从 header 解出标签字符数。
        /// </summary>
        /// <param name="header">条目头。</param>
        /// <returns>标签字符数。</returns>
        public static int UnpackLabelCharCount(uint header)
        {
            return (int)((header >> LabelCharCountShift) & LabelCharCountMask);
        }

        /// <summary>
        /// 从 header 解出浮点显示位数。
        /// </summary>
        /// <param name="header">条目头。</param>
        /// <returns>浮点显示位数。</returns>
        public static int UnpackDecimalDigits(uint header)
        {
            return (int)((header >> DecimalDigitsShift) & DecimalDigitsMask);
        }

        /// <summary>
        /// 把一个 <see cref="Vector4"/> 载荷按类型转成 4 个 uint（asuint 语义）。
        /// </summary>
        /// <param name="type">值类型。</param>
        /// <param name="value">数值载荷。</param>
        /// <returns>4 个 uint；不足分量补 0。</returns>
        public static uint[] PackValue(DebugValueType type, Vector4 value)
        {
            uint[] packed = new uint[4];

            switch (type)
            {
                case DebugValueType.Bool:
                    packed[0] = value.x != 0f ? 1u : 0u;
                    break;

                case DebugValueType.Float:
                    packed[0] = ToUint(value.x);
                    break;

                case DebugValueType.Float2:
                    packed[0] = ToUint(value.x);
                    packed[1] = ToUint(value.y);
                    break;

                case DebugValueType.Float3:
                    packed[0] = ToUint(value.x);
                    packed[1] = ToUint(value.y);
                    packed[2] = ToUint(value.z);
                    break;

                case DebugValueType.Float4:
                    packed[0] = ToUint(value.x);
                    packed[1] = ToUint(value.y);
                    packed[2] = ToUint(value.z);
                    packed[3] = ToUint(value.w);
                    break;

                default:
                    // 整数族：直接按位截断（负值走补码，与 HLSL asint/asuinth 语义一致）。
                    packed[0] = (uint)value.x;
                    packed[1] = (uint)value.y;
                    packed[2] = (uint)value.z;
                    packed[3] = (uint)value.w;
                    break;
            }

            return packed;
        }

        /// <summary>
        /// 把 float 按位转换为 uint（不改变 bit pattern）。
        /// </summary>
        /// <param name="value">输入浮点。</param>
        /// <returns>对应的 uint 位模式。</returns>
        public static uint ToUint(float value)
        {
            return unchecked((uint)BitConverter.SingleToInt32Bits(value));
        }

        /// <summary>
        /// 把 uint 位模式还原为 float。
        /// </summary>
        /// <param name="value">uint 位模式。</param>
        /// <returns>对应的浮点。</returns>
        public static float ToFloat(uint value)
        {
            return BitConverter.Int32BitsToSingle(unchecked((int)value));
        }
    }
}
