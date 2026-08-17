// <copyright file="PassConfigsTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for <see cref="ForwardOpaqueConfig"/>, <see cref="SkyConfig"/>,
    /// and <see cref="TransparencyConfig"/> in <c>Runtime/Configs/</c>.
    /// Verifies serialization roundtrip and <see cref="PassConfigBase.ApplyToPass"/> behaviour.
    /// </summary>
    public sealed class PassConfigsTests
    {
        #region Test Helpers

        /// <summary>
        /// A minimal <see cref="PassConfigBase"/> used when we need a concrete but
        /// generic config that holds exactly the values being tested.
        /// </summary>
        private class TestPass : Pass
        {
            public TestPass(string name) : base(name) { }

            public override void SetupSlots() { }
            public override void Initialize(CameraContext context) { }

            public override void Record(
                UnityEngine.Experimental.Rendering.RenderGraphModule.RenderGraph renderGraph)
            {
            }
        }

        #endregion

        #region ForwardOpaqueConfig

        /// <summary>
        /// A <see cref="ForwardOpaqueConfig"/> created via
        /// <see cref="ScriptableObject.CreateInstance{T}"/> survives a
        /// serialization roundtrip (<see cref="ScriptableObject.Instantiate{T}(T)"/>)
        /// with all field values preserved.
        /// </summary>
        [Test]
        public void ForwardOpaqueConfig_SerializationRoundtrip()
        {
            // ── Arrange ──
            var original = ScriptableObject.CreateInstance<ForwardOpaqueConfig>();
            original.PassName = "ForwardOpaque_Main";
            original.RenderQueueRange = HNRenderQueue.OpaqueNoAlphaTest;
            original.RenderingLayerMask = 0x00000007;
            original.LayerMask = (LayerMask)(1 << 5);

            // ── Act (roundtrip via Unity serialization) ──
            var copy = ScriptableObject.Instantiate(original);

            // ── Assert ──
            Assert.That(copy, Is.Not.Null,
                "Instantiate must return a non-null copy.");
            Assert.That(copy, Is.Not.SameAs(original),
                "Instantiate must create an independent object.");
            Assert.That(copy.PassName, Is.EqualTo("ForwardOpaque_Main"),
                "PassName must survive the roundtrip.");
            Assert.That(copy.RenderingLayerMask, Is.EqualTo(0x00000007u),
                "RenderingLayerMask must survive the roundtrip.");
            Assert.That(copy.LayerMask.value, Is.EqualTo(1 << 5),
                "LayerMask must survive the roundtrip.");

            // RenderQueueRange is backed by serializable int fields, so
            // ScriptableObject.Instantiate preserves both bounds correctly.
            Assert.That(copy.RenderQueueRange.lowerBound,
                Is.EqualTo(HNRenderQueue.OpaqueNoAlphaTest.lowerBound),
                "RenderQueueRange lower bound must survive the roundtrip.");
            Assert.That(copy.RenderQueueRange.upperBound,
                Is.EqualTo(HNRenderQueue.OpaqueNoAlphaTest.upperBound),
                "RenderQueueRange upper bound must survive the roundtrip.");

            UnityEngine.Object.DestroyImmediate(original);
            UnityEngine.Object.DestroyImmediate(copy);
        }

        /// <summary>
        /// <see cref="ForwardOpaqueConfig.ApplyToPass"/> must enable the pass
        /// and set <see cref="ForwardOpaquePass.RenderingLayerMask"/> on a
        /// compatible pass instance.
        /// </summary>
        [Test]
        public void ForwardOpaqueConfig_ApplyToPass_ModulatesPassState()
        {
            // ── Arrange ──
            var config = ScriptableObject.CreateInstance<ForwardOpaqueConfig>();
            config.RenderingLayerMask = 0x00000003;

            var pass = new ForwardOpaquePass("TestForwardOpaque");
            pass.IsEnabled = false;

            // ── Act ──
            config.ApplyToPass(pass);

            // ── Assert ──
            Assert.That(pass.IsEnabled, Is.True,
                "ApplyToPass must enable the pass.");
            Assert.That(pass.RenderingLayerMask, Is.EqualTo(0x00000003u),
                "ApplyToPass must copy RenderingLayerMask onto the pass.");

            UnityEngine.Object.DestroyImmediate(config);
        }

        /// <summary>
        /// <see cref="ForwardOpaqueConfig.ApplyToPass"/> with a <c>null</c> pass
        /// must not throw.
        /// </summary>
        [Test]
        public void ForwardOpaqueConfig_ApplyToPass_NullPass_DoesNotThrow()
        {
            var config = ScriptableObject.CreateInstance<ForwardOpaqueConfig>();

            Assert.That(
                () => config.ApplyToPass(null),
                Throws.Nothing,
                "ApplyToPass(null) must not throw.");

            UnityEngine.Object.DestroyImmediate(config);
        }

        #endregion

        #region SkyConfig

        /// <summary>
        /// A <see cref="SkyConfig"/> created via
        /// <see cref="ScriptableObject.CreateInstance{T}"/> survives a
        /// serialization roundtrip with all field values preserved.
        /// </summary>
        [Test]
        public void SkyConfig_SerializationRoundtrip()
        {
            // ── Arrange ──
            var original = ScriptableObject.CreateInstance<SkyConfig>();
            original.PassName = "Sky_Main";
            original.SkyIntensity = 0.5f;
            original.SkyTint = new Color(0.3f, 0.5f, 0.8f, 1f);
            original.WriteDepth = false;
            original.DepthTest = CompareFunction.Always;

            // ── Act ──
            var copy = ScriptableObject.Instantiate(original);

            // ── Assert ──
            Assert.That(copy, Is.Not.Null);
            Assert.That(copy, Is.Not.SameAs(original));
            Assert.That(copy.PassName, Is.EqualTo("Sky_Main"));
            Assert.That(copy.SkyIntensity, Is.EqualTo(0.5f));
            Assert.That(copy.SkyTint, Is.EqualTo(new Color(0.3f, 0.5f, 0.8f, 1f)));
            Assert.That(copy.WriteDepth, Is.False);
            Assert.That(copy.DepthTest, Is.EqualTo(CompareFunction.Always));

            UnityEngine.Object.DestroyImmediate(original);
            UnityEngine.Object.DestroyImmediate(copy);
        }

        /// <summary>
        /// <see cref="SkyConfig.ApplyToPass"/> sets the pass enabled state based
        /// on <see cref="SkyConfig.SkyIntensity"/>.
        /// </summary>
        [Test]
        public void SkyConfig_ApplyToPass_DisablesPassWhenIntensityIsZero()
        {
            // ── Arrange ──
            var config = ScriptableObject.CreateInstance<SkyConfig>();
            config.SkyIntensity = 0f;

            var pass = new TestPass("TestSky");

            // ── Act ──
            config.ApplyToPass(pass);

            // ── Assert ──
            Assert.That(pass.IsEnabled, Is.False,
                "A zero SkyIntensity must disable the pass.");

            UnityEngine.Object.DestroyImmediate(config);
        }

        /// <summary>
        /// <see cref="SkyConfig.ApplyToPass"/> enables the pass when intensity
        /// is positive.
        /// </summary>
        [Test]
        public void SkyConfig_ApplyToPass_EnablesPassWhenIntensityIsPositive()
        {
            var config = ScriptableObject.CreateInstance<SkyConfig>();
            config.SkyIntensity = 0.8f;

            var pass = new TestPass("TestSky");
            pass.IsEnabled = false;

            config.ApplyToPass(pass);

            Assert.That(pass.IsEnabled, Is.True);

            UnityEngine.Object.DestroyImmediate(config);
        }

        /// <summary>
        /// <see cref="SkyConfig.ApplyToPass"/> with <c>null</c> does not throw.
        /// </summary>
        [Test]
        public void SkyConfig_ApplyToPass_NullPass_DoesNotThrow()
        {
            var config = ScriptableObject.CreateInstance<SkyConfig>();

            Assert.That(
                () => config.ApplyToPass(null),
                Throws.Nothing);

            UnityEngine.Object.DestroyImmediate(config);
        }

        #endregion

        #region TransparencyConfig

        /// <summary>
        /// A <see cref="TransparencyConfig"/> created via
        /// <see cref="ScriptableObject.CreateInstance{T}"/> survives a
        /// serialization roundtrip with all field values preserved.
        /// </summary>
        [Test]
        public void TransparencyConfig_SerializationRoundtrip()
        {
            // ── Arrange ──
            var original = ScriptableObject.CreateInstance<TransparencyConfig>();
            original.PassName = "Transparency_Main";
            original.RenderQueueRange = HNRenderQueue.Transparent;
            original.RenderingLayerMask = 0x0000000F;
            original.LayerMask = (LayerMask)(1 << 8);
            original.SortBackToFront = false;

            // ── Act ──
            var copy = ScriptableObject.Instantiate(original);

            // ── Assert ──
            Assert.That(copy, Is.Not.Null);
            Assert.That(copy, Is.Not.SameAs(original));
            Assert.That(copy.PassName, Is.EqualTo("Transparency_Main"));
            Assert.That(copy.RenderingLayerMask, Is.EqualTo(0x0000000Fu));
            Assert.That(copy.LayerMask.value, Is.EqualTo(1 << 8));
            Assert.That(copy.SortBackToFront, Is.False);

            Assert.That(copy.RenderQueueRange.lowerBound,
                Is.EqualTo(HNRenderQueue.Transparent.lowerBound));
            Assert.That(copy.RenderQueueRange.upperBound,
                Is.EqualTo(HNRenderQueue.Transparent.upperBound));

            UnityEngine.Object.DestroyImmediate(original);
            UnityEngine.Object.DestroyImmediate(copy);
        }

        /// <summary>
        /// <see cref="TransparencyConfig.ApplyToPass"/> must enable the pass
        /// and set <see cref="TransparencyPass.RenderingLayerMask"/> on a
        /// compatible pass instance.
        /// </summary>
        [Test]
        public void TransparencyConfig_ApplyToPass_ModulatesPassState()
        {
            // ── Arrange ──
            var config = ScriptableObject.CreateInstance<TransparencyConfig>();
            config.RenderingLayerMask = 0x00000005;

            var pass = new TransparencyPass("TestTransparency");
            pass.IsEnabled = false;

            // ── Act ──
            config.ApplyToPass(pass);

            // ── Assert ──
            Assert.That(pass.IsEnabled, Is.True,
                "ApplyToPass must enable the pass.");
            Assert.That(pass.RenderingLayerMask, Is.EqualTo(0x00000005u),
                "ApplyToPass must copy RenderingLayerMask onto the pass.");

            UnityEngine.Object.DestroyImmediate(config);
        }

        /// <summary>
        /// <see cref="TransparencyConfig.ApplyToPass"/> with <c>null</c> does not throw.
        /// </summary>
        [Test]
        public void TransparencyConfig_ApplyToPass_NullPass_DoesNotThrow()
        {
            var config = ScriptableObject.CreateInstance<TransparencyConfig>();

            Assert.That(
                () => config.ApplyToPass(null),
                Throws.Nothing);

            UnityEngine.Object.DestroyImmediate(config);
        }

        #endregion
    }
}
