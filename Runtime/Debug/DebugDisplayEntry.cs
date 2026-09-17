// <copyright file="DebugDisplayEntry.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEngine;

namespace HN.HNRP
{
    /// <summary>
    /// 单条单值显示指令：分组路径 + 名称 + 类型 + 数值 + 浮点显示位数 + 注册点。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 条目本身是<b>注册信息</b>，跨帧保留（由 <see cref="RenderDebugManager"/> 持有）；
    /// 数值每帧可经 <see cref="HNRPDebug.SetValue(string,string,float)"/> 等接口更新后
    /// 上传到 GPU。GPU 负责格式化与文本绘制，CPU 不参与逐字符工作。
    /// </para>
    /// <para>
    /// 分组用 <c>"/"</c> 分隔，支持多级嵌套（如 <c>"Animation/Value"</c>）。
    /// 显示时按分组路径自上而下排列，分组本身也占一行（层级用缩进表示）。
    /// </para>
    /// </remarks>
    public sealed class DebugDisplayEntry
    {
        /// <summary>
        /// 注册唯一键：<see cref="Group"/> 非空时为 <c>"Group/Name"</c>，否则就是
        /// <see cref="Name"/>。同名重复注册只更新数值，不新增条目。
        /// </summary>
        public string Label;

        /// <summary>
        /// 分组路径（<c>"/"</c> 分隔，可多级）；未分组时为空串。
        /// </summary>
        public string Group;

        /// <summary>条目名称（显示时的叶子名）。</summary>
        public string Name;

        /// <summary>分组路径的各级名称（<see cref="Group"/> 按 <c>"/"</c> 拆分）。</summary>
        public string[] GroupSegments;

        /// <summary>值类型（决定 GPU 侧的格式化方式）。</summary>
        public DebugValueType Type;

        /// <summary>数值载荷；按 <see cref="Type"/> 解释有效分量。</summary>
        public Vector4 Value;

        /// <summary>浮点显示位数（0..7）。</summary>
        public int DecimalDigits;

        /// <summary>
        /// 注册点：首次注册该条目的调用方源文件名（不含扩展名）。
        /// </summary>
        /// <remarks>
        /// 由 <see cref="HNRPDebug"/> 经 <c>[CallerFilePath]</c> 自动捕获，
        /// 用于在 GlobalSettings 面板里按来源分组管理显示开关。
        /// 取首次注册的值（后续同名注册不覆盖），保证分组稳定。
        /// </remarks>
        public string Source;

        /// <summary>
        /// 是否参与显示。关闭后该条目不会被上传到 GPU（行号按可见条目重新紧凑排布）。
        /// </summary>
        public bool Visible;

        /// <summary>
        /// 初始化 <see cref="DebugDisplayEntry"/> 的新实例。
        /// </summary>
        /// <param name="group">分组路径；未分组时传空串。</param>
        /// <param name="name">条目名称。</param>
        /// <param name="type">值类型。</param>
        /// <param name="value">数值载荷。</param>
        /// <param name="decimalDigits">浮点显示位数。</param>
        /// <param name="source">注册点（调用方源文件名）。</param>
        public DebugDisplayEntry(
            string group,
            string name,
            DebugValueType type,
            Vector4 value,
            int decimalDigits,
            string source)
        {
            Group = group ?? string.Empty;
            Name = name;
            Label = Group.Length == 0 ? Name : $"{Group}/{Name}";
            GroupSegments = Group.Length == 0
                ? System.Array.Empty<string>()
                : Group.Split('/');
            Type = type;
            Value = value;
            DecimalDigits = decimalDigits;
            Source = source;
            Visible = true;
        }
    }
}
