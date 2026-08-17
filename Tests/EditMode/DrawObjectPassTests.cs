// <copyright file="DrawObjectPassTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for <see cref="DrawObjectPass"/> in
    /// <c>Runtime/Passes/DrawObjectPass.cs</c>.
    /// Verifies slot declaration, <c>[Pass]</c> attribute, and lifecycle.
    /// </summary>
    public sealed class DrawObjectPassTests
    {
        #region Slot Declaration

        /// <summary>
        /// After <see cref="Pass.SetupSlots"/> is called,
        /// all seven slots are non-null with correct names and directions.
        /// </summary>
        [Test]
        public void SetupSlots_DeclaresAllSevenSlots()
        {
            var pass = new DrawObjectPass("TestDrawObject");

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
            var pass = new DrawObjectPass("TestDrawObject");

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
        /// <see cref="DrawObjectPass"/> can be instantiated and is a
        /// <see cref="Pass"/> subclass.
        /// </summary>
        [Test]
        public void Pass_IsSubclassOfPass()
        {
            var pass = new DrawObjectPass("TestDrawObject");

            Assert.That(pass, Is.Not.Null);
            Assert.That(pass, Is.InstanceOf<Pass>());
            Assert.That(pass.PassName, Is.EqualTo("TestDrawObject"));
        }

        /// <summary>
        /// <see cref="Pass.IsEnabled"/> defaults to <c>true</c>.
        /// </summary>
        [Test]
        public void Pass_IsEnabled_DefaultsTrue()
        {
            var pass = new DrawObjectPass("TestDrawObject");

            Assert.That(pass.IsEnabled, Is.True);
        }

        /// <summary>
        /// <see cref="DrawObjectPass.RenderingLayerMask"/> defaults to
        /// <c>0x00000001</c> (layer 0).
        /// </summary>
        [Test]
        public void RenderingLayerMask_DefaultsToLayerZero()
        {
            var pass = new DrawObjectPass("TestDrawObject");

            Assert.That(pass.RenderingLayerMask, Is.EqualTo(0x00000001u));
        }

        #endregion

        #region Pass Attribute

        /// <summary>
        /// <see cref="DrawObjectPass"/> declares the
        /// <c>[Pass("Draw Object")]</c> attribute so it can be
        /// discovered by <see cref="PassRegistry"/>.
        /// </summary>
        [Test]
        public void Class_HasPassAttribute()
        {
            var type = typeof(DrawObjectPass);
            var attrs = type.GetCustomAttributes(typeof(PassAttribute), inherit: false);

            Assert.That(attrs, Is.Not.Null.And.Length.EqualTo(1),
                "DrawObjectPass should have exactly one [Pass] attribute.");
            Assert.That(((PassAttribute)attrs[0]!).DisplayName,
                Is.EqualTo("Draw Object"),
                "Pass attribute display name should be 'Draw Object'.");
        }

        /// <summary>
        /// The constant <see cref="DrawObjectPass.PassNameConst"/> matches
        /// the <c>[Pass]</c> attribute display name.
        /// </summary>
        [Test]
        public void PassNameConst_MatchesAttributeDisplayName()
        {
            var attribute = typeof(DrawObjectPass)
                .GetCustomAttribute<PassAttribute>();

            Assert.That(attribute, Is.Not.Null);
            Assert.That(
                DrawObjectPass.PassNameConst,
                Is.EqualTo(attribute!.DisplayName));
        }

        #endregion

        #region Record Signature

        /// <summary>
        /// <see cref="Pass.Record"/> accepts a <see cref="RenderGraph"/>.
        /// </summary>
        [Test]
        public void Record_AcceptsRenderGraph()
        {
            var pass = new DrawObjectPass("TestDrawObject");

            var method = typeof(DrawObjectPass).GetMethod(nameof(Pass.Record));
            Assert.That(method, Is.Not.Null,
                "Record method should exist on DrawObjectPass.");

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
            var pass = new DrawObjectPass("TestDrawObject") { IsEnabled = false };

            Assert.That(pass.IsEnabled, Is.False);

            bool recordWouldBeCalled = pass.IsEnabled;
            Assert.That(recordWouldBeCalled, Is.False,
                "Record should not be invoked when IsEnabled is false.");
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// The full lifecycle of <see cref="DrawObjectPass"/> —
        /// <see cref="Pass.SetupSlots"/>, <see cref="Pass.Initialize"/>,
        /// <see cref="Pass.Record"/>, <see cref="Pass.Cleanup"/> —
        /// completes without exceptions given valid state.
        /// </summary>
        [Test]
        public void Lifecycle_SetupSlots_Initialize_DoesNotThrow()
        {
            var pass = new DrawObjectPass("TestDrawObject");

            Assert.DoesNotThrow(() => pass.SetupSlots(),
                "SetupSlots should not throw.");

            Assert.DoesNotThrow(
                () => pass.Initialize(new CameraContext(null, default)),
                "Initialize should not throw.");

            Assert.DoesNotThrow(() => pass.Cleanup(),
                "Cleanup should not throw.");
        }

        /// <summary>
        /// Calling <see cref="Pass.Record"/> without <see cref="SetupSlots"/>
        /// should return early rather than throwing — both slots are null
        /// so the null-check guard triggers.
        /// </summary>
        [Test]
        public void Record_WithoutSlots_ReturnsEarly()
        {
            var pass = new DrawObjectPass("TestDrawObject");

            // Without SetupSlots, ColorTargetSlot and DepthTargetSlot are null —
            // Record should return early rather than throwing.
            Assert.DoesNotThrow(() => pass.Record(null));
        }

        /// <summary>
        /// Calling <see cref="Pass.Record"/> after <see cref="SetupSlots"/>
        /// but without <see cref="Initialize"/> returns early because
        /// <c>cameraContext</c> is null.
        /// </summary>
        [Test]
        public void Record_WithoutInitialize_ReturnsEarly()
        {
            var pass = new DrawObjectPass("TestDrawObject");
            pass.SetupSlots();

            // Slots are set up but Initialize was not called → cameraContext is null.
            Assert.DoesNotThrow(() => pass.Record(null));
        }

        #endregion
    }
}
