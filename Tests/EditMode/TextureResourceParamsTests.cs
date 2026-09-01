// <copyright file="TextureResourceParamsTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for <see cref="TextureResourceParams"/> and <see cref="RendererListParams"/> —
    /// the pass-owned value-type resource parameters that replaced the legacy
    /// resource definition / resource node layer.
    /// </summary>
    public sealed class TextureResourceParamsTests
    {
        #region TextureResourceParams Defaults

        /// <summary>
        /// <see cref="TextureResourceParams.CreateDefault"/> carries the documented
        /// defaults: R8G8B8A8 color format, no depth, full-resolution scale,
        /// Bilinear / Repeat / Tex2D, no mip, clear enabled with black clear color.
        /// </summary>
        [Test]
        public void TextureResourceParams_CreateDefault_HasDocumentedDefaults()
        {
            TextureResourceParams p = TextureResourceParams.CreateDefault();

            Assert.That(p.ColorFormat, Is.EqualTo(GraphicsFormat.R8G8B8A8_UNorm),
                "Default ColorFormat should be R8G8B8A8_UNorm.");
            Assert.That(p.DepthBits, Is.EqualTo(DepthBits.None),
                "Default DepthBits should be None.");
            Assert.That(p.TextureScale, Is.EqualTo(Vector2.one),
                "Default TextureScale should be full resolution.");
            Assert.That(p.Width, Is.EqualTo(0),
                "Default Width should be 0 (camera-scale mode).");
            Assert.That(p.Height, Is.EqualTo(0),
                "Default Height should be 0 (camera-scale mode).");
            Assert.That(p.FilterMode, Is.EqualTo(FilterMode.Bilinear),
                "Default FilterMode should be Bilinear.");
            Assert.That(p.WrapMode, Is.EqualTo(TextureWrapMode.Repeat),
                "Default WrapMode should be Repeat.");
            Assert.That(p.TextureDimension, Is.EqualTo(TextureDimension.Tex2D),
                "Default TextureDimension should be Tex2D.");
            Assert.That(p.UseMipMap, Is.False,
                "Default UseMipMap should be false.");
            Assert.That(p.ClearBuffer, Is.True,
                "Default ClearBuffer should be true.");
            Assert.That(p.ClearColor, Is.EqualTo(Color.black),
                "Default ClearColor should be black.");
        }

        /// <summary>
        /// <see cref="TextureResourceParams.CreateDesc"/> maps parameters onto a
        /// <see cref="TextureDesc"/>, including the camera-scaled size mode.
        /// </summary>
        [Test]
        public void TextureResourceParams_CreateDesc_ScalesByCamera()
        {
            var cameraGo = new GameObject("TextureResourceParamsTestsCamera");
            var camera = cameraGo.AddComponent<Camera>();
            try
            {
                TextureResourceParams p = TextureResourceParams.CreateDefault();
                TextureDesc desc = p.CreateDesc("Color Buffer", camera);

                Assert.That(desc.name, Is.EqualTo("Color Buffer"));
                Assert.That(desc.colorFormat, Is.EqualTo(GraphicsFormat.R8G8B8A8_UNorm));
                Assert.That(desc.depthBufferBits, Is.EqualTo(DepthBits.None));
                Assert.That(desc.width, Is.EqualTo(Mathf.Max(1, camera.pixelWidth)));
                Assert.That(desc.height, Is.EqualTo(Mathf.Max(1, camera.pixelHeight)));
                Assert.That(desc.clearBuffer, Is.True);
                Assert.That(desc.clearColor, Is.EqualTo(Color.black));
                Assert.That(desc.useMipMap, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(cameraGo);
            }
        }

        /// <summary>
        /// <see cref="TextureResourceParams.CreateDesc"/> uses the fixed
        /// Width / Height when both are positive, ignoring camera pixel size.
        /// </summary>
        [Test]
        public void TextureResourceParams_CreateDesc_UsesFixedSize()
        {
            var cameraGo = new GameObject("TextureResourceParamsTestsCamera");
            var camera = cameraGo.AddComponent<Camera>();
            try
            {
                TextureResourceParams p = TextureResourceParams.CreateDefault();
                p.Width = 4096;
                p.Height = 2048;
                p.ColorFormat = GraphicsFormat.B10G11R11_UFloatPack32;
                p.DepthBits = DepthBits.Depth32;

                TextureDesc desc = p.CreateDesc("Atlas", camera);

                Assert.That(desc.width, Is.EqualTo(4096));
                Assert.That(desc.height, Is.EqualTo(2048));
                Assert.That(desc.colorFormat, Is.EqualTo(GraphicsFormat.B10G11R11_UFloatPack32));
                Assert.That(desc.depthBufferBits, Is.EqualTo(DepthBits.Depth32));
            }
            finally
            {
                Object.DestroyImmediate(cameraGo);
            }
        }

        #endregion

        #region RendererListParams Defaults

        /// <summary>
        /// <see cref="RendererListParams.CreateDefault"/> carries the documented
        /// defaults: opaque list kind and layer mask <c>0x00000001</c>.
        /// </summary>
        [Test]
        public void RendererListParams_CreateDefault_HasDocumentedDefaults()
        {
            RendererListParams p = RendererListParams.CreateDefault();

            Assert.That(p.ListKind, Is.EqualTo(RenderListKind.Opaque),
                "Default ListKind should be Opaque.");
            Assert.That(p.RenderingLayerMask, Is.EqualTo(0x00000001u),
                "Default RenderingLayerMask should be 0x00000001.");
        }

        #endregion
    }
}
