// <copyright file="TextureResourceDefinition.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    /// <summary>
    /// 纹理资源的定义。描述运行时分配（或导入）一张渲染图纹理所需的全部参数。
    /// </summary>
    /// <remarks>
    /// 仅纹理相关字段；ComputeBuffer 与 RendererList 参数见各自定义子类。
    /// </remarks>
    [Serializable]
    public sealed class TextureResourceDefinition : ResourceDefinition
    {
        /// <summary>
        /// 分配纹理的颜色格式。深度纹理忽略此字段（改由 <see cref="DepthBits"/> 控制）。
        /// </summary>
        public GraphicsFormat ColorFormat = GraphicsFormat.R8G8B8A8_UNorm;

        /// <summary>
        /// 深度缓冲位数。<see cref="DepthBits.None"/> 表示非深度纹理。
        /// </summary>
        public DepthBits DepthBits = DepthBits.None;

        /// <summary>
        /// 相对相机像素尺寸的缩放系数。默认全分辨率。
        /// 当 <see cref="Width"/> 与 <see cref="Height"/> 均为正时被忽略（固定尺寸模式）。
        /// </summary>
        public Vector2 TextureScale = Vector2.one;

        /// <summary>
        /// 固定纹理宽度（像素）。与 <see cref="Height"/> 同时为正时使用固定尺寸，
        /// 否则按相机缩放。默认 0（相机缩放模式）。
        /// </summary>
        public int Width;

        /// <summary>
        /// 固定纹理高度（像素）。与 <see cref="Width"/> 同时为正时使用固定尺寸。
        /// 默认 0。
        /// </summary>
        public int Height;

        /// <summary>
        /// 采样过滤模式。
        /// </summary>
        public FilterMode FilterMode;

        /// <summary>
        /// UV 包裹模式。
        /// </summary>
        public TextureWrapMode WrapMode;

        /// <summary>
        /// 纹理维度。
        /// </summary>
        public TextureDimension TextureDimension;

        /// <summary>
        /// 是否生成 mipmap。默认 <c>false</c>。
        /// </summary>
        public bool UseMipMap;

        /// <summary>
        /// 是否自动生成 mipmap。
        /// </summary>
        public bool AutoGenerateMips;

        /// <summary>
        /// 是否在首次使用前清空。清空由渲染图内联到首个写入该资源的 pass。
        /// </summary>
        public bool ClearBuffer = true;

        /// <summary>
        /// <see cref="ClearBuffer"/> 为 <c>true</c> 时的清空颜色。
        /// </summary>
        public Color ClearColor = Color.black;

        /// <summary>
        /// 外部纹理名（如 "emptyTexture"）。非空时运行时从
        /// <see cref="HNRenderPipelineRuntimeResources"/> 导入该纹理，而非按相机分配新 RenderTexture。
        /// </summary>
        public string ExternalTextureName;

        /// <inheritdoc />
        public override ResourceKind Kind => ResourceKind.Texture;

        /// <inheritdoc />
        public override ResourceNode CreateNode() => new TextureResourceNode(this);

        /// <inheritdoc />
        public override void CopyFrom(ResourceDefinition source)
        {
            if (source is TextureResourceDefinition s)
            {
                ColorFormat = s.ColorFormat;
                DepthBits = s.DepthBits;
                TextureScale = s.TextureScale;
                Width = s.Width;
                Height = s.Height;
                FilterMode = s.FilterMode;
                WrapMode = s.WrapMode;
                TextureDimension = s.TextureDimension;
                UseMipMap = s.UseMipMap;
                AutoGenerateMips = s.AutoGenerateMips;
                ClearBuffer = s.ClearBuffer;
                ClearColor = s.ClearColor;
                ExternalTextureName = s.ExternalTextureName;
            }
        }

        /// <inheritdoc />
        public override IReadOnlyList<IResourcePreset> Presets => s_Presets;

        /// <summary>
        /// 内置预设集合。新增预设：加一个静态字段并追加到此数组。
        /// </summary>
        private static readonly IResourcePreset[] s_Presets =
        {
            new ResourcePreset<TextureResourceDefinition>("Default LDR", new TextureResourceDefinition()),
            new ResourcePreset<TextureResourceDefinition>(
                "Depth 32",
                new TextureResourceDefinition { DepthBits = DepthBits.Depth32 }),
            new ResourcePreset<TextureResourceDefinition>(
                "HDR Atlas 4096",
                new TextureResourceDefinition
                {
                    Width = 4096,
                    Height = 4096,
                    ColorFormat = GraphicsFormat.B10G11R11_UFloatPack32,
                    ClearBuffer = true,
                    ClearColor = Color.black,
                    UseMipMap = true,
                    AutoGenerateMips = false,
                    FilterMode = FilterMode.Trilinear,
                    WrapMode = TextureWrapMode.Clamp,
                    TextureDimension = TextureDimension.Tex2D,
                }),
        };
    }
}
