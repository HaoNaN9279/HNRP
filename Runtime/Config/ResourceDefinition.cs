// <copyright file="ResourceDefinition.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace HN.HNRP
{
    /// <summary>
    /// 渲染图资源节点的抽象基类定义。每种资源类型（纹理 / ComputeBuffer / RendererList）
    /// 由独立的具体子类承载其参数，互不混用。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 序列化通过 <see cref="RenderGraphAsset"/> 上的
    /// <c>[SerializeReference] List&lt;ResourceDefinition&gt;</c> 实现多态，
    /// 因此本类型为纯 <c>[Serializable]</c> class，非 ScriptableObject。
    /// </para>
    /// <para>
    /// 具体子类负责：<see cref="Kind"/> 类型标签、<see cref="CreateNode"/> 工厂、
    /// <see cref="CopyFrom"/> 参数拷贝（预设套用的基础）、<see cref="Presets"/> 预设集合。
    /// </para>
    /// </remarks>
    [Serializable]
    public abstract class ResourceDefinition
    {
        /// <summary>
        /// 资源名，用于匹配 <see cref="ResourceConnection.ResourceName"/>。
        /// 必须非空且在渲染图资源内唯一。
        /// </summary>
        public string ResourceName;

        /// <summary>
        /// 该资源定义的稳定类型标签（由具体子类派生返回）。
        /// 用于编辑器显示与日志，不再用于序列化分派。
        /// </summary>
        public abstract ResourceKind Kind { get; }

        /// <summary>
        /// 创建对应的运行时 <see cref="ResourceNode"/> 实例。
        /// 替代原先按 <see cref="ResourceKind"/> switch 的创建逻辑。
        /// </summary>
        /// <returns>与定义类型匹配的资源节点。</returns>
        public abstract ResourceNode CreateNode();

        /// <summary>
        /// 从同类型的另一个定义拷贝全部参数。
        /// </summary>
        /// <param name="source">源定义（须与目标同具体类型）。</param>
        public abstract void CopyFrom(ResourceDefinition source);

        /// <summary>
        /// 该类型可用的预设集合。编辑器下拉菜单据此列出可选预设。
        /// </summary>
        public abstract IReadOnlyList<IResourcePreset> Presets { get; }
    }
}
