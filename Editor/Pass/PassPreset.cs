// <copyright file="PassPreset.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// 非泛型 Pass 预设视图。编辑器下拉菜单统一枚举任意 <see cref="Pass"/>
    /// 类型的预设，无需关心具体类型。
    /// </summary>
    /// <remarks>
    /// 预设是纯编辑器功能，定义在对应 <see cref="PassEditor"/> 下，不进入 Player 包体。
    /// </remarks>
    public interface IPassPreset
    {
        /// <summary>
        /// 预设显示名（编辑器下拉项文本）。
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 把预设参数值拷贝到指定 <paramref name="pass"/>。
        /// </summary>
        /// <param name="pass">目标 Pass（必须与预设模板同具体类型）。</param>
        void ApplyTo(Pass pass);
    }

    /// <summary>
    /// 命名参数预设：携带一个同类型模板 Pass，套用时把模板参数值拷贝进目标 Pass。
    /// </summary>
    /// <typeparam name="T">预设对应的 <see cref="Pass"/> 具体子类。</typeparam>
    /// <remarks>
    /// 预设语义为<b>值复制</b>：套用后目标 Pass 保留自身字段，仍可继续微调；
    /// 预设改动不反向传播。运行时读的是 Pass 自身字段，预设零参与。
    /// 实例名（<see cref="Pass.PassName"/>）不被拷贝——它是 Pass 在图中的身份标识。
    /// </remarks>
    public sealed class PassPreset<T> : IPassPreset
        where T : Pass
    {
        /// <summary>预设显示名。</summary>
        private readonly string m_Name;

        /// <summary>预设模板实例，字段值作为套用来源。</summary>
        private readonly T m_Template;

        /// <summary>
        /// 初始化一个新的预设实例。
        /// </summary>
        /// <param name="name">预设显示名。</param>
        /// <param name="template">预设模板实例，其参数值将在套用时被拷贝。</param>
        public PassPreset(string name, T template)
        {
            m_Name = name;
            m_Template = template;
        }

        /// <inheritdoc />
        public string Name => m_Name;

        /// <inheritdoc />
        public void ApplyTo(Pass pass)
        {
            ((T)pass).CopyFrom(m_Template);
        }
    }
}
