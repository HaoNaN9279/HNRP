// <copyright file="PassFieldLayout.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// Pass 面板字段布局声明：把「分组 / 隐藏 / 条件显示 / 条件灰显」从绘制代码里
    /// 分离为纯数据，由 <see cref="PassEditor.ConfigureFieldLayout"/> 声明，
    /// <see cref="PassParametersGUI.DrawParameters"/> 据此绘制。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 字段以<b>顶层 <c>[SerializeField]</c> 字段名</b>为标识。嵌套可序列化 struct
    /// 的内部叶子不单独声明，随其所属顶层字段整组处理（该组内部仍按反射展开）。
    /// </para>
    /// <para>
    /// 未出现在任何 section 中的字段按反射顺序绘制在全部 section 之后，
    /// 因此在 <see cref="PassEditor.ConfigureFieldLayout"/> 里忽略某个字段是安全的
    /// （不会隐藏它）。
    /// </para>
    /// </remarks>
    public sealed class PassFieldLayout
    {
        /// <summary>
        /// 已声明隐藏的顶层字段名（按声明顺序）。
        /// </summary>
        private readonly List<string> hiddenFields = new();

        /// <summary>
        /// 已声明的分组（按声明顺序）。
        /// </summary>
        private readonly List<PassFieldSection> sections = new();

        /// <summary>
        /// 获取已声明隐藏的顶层字段名。
        /// </summary>
        public IReadOnlyList<string> HiddenFields => hiddenFields;

        /// <summary>
        /// 获取已声明的分组，顺序即绘制顺序。
        /// </summary>
        public IReadOnlyList<PassFieldSection> Sections => sections;

        /// <summary>
        /// 隐藏一个或多个顶层字段（整组隐藏，含嵌套 struct 的全部叶子）。
        /// 用于隐藏由运行时上下文驱动、不应被用户覆盖的参数。
        /// </summary>
        /// <param name="rootFieldNames">顶层 <c>[SerializeField]</c> 字段名。可叠加调用。</param>
        /// <returns>本布局，支持链式调用。</returns>
        public PassFieldLayout Hide(params string[] rootFieldNames)
        {
            AddNames(hiddenFields, rootFieldNames);
            return this;
        }

        /// <summary>
        /// 声明一个参数分组。分组标题以粗体绘制，其成员字段按
        /// <paramref name="rootFieldNames"/> 的顺序排列。
        /// </summary>
        /// <param name="label">分组标题。</param>
        /// <param name="rootFieldNames">该组包含的顶层字段名。</param>
        /// <returns>该分组，可继续声明显示 / 灰显条件。</returns>
        public PassFieldSection Section(string label, params string[] rootFieldNames)
        {
            var section = new PassFieldSection(label, rootFieldNames);
            sections.Add(section);
            return section;
        }

        /// <summary>
        /// 判断某顶层字段是否已被声明隐藏。
        /// </summary>
        /// <param name="rootFieldName">顶层字段名。</param>
        /// <returns>已声明隐藏时返回 <c>true</c>。</returns>
        public bool IsHidden(string rootFieldName)
        {
            return rootFieldName != null && hiddenFields.Contains(rootFieldName);
        }

        /// <summary>
        /// 把非空字段名追加进目标列表（忽略 null / 空串 / 重复项）。
        /// </summary>
        private static void AddNames(List<string> target, string[] names)
        {
            if (names == null)
            {
                return;
            }

            foreach (string name in names)
            {
                if (!string.IsNullOrEmpty(name) && !target.Contains(name))
                {
                    target.Add(name);
                }
            }
        }
    }

    /// <summary>
    /// Pass 面板中的一个参数分组：标题 + 成员顶层字段名 + 可选的显示 / 灰显条件。
    /// </summary>
    public sealed class PassFieldSection
    {
        /// <summary>
        /// 分组成员字段名。
        /// </summary>
        private readonly List<string> rootFieldNames = new();

        /// <summary>
        /// 初始化新的参数分组。
        /// </summary>
        /// <param name="label">分组标题。</param>
        /// <param name="rootFieldNames">该组包含的顶层字段名。</param>
        internal PassFieldSection(string label, string[] rootFieldNames)
        {
            Label = label ?? string.Empty;

            if (rootFieldNames == null)
            {
                return;
            }

            foreach (string name in rootFieldNames)
            {
                if (!string.IsNullOrEmpty(name) && !this.rootFieldNames.Contains(name))
                {
                    this.rootFieldNames.Add(name);
                }
            }
        }

        /// <summary>
        /// 获取分组标题。
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// 获取分组成员字段名（绘制顺序）。
        /// </summary>
        public IReadOnlyList<string> RootFieldNames => rootFieldNames;

        /// <summary>
        /// 获取显示条件：返回 <c>false</c> 时整组不绘制（其字段也不会落到未分组区）。
        /// 为 <c>null</c> 时始终显示。
        /// </summary>
        public Func<RenderGraphPassGUIContext, bool> ShowWhenPredicate { get; private set; }

        /// <summary>
        /// 获取灰显条件：返回 <c>true</c> 时整组保持可见但禁用编辑，
        /// 用于提示「依赖的开关未打开」。为 <c>null</c> 时始终可编辑。
        /// </summary>
        public Func<RenderGraphPassGUIContext, bool> DisableWhenPredicate { get; private set; }

        /// <summary>
        /// 声明显示条件。
        /// </summary>
        /// <param name="predicate">返回 <c>false</c> 时隐藏整组。</param>
        /// <returns>本分组，支持链式调用。</returns>
        public PassFieldSection ShowWhen(Func<RenderGraphPassGUIContext, bool> predicate)
        {
            ShowWhenPredicate = predicate;
            return this;
        }

        /// <summary>
        /// 声明灰显条件。
        /// </summary>
        /// <param name="predicate">返回 <c>true</c> 时整组灰显。</param>
        /// <returns>本分组，支持链式调用。</returns>
        public PassFieldSection DisableWhen(Func<RenderGraphPassGUIContext, bool> predicate)
        {
            DisableWhenPredicate = predicate;
            return this;
        }
    }
}
