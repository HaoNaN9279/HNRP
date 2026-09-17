// <copyright file="RenderDebugSettings.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using UnityEngine;

namespace HN.HNRP
{
    /// <summary>
    /// 渲染调试配置（<b>进程级</b>，所有相机共享）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 刻意不做成相机级配置：调试显示是「看渲染结果」的手段，一次开关就应对全部相机
    /// 生效，逐个相机配置既繁琐又容易漏。因此本结构只保留<b>运行时选择</b>
    /// （总开关、SceneView 开关、三段显示开关、每像素通道、色带、预览源），
    /// 而布局类的「不重要参数」（文本区位置、字号、预览边长 / mip、色带越界处理）
    /// 统一放在 <see cref="HNRenderPipelineGlobalSettings.DebugDefaults"/> 里，
    /// 由 <see cref="RenderDebugManager.ResolveState"/> 解析时合并。
    /// </para>
    /// <para>相机维度的差异只保留一项：SceneView 相机可单独关闭。</para>
    /// </remarks>
    [Serializable]
    public struct RenderDebugSettings
    {
        /// <summary>渲染调试总开关。</summary>
        [Tooltip("渲染调试总开关")]
        public bool Enabled;

        /// <summary>SceneView 相机是否参与调试显示（Game / 其它相机不受此开关影响）。</summary>
        [Tooltip("SceneView 相机是否参与调试显示")]
        public bool AffectsSceneView;

        /// <summary>三段式显示开关（数值 / 每像素 / 预览）。</summary>
        [Tooltip("三段式显示开关")]
        public DebugDisplayMode DisplayMode;

        /// <summary>
        /// 每像素通道来源 pass 的<b>注册显示名</b>
        /// （<see cref="PassAttribute.DisplayName"/>，如 "Draw Object"）。
        /// </summary>
        /// <remarks>
        /// 用注册显示名而非实例名：同一类型在渲染图内可有多个实例
        /// （如 <c>forwardOpaque</c> / <c>transparency</c>），它们应共享同一通道选择。
        /// </remarks>
        [Tooltip("每像素通道来源 pass 的注册显示名")]
        public string PerPixelPassName;

        /// <summary>选中的每像素通道 ID（由来源 pass 定义）。</summary>
        [Tooltip("选中的每像素通道 ID")]
        public int PerPixelChannelId;

        /// <summary>每像素值的颜色渐变映射配置。</summary>
        [Tooltip("颜色渐变映射配置")]
        public DebugColorMapSettings ColorMap;

        /// <summary>纹理预览来源 pass（实例名或注册显示名）。</summary>
        [Tooltip("纹理预览来源 pass")]
        public string PreviewPassName;

        /// <summary>预览 texture array 的 slice 索引。</summary>
        [Tooltip("预览 texture array 的 slice")]
        public int PreviewSlice;

        /// <summary>
        /// 文档化默认配置：关闭、SceneView 生效、三段全开、未选择通道。
        /// </summary>
        public static RenderDebugSettings Default
        {
            get
            {
                return new RenderDebugSettings
                {
                    Enabled = false,
                    AffectsSceneView = true,
                    DisplayMode = DebugDisplayMode.All,
                    PerPixelPassName = null,
                    PerPixelChannelId = -1,
                    ColorMap = DebugColorMapSettings.Default,
                    PreviewPassName = null,
                    PreviewSlice = 0,
                };
            }
        }

        /// <summary>「数值」段是否应绘制。</summary>
        public bool DrawSingleValues => Enabled && (DisplayMode & DebugDisplayMode.SingleValues) != 0;

        /// <summary>「每像素」段是否应生效。</summary>
        public bool DrawPerPixel =>
            Enabled
            && (DisplayMode & DebugDisplayMode.PerPixel) != 0
            && ColorMap.Enabled
            && !string.IsNullOrEmpty(PerPixelPassName)
            && PerPixelChannelId >= 0;

        /// <summary>「预览」段是否应绘制。</summary>
        public bool DrawTexturePreview =>
            Enabled
            && (DisplayMode & DebugDisplayMode.TexturePreview) != 0
            && !string.IsNullOrEmpty(PreviewPassName);
    }
}
