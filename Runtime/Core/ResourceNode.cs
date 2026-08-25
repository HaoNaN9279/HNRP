// <copyright file="ResourceNode.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace HN.HNRP
{
    /// <summary>
    /// Runtime counterpart of a <see cref="ResourceDefinition"/>.
    /// Represents a render graph resource (texture, compute buffer, or renderer list)
    /// referenced by name.
    /// </summary>
    /// <remarks>
    /// <para><b>Resource model:</b></para>
    /// <list type="bullet">
    ///   <item>A resource has only outputs. It is allocated at the start of the
    ///   pass chain by <see cref="Resolve"/> and every pass that reads or writes
    ///   it wires the resource into one of its input slots. Intermediate data
    ///   produced by a pass flows through <see cref="SlotConnection"/>, not
    ///   through resource nodes.</item>
    ///   <item><see cref="ConsumerSlots"/> are the pass input slots that read or
    ///   write this resource. They are used to derive execution-order
    ///   dependencies.</item>
    ///   <item>A texture resource may instead be imported from an external
    ///   runtime texture (see <see cref="ResourceDefinition.ExternalTextureName"/>)
    ///   rather than allocated per-frame.</item>
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
        /// Called once at the start of <see cref="CameraRenderer.Render"/>.
        /// The base implementation is a no-op; concrete subclasses allocate or
        /// import their render graph resource here.
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
        /// Cached RTHandle wrapper around the imported external texture.
        /// Allocated once and reused every frame (the external texture is a
        /// pipeline-owned singleton, e.g. <c>emptyTexture</c>).
        /// </summary>
        private RTHandle m_ImportedRTHandle;

        /// <inheritdoc />
        public override object GetHandle()
        {
            return m_Handle;
        }

        /// <inheritdoc />
        public override void Resolve(RenderGraph renderGraph, CameraContext ctx)
        {
            // External texture import: the texture comes from the pipeline's
            // runtime resources rather than being allocated per-frame.
            if (!string.IsNullOrEmpty(Definition.ExternalTextureName))
            {
                Texture tex = ctx.RuntimeResources?.GetExternalTexture(Definition.ExternalTextureName);
                if (tex != null)
                {
                    if (m_ImportedRTHandle == null)
                    {
                        m_ImportedRTHandle = RTHandles.Alloc(tex);
                    }

                    m_Handle = renderGraph.ImportTexture(m_ImportedRTHandle);
                    return;
                }

                Debug.LogWarning(
                    $"TextureResourceNode.Resolve: External texture " +
                    $"'{Definition.ExternalTextureName}' for resource '{ResourceName}' was not " +
                    $"found in the pipeline runtime resources. Leaving the default handle.");
                return;
            }

            if (ctx.Camera == null)
            {
                return;
            }

            // Fixed-size mode: use Width/Height directly.
            // Camera-scaled mode: scale camera pixel dimensions.
            int texWidth = Definition.Width > 0
                ? Definition.Width
                : Mathf.Max(1, Mathf.RoundToInt(ctx.Camera.pixelWidth * Definition.TextureScale.x));
            int texHeight = Definition.Height > 0
                ? Definition.Height
                : Mathf.Max(1, Mathf.RoundToInt(ctx.Camera.pixelHeight * Definition.TextureScale.y));

            var desc = new TextureDesc(texWidth, texHeight, false, false)
            {
                colorFormat = Definition.ColorFormat,
                depthBufferBits = Definition.DepthBits,
                clearBuffer = Definition.ClearBuffer,
                clearColor = Definition.ClearColor,
                useMipMap = Definition.UseMipMap,
                autoGenerateMips = Definition.AutoGenerateMips,
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

        /// <inheritdoc />
        public override object GetHandle()
        {
            return m_Handle;
        }

        /// <inheritdoc />
        public override void Resolve(RenderGraph renderGraph, CameraContext ctx)
        {
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
