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
    /// </summary>
    public sealed class ResourceNodeTests
    {
        #region Test Pass Subclasses

        /// <summary>
        /// A minimal producer pass that registers one texture output slot and one
        /// compute buffer output slot. Used to verify that a resource node with a
        /// producer reads its handle from the producer slot.
        /// </summary>
        private sealed class FakeProducerPass : Pass
        {
            /// <summary>
            /// The registered texture output slot.
            /// </summary>
            public TextureSlot TextureOutput { get; private set; }

            /// <summary>
            /// The registered compute buffer output slot.
            /// </summary>
            public ComputeBufferSlot BufferOutput { get; private set; }

            /// <summary>
            /// Initializes a new instance with the given name.
            /// </summary>
            /// <param name="name">The pass instance name.</param>
            public FakeProducerPass(string name)
                : base(name)
            {
            }

            /// <inheritdoc />
            public override void SetupSlots()
            {
                TextureOutput = new TextureSlot("TexOut", SlotDirection.Output);
                RegisterSlot(TextureOutput);
                BufferOutput = new ComputeBufferSlot("BufOut", SlotDirection.Output);
                RegisterSlot(BufferOutput);
            }

            /// <inheritdoc />
            public override void Initialize(CameraContext context)
            {
            }

            /// <inheritdoc />
            public override void Record(RenderGraph renderGraph)
            {
            }
        }

        #endregion

        #region ResourceDefinition Defaults

        /// <summary>
        /// A fresh <see cref="ResourceDefinition"/> carries the documented defaults:
        /// R8G8B8A8 color format, no depth, full-resolution scale, clear enabled,
        /// black clear color, opaque list kind, and layer mask <c>0x00000001</c>.
        /// </summary>
        [Test]
        public void ResourceDefinition_HasDocumentedDefaults()
        {
            var def = new ResourceDefinition();

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
            Assert.That(def.ListKind, Is.EqualTo(RenderListKind.Opaque),
                "Default ListKind should be Opaque.");
            Assert.That(def.RenderingLayerMask, Is.EqualTo(0x00000001u),
                "Default RenderingLayerMask should be 0x00000001.");
        }

        #endregion

        #region ResourceConnection Field Assignment

        /// <summary>
        /// <see cref="ResourceConnection"/> is a plain serializable data holder;
        /// all four fields can be assigned and read back.
        /// </summary>
        [Test]
        public void ResourceConnection_AssignableFields()
        {
            var conn = new ResourceConnection
            {
                ResourceName = "ColorBuffer",
                PassName = "forwardOpaque",
                SlotName = "ColorTarget",
                Direction = ResourceConnectionDirection.ResourceToPass,
            };

            Assert.That(conn.ResourceName, Is.EqualTo("ColorBuffer"));
            Assert.That(conn.PassName, Is.EqualTo("forwardOpaque"));
            Assert.That(conn.SlotName, Is.EqualTo("ColorTarget"));
            Assert.That(conn.Direction, Is.EqualTo(ResourceConnectionDirection.ResourceToPass));

            conn.Direction = ResourceConnectionDirection.PassToResource;
            Assert.That(conn.Direction, Is.EqualTo(ResourceConnectionDirection.PassToResource));
        }

        #endregion

        #region TextureResourceNode

        /// <summary>
        /// Without a producer and before <see cref="TextureResourceNode.Resolve"/>,
        /// <see cref="TextureResourceNode.GetHandle"/> returns the default
        /// (invalid) <see cref="TextureHandle"/>.
        /// </summary>
        [Test]
        public void TextureResourceNode_GetHandle_WithoutProducer_ReturnsDefault()
        {
            var node = new TextureResourceNode();

            Assert.That(node.HasProducer, Is.False);
            Assert.That(node.GetHandle(), Is.EqualTo(default(TextureHandle)),
                "An unresolved, producer-less texture node should expose a default handle.");
        }

        /// <summary>
        /// <see cref="TextureResourceNode.HasProducer"/> reflects whether a
        /// producer slot has been assigned.
        /// </summary>
        [Test]
        public void TextureResourceNode_HasProducer_TracksProducerSlot()
        {
            var producer = new FakeProducerPass("Producer");
            producer.SetupSlots();

            var node = new TextureResourceNode();
            Assert.That(node.HasProducer, Is.False);

            node.ProducerSlot = producer.TextureOutput;
            Assert.That(node.HasProducer, Is.True);
        }

        /// <summary>
        /// A <see cref="TextureResourceNode"/> with a producer reads its handle
        /// directly from the producer's output slot.
        /// </summary>
        [Test]
        public void TextureResourceNode_GetHandle_WithProducer_ReadsProducerSlot()
        {
            var producer = new FakeProducerPass("Producer");
            producer.SetupSlots();

            var value = default(TextureHandle);
            producer.TextureOutput.SetHandle(value);

            var node = new TextureResourceNode { ProducerSlot = producer.TextureOutput };

            Assert.That(node.HasProducer, Is.True);
            Assert.That(node.GetHandle(), Is.EqualTo(value),
                "A produced texture node should read the handle set on its producer slot.");
        }

        #endregion

        #region ComputeBufferResourceNode

        /// <summary>
        /// Without a producer, <see cref="ComputeBufferResourceNode.GetHandle"/>
        /// returns the default (invalid) <see cref="ComputeBufferHandle"/>.
        /// </summary>
        [Test]
        public void ComputeBufferResourceNode_GetHandle_WithoutProducer_ReturnsDefault()
        {
            var node = new ComputeBufferResourceNode();

            Assert.That(node.HasProducer, Is.False);
            Assert.That(node.GetHandle(), Is.EqualTo(default(ComputeBufferHandle)));
        }

        /// <summary>
        /// A <see cref="ComputeBufferResourceNode"/> with a producer reads its
        /// handle directly from the producer's output slot.
        /// </summary>
        [Test]
        public void ComputeBufferResourceNode_GetHandle_WithProducer_ReadsProducerSlot()
        {
            var producer = new FakeProducerPass("Producer");
            producer.SetupSlots();

            var value = default(ComputeBufferHandle);
            producer.BufferOutput.SetHandle(value);

            var node = new ComputeBufferResourceNode { ProducerSlot = producer.BufferOutput };

            Assert.That(node.HasProducer, Is.True);
            Assert.That(node.GetHandle(), Is.EqualTo(value),
                "A produced compute buffer node should read the handle set on its producer slot.");
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
            var node = new RendererListResourceNode();

            Assert.That(node.GetHandle(), Is.EqualTo(default(RendererListHandle)));
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
            var node = new TextureResourceNode();

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
            var bufferNode = new ComputeBufferResourceNode();

            Assert.Throws<ArgumentException>(() => input.ConnectResource(bufferNode),
                "A texture input slot must reject a compute buffer resource node.");

            var bufferInput = new ComputeBufferSlot("BufIn", SlotDirection.Input);
            var textureNode = new TextureResourceNode();
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
            var node = new TextureResourceNode();

            input.ConnectResource(node);

            Assert.That(input.IsConnected, Is.True,
                "A connected resource must mark the input slot as connected.");
            Assert.That(input.ConnectedResource, Is.SameAs(node),
                "ConnectedResource should reference the connected node.");
        }

        /// <summary>
        /// <see cref="PassSlot{T}.ReadHandle"/> reads through the connected
        /// resource node branch: with a producer-less node it returns the node's
        /// current handle instead of throwing (a pass-to-pass connection was
        /// never made).
        /// </summary>
        [Test]
        public void ReadHandle_ConnectedResource_WithoutProducer_ReturnsNodeHandle()
        {
            var input = new TextureSlot("TexIn", SlotDirection.Input);
            var node = new TextureResourceNode();
            input.ConnectResource(node);

            Assert.DoesNotThrow(() => input.ReadHandle());
            Assert.That(input.ReadHandle(), Is.EqualTo(default(TextureHandle)),
                "An input connected to a producer-less node should read the node's handle.");
        }

        /// <summary>
        /// <see cref="PassSlot{T}.ReadHandle"/> with a connected resource node
        /// that has a producer returns the producer slot's handle (bypassing the
        /// pass-to-pass <c>connectedOutput</c> chain).
        /// </summary>
        [Test]
        public void ReadHandle_ConnectedResource_WithProducer_ReturnsProducerHandle()
        {
            var producer = new FakeProducerPass("Producer");
            producer.SetupSlots();

            var value = default(TextureHandle);
            producer.TextureOutput.SetHandle(value);

            var node = new TextureResourceNode { ProducerSlot = producer.TextureOutput };
            var input = new TextureSlot("TexIn", SlotDirection.Input);
            input.ConnectResource(node);

            Assert.That(input.ReadHandle(), Is.EqualTo(value),
                "The input should read the produced handle through the resource node.");
        }

        #endregion
    }
}
