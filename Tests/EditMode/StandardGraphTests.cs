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

        // ── Stub pass classes registered with matching [Pass] names ──
        // These are minimal implementations so Build() can resolve pass types
        // from PassRegistry without requiring the full pass implementations.

        [Pass("Build Light Data")]
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

        [Pass("Cluster Culling Probe")]
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

        [Pass("Cluster Culling Light")]
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

        [Pass("Color Buffer Input")]
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

        [Pass("Depth Buffer Input")]
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

        [Pass("Forward Opaque")]
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

        [Pass("Builtin Sky")]
        private sealed class StubSky : Pass
        {
            public StubSky(string name) : base(name) { }
            public override void SetupSlots()
            {
                new TextureSlot("ColorTarget", SlotDirection.Output);
                new TextureSlot("DepthTarget", SlotDirection.Output);
            }

            public override void Initialize(CameraContext context) { }
            public override void Record(RenderGraph renderGraph) { }
        }

        [Pass("Transparency")]
        private sealed class StubTransparency : Pass
        {
            public StubTransparency(string name) : base(name) { }
            public override void SetupSlots()
            {
                new TextureSlot("ColorTarget", SlotDirection.Output);
                new TextureSlot("DepthTarget", SlotDirection.Output);
            }

            public override void Initialize(CameraContext context) { }
            public override void Record(RenderGraph renderGraph) { }
        }

        [Pass("Editor Wire Overlay")]
        private sealed class StubWireOverlay : Pass
        {
            public StubWireOverlay(string name) : base(name) { }
            public override void SetupSlots()
            {
                new TextureSlot("ColorTarget", SlotDirection.Output);
            }

            public override void Initialize(CameraContext context) { }
            public override void Record(RenderGraph renderGraph) { }
        }

        [Pass("Render Output")]
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
        /// Registers all stub pass types before each test.
        /// </summary>
        [SetUp]
        public void SetUp()
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
