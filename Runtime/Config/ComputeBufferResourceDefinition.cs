// <copyright file="ComputeBufferResourceDefinition.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace HN.HNRP
{
    /// <summary>
    /// ComputeBuffer 资源的定义。描述运行时分配一个渲染图计算缓冲所需的参数。
    /// </summary>
    [Serializable]
    public sealed class ComputeBufferResourceDefinition : ResourceDefinition
    {
        /// <summary>
        /// 分配计算缓冲的元素数量。
        /// </summary>
        public int BufferCount;

        /// <summary>
        /// 每个元素的字节跨度。
        /// </summary>
        public int BufferStride;

        /// <inheritdoc />
        public override ResourceKind Kind => ResourceKind.ComputeBuffer;

        /// <inheritdoc />
        public override ResourceNode CreateNode() => new ComputeBufferResourceNode(this);

        /// <inheritdoc />
        public override void CopyFrom(ResourceDefinition source)
        {
            if (source is ComputeBufferResourceDefinition s)
            {
                BufferCount = s.BufferCount;
                BufferStride = s.BufferStride;
            }
        }

        /// <inheritdoc />
        public override IReadOnlyList<IResourcePreset> Presets => s_Presets;

        /// <summary>
        /// 内置预设集合。缓冲元素数/跨度随场景变化，暂无通用预设，返回空。
        /// 新增预设：加静态字段并追加到此数组。
        /// </summary>
        private static readonly IResourcePreset[] s_Presets = System.Array.Empty<IResourcePreset>();
    }
}
