// <copyright file="IPassDebugPreviewProvider.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEngine;

namespace HN.HNRP
{
    /// <summary>
    /// 由「愿意把内部渲染目标暴露给调试预览」的 pass 实现的契约。
    /// </summary>
    /// <remarks>
    /// 与 <see cref="IPassDebugProvider"/> 分开定义：前者描述「可被数值化查看的通道」，
    /// 本接口描述「可被当作图片看的中间纹理」（如阴影图集、反射探针图集）。
    /// 两者相互独立，pass 可以只实现其中之一。
    /// </remarks>
    public interface IPassDebugPreviewProvider
    {
        /// <summary>
        /// 取本 pass 当前可供预览的纹理。
        /// </summary>
        /// <param name="texture">可预览纹理；不可用时为 <c>null</c>。</param>
        /// <param name="sliceCount">texture array 的 slice 数量（非数组纹理为 1）。</param>
        /// <param name="mipCount">可预览的 mip 级数（无 mip 为 1）。</param>
        /// <returns>当前存在可预览纹理时返回 <c>true</c>。</returns>
        bool TryGetDebugPreview(out Texture texture, out int sliceCount, out int mipCount);
    }
}
