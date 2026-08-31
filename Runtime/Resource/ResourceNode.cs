// <copyright file="ResourceNode.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace HN.HNRP
{
    /// <summary>
    /// 强类型渲染图资源句柄提供者。消除 <c>GetHandle()</c> 返回 <see cref="object"/>
    /// 造成的每帧装箱，让 <see cref="PassSlot{T}.ReadHandle"/> 直接拿到值类型句柄。
    /// </summary>
    /// <typeparam name="T">
    /// 渲染图资源句柄 struct（<see cref="TextureHandle"/> / <see cref="ComputeBufferHandle"/> /
    /// <see cref="RendererListHandle"/>）。
    /// </typeparam>
    public interface IResourceHandleProvider<T>
    {
        /// <summary>
        /// 返回该资源节点的强类型句柄。零装箱。
        /// </summary>
        /// <returns>当前的渲染图资源句柄。</returns>
        T GetHandle();
    }

    /// <summary>
    /// 运行时资源节点。对应一个 <see cref="ResourceDefinition"/>，
    /// 表示按名字引用的渲染图资源（纹理 / 计算缓冲 / 渲染器列表）。
    /// </summary>
    /// <remarks>
    /// <para><b>资源模型：</b></para>
    /// <list type="bullet">
    ///   <item>资源只有输出。它在 pass 链开始处由 <see cref="Resolve"/> 分配，
    ///   每个读写它的 pass 把资源接入自己的某个输入槽。</item>
    ///   <item><see cref="ConsumerSlots"/> 是读写该资源的 pass 输入槽，用于推导执行顺序依赖。</item>
    ///   <item>纹理资源可改为从外部运行时纹理导入（见
    ///   <see cref="TextureResourceDefinition.ExternalTextureName"/>），而非每帧分配。</item>
    /// </list>
    /// </remarks>
    public abstract class ResourceNode
    {
        /// <summary>
        /// 资源名，匹配 <see cref="ResourceDefinition.ResourceName"/>。
        /// </summary>
        public string ResourceName;

        /// <summary>
        /// 该节点所构建自的资源定义。
        /// </summary>
        public ResourceDefinition Definition { get; }

        /// <summary>
        /// 该资源的稳定类型标签。
        /// </summary>
        public abstract ResourceKind Kind { get; }

        /// <summary>
        /// 读写该资源的 pass 输入槽。
        /// </summary>
        public List<PassSlot> ConsumerSlots = new();

        /// <summary>
        /// 初始化资源节点，从定义继承名称。
        /// </summary>
        /// <param name="definition">构建该节点的资源定义。</param>
        protected ResourceNode(ResourceDefinition definition)
        {
            Definition = definition;
            ResourceName = definition.ResourceName;
        }

        /// <summary>
        /// 解析当前帧的资源句柄。在 <see cref="CameraRenderer.Render"/> 开始时调用一次。
        /// 基类为空实现；具体子类在此分配或导入其渲染图资源。
        /// </summary>
        /// <param name="renderGraph">分配资源的目标渲染图。</param>
        /// <param name="ctx">每相机渲染上下文。</param>
        public virtual void Resolve(RenderGraph renderGraph, CameraContext ctx)
        {
        }
    }

    /// <summary>
    /// 携带 <see cref="TextureHandle"/> 的资源节点。
    /// </summary>
    public sealed class TextureResourceNode : ResourceNode, IResourceHandleProvider<TextureHandle>
    {
        private readonly TextureResourceDefinition m_Definition;
        private TextureHandle m_Handle;

        /// <summary>
        /// 包裹导入外部纹理的缓存 RTHandle。分配一次后每帧复用
        /// （外部纹理是管线拥有的单例，如 <c>emptyTexture</c>）。
        /// </summary>
        private RTHandle m_ImportedRTHandle;

        /// <summary>
        /// 初始化纹理资源节点。
        /// </summary>
        /// <param name="definition">纹理资源定义。</param>
        public TextureResourceNode(TextureResourceDefinition definition)
            : base(definition)
        {
            m_Definition = definition;
        }

        /// <inheritdoc />
        public override ResourceKind Kind => ResourceKind.Texture;

        /// <inheritdoc />
        public TextureHandle GetHandle()
        {
            return m_Handle;
        }

        /// <inheritdoc />
        public override void Resolve(RenderGraph renderGraph, CameraContext ctx)
        {
            // 外部纹理导入：纹理来自管线运行时资源而非每帧分配。
            if (!string.IsNullOrEmpty(m_Definition.ExternalTextureName))
            {
                Texture tex = ctx.RuntimeResources?.GetExternalTexture(m_Definition.ExternalTextureName);
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
                    $"'{m_Definition.ExternalTextureName}' for resource '{ResourceName}' was not " +
                    $"found in the pipeline runtime resources. Leaving the default handle.");
                return;
            }

            if (ctx.Camera == null)
            {
                return;
            }

            // 固定尺寸模式：直接使用 Width/Height。
            // 相机缩放模式：缩放相机像素尺寸。
            int texWidth = m_Definition.Width > 0
                ? m_Definition.Width
                : Mathf.Max(1, Mathf.RoundToInt(ctx.Camera.pixelWidth * m_Definition.TextureScale.x));
            int texHeight = m_Definition.Height > 0
                ? m_Definition.Height
                : Mathf.Max(1, Mathf.RoundToInt(ctx.Camera.pixelHeight * m_Definition.TextureScale.y));

            var desc = new TextureDesc(texWidth, texHeight, false, false)
            {
                colorFormat = m_Definition.ColorFormat,
                depthBufferBits = m_Definition.DepthBits,
                clearBuffer = m_Definition.ClearBuffer,
                clearColor = m_Definition.ClearColor,
                useMipMap = m_Definition.UseMipMap,
                autoGenerateMips = m_Definition.AutoGenerateMips,
                name = ResourceName,
            };

            m_Handle = renderGraph.CreateTexture(desc);
        }
    }

    /// <summary>
    /// 携带 <see cref="ComputeBufferHandle"/> 的资源节点。
    /// </summary>
    public sealed class ComputeBufferResourceNode : ResourceNode, IResourceHandleProvider<ComputeBufferHandle>
    {
        private readonly ComputeBufferResourceDefinition m_Definition;
        private ComputeBufferHandle m_Handle;

        /// <summary>
        /// 初始化计算缓冲资源节点。
        /// </summary>
        /// <param name="definition">计算缓冲资源定义。</param>
        public ComputeBufferResourceNode(ComputeBufferResourceDefinition definition)
            : base(definition)
        {
            m_Definition = definition;
        }

        /// <inheritdoc />
        public override ResourceKind Kind => ResourceKind.ComputeBuffer;

        /// <inheritdoc />
        public ComputeBufferHandle GetHandle()
        {
            return m_Handle;
        }

        /// <inheritdoc />
        public override void Resolve(RenderGraph renderGraph, CameraContext ctx)
        {
            m_Handle = renderGraph.CreateComputeBuffer(
                new ComputeBufferDesc(m_Definition.BufferCount, m_Definition.BufferStride)
                {
                    name = ResourceName,
                });
        }
    }

    /// <summary>
    /// 携带 <see cref="RendererListHandle"/> 的资源节点。
    /// 渲染器列表每帧从相机裁剪结果解析，没有生产者 pass 概念。
    /// </summary>
    public sealed class RendererListResourceNode : ResourceNode, IResourceHandleProvider<RendererListHandle>
    {
        private readonly RendererListResourceDefinition m_Definition;
        private RendererListHandle m_Handle;

        /// <summary>
        /// 初始化渲染器列表资源节点。
        /// </summary>
        /// <param name="definition">渲染器列表资源定义。</param>
        public RendererListResourceNode(RendererListResourceDefinition definition)
            : base(definition)
        {
            m_Definition = definition;
        }

        /// <inheritdoc />
        public override ResourceKind Kind => ResourceKind.RendererList;

        /// <inheritdoc />
        public RendererListHandle GetHandle()
        {
            return m_Handle;
        }

        /// <inheritdoc />
        public override void Resolve(RenderGraph renderGraph, CameraContext ctx)
        {
            // 无有效裁剪结果则无法构建渲染器列表描述符——无效描述符会在渲染图编译时抛异常。
            // 保持句柄默认；消费 pass 在句柄无效时跳过录制。
            if (!ctx.HasCullingResults || ctx.Camera == null)
            {
                return;
            }

            RendererListDesc desc = m_Definition.ListKind == RenderListKind.Opaque
                ? HNRenderPipelineUtils.GetOpaqueRendererListDesc(
                    ShaderPassNames.AllForwardNames,
                    ctx.CullingResults,
                    ctx.Camera,
                    m_Definition.RenderingLayerMask)
                : HNRenderPipelineUtils.GetTransparentRendererListDesc(
                    ShaderPassNames.AllForwardNames,
                    ctx.CullingResults,
                    ctx.Camera,
                    m_Definition.RenderingLayerMask);

            m_Handle = renderGraph.CreateRendererList(desc);
        }
    }
}
