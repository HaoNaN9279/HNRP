// <copyright file="ResourceDefinition.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    /// <summary>
    /// Serializable asset-level definition of a render graph resource node.
    /// Describes how the runtime resource is allocated (or imported) each frame.
    /// </summary>
    /// <remarks>
    /// Only the fields relevant to <see cref="ResourceKind"/> are used:
    /// <list type="bullet">
    ///   <item><b>Texture</b> — <see cref="ColorFormat"/>, <see cref="DepthBits"/>, <see cref="TextureScale"/>, <see cref="ClearBuffer"/>, <see cref="ClearColor"/>, or <see cref="ExternalTextureName"/> for externally imported textures.</item>
    ///   <item><b>ComputeBuffer</b> — <see cref="BufferCount"/>, <see cref="BufferStride"/>.</item>
    ///   <item><b>RendererList</b> — <see cref="ListKind"/>, <see cref="RenderingLayerMask"/>.</item>
    /// </list>
    /// </remarks>
    [Serializable]
    public class ResourceDefinition
    {
        /// <summary>
        /// The name used to match this definition against
        /// <see cref="ResourceConnection.ResourceName"/> entries.
        /// Must be non-null and unique within the render graph asset.
        /// </summary>
        public string ResourceName;

        /// <summary>
        /// The kind of resource this definition describes.
        /// </summary>
        public ResourceKind ResourceKind;

        // ── Texture (only when ResourceKind == Texture) ──

        /// <summary>
        /// The color format of the allocated texture.
        /// Ignored for depth-only textures (use <see cref="DepthBits"/> instead).
        /// </summary>
        public GraphicsFormat ColorFormat = GraphicsFormat.R8G8B8A8_UNorm;

        /// <summary>
        /// The depth buffer bit count. <see cref="DepthBits.None"/> for a
        /// non-depth texture.
        /// </summary>
        public DepthBits DepthBits = DepthBits.None;

        /// <summary>
        /// Scale factor applied to the camera's pixel dimensions when sizing
        /// the allocated texture. Default is full resolution.
        /// Ignored when <see cref="Width"/> and <see cref="Height"/> are positive
        /// (fixed-size texture mode).
        /// </summary>
        public Vector2 TextureScale = Vector2.one;

        /// <summary>
        /// Fixed texture width in pixels. When positive (along with
        /// <see cref="Height"/>), the texture uses fixed dimensions instead
        /// of camera-scaled dimensions. Default is 0 (camera-scaled mode).
        /// </summary>
        public int Width;

        /// <summary>
        /// Fixed texture height in pixels. When positive (along with
        /// <see cref="Width"/>), the texture uses fixed dimensions instead
        /// of camera-scaled dimensions. Default is 0 (camera-scaled mode).
        /// </summary>
        public int Height;

        /// <summary>
        /// Whether the allocated texture should have mipmaps.
        /// Default is <c>false</c>.
        /// </summary>
        public bool UseMipMap;

        /// <summary>
        /// Whether the allocated texture should have its mipmaps automatically generated.
        /// </summary>
        public bool AutoGenerateMips;

        /// <summary>
        /// Whether the allocated texture should be cleared before first use.
        /// The clear is inlined by the render graph into the first pass that
        /// writes the resource.
        /// </summary>
        public bool ClearBuffer = true;

        /// <summary>
        /// The clear color used when <see cref="ClearBuffer"/> is <c>true</c>.
        /// </summary>
        public Color ClearColor = Color.black;

        /// <summary>
        /// External texture name (e.g. "emptyTexture"). When non-empty the
        /// texture resource is imported at runtime from
        /// <see cref="HNRenderPipelineRuntimeResources"/> instead of allocating a
        /// new RenderTexture sized by the camera.
        /// </summary>
        public string ExternalTextureName;

        // ── ComputeBuffer (only when ResourceKind == ComputeBuffer) ──

        /// <summary>
        /// The element count of the allocated compute buffer.
        /// </summary>
        public int BufferCount;

        /// <summary>
        /// The byte stride of each element in the allocated compute buffer.
        /// </summary>
        public int BufferStride;

        // ── RendererList (only when ResourceKind == RendererList) ──

        /// <summary>
        /// Which render-queue scope to use when building the renderer list
        /// descriptor (opaque or transparent).
        /// </summary>
        public RenderListKind ListKind = RenderListKind.Opaque;

        /// <summary>
        /// The rendering layer mask applied when building the renderer list
        /// descriptor. Only renderers on matching rendering layers are included.
        /// </summary>
        public uint RenderingLayerMask = 0x00000001;
    }
}
