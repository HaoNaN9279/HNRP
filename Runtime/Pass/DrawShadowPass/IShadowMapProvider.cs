// <copyright file="IShadowMapProvider.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    /// <summary>
    /// 「阴影图集 / 阴影表生产者」契约。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 实现者（<see cref="DrawShadowPass"/>）持有跨帧存活的阴影图集（<see cref="Texture2DArray"/>）
    /// 与三张结构化缓冲（<c>_ShadowLightDatas</c> / <c>_ShadowMapDatas</c> /
    /// <c>_ShadowCameraDatas</c>），并通过本接口把资源暴露给依赖它的消费者。
    /// </para>
    /// <para>
    /// 消费者（<see cref="ScreenSpaceShadowMaskPass"/>）从自身的
    /// <see cref="Pass.Dependencies"/> 中发现实现者，随后：
    /// <list type="bullet">
    ///   <item>用 <see cref="IGlobalShaderResource.BindGlobalShaderResources"/> 绑定图集 /
    ///     三张表 / 阴影标量常量缓冲（复用生产者既有绑定知识）；</item>
    ///   <item>用 <see cref="ShadowMapProduced"/> 判定本帧是否确有产出，未产出时跳过绘制
    ///     （避免读取上一帧的陈旧缓冲）。</item>
    /// </list>
    /// </para>
    /// <para>
    /// 图集与表属 <b>Persistent</b> 资源（随生产者 pass 实例跨帧存活，见 ADR-020），
    /// 不经 RenderGraph 句柄传递，故消费端只用接口读取，不做释放。
    /// </para>
    /// </remarks>
    public interface IShadowMapProvider : IGlobalShaderResource
    {
        /// <summary>
        /// 获取本帧是否已产出有效的阴影图集与阴影表。
        /// </summary>
        bool ShadowMapProduced { get; }

        /// <summary>
        /// 获取阴影图集（<c>Texture2DArray</c>）；尚未分配时为 <c>null</c>。
        /// </summary>
        RTHandle ShadowAtlas { get; }

        /// <summary>
        /// 获取按 light 的阴影元数据缓冲（<c>_ShadowLightDatas</c>）；尚未分配时为 <c>null</c>。
        /// </summary>
        ComputeBuffer ShadowLightDatasBuffer { get; }

        /// <summary>
        /// 获取按 atlas 槽的阴影数据缓冲（<c>_ShadowMapDatas</c>）；尚未分配时为 <c>null</c>。
        /// </summary>
        ComputeBuffer ShadowMapDatasBuffer { get; }

        /// <summary>
        /// 获取每方向光阴影参数缓冲（<c>_ShadowCameraDatas</c>）；尚未分配时为 <c>null</c>。
        /// </summary>
        ComputeBuffer ShadowCameraDatasBuffer { get; }
    }
}
