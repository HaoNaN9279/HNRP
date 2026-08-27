// <copyright file="ResourceKind.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

namespace HN.HNRP
{
    /// <summary>
    /// 渲染图资源节点的稳定类型标签。
    /// 由具体 <see cref="ResourceDefinition"/> 子类派生返回，用于编辑器显示与日志。
    /// 具体节点类型由定义类型本身决定（见 <see cref="ResourceDefinition.CreateNode"/>）。
    /// </summary>
    public enum ResourceKind
    {
        /// <summary>
        /// A texture resource (e.g. color / depth target).
        /// </summary>
        Texture,

        /// <summary>
        /// A GPU compute buffer resource.
        /// </summary>
        ComputeBuffer,

        /// <summary>
        /// A renderer list resource produced from the camera's culling results.
        /// </summary>
        RendererList,
    }

    /// <summary>
    /// The render-queue scope used to build a <see cref="UnityEngine.Rendering.RendererUtils.RendererListDesc"/>.
    /// Maps to <c>HNRenderPipelineUtils.GetOpaqueRendererListDesc</c> /
    /// <c>GetTransparentRendererListDesc</c>.
    /// </summary>
    public enum RenderListKind
    {
        /// <summary>
        /// Opaque render queue range, sorted with opaque sorting criteria.
        /// </summary>
        Opaque,

        /// <summary>
        /// Transparent render queue range, sorted back-to-front.
        /// </summary>
        Transparent,
    }
}
