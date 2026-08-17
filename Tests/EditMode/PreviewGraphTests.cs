// <copyright file="PreviewGraphTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HN.HNRP;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for <c>PreviewGraph.asset</c> — the lightweight render graph
    /// used for preview cameras. Verifies that the asset exists, has the
    /// correct passes (minimal set), omits heavy passes, and has appropriate
    /// preview-oriented settings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// PreviewGraph is designed to be a stripped-down version of StandardGraph.
    /// It includes only the minimum passes needed for basic opaque rendering:
    /// color/depth buffer input, forward opaque, and render output.
    /// </para>
    /// <para>
    /// Passes intentionally <b>excluded</b> from PreviewGraph:
    /// <list type="bullet">
    ///   <item>Build Light Data — lighting data collection is skipped</item>
    ///   <item>Cluster Culling Light — light culling is skipped</item>
    ///   <item>Cluster Culling Probe — reflection probe culling is skipped</item>
    ///   <item>Builtin Sky — skybox rendering is skipped</item>
    ///   <item>Transparency — transparent object rendering is skipped</item>
    ///   <item>Editor Wire Overlay — editor debug overlay is skipped</item>
    ///   <item>Draw Object — draw object pass is skipped</item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class PreviewGraphTests
    {
        #region Setup

        /// <summary>
        /// Ensures <see cref="PassRegistry"/> is populated before each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            PassRegistry.RegisterAll();
        }

        #endregion

        #region Asset Existence & Type

        /// <summary>
        /// PreviewGraph.asset exists in Resources and is a valid
        /// <see cref="RenderGraphAsset"/>.
        /// </summary>
        [Test]
        public void PreviewGraph_CanBeLoaded_FromResources()
        {
            var asset = Resources.Load<RenderGraphAsset>("RenderGraphs/PreviewGraph");

            Assert.That(asset, Is.Not.Null,
                "PreviewGraph.asset should exist at Runtime/Resources/RenderGraphs/PreviewGraph.asset.");
            Assert.That(asset, Is.InstanceOf<RenderGraphAsset>(),
                "Loaded asset should be a RenderGraphAsset.");
        }

        #endregion

        #region Pass Composition

        /// <summary>
        /// PreviewGraph contains exactly the expected minimal pass set:
        /// Color Buffer Input, Depth Buffer Input, Forward Opaque, and Render Output.
        /// </summary>
        [Test]
        public void PreviewGraph_HasExpectedPasses()
        {
            var asset = Resources.Load<RenderGraphAsset>("RenderGraphs/PreviewGraph");
            Assume.That(asset, Is.Not.Null, "PreviewGraph asset must exist for this test.");

            List<PassDefinition> passes = asset.Passes;

            Assert.That(passes, Is.Not.Null,
                "Passes list should not be null.");
            Assert.That(passes.Count, Is.EqualTo(4),
                "PreviewGraph should have exactly 4 passes.");

            // Verify each expected pass type and instance name.
            Assert.That(passes[0].PassType, Is.EqualTo("Color Buffer Input"),
                "First pass should be Color Buffer Input.");
            Assert.That(passes[0].InstanceName, Is.EqualTo("Color Target"),
                "Color Buffer Input instance should be named 'Color Target'.");

            Assert.That(passes[1].PassType, Is.EqualTo("Depth Buffer Input"),
                "Second pass should be Depth Buffer Input.");
            Assert.That(passes[1].InstanceName, Is.EqualTo("Depth Target"),
                "Depth Buffer Input instance should be named 'Depth Target'.");

            Assert.That(passes[2].PassType, Is.EqualTo("Forward Opaque"),
                "Third pass should be Forward Opaque.");
            Assert.That(passes[2].InstanceName, Is.EqualTo("Opaque"),
                "Forward Opaque instance should be named 'Opaque'.");

            Assert.That(passes[3].PassType, Is.EqualTo("Render Output"),
                "Fourth pass should be Render Output.");
            Assert.That(passes[3].InstanceName, Is.EqualTo("Final Blit"),
                "Render Output instance should be named 'Final Blit'.");
        }

        /// <summary>
        /// PreviewGraph does NOT include heavy passes that are in StandardGraph:
        /// Build Light Data, Cluster Culling Light, Cluster Culling Probe,
        /// Builtin Sky, Transparency, Editor Wire Overlay, Draw Object.
        /// </summary>
        [Test]
        public void PreviewGraph_ExcludesHeavyPasses()
        {
            var asset = Resources.Load<RenderGraphAsset>("RenderGraphs/PreviewGraph");
            Assume.That(asset, Is.Not.Null, "PreviewGraph asset must exist for this test.");

            List<PassDefinition> passes = asset.Passes;

            // Collect all pass types for easy checking.
            var passTypeNames = new HashSet<string>();
            foreach (PassDefinition def in passes)
            {
                passTypeNames.Add(def.PassType);
            }

            Assert.That(passTypeNames.Contains("Build Light Data"), Is.False,
                "PreviewGraph should NOT include Build Light Data (lighting is skipped).");
            Assert.That(passTypeNames.Contains("Cluster Culling Light"), Is.False,
                "PreviewGraph should NOT include Cluster Culling Light.");
            Assert.That(passTypeNames.Contains("Cluster Culling Probe"), Is.False,
                "PreviewGraph should NOT include Cluster Culling Probe.");
            Assert.That(passTypeNames.Contains("Builtin Sky"), Is.False,
                "PreviewGraph should NOT include Builtin Sky.");
            Assert.That(passTypeNames.Contains("Transparency"), Is.False,
                "PreviewGraph should NOT include Transparency.");
            Assert.That(passTypeNames.Contains("Editor Wire Overlay"), Is.False,
                "PreviewGraph should NOT include Editor Wire Overlay.");
            Assert.That(passTypeNames.Contains("Draw Object"), Is.False,
                "PreviewGraph should NOT include Draw Object.");
        }

        #endregion

        #region Connections

        /// <summary>
        /// PreviewGraph has the expected slot connections wiring
        /// color and depth targets through the pipeline.
        /// </summary>
        [Test]
        public void PreviewGraph_HasExpectedConnections()
        {
            var asset = Resources.Load<RenderGraphAsset>("RenderGraphs/PreviewGraph");
            Assume.That(asset, Is.Not.Null, "PreviewGraph asset must exist for this test.");

            List<SlotConnection> connections = asset.Connections;

            Assert.That(connections, Is.Not.Null,
                "Connections list should not be null.");
            Assert.That(connections.Count, Is.EqualTo(3),
                "PreviewGraph should have exactly 3 slot connections.");

            // Connection 1: Color Target → Opaque (colorTarget)
            Assert.That(connections[0].SourcePass, Is.EqualTo("Color Target"),
                "First connection source should be 'Color Target'.");
            Assert.That(connections[0].SourceSlot, Is.EqualTo("colorTargetSlot"),
                "First connection source slot should be 'colorTargetSlot'.");
            Assert.That(connections[0].TargetPass, Is.EqualTo("Opaque"),
                "First connection target should be 'Opaque'.");
            Assert.That(connections[0].TargetSlot, Is.EqualTo("colorTargetSlot"),
                "First connection target slot should be 'colorTargetSlot'.");

            // Connection 2: Depth Target → Opaque (depthTarget)
            Assert.That(connections[1].SourcePass, Is.EqualTo("Depth Target"),
                "Second connection source should be 'Depth Target'.");
            Assert.That(connections[1].SourceSlot, Is.EqualTo("depthTargetSlot"),
                "Second connection source slot should be 'depthTargetSlot'.");
            Assert.That(connections[1].TargetPass, Is.EqualTo("Opaque"),
                "Second connection target should be 'Opaque'.");
            Assert.That(connections[1].TargetSlot, Is.EqualTo("depthTargetSlot"),
                "Second connection target slot should be 'depthTargetSlot'.");

            // Connection 3: Opaque → Final Blit (colorTarget)
            Assert.That(connections[2].SourcePass, Is.EqualTo("Opaque"),
                "Third connection source should be 'Opaque'.");
            Assert.That(connections[2].SourceSlot, Is.EqualTo("colorTargetSlot"),
                "Third connection source slot should be 'colorTargetSlot'.");
            Assert.That(connections[2].TargetPass, Is.EqualTo("Final Blit"),
                "Third connection target should be 'Final Blit'.");
            Assert.That(connections[2].TargetSlot, Is.EqualTo("colorTargetSlot"),
                "Third connection target slot should be 'colorTargetSlot'.");
        }

        #endregion

        #region Build — Runtime Pass Instantiation

        /// <summary>
        /// <see cref="RenderGraphAsset.Build"/> successfully instantiates
        /// all four passes from the PreviewGraph asset.
        /// </summary>
        [Test]
        public void Build_FromPreviewGraph_InstantiatesAllPasses()
        {
            var asset = Resources.Load<RenderGraphAsset>("RenderGraphs/PreviewGraph");
            Assume.That(asset, Is.Not.Null, "PreviewGraph asset must exist for this test.");

            List<Pass> result = asset.Build(renderer: null);

            Assert.That(result, Is.Not.Null,
                "Build should return a non-null list.");
            Assert.That(result.Count, Is.EqualTo(4),
                "Build should instantiate all 4 passes from PreviewGraph.");
            Assert.That(result.TrueForAll(p => p.IsEnabled), Is.True,
                "All PreviewGraph passes should be enabled by default.");
        }

        #endregion

        #region Settings

        /// <summary>
        /// PreviewGraph.Settings has preview-appropriate values:
        /// SHEvalMode is PerVertex (default) and AllowHDR is false.
        /// </summary>
        [Test]
        public void Settings_HasPreviewAppropriateDefaults()
        {
            var asset = Resources.Load<RenderGraphAsset>("RenderGraphs/PreviewGraph");
            Assume.That(asset, Is.Not.Null, "PreviewGraph asset must exist for this test.");

            Assert.That(asset.Settings.SHEvalMode, Is.EqualTo(SHEvalMode.PerVertex),
                "Preview should use PerVertex SH evaluation (simpler lighting).");
            Assert.That(asset.Settings.AllowHDR, Is.False,
                "Preview should not allocate HDR targets.");
        }

        #endregion
    }
}
