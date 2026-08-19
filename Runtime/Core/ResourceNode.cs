// <copyright file="ResourceNode.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RendererUtils;

namespace HN.HNRP
{
    /// <summary>
    /// Runtime counterpart of a <see cref="ResourceDefinition"/>.
    /// Represents a render graph resource (texture, compute buffer, or renderer list)
    /// that passes reference by name.
    /// </summary>
    /// <remarks>
    /// <para><b>Producer / consumer model:</b></para>
    /// <list type="bullet">
    ///   <item>A resource may have at most one <see cref="ProducerSlot"/> — an output
    ///   slot of a producer pass (wired via
    ///   <see cref="ResourceConnectionDirection.PassToResource"/>). When present, the
    ///   resource handle is read from that slot at consumer record time
    ///   (<see cref="GetHandle"/>), so the producer pass must be recorded first
    ///   (guaranteed by topological sort at build time).</item>
    ///   <item>Without a producer, the resource is allocated once per frame by
    ///   <see cref="Resolve"/> (called at the start of
    ///   <see cref="CameraRenderer.Render"/>).</item>
    ///   <item><see cref="ConsumerSlots"/> are the input slots that read this
    ///   resource. They are used to derive execution-order dependencies.</item>
    /// </list>
    /// </remarks>
    public abstract class ResourceNode
    {
        /// <summary>
        /// The name of this resource. Matches
        /// <see cref="ResourceDefinition.ResourceName"/>.
        /// </summary>
        public string ResourceName;

        /// <summary>
        /// The kind of this resource.
        /// </summary>
        public ResourceKind Kind;

        /// <summary>
        /// The asset definition this node was built from.
        /// </summary>
        public ResourceDefinition Definition;

        /// <summary>
        /// The producer pass output slot (<see cref="SlotDirection.Output"/>),
        /// or <c>null</c> when this resource has no producer (allocated by
        /// <see cref="Resolve"/>).
        /// </summary>
        public PassSlot ProducerSlot;

        /// <summary>
        /// The consumer pass input slots that read this resource.
        /// </summary>
        public List<PassSlot> ConsumerSlots = new();

        /// <summary>
        /// Returns the render graph resource handle for this node.
        /// Consumers call this during <see cref="Pass.Record"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="TextureHandle"/>, <see cref="ComputeBufferHandle"/>, or
        /// <see cref="RendererListHandle"/> as a boxed <see cref="object"/>.
        /// The concrete type is guaranteed by the slot's
        /// <see cref="PassSlot{T}.CanConnectTo(ResourceNode)"/> type check.
        /// </returns>
        public abstract object GetHandle();

        /// <summary>
        /// Resolves the resource handle for the current frame.
        /// Called once at the start of <see cref="CameraRenderer.Render"/> for
        /// resources without a producer. The base implementation is a no-op;
        /// concrete subclasses allocate their render graph resource here.
        /// </summary>
        /// <param name="renderGraph">The render graph to allocate the resource in.</param>
        /// <param name="ctx">The per-camera rendering context.</param>
        public virtual void Resolve(RenderGraph renderGraph, CameraContext ctx)
        {
        }
    }

    /// <summary>
    /// A <see cref="ResourceNode"/> carrying a <see cref="TextureHandle"/>.
    /// </summary>
    public sealed class TextureResourceNode : ResourceNode
    {
        private TextureHandle m_Handle;

        /// <summary>
        /// Gets a value indicating whether this resource is produced by a pass
        /// (<see cref="ResourceNode.ProducerSlot"/> != <c>null</c>).
        /// </summary>
        public bool HasProducer => ProducerSlot != null;

        /// <inheritdoc />
        public override object GetHandle()
        {
            return HasProducer
                ? ((TextureSlot)ProducerSlot).ReadHandle()
                : m_Handle;
        }

        /// <inheritdoc />
        public override void Resolve(RenderGraph renderGraph, CameraContext ctx)
        {
            if (HasProducer || ctx.Camera == null)
            {
                return;
            }

            var desc = new TextureDesc(
                Mathf.Max(1, Mathf.RoundToInt(ctx.Camera.pixelWidth * Definition.TextureScale.x)),
                Mathf.Max(1, Mathf.RoundToInt(ctx.Camera.pixelHeight * Definition.TextureScale.y)),
                false, false)
            {
                colorFormat = Definition.ColorFormat,
                depthBufferBits = Definition.DepthBits,
                clearBuffer = Definition.ClearBuffer,
                clearColor = Definition.ClearColor,
                name = ResourceName,
            };

            m_Handle = renderGraph.CreateTexture(desc);
        }
    }

    /// <summary>
    /// A <see cref="ResourceNode"/> carrying a <see cref="ComputeBufferHandle"/>.
    /// </summary>
    public sealed class ComputeBufferResourceNode : ResourceNode
    {
        private ComputeBufferHandle m_Handle;

        /// <summary>
        /// Gets a value indicating whether this resource is produced by a pass
        /// (<see cref="ResourceNode.ProducerSlot"/> != <c>null</c>).
        /// </summary>
        public bool HasProducer => ProducerSlot != null;

        /// <inheritdoc />
        public override object GetHandle()
        {
            return HasProducer
                ? ((ComputeBufferSlot)ProducerSlot).ReadHandle()
                : m_Handle;
        }

        /// <inheritdoc />
        public override void Resolve(RenderGraph renderGraph, CameraContext ctx)
        {
            if (HasProducer)
            {
                return;
            }

            m_Handle = renderGraph.CreateComputeBuffer(
                new ComputeBufferDesc(Definition.BufferCount, Definition.BufferStride)
                {
                    name = ResourceName,
                });
        }
    }

    /// <summary>
    /// A <see cref="ResourceNode"/> carrying a <see cref="RendererListHandle"/>.
    /// Renderer lists are always resolved from the camera's culling results each
    /// frame and have no producer pass concept.
    /// </summary>
    public sealed class RendererListResourceNode : ResourceNode
    {
        private RendererListHandle m_Handle;

        /// <inheritdoc />
        public override object GetHandle()
        {
            return m_Handle;
        }

        /// <inheritdoc />
        public override void Resolve(RenderGraph renderGraph, CameraContext ctx)
        {
            // Without valid culling results we cannot build a renderer list
            // descriptor — an invalid descriptor throws during render graph
            // compilation. Leave the handle default; consumer passes skip
            // recording when the handle is invalid.
            if (!ctx.HasCullingResults || ctx.Camera == null)
            {
                return;
            }

            RendererListDesc desc = Definition.ListKind == RenderListKind.Opaque
                ? HNRenderPipelineUtils.GetOpaqueRendererListDesc(
                    ShaderPassNames.AllForwardNames,
                    ctx.CullingResults,
                    ctx.Camera,
                    Definition.RenderingLayerMask)
                : HNRenderPipelineUtils.GetTransparentRendererListDesc(
                    ShaderPassNames.AllForwardNames,
                    ctx.CullingResults,
                    ctx.Camera,
                    Definition.RenderingLayerMask);

            m_Handle = renderGraph.CreateRendererList(desc);
        }
    }
}
