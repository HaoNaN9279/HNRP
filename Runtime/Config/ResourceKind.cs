// <copyright file="ResourceKind.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

namespace HN.HNRP
{
    /// <summary>
    /// The kind of a render graph resource node.
    /// Determines which concrete <see cref="ResourceNode"/> subclass is created
    /// at build time and which handles it carries at runtime.
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

    /// <summary>
    /// The direction of a <see cref="ResourceConnection"/> edge.
    /// </summary>
    public enum ResourceConnectionDirection
    {
        /// <summary>
        /// The resource node flows into a pass input slot
        /// (<see cref="ResourceNode.ConsumerSlots"/>).
        /// </summary>
        ResourceToPass,

        /// <summary>
        /// A pass output slot produces this resource
        /// (<see cref="ResourceNode.ProducerSlot"/>).
        /// </summary>
        PassToResource,
    }
}
