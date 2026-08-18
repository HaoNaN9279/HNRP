// <copyright file="StandardGraphTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using HN.HNRP;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for <c>Runtime/Resources/RenderGraphs/StandardGraph.asset</c>.
    /// Verifies the asset loads correctly, <see cref="RenderGraphAsset.Build"/>
    /// produces 10 passes, and all <see cref="SlotConnection"/> entries are valid.
    /// </summary>
    public sealed class StandardGraphTests
    {
        #region Test Pass Stubs

        // ── Stub pass classes registered manually with matching display names ──
        // These are minimal implementations so Build() can resolve pass types
        // from PassRegistry without requiring the full pass implementations.
        // They carry no [Pass] attribute: attribute-based registration would let
        // the reflection scan in PassRegistry.RegisterAll() overwrite the real
        // passes with these stubs (Stub registry pollution).
        // RegisterStubPasses() re-registers them per-test, and TearDown() calls
        // PassRegistry.RegisterAll() to restore the clean registry (real passes
        // only) before the next test.

        private sealed class StubBuildLightData : Pass
        {
            public StubBuildLightData(string name) : base(name) { }
            public override void SetupSlots()
            {
                new ComputeBufferSlot("lightDatasBuffer", SlotDirection.Output);
            }

            public override void Initialize(CameraContext context) { }
            public override void Record(RenderGraph renderGraph) { }
        }

        private sealed class StubClusterProbe : Pass
        {
            public StubClusterProbe(string name) : base(name) { }
            public override void SetupSlots()
            {
                new TextureSlot("reflectionProbeAtlas", SlotDirection.Output);
                new ComputeBufferSlot("clusterCullingReflectionProbeMaskBuffer", SlotDirection.Output);
                new ComputeBufferSlot("clusterCullingReflectionProbeDatasBuffer", SlotDirection.Output);
            }

            public override void Initialize(CameraContext context) { }
            public override void Record(RenderGraph renderGraph) { }
        }

        private sealed class StubClusterLight : Pass
        {
            public StubClusterLight(string name) : base(name) { }
            public override void SetupSlots()
            {
                new ComputeBufferSlot("lightDatasBuffer", SlotDirection.Input);
                new ComputeBufferSlot("clusterCullingLightMaskBuffer", SlotDirection.Output);
            }

            public override void Initialize(CameraContext context) { }
            public override void Record(RenderGraph renderGraph) { }
        }

        private sealed class StubColorInput : Pass
        {
            public StubColorInput(string name) : base(name) { }
            public override void SetupSlots()
            {
                new TextureSlot("colorTargetSlot", SlotDirection.Output);
            }

            public override void Initialize(CameraContext context) { }
            public override void Record(RenderGraph renderGraph) { }
        }

        private sealed class StubDepthInput : Pass
        {
            public StubDepthInput(string name) : base(name) { }
            public override void SetupSlots()
            {
                new TextureSlot("DepthTarget", SlotDirection.Output);
            }

            public override void Initialize(CameraContext context) { }
            public override void Record(RenderGraph renderGraph) { }
        }

        private sealed class StubForwardOpaque : Pass
        {
            public StubForwardOpaque(string name) : base(name) { }
            public override void SetupSlots()
            {
                new TextureSlot("ColorTarget", SlotDirection.Output);
                new TextureSlot("DepthTarget", SlotDirection.Output);
                new ComputeBufferSlot("LightDatas", SlotDirection.Input);
                new TextureSlot("ReflectionProbeAtlas", SlotDirection.Input);
                new ComputeBufferSlot("ProbeMask", SlotDirection.Input);
                new ComputeBufferSlot("ProbeDatas", SlotDirection.Input);
                new ComputeBufferSlot("LightMask", SlotDirection.Input);
            }

            public override void Initialize(CameraContext context) { }
            public override void Record(RenderGraph renderGraph) { }
        }

        private sealed class StubSky : Pass
        {
            public StubSky(string name) : base(name) { }
            public override void SetupSlots()
            {
                new TextureSlot("ColorTarget", SlotDirection.Input);
                new TextureSlot("DepthTarget", SlotDirection.Input);
            }

            public override void Initialize(CameraContext context) { }
            public override void Record(RenderGraph renderGraph) { }
        }

        private sealed class StubTransparency : Pass
        {
            public StubTransparency(string name) : base(name) { }
            public override void SetupSlots()
            {
                new TextureSlot("ColorTarget", SlotDirection.Input);
                new TextureSlot("DepthTarget", SlotDirection.Input);
            }

            public override void Initialize(CameraContext context) { }
            public override void Record(RenderGraph renderGraph) { }
        }

        private sealed class StubWireOverlay : Pass
        {
            public StubWireOverlay(string name) : base(name) { }
            public override void SetupSlots()
            {
                new TextureSlot("ColorTarget", SlotDirection.Input);
            }

            public override void Initialize(CameraContext context) { }
            public override void Record(RenderGraph renderGraph) { }
        }

        private sealed class StubRenderOutput : Pass
        {
            public StubRenderOutput(string name) : base(name) { }
            public override void SetupSlots()
            {
                new TextureSlot("ColorTarget", SlotDirection.Input);
            }

            public override void Initialize(CameraContext context) { }
            public override void Record(RenderGraph renderGraph) { }
        }

        #endregion

        #region Setup / Teardown

        /// <summary>
        /// Builds the clean registry (real passes only), then registers all stub
        /// pass types under their matching display names before each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            PassRegistry.RegisterAll();
            RegisterStubPasses();
        }

        private static void RegisterStubPasses()
        {
            PassRegistry.Register("Build Light Data", typeof(StubBuildLightData));
            PassRegistry.Register("Cluster Culling Probe", typeof(StubClusterProbe));
            PassRegistry.Register("Cluster Culling Light", typeof(StubClusterLight));
            PassRegistry.Register("Color Buffer Input", typeof(StubColorInput));
            PassRegistry.Register("Depth Buffer Input", typeof(StubDepthInput));
            PassRegistry.Register("Forward Opaque", typeof(StubForwardOpaque));
            PassRegistry.Register("Builtin Sky", typeof(StubSky));
            PassRegistry.Register("Transparency", typeof(StubTransparency));
            PassRegistry.Register("Editor Wire Overlay", typeof(StubWireOverlay));
            PassRegistry.Register("Render Output", typeof(StubRenderOutput));
        }

        /// <summary>
        /// Restores the clean registry (real passes only) after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            PassRegistry.RegisterAll();
        }

        #endregion

        #region Asset Loading

        /// <summary>
        /// <c>StandardGraph.asset</c> can be loaded from <c>Resources/RenderGraphs</c>
        /// and is a non-null <see cref="RenderGraphAsset"/>.
        /// </summary>
        [Test]
        public void Load_Asset_IsNotNullAndCorrectType()
        {
            var asset = Resources.Load<RenderGraphAsset>("RenderGraphs/StandardGraph");

            Assert.That(asset, Is.Not.Null,
                "StandardGraph.asset should be loadable from Resources/RenderGraphs.");
            Assert.That(asset, Is.InstanceOf<RenderGraphAsset>(),
                "Loaded asset should be a RenderGraphAsset.");
        }

        #endregion

        #region Pass Count

        /// <summary>
        /// <see cref="RenderGraphAsset.Build"/> produces exactly 10 passes
        /// matching the 10 <see cref="PassDefinition"/> entries in the asset.
        /// Expected order: buildLight, clusterProbe, clusterLight, colorInput,
        /// depthInput, forwardOpaque, sky, transparency, wireOverlay, finalBlit.
        /// </summary>
        [Test]
        public void Build_ProducesTenPasses()
        {
            var asset = Resources.Load<RenderGraphAsset>("RenderGraphs/StandardGraph");
            Assume.That(asset, Is.Not.Null,
                "Test requires StandardGraph.asset to be loadable.");

            List<Pass> result = asset.Build(renderer: null);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(10),
                "Build should produce exactly 10 passes.");

            // Verify each expected instance name is present.
            string[] expectedNames =
            {
                "buildLight", "clusterProbe", "clusterLight",
                "colorInput", "depthInput", "forwardOpaque",
                "sky", "transparency", "wireOverlay", "finalBlit",
            };

            foreach (string name in expectedNames)
            {
                Assert.That(
                    result.Exists(p => p.PassName == name),
                    Is.True,
                    $"Pass '{name}' should be present in the build result.");
            }
        }

        #endregion

        #region Connections Validation

        /// <summary>
        /// All 14 <see cref="SlotConnection"/> entries in the asset are valid,
        /// and all referenced source/target passes exist in the pass list.
        /// </summary>
        [Test]
        public void Connections_AllValidAndReferenced()
        {
            var asset = Resources.Load<RenderGraphAsset>("RenderGraphs/StandardGraph");
            Assume.That(asset, Is.Not.Null);

            Assert.That(asset.Connections.Count, Is.EqualTo(14),
                "StandardGraph should have exactly 14 slot connections.");

            // Build so we can cross-reference connection pass names with
            // instantiated passes.
            List<Pass> builtPasses = asset.Build(renderer: null);

            // Collect pass names for existence checks.
            var passNames = new HashSet<string>();
            foreach (Pass p in builtPasses)
            {
                passNames.Add(p.PassName);
            }

            foreach (SlotConnection conn in asset.Connections)
            {
                Assert.That(conn.IsValid(), Is.True,
                    $"Connection {conn.SourcePass}.{conn.SourceSlot} → " +
                    $"{conn.TargetPass}.{conn.TargetSlot} should be valid.");

                Assert.That(passNames.Contains(conn.SourcePass), Is.True,
                    $"SourcePass '{conn.SourcePass}' should exist in the pass list.");

                Assert.That(passNames.Contains(conn.TargetPass), Is.True,
                    $"TargetPass '{conn.TargetPass}' should exist in the pass list.");
            }
        }

        #endregion

        #region Settings

        /// <summary>
        /// <see cref="RenderGraphAsset.Settings"/> reflect the configured values:
        /// <see cref="RenderGraphSettings.SHEvalMode"/> is <c>PerPixel</c>,
        /// <see cref="RenderGraphSettings.AllowHDR"/> is <c>true</c>.
        /// </summary>
        [Test]
        public void Settings_HasCorrectValues()
        {
            var asset = Resources.Load<RenderGraphAsset>("RenderGraphs/StandardGraph");
            Assume.That(asset, Is.Not.Null);

            Assert.That(asset.Settings.SHEvalMode, Is.EqualTo(SHEvalMode.PerPixel),
                "Standard graph should use PerPixel SH evaluation.");
            Assert.That(asset.Settings.AllowHDR, Is.True,
                "Standard graph should allow HDR.");
        }

        #endregion
    }
}
