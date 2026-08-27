// <copyright file="RendererListResourceDefinition.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace HN.HNRP
{
    /// <summary>
    /// RendererList 资源的定义。描述从相机裁剪结果构建渲染器列表所需的参数。
    /// </summary>
    [Serializable]
    public sealed class RendererListResourceDefinition : ResourceDefinition
    {
        /// <summary>
        /// 构建渲染器列表使用的渲染队列范围（不透明或透明）。
        /// </summary>
        public RenderListKind ListKind = RenderListKind.Opaque;

        /// <summary>
        /// 构建渲染器列表时应用的渲染层掩码，仅匹配层上的渲染器被包含。
        /// </summary>
        public uint RenderingLayerMask = 0x00000001;

        /// <inheritdoc />
        public override ResourceKind Kind => ResourceKind.RendererList;

        /// <inheritdoc />
        public override ResourceNode CreateNode() => new RendererListResourceNode(this);

        /// <inheritdoc />
        public override void CopyFrom(ResourceDefinition source)
        {
            if (source is RendererListResourceDefinition s)
            {
                ListKind = s.ListKind;
                RenderingLayerMask = s.RenderingLayerMask;
            }
        }

        /// <inheritdoc />
        public override IReadOnlyList<IResourcePreset> Presets => s_Presets;

        /// <summary>
        /// 内置预设集合。新增预设：加静态字段并追加到此数组。
        /// </summary>
        private static readonly IResourcePreset[] s_Presets =
        {
            new ResourcePreset<RendererListResourceDefinition>(
                "Opaque",
                new RendererListResourceDefinition { ListKind = RenderListKind.Opaque, RenderingLayerMask = 0x00000001 }),
            new ResourcePreset<RendererListResourceDefinition>(
                "Transparent",
                new RendererListResourceDefinition { ListKind = RenderListKind.Transparent, RenderingLayerMask = 0x00000001 }),
        };
    }
}
