// <copyright file="ResourcePreset.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace HN.HNRP
{
    /// <summary>
    /// 非泛型资源预设视图。编辑器下拉菜单统一枚举任意 <see cref="ResourceDefinition"/>
    /// 子类的预设，无需关心具体类型。
    /// </summary>
    /// <remarks>
    /// 预设仅服务编辑器与代码初始化，运行时渲染循环完全不触碰预设，
    /// 因此预设机制零 GC 开销。
    /// </remarks>
    public interface IResourcePreset
    {
        /// <summary>
        /// 预设显示名（编辑器下拉项文本）。
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 把预设参数值拷贝到指定 <paramref name="definition"/>。
        /// </summary>
        /// <param name="definition">目标定义（必须与预设模板同具体类型）。</param>
        void ApplyTo(ResourceDefinition definition);
    }

    /// <summary>
    /// 命名参数预设：携带一个同类型模板实例，套用时把模板字段值拷贝进目标定义。
    /// </summary>
    /// <typeparam name="T">预设对应的 <see cref="ResourceDefinition"/> 具体子类。</typeparam>
    /// <remarks>
    /// 预设语义为<b>值复制</b>：套用后目标定义保留自身字段，仍可继续微调；
    /// 预设改动不反向传播。运行时读的是定义自身字段，预设零参与。
    /// </remarks>
    public sealed class ResourcePreset<T> : IResourcePreset
        where T : ResourceDefinition
    {
        /// <summary>预设显示名。</summary>
        private readonly string m_Name;

        /// <summary>预设模板实例，字段值作为套用来源。</summary>
        private readonly T m_Template;

        /// <summary>
        /// 初始化一个新的预设实例。
        /// </summary>
        /// <param name="name">预设显示名。</param>
        /// <param name="template">预设模板实例，其字段值将在套用时被拷贝。</param>
        public ResourcePreset(string name, T template)
        {
            m_Name = name;
            m_Template = template;
        }

        /// <inheritdoc />
        public string Name => m_Name;

        /// <inheritdoc />
        public void ApplyTo(ResourceDefinition definition)
        {
            ((T)definition).CopyFrom(m_Template);
        }
    }
}
