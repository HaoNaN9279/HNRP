// <copyright file="DebugStringCodec.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace HN.HNRP
{
    /// <summary>
    /// 把标签字符串翻译为 shader 可直接索引的字模下标。
    /// </summary>
    /// <remarks>
    /// <para>
    /// HLSL 中不存在字符字面量，因此字符 → 字模块的映射完全由数据驱动：
    /// CPU 把 ASCII 值按 4 字节打包进 uint，shader 解包后减去
    /// <see cref="AsciiStart"/> 即得字模图集下标。
    /// </para>
    /// <para>
    /// 打包方式：每个 uint 容纳 4 个字符，低字节在前。
    /// 例：<c>"Abcd"</c> → <c>A | b&lt;&lt;8 | c&lt;&lt;16 | d&lt;&lt;24</c>。
    /// </para>
    /// </remarks>
    public static class DebugStringCodec
    {
        /// <summary>字模图集覆盖的起始 ASCII（与 <c>DEBUG_FONT_TEXT_ASCII_START</c> 一致）。</summary>
        public const byte AsciiStart = 32;

        /// <summary>字模图集覆盖的结束 ASCII（含）。</summary>
        public const byte AsciiEnd = 126;

        /// <summary>超出字模覆盖范围时使用的替代字符（<c>'?'</c>）。</summary>
        public const byte SubstituteAscii = (byte)'?';

        /// <summary>每个 uint 容纳的字符数。</summary>
        public const int CharsPerUint = 4;

        /// <summary>单个标签允许的最大字符数（由 GPU 侧的 4 个 uint 决定）。</summary>
        public const int MaxLabelChars = 16;

        /// <summary>单个标签在字符串缓冲中占用的 uint 数。</summary>
        public const int UintsPerLabel = MaxLabelChars / CharsPerUint;

        /// <summary>
        /// 计算 <paramref name="text"/> 打包后需要的 uint 数。
        /// </summary>
        /// <param name="text">标签文本；<c>null</c> 视为空串。</param>
        /// <returns>需要的 uint 数（不超过 <see cref="UintsPerLabel"/>）。</returns>
        public static int GetUintCount(string text)
        {
            int count = GetCharCount(text);
            return (count + CharsPerUint - 1) / CharsPerUint;
        }

        /// <summary>
        /// 取有效字符数（截断到 <see cref="MaxLabelChars"/>）。
        /// </summary>
        /// <param name="text">标签文本；<c>null</c> 视为空串。</param>
        /// <returns>有效字符数。</returns>
        public static int GetCharCount(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            return text.Length < MaxLabelChars ? text.Length : MaxLabelChars;
        }

        /// <summary>
        /// 把标签打包进 <paramref name="dst"/>。
        /// </summary>
        /// <param name="text">标签文本；<c>null</c> 视为空串。</param>
        /// <param name="dst">目标缓冲；不足位置追加，已有位置覆盖。</param>
        /// <param name="dstOffset">起始写入下标。</param>
        /// <returns>实际写入的 uint 数。</returns>
        public static int Encode(string text, uint[] dst, int dstOffset)
        {
            int charCount = GetCharCount(text);
            int uintCount = (charCount + CharsPerUint - 1) / CharsPerUint;

            for (int i = 0; i < uintCount; i++)
            {
                uint packed = 0;
                for (int b = 0; b < CharsPerUint; b++)
                {
                    int charIndex = (i * CharsPerUint) + b;
                    if (charIndex >= charCount)
                    {
                        break;
                    }

                    packed |= (uint)NormalizeAscii(text[charIndex]) << (b * 8);
                }

                dst[dstOffset + i] = packed;
            }

            return uintCount;
        }

        /// <summary>
        /// 把标签打包进列表（追加，不预留填充）。
        /// </summary>
        /// <param name="text">标签文本；<c>null</c> 视为空串。</param>
        /// <param name="dst">目标列表。</param>
        /// <returns>追加的 uint 数。</returns>
        public static int Encode(string text, List<uint> dst)
        {
            int charCount = GetCharCount(text);
            int uintCount = (charCount + CharsPerUint - 1) / CharsPerUint;

            for (int i = 0; i < uintCount; i++)
            {
                uint packed = 0;
                for (int b = 0; b < CharsPerUint; b++)
                {
                    int charIndex = (i * CharsPerUint) + b;
                    if (charIndex >= charCount)
                    {
                        break;
                    }

                    packed |= (uint)NormalizeAscii(text[charIndex]) << (b * 8);
                }

                dst.Add(packed);
            }

            return uintCount;
        }

        /// <summary>
        /// 解码（用于测试与调试显示）。
        /// </summary>
        /// <param name="src">打包数据。</param>
        /// <param name="offset">起始下标。</param>
        /// <param name="charCount">要解出的字符数。</param>
        /// <returns>还原出的文本。</returns>
        public static string Decode(uint[] src, int offset, int charCount)
        {
            char[] buffer = new char[charCount];
            for (int i = 0; i < charCount; i++)
            {
                uint packed = src[offset + (i / CharsPerUint)];
                buffer[i] = (char)((packed >> ((i % CharsPerUint) * 8)) & 0xFFu);
            }

            return new string(buffer);
        }

        /// <summary>
        /// 把任意字符归一化到字模图集覆盖的 ASCII 区间。
        /// </summary>
        /// <param name="c">输入字符。</param>
        /// <returns>归一化后的 ASCII 值。</returns>
        public static byte NormalizeAscii(char c)
        {
            return (c >= AsciiStart && c <= AsciiEnd) ? (byte)c : SubstituteAscii;
        }
    }
}
