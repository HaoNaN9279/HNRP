// <copyright file="ResourceNodeTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for the resource-node layer of the render graph:
    /// <see cref="ResourceDefinition"/>, <see cref="ResourceConnection"/>,
    /// <see cref="TextureResourceNode"/>, <see cref="ComputeBufferResourceNode"/>,
    /// <see cref="RendererListResourceNode"/>, and the
    /// <see cref="PassSlot.ConnectResource"/> / <see cref="PassSlot{T}.ReadHandle"/>
    /// resource branch. Pure unit tests — no <see cref="RenderGraph"/> is executed.
    /// External-texture import is covered through
    /// <see cref="HNRenderPipelineRuntimeResources.GetExternalTexture"/> and the
    /// missing-texture fallback of <see cref="TextureResourceNode.Resolve"/>.
    /// </summary>
    public sealed class ResourceNodeTests
    {
        #region Test Helpers

        /// <summary>
        /// Creates a test <see cref="Camera"/> attached to a new
        /// <see cref="GameObject"/>. The caller is responsible for destroying the
        /// GameObject.
        /// </summary>
        private static Camera CreateTestCamera()
        {
            var go = new GameObject("ResourceNodeTestsCamera");
            return go.AddComponent<Camera>();
        }

        #endregion

        #region ResourceDefinition Defaults

        /// <summary>
        /// A fresh <see cref="TextureResourceDefinition"/> carries the documented defaults:
        /// R8G8B8A8 color format, no depth, full-resolution scale, clear enabled,
        /// and black clear color.
        /// </summary>
        [Test]
        public void TextureResourceDefinition_HasDocumentedDefaults()
        {
            var def = new TextureResourceDefinition();

            Assert.That(def.ResourceName, Is.Null,
                "ResourceName has no default value.");
            Assert.That(def.ColorFormat, Is.EqualTo(GraphicsFormat.R8G8B8A8_UNorm),
                "Default ColorFormat should be R8G8B8A8_UNorm.");
            Assert.That(def.DepthBits, Is.EqualTo(DepthBits.None),
                "Default DepthBits should be None.");
            Assert.That(def.TextureScale, Is.EqualTo(Vector2.one),
                "Default TextureScale should be full resolution.");
            Assert.That(def.ClearBuffer, Is.True,
                "Default ClearBuffer should be true.");
            Assert.That(def.ClearColor, Is.EqualTo(Color.black),
                "Default ClearColor should be black.");
        }

        /// <summary>
        /// A fresh <see cref="RendererListResourceDefinition"/> carries the documented
        /// defaults: opaque list kind and layer mask <c>0x00000001</c>.
        /// </summary>
        [Test]
        public void RendererListResourceDefinition_HasDocumentedDefaults()
        {
            var def = new RendererListResourceDefinition();

            Assert.That(def.ListKind, Is.EqualTo(RenderListKind.Opaque),
                "Default ListKind should be Opaque.");
            Assert.That(def.RenderingLayerMask, Is.EqualTo(0x00000001u),
                "Default RenderingLayerMask should be 0x00000001.");
        }

        #endregion

        #region ResourceConnection Field Assignment

        /// <summary>
        /// <see cref="ResourceConnection"/> is a plain serializable data holder;
        /// its three name fields can be assigned and read back. There is no
        /// direction field — a connection always means the named resource feeds
        /// the named pass input slot.
        /// </summary>
        [Test]
        public void ResourceConnection_AssignableFields()
        {
            var conn = new ResourceConnection
            {
                ResourceName = "ColorBuffer",
                PassName = "forwardOpaque",
                SlotName = "ColorTarget",
            };

            Assert.That(conn.ResourceName, Is.EqualTo("ColorBuffer"));
            Assert.That(conn.PassName, Is.EqualTo("forwardOpaque"));
            Assert.That(conn.SlotName, Is.EqualTo("ColorTarget"));
        }

        #endregion

        #region TextureResourceNode

        /// <summary>
        /// Before <see cref="TextureResourceNode.Resolve"/>, GetHandle returns
        /// the default (invalid) <see cref="TextureHandle"/> — the handle is only
        /// assigned during Resolve (allocation or external import).
        /// </summary>
        [Test]
        public void TextureResourceNode_GetHandle_Unresolved_ReturnsDefault()
        {
            var node = new TextureResourceNode(new TextureResourceDefinition());

            Assert.That(node.GetHandle(), Is.EqualTo(default(TextureHandle)),
                "An unresolved texture node should expose a default handle.");
        }

        #endregion

        #region ComputeBufferResourceNode

        /// <summary>
        /// Before <see cref="ComputeBufferResourceNode.Resolve"/>, GetHandle
        /// returns the default (invalid) <see cref="ComputeBufferHandle"/> — the
        /// handle is only assigned during Resolve.
        /// </summary>
        [Test]
        public void ComputeBufferResourceNode_GetHandle_Unresolved_ReturnsDefault()
        {
            var node = new ComputeBufferResourceNode(new ComputeBufferResourceDefinition());

            Assert.That(node.GetHandle(), Is.EqualTo(default(ComputeBufferHandle)),
                "An unresolved compute buffer node should expose a default handle.");
        }

        #endregion

        #region RendererListResourceNode

        /// <summary>
        /// Renderer lists have no producer concept; before
        /// <see cref="RendererListResourceNode.Resolve"/> the node exposes a
        /// default (invalid) <see cref="RendererListHandle"/>.
        /// </summary>
        [Test]
        public void RendererListResourceNode_GetHandle_ReturnsDefault()
        {
            var node = new RendererListResourceNode(new RendererListResourceDefinition());

            Assert.That(node.GetHandle(), Is.EqualTo(default(RendererListHandle)));
        }

        #endregion

        #region External Texture Import

        /// <summary>
        /// <see cref="HNRenderPipelineRuntimeResources.GetExternalTexture"/>
        /// resolves the well-known <c>"emptyTexture"</c> name to the pipeline's
        /// empty texture (see <see cref="HNRenderPipelineRuntimeResources.emptyTexture"/>).
        /// </summary>
        [Test]
        public void RuntimeResources_GetExternalTexture_KnownName_ReturnsTexture()
        {
            var resources = ScriptableObject.CreateInstance<HNRenderPipelineRuntimeResources>();
            try
            {
                Texture tex = resources.GetExternalTexture("emptyTexture");

                Assert.That(tex, Is.Not.Null,
                    "GetExternalTexture(\"emptyTexture\") should return the pipeline empty texture.");
                Assert.That(tex, Is.SameAs(Texture2D.blackTexture),
                    "emptyTexture should be Texture2D.blackTexture.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(resources);
            }
        }

        /// <summary>
        /// <see cref="HNRenderPipelineRuntimeResources.GetExternalTexture"/>
        /// returns <c>null</c> for unknown texture names.
        /// </summary>
        [Test]
        public void RuntimeResources_GetExternalTexture_UnknownName_ReturnsNull()
        {
            var resources = ScriptableObject.CreateInstance<HNRenderPipelineRuntimeResources>();
            try
            {
                Assert.That(resources.GetExternalTexture("doesNotExist"), Is.Null,
                    "An unknown external texture name should resolve to null.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(resources);
            }
        }

        /// <summary>
        /// A <see cref="TextureResourceNode"/> whose external texture is missing
        /// keeps its default handle: Resolve logs a warning and returns without
        /// touching the render graph (the handle stays invalid, matching the
        /// documented fallback).
        /// </summary>
        [Test]
        public void TextureResourceNode_Resolve_ExternalTextureMissing_KeepsDefaultHandle()
        {
            var node = new TextureResourceNode(
                new TextureResourceDefinition
                {
                    ResourceName = "UnknownExternalTex",
                    ExternalTextureName = "doesNotExist",
                });

            var resources = ScriptableObject.CreateInstance<HNRenderPipelineRuntimeResources>();
            var camera = CreateTestCamera();
            try
            {
                var ctx = new CameraContext(camera, new ScriptableRenderContext())
                {
                    RuntimeResources = resources,
                };

                Assert.DoesNotThrow(() => node.Resolve(renderGraph: null, ctx),
                    "Resolve with a missing external texture should not throw.");
                Assert.That(node.GetHandle(), Is.EqualTo(default(TextureHandle)),
                    "A node whose external texture is missing should keep the default handle.");

                ctx.Dispose();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
                UnityEngine.Object.DestroyImmediate(resources);
            }
        }

        #endregion

        #region PassSlot.ConnectResource

        /// <summary>
        /// <see cref="PassSlot.ConnectResource"/> rejects <c>null</c>.
        /// </summary>
        [Test]
        public void ConnectResource_NullNode_Throws()
        {
            var input = new TextureSlot("TexIn", SlotDirection.Input);

            Assert.Throws<ArgumentNullException>(() => input.ConnectResource(null));
        }

        /// <summary>
        /// <see cref="PassSlot.ConnectResource"/> on an output slot is not allowed.
        /// </summary>
        [Test]
        public void ConnectResource_OnOutputSlot_Throws()
        {
            var output = new TextureSlot("TexOut", SlotDirection.Output);
            var node = new TextureResourceNode(new TextureResourceDefinition());

            Assert.Throws<InvalidOperationException>(() => output.ConnectResource(node));
        }

        /// <summary>
        /// Connecting a <see cref="TextureSlot"/> to a
        /// <see cref="ComputeBufferResourceNode"/> throws
        /// <see cref="ArgumentException"/> — the resource node type must match
        /// the slot's resource type.
        /// </summary>
        [Test]
        public void ConnectResource_TypeMismatch_Throws()
        {
            var input = new TextureSlot("TexIn", SlotDirection.Input);
            var bufferNode = new ComputeBufferResourceNode(new ComputeBufferResourceDefinition());

            Assert.Throws<ArgumentException>(() => input.ConnectResource(bufferNode),
                "A texture input slot must reject a compute buffer resource node.");

            var bufferInput = new ComputeBufferSlot("BufIn", SlotDirection.Input);
            var textureNode = new TextureResourceNode(new TextureResourceDefinition());
            Assert.Throws<ArgumentException>(() => bufferInput.ConnectResource(textureNode),
                "A compute buffer input slot must reject a texture resource node.");
        }

        /// <summary>
        /// A successful <see cref="PassSlot.ConnectResource"/> marks the input
        /// slot as connected and stores the resource node.
        /// </summary>
        [Test]
        public void ConnectResource_Success_SetsConnectedState()
        {
            var input = new TextureSlot("TexIn", SlotDirection.Input);
            var node = new TextureResourceNode(new TextureResourceDefinition());

            input.ConnectResource(node);

            Assert.That(input.IsConnected, Is.True,
                "A connected resource must mark the input slot as connected.");
            Assert.That(input.ConnectedResource, Is.SameAs(node),
                "ConnectedResource should reference the connected node.");
        }

        /// <summary>
        /// <see cref="PassSlot{T}.ReadHandle"/> reads through the connected
        /// resource node branch: an input connected to a resource node returns
        /// the node's current handle instead of throwing (a pass-to-pass
        /// connection was never made). The resource node owns its handle — it is
        /// assigned by <see cref="TextureResourceNode.Resolve"/>, not by a
        /// producer slot.
        /// </summary>
        [Test]
        public void ReadHandle_ConnectedResource_ReturnsNodeHandle()
        {
            var input = new TextureSlot("TexIn", SlotDirection.Input);
            var node = new TextureResourceNode(new TextureResourceDefinition());
            input.ConnectResource(node);

            Assert.DoesNotThrow(() => input.ReadHandle());
            Assert.That(input.ReadHandle(), Is.EqualTo(default(TextureHandle)),
                "An input connected to a resource node should read the node's handle.");
        }

        #endregion
    }
}
