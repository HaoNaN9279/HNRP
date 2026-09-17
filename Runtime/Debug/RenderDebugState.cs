// <copyright file="RenderDebugState.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEngine;

namespace HN.HNRP
{
    /// <summary>
    /// 帧级调试运行时状态：把进程级 <see cref="RenderDebugSettings"/> 与
    /// <see cref="RenderDebugDefaults"/> 合并为「本帧到底要画什么」的最终结论，
    /// 供显示 pass 与各生产者 pass 复用。
    /// </summary>
    /// <remarks>
    /// 状态对象按帧创建（挂 <see cref="CameraContext"/>），不跨帧保留；
    /// 需跨帧保留的只有 GPU 资源、单值注册表与全局配置
    /// （见 <see cref="RenderDebugManager"/>）。
    /// </remarks>
    public sealed class RenderDebugState
    {
        /// <summary>本帧生效的运行时配置快照（开关 / 通道 / 色带 / 预览源）。</summary>
        public RenderDebugSettings Settings;

        /// <summary>是否绘制单值文本列表。</summary>
        public bool SingleValuesActive;

        /// <summary>是否启用每像素颜色映射。</summary>
        public bool PerPixelActive;

        /// <summary>是否绘制纹理预览。</summary>
        public bool TexturePreviewActive;

        /// <summary>本帧上传到 GPU 的单值条目数。</summary>
        public int EntryCount;

        /// <summary>每像素颜色映射的来源 pass 注册显示名。</summary>
        public string PerPixelPassName;

        /// <summary>每像素颜色映射选中的通道 ID。</summary>
        public int PerPixelChannelId;

        /// <summary>纹理预览的源纹理；为 <c>null</c> 时预览不可用。</summary>
        public Texture PreviewTexture;

        /// <summary>预览 texture array 的 slice。</summary>
        public int PreviewSlice;

        /// <summary>文本区左上角（归一化屏幕坐标，原点在左上）——来自 GlobalSettings 默认值。</summary>
        public Rect OverlayRect;

        /// <summary>文本缩放——来自 GlobalSettings 默认值。</summary>
        public float FontScale;

        /// <summary>预览区边长（像素）——来自 GlobalSettings 默认值。</summary>
        public float PreviewSize;

        /// <summary>预览 mip 级别——来自 GlobalSettings 默认值。</summary>
        public int PreviewMip;

        /// <summary>
        /// 叠层是否需要 Y 翻转。
        /// </summary>
        /// <remarks>
        /// 取 <see cref="CameraContext.Flip"/> 的取反：最终 blit 已做 Y 翻转时
        /// （GameView / 直接写后备缓冲），相机目标即为显示方向，叠层按原样绘制；
        /// 未做翻转时（SceneView）目标上下倒置存储、由消费方翻回，叠层必须自行翻转。
        /// </remarks>
        public bool FlipY;

        /// <summary>本帧是否存在任何需要绘制的内容。</summary>
        public bool Any => SingleValuesActive || PerPixelActive || TexturePreviewActive;

        /// <summary>
        /// 重置为「本帧无调试内容」。
        /// </summary>
        public void Reset()
        {
            Settings = RenderDebugSettings.Default;
            SingleValuesActive = false;
            PerPixelActive = false;
            TexturePreviewActive = false;
            EntryCount = 0;
            PerPixelPassName = null;
            PerPixelChannelId = -1;
            PreviewTexture = null;
            PreviewSlice = 0;
            OverlayRect = RenderDebugDefaults.CreateDefault().OverlayRect;
            FontScale = 0.5f;
            PreviewSize = 256f;
            PreviewMip = 0;
        }
    }
}
