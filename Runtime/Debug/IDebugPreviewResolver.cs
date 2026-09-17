// <copyright file="IDebugPreviewResolver.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEngine;

namespace HN.HNRP
{
    /// <summary>
    /// 纹理预览源的解析器。由宿主（<see cref="CameraRenderer"/>）实现，
    /// 使 <see cref="RenderDebugManager"/> 无需依赖 pass 列表即可解析预览源。
    /// </summary>
    public interface IDebugPreviewResolver
    {
        /// <summary>
        /// 尝试解析本帧纹理预览要显示的纹理。
        /// </summary>
        /// <param name="context">当前帧的相机上下文。</param>
        /// <param name="texture">解析出的纹理；失败时为 <c>null</c>。</param>
        /// <param name="slice">texture array 的 slice 索引。</param>
        /// <param name="mip">mip 级别。</param>
        /// <returns>解析成功返回 <c>true</c>。</returns>
        bool TryResolvePreview(CameraContext context, out Texture texture, out int slice, out int mip);
    }
}
