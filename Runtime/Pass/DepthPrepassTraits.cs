// <copyright file="DepthPrepassTraits.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

namespace HN.HNRP
{
    /// <summary>
    /// 「不透明深度预渲染（PreZ）」资源语义契约（R 通道）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 由深度预渲染生产者（<see cref="DepthOnlyPass"/> / <see cref="DepthNormalPass"/>）
    /// 在其深度输出上发布；消费者（如 <see cref="ForwardPass"/>）据此判断能否用
    /// <c>ZTest Equal</c> 复用预渲染深度、减少 overdraw。
    /// </para>
    /// <para>
    /// 契约与实现均由本特征拥有 —— 核心通道（<see cref="PassSlot"/> /
    /// <see cref="IResourceTraits"/>）不感知本语义，新增其它语义无需改动它们。
    /// </para>
    /// </remarks>
    public interface IDepthPrepassTraits : IResourceTraits
    {
        /// <summary>
        /// 本预渲染深度覆盖的渲染层掩码。
        /// </summary>
        uint CoverageLayerMask { get; }

        /// <summary>
        /// 判断本预渲染深度是否可安全地作为给定消费者的 PreZ 使用。
        /// </summary>
        /// <param name="consumerLayerMask">消费者的渲染层掩码。</param>
        /// <returns>消费者渲染层被完全覆盖时返回 <c>true</c>。</returns>
        /// <remarks>
        /// 覆盖校验是 PreZ 的正确性前提：预渲染深度若未覆盖消费者的全部渲染层，
        /// 使用 <c>ZTest Equal</c> 会丢弃缺失的几何。
        /// </remarks>
        bool IsUsablePrepassDepth(uint consumerLayerMask);
    }

    /// <summary>
    /// <see cref="IDepthPrepassTraits"/> 的默认实现：不可变，按覆盖掩码构造。
    /// </summary>
    /// <remarks>
    /// 覆盖掩码来自生产者的渲染器列表参数（pass 配置），非每帧状态，
    /// 因此实现不可变、可在 pass 构造期创建一次并长期复用。
    /// </remarks>
    internal sealed class DepthPrepassTraits : IDepthPrepassTraits
    {
        /// <summary>
        /// 初始化 <see cref="DepthPrepassTraits"/> 的新实例。
        /// </summary>
        /// <param name="coverageLayerMask">本预渲染深度覆盖的渲染层掩码。</param>
        public DepthPrepassTraits(uint coverageLayerMask)
        {
            CoverageLayerMask = coverageLayerMask;
        }

        /// <inheritdoc />
        public uint CoverageLayerMask { get; }

        /// <inheritdoc />
        public bool IsUsablePrepassDepth(uint consumerLayerMask)
        {
            return (CoverageLayerMask & consumerLayerMask) == consumerLayerMask;
        }
    }
}
