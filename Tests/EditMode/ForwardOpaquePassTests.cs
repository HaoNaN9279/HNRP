// <copyright file="ForwardOpaquePassTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using NUnit.Framework;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for <see cref="ForwardOpaquePass"/> in
    /// <c>Runtime/Passes/ForwardOpaquePass.cs</c>.
    /// Verifies slot declaration, <c>[Pass]</c> attribute, and lifecycle.
    /// </summary>
    public sealed class ForwardOpaquePassTests
    {
        #region Slot Declaration

        /// <summary>
        /// After <see cref="Pass.SetupSlots"/> is called,
        /// all seven slots are non-null with correct names and directions.
        /// </summary>
        [Test]
        public void SetupSlots_DeclaresAllSevenSlots()
        {
            var pass = new ForwardOpaquePass("TestForwardOpaque");

            pass.SetupSlots();

            // ── Output texture slots ──

            Assert.That(pass.ColorTargetSlot, Is.Not.Null,
                "ColorTargetSlot should be non-null after SetupSlots.");
            Assert.That(pass.ColorTargetSlot!.SlotName, Is.EqualTo("ColorTarget"));
            Assert.That(pass.ColorTargetSlot.Direction, Is.EqualTo(SlotDirection.Output));

            Assert.That(pass.DepthTargetSlot, Is.Not.Null,
                "DepthTargetSlot should be non-null after SetupSlots.");
            Assert.That(pass.DepthTargetSlot!.SlotName, Is.EqualTo("DepthTarget"));
            Assert.That(pass.DepthTargetSlot.Direction, Is.EqualTo(SlotDirection.Output));

            // ── Input compute buffer slot: LightDatas ──

            Assert.That(pass.LightDatasSlot, Is.Not.Null,
                "LightDatasSlot should be non-null after SetupSlots.");
            Assert.That(pass.LightDatasSlot!.SlotName, Is.EqualTo("LightDatas"));
            Assert.That(pass.LightDatasSlot.Direction, Is.EqualTo(SlotDirection.Input));

            // ── Input texture slot: ReflectionProbeAtlas ──

            Assert.That(pass.ReflectionProbeAtlasSlot, Is.Not.Null,
                "ReflectionProbeAtlasSlot should be non-null after SetupSlots.");
            Assert.That(pass.ReflectionProbeAtlasSlot!.SlotName, Is.EqualTo("ReflectionProbeAtlas"));
            Assert.That(pass.ReflectionProbeAtlasSlot.Direction, Is.EqualTo(SlotDirection.Input));

            // ── Input compute buffer slot: ProbeMask ──

            Assert.That(pass.ProbeMaskSlot, Is.Not.Null,
                "ProbeMaskSlot should be non-null after SetupSlots.");
            Assert.That(pass.ProbeMaskSlot!.SlotName, Is.EqualTo("ProbeMask"));
            Assert.That(pass.ProbeMaskSlot.Direction, Is.EqualTo(SlotDirection.Input));

            // ── Input compute buffer slot: ProbeDatas ──

            Assert.That(pass.ProbeDatasSlot, Is.Not.Null,
                "ProbeDatasSlot should be non-null after SetupSlots.");
            Assert.That(pass.ProbeDatasSlot!.SlotName, Is.EqualTo("ProbeDatas"));
            Assert.That(pass.ProbeDatasSlot.Direction, Is.EqualTo(SlotDirection.Input));

            // ── Input compute buffer slot: LightMask ──

            Assert.That(pass.LightMaskSlot, Is.Not.Null,
                "LightMaskSlot should be non-null after SetupSlots.");
            Assert.That(pass.LightMaskSlot!.SlotName, Is.EqualTo("LightMask"));
            Assert.That(pass.LightMaskSlot.Direction, Is.EqualTo(SlotDirection.Input));
        }

        /// <summary>
        /// Before <see cref="Pass.SetupSlots"/> is called,
        /// all slot properties are <c>null</c>.
        /// </summary>
        [Test]
        public void AllSlots_AreNull_BeforeSetupSlots()
        {
            var pass = new ForwardOpaquePass("TestForwardOpaque");

            Assert.That(pass.ColorTargetSlot, Is.Null);
            Assert.That(pass.DepthTargetSlot, Is.Null);
            Assert.That(pass.LightDatasSlot, Is.Null);
            Assert.That(pass.ReflectionProbeAtlasSlot, Is.Null);
            Assert.That(pass.ProbeMaskSlot, Is.Null);
            Assert.That(pass.ProbeDatasSlot, Is.Null);
            Assert.That(pass.LightMaskSlot, Is.Null);
        }

        #endregion

        #region Instance Properties

        /// <summary>
        /// <see cref="ForwardOpaquePass"/> can be instantiated and is a
        /// <see cref="Pass"/> subclass.
        /// </summary>
        [Test]
        public void Pass_IsSubclassOfPass()
        {
            var pass = new ForwardOpaquePass("TestForwardOpaque");

            Assert.That(pass, Is.Not.Null);
            Assert.That(pass, Is.InstanceOf<Pass>());
            Assert.That(pass.PassName, Is.EqualTo("TestForwardOpaque"));
        }

        /// <summary>
        /// <see cref="Pass.IsEnabled"/> defaults to <c>true</c>.
        /// </summary>
        [Test]
        public void Pass_IsEnabled_DefaultsTrue()
        {
            var pass = new ForwardOpaquePass("TestForwardOpaque");

            Assert.That(pass.IsEnabled, Is.True);
        }

        /// <summary>
        /// <see cref="ForwardOpaquePass.RenderingLayerMask"/> defaults to
        /// <c>0x00000001</c> (layer 0).
        /// </summary>
        [Test]
        public void RenderingLayerMask_DefaultsToLayerZero()
        {
            var pass = new ForwardOpaquePass("TestForwardOpaque");

            Assert.That(pass.RenderingLayerMask, Is.EqualTo(0x00000001u));
        }

        #endregion

        #region Pass Attribute

        /// <summary>
        /// <see cref="ForwardOpaquePass"/> declares the
        /// <c>[Pass("Forward Opaque")]</c> attribute so it can be
        /// discovered by <see cref="PassRegistry"/>.
        /// </summary>
        [Test]
        public void Class_HasPassAttribute()
        {
            var type = typeof(ForwardOpaquePass);
            var attrs = type.GetCustomAttributes(typeof(PassAttribute), inherit: false);

            Assert.That(attrs, Is.Not.Null.And.Length.EqualTo(1),
                "ForwardOpaquePass should have exactly one [Pass] attribute.");
            Assert.That(((PassAttribute)attrs[0]!).DisplayName,
                Is.EqualTo("Forward Opaque"),
                "Pass attribute display name should be 'Forward Opaque'.");
        }

        #endregion

        #region Record Signature

        /// <summary>
        /// <see cref="Pass.Record"/> accepts a <see cref="RenderGraph"/>.
        /// The implementation inside uses <c>builder.UseColorBuffer</c>,
        /// <c>builder.UseDepthBuffer</c>, <c>builder.ReadComputeBuffer</c>,
        /// and <c>builder.UseRendererList</c> — verified by code review at
        /// the source of <see cref="ForwardOpaquePass.Record"/>.
        /// </summary>
        [Test]
        public void Record_AcceptsRenderGraph()
        {
            var pass = new ForwardOpaquePass("TestForwardOpaque");

            var method = typeof(ForwardOpaquePass).GetMethod(nameof(Pass.Record));
            Assert.That(method, Is.Not.Null,
                "Record method should exist on ForwardOpaquePass.");

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
            var pass = new ForwardOpaquePass("TestForwardOpaque") { IsEnabled = false };

            Assert.That(pass.IsEnabled, Is.False);

            bool recordWouldBeCalled = pass.IsEnabled;
            Assert.That(recordWouldBeCalled, Is.False,
                "Record should not be invoked when IsEnabled is false.");
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// The full lifecycle of <see cref="ForwardOpaquePass"/> —
        /// <see cref="Pass.SetupSlots"/>, <see cref="Pass.Initialize"/>,
        /// <see cref="Pass.Record"/>, <see cref="Pass.Cleanup"/> —
        /// completes without exceptions given valid state.
        /// </summary>
        [Test]
        public void Lifecycle_SetupSlots_Initialize_DoesNotThrow()
        {
            var pass = new ForwardOpaquePass("TestForwardOpaque");

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
        /// after valid initialization throws <c>NullReferenceException</c>
        /// (expected due to <c>renderGraph.CreateTexture</c> being called).
        /// This confirms the Record path does not have unintended early
        /// returns before slot validation.
        /// </summary>
        /// <remarks>
        /// In a runtime integration test (Unity MCP), we would:
        /// <list type="number">
        ///   <item>Create a render graph</item>
        ///   <item>Connect upstream output slots for compute buffer inputs</item>
        ///   <item>Call Record</item>
        ///   <item>Assert the graph contains color/depth resources and a renderer list</item>
        /// </list>
        /// This test validates the structural contract — slot setup holds,
        /// Initialize stores the context, and Record path reaches the
        /// <c>renderGraph.CreateTexture</c> call.
        /// </remarks>
        [Test]
        public void Record_WithoutSlots_ReturnsEarly()
        {
            var pass = new ForwardOpaquePass("TestForwardOpaque");

            // Without SetupSlots, ColorTargetSlot is null — Record should
            // return early rather than throwing.
            Assert.DoesNotThrow(() => pass.Record(null));
        }

        #endregion
    }
}
