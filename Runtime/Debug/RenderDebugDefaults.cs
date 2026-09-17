// <copyright file="RenderDebugDefaults.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using UnityEngine;

namespace HN.HNRP
{
    /// <summary>
    /// 渲染调试的默认布局与兜底参数，挂在
    /// <see cref="HNRenderPipelineGlobalSettings.DebugDefaults"/> 上。
    /// </summary>
    /// <remarks>
    /// 这里只放「进过一次就很少再改」的参数：文本区位置 / 字号、预览边长 / mip、
    /// 色带越界处理。它们刻意不出现在 SceneView 浮动面板里，避免面板被低频参数淹没。
    /// 每帧由 <see cref="RenderDebugManager.ResolveState"/> 合并进帧级状态。
    /// </remarks>
    [Serializable]
    public struct RenderDebugDefaults
    {
        /// <summary>单值文本区左上角（归一化屏幕坐标，原点在左上）。</summary>
        [Tooltip("单值文本区左上角（归一化屏幕坐标，原点在左上）")]
        public Rect OverlayRect;

        /// <summary>文本缩放（1 = 字模原始 16 像素行高）。</summary>
        [Range(0.5f, 4f)]
        [Tooltip("文本缩放（1 = 字模原始行高）")]
        public float FontScale;

        /// <summary>纹理预览区边长（像素）。</summary>
        [Tooltip("纹理预览区边长（像素）")]
        public float PreviewSize;

        /// <summary>纹理预览的 mip 级别。</summary>
        [Tooltip("纹理预览的 mip 级别")]
        public int PreviewMip;

        /// <summary>色带越界处理：<c>false</c> = Clamp，<c>true</c> = Wrap。</summary>
        [Tooltip("色带越界处理：false = Clamp，true = Wrap")]
        public bool ColorMapWrap;

        /// <summary>
        /// 文档化默认值：左上角文本区、字号 0.5、预览 256 像素、mip 0、Clamp。
        /// </summary>
        public static RenderDebugDefaults CreateDefault()
        {
            return new RenderDebugDefaults
            {
                OverlayRect = new Rect(0.01f, 0.01f, 0.4f, 0.9f),
                FontScale = 1.0f,
                PreviewSize = 256f,
                PreviewMip = 0,
                ColorMapWrap = false,
            };
        }
    }
}
