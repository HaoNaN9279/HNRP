// <copyright file="ILightDatasProvider.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEngine;

namespace HN.HNRP
{
    /// <summary>
    /// 「光照数据缓冲生产者」契约。
    /// </summary>
    /// <remarks>
    /// 实现者（<see cref="BuildLightDataPass"/>）持有一份跨帧存活的光照数据结构化
    /// 缓冲，并通过本接口把该缓冲暴露给依赖它的消费者（<see cref="ClusterCullingLightPass"/>
    /// 需要把它绑定为 compute shader 的 SRV）。消费者从自身的
    /// <see cref="Pass.Dependencies"/> 中发现实现者，不硬编码具体生产者类型。
    /// </remarks>
    public interface ILightDatasProvider
    {
        /// <summary>
        /// 获取本生产者持有的光照数据结构化缓冲；尚未分配时为 <c>null</c>。
        /// </summary>
        ComputeBuffer LightDatasBuffer { get; }
    }
}
