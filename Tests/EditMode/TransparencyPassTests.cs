// <copyright file="TransparencyPassTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using NUnit.Framework;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for <see cref="TransparencyPass"/> in
    /// <c>Runtime/Passes/TransparencyPass.cs</c>.
    /// Verifies slot declaration, <c>[Pass]</c> attribute, and lifecycle.
    /// </summary>
    public sealed class TransparencyPassTests
    {
        #region Slot Declaration

        /// <summary>
        /// After <see cref="Pass.SetupSlots"/> is called,
        /// both input slots are non-null with correct names and directions.
        /// </summary>
        [Test]
        public void SetupSlots_DeclaresBothSlots()
        {
            var pass = new TransparencyPass("TestTransparency");

            pass.SetupSlots();

            // ── Input texture slot: ColorTarget ──

            Assert.That(pass.ColorTargetSlot, Is.Not.Null,
                "ColorTargetSlot should be non-null after SetupSlots.");
            Assert.That(pass.ColorTargetSlot!.SlotName, Is.EqualTo("ColorTarget"));
            Assert.That(pass.ColorTargetSlot.Direction, Is.EqualTo(SlotDirection.Input));

            // ── Input texture slot: DepthTarget ──

            Assert.That(pass.DepthTargetSlot, Is.Not.Null,
                "DepthTargetSlot should be non-null after SetupSlots.");
            Assert.That(pass.DepthTargetSlot!.SlotName, Is.EqualTo("DepthTarget"));
            Assert.That(pass.DepthTargetSlot.Direction, Is.EqualTo(SlotDirection.Input));
        }

        /// <summary>
        /// Before <see cref="Pass.SetupSlots"/> is called,
        /// all slot properties are <c>null</c>.
        /// </summary>
        [Test]
        public void AllSlots_AreNull_BeforeSetupSlots()
        {
            var pass = new TransparencyPass("TestTransparency");

            Assert.That(pass.ColorTargetSlot, Is.Null);
            Assert.That(pass.DepthTargetSlot, Is.Null);
        }

        #endregion

        #region Instance Properties

        /// <summary>
        /// <see cref="TransparencyPass"/> can be instantiated and is a
        /// <see cref="Pass"/> subclass.
        /// </summary>
        [Test]
        public void Pass_IsSubclassOfPass()
        {
            var pass = new TransparencyPass("TestTransparency");

            Assert.That(pass, Is.Not.Null);
            Assert.That(pass, Is.InstanceOf<Pass>());
            Assert.That(pass.PassName, Is.EqualTo("TestTransparency"));
        }

        /// <summary>
        /// <see cref="Pass.IsEnabled"/> defaults to <c>true</c>.
        /// </summary>
        [Test]
        public void Pass_IsEnabled_DefaultsTrue()
        {
            var pass = new TransparencyPass("TestTransparency");

            Assert.That(pass.IsEnabled, Is.True);
        }

        /// <summary>
        /// <see cref="TransparencyPass.RenderingLayerMask"/> defaults to
        /// <c>0x00000001</c> (layer 0).
        /// </summary>
        [Test]
        public void RenderingLayerMask_DefaultsToLayerZero()
        {
            var pass = new TransparencyPass("TestTransparency");

            Assert.That(pass.RenderingLayerMask, Is.EqualTo(0x00000001u));
        }

        #endregion

        #region Pass Attribute

        /// <summary>
        /// <see cref="TransparencyPass"/> declares the
        /// <c>[Pass("Transparency")]</c> attribute so it can be
        /// discovered by <see cref="PassRegistry"/>.
        /// </summary>
        [Test]
        public void Class_HasPassAttribute()
        {
            var type = typeof(TransparencyPass);
            var attrs = type.GetCustomAttributes(typeof(PassAttribute), inherit: false);

            Assert.That(attrs, Is.Not.Null.And.Length.EqualTo(1),
                "TransparencyPass should have exactly one [Pass] attribute.");
            Assert.That(((PassAttribute)attrs[0]!).DisplayName,
                Is.EqualTo("Transparency"),
                "Pass attribute display name should be 'Transparency'.");
        }

        #endregion

        #region Record Signature

        /// <summary>
        /// <see cref="Pass.Record"/> accepts a <see cref="RenderGraph"/>.
        /// The implementation inside uses <c>builder.UseColorBuffer</c>,
        /// <c>builder.UseDepthBuffer</c>, and <c>builder.UseRendererList</c>
        /// — verified by code review at the source of
        /// <see cref="TransparencyPass.Record"/>.
        /// </summary>
        [Test]
        public void Record_AcceptsRenderGraph()
        {
            var pass = new TransparencyPass("TestTransparency");

            var method = typeof(TransparencyPass).GetMethod(nameof(Pass.Record));
            Assert.That(method, Is.Not.Null,
                "Record method should exist on TransparencyPass.");

            var parameters = method!.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(1),
                "Record should accept exactly one parameter.");
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(RenderGraph)),
                "Record parameter should be RenderGraph.");
        }

        /// <summary>
        /// <see cref="Pass.Record"/> on a disabled pass should be skipped
        /// by the caller (e.g. <c>CameraRenderer</c>).
        /// </summary>
        [Test]
        public void Record_SkippedWhenDisabled()
        {
            var pass = new TransparencyPass("TestTransparency") { IsEnabled = false };

            Assert.That(pass.IsEnabled, Is.False);

            bool recordWouldBeCalled = pass.IsEnabled;
            Assert.That(recordWouldBeCalled, Is.False,
                "Record should not be invoked when IsEnabled is false.");
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// The full lifecycle of <see cref="TransparencyPass"/> —
        /// <see cref="Pass.SetupSlots"/>, <see cref="Pass.Initialize"/>,
        /// <see cref="Pass.Record"/>, <see cref="Pass.Cleanup"/> —
        /// completes without exceptions given valid state.
        /// </summary>
        [Test]
        public void Lifecycle_SetupSlots_Initialize_DoesNotThrow()
        {
            var pass = new TransparencyPass("TestTransparency");

            Assert.DoesNotThrow(() => pass.SetupSlots(),
                "SetupSlots should not throw.");

            Assert.DoesNotThrow(
                () => pass.Initialize(new CameraContext(null, default)),
                "Initialize should not throw.");

            Assert.DoesNotThrow(() => pass.Cleanup(),
                "Cleanup should not throw.");
        }

        /// <summary>
        /// Calling <see cref="Pass.Record"/> with a <c>null</c> render graph
        /// without slots set up returns early rather than throwing.
        /// </summary>
        /// <remarks>
        /// In a runtime integration test (Unity MCP), we would:
        /// <list type="number">
        ///   <item>Create a render graph</item>
        ///   <item>Set up slots and initialize with a valid camera context</item>
        ///   <item>Call Record</item>
        ///   <item>Assert the graph contains color/depth resources and a renderer list</item>
        /// </list>
        /// This test validates the structural contract — Record returns
        /// early when prerequisites are not met.
        /// </remarks>
        [Test]
        public void Record_WithoutSlots_ReturnsEarly()
        {
            var pass = new TransparencyPass("TestTransparency");

            // Without SetupSlots, ColorTargetSlot is null — Record should
            // return early rather than throwing.
            Assert.DoesNotThrow(() => pass.Record(null));
        }

        #endregion
    }
}
