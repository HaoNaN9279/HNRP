// <copyright file="RenderGraphAssetTests.cs" company="HN">
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
    /// Tests for <see cref="RenderGraphAsset"/> in <c>Runtime/Config/RenderGraphAsset.cs</c>.
    /// Verifies Build() pass instantiation, slot-connection name resolution,
    /// and enabled-pass filtering.
    /// </summary>
    public sealed class RenderGraphAssetTests
    {
        #region Test Pass Subclasses

        /// <summary>
        /// Minimal pass used for build tests. Registered as <c>"TestPassA"</c>.
        /// </summary>
        [Pass("TestPassA")]
        private sealed class TestPassA : Pass
        {
            /// <summary>
            /// Initializes a new instance with the given name.
            /// </summary>
            /// <param name="name">The pass instance name.</param>
            public TestPassA(string name)
                : base(name)
            {
            }

            /// <inheritdoc />
            public override void SetupSlots()
            {
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

        /// <summary>
        /// Minimal pass used for build tests. Registered as <c>"TestPassB"</c>.
        /// </summary>
        [Pass("TestPassB")]
        private sealed class TestPassB : Pass
        {
            /// <summary>
            /// Initializes a new instance with the given name.
            /// </summary>
            /// <param name="name">The pass instance name.</param>
            public TestPassB(string name)
                : base(name)
            {
            }

            /// <inheritdoc />
            public override void SetupSlots()
            {
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

        /// <summary>
        /// Pass that starts disabled. Registered as <c>"TestPassDisabled"</c>.
        /// Used to verify that <see cref="RenderGraphAsset.Build"/> filters
        /// out passes where <see cref="Pass.IsEnabled"/> is <c>false</c>.
        /// </summary>
        [Pass("TestPassDisabled")]
        private sealed class TestPassDisabled : Pass
        {
            /// <summary>
            /// Initializes a new instance that is disabled by default.
            /// </summary>
            /// <param name="name">The pass instance name.</param>
            public TestPassDisabled(string name)
                : base(name)
            {
                IsEnabled = false;
            }

            /// <inheritdoc />
            public override void SetupSlots()
            {
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

        #region Build — Pass Instantiation

        /// <summary>
        /// <see cref="RenderGraphAsset.Build"/> instantiates the correct number of
        /// passes from <see cref="RenderGraphAsset.Passes"/> definitions, each with
        /// the expected <see cref="Pass.PassName"/>.
        /// </summary>
        [Test]
        public void Build_InstantiatesPasses()
        {
            var asset = ScriptableObject.CreateInstance<RenderGraphAsset>();
            asset.Passes.Add(PassDefinition.Create("TestPassA", "Alpha"));
            asset.Passes.Add(PassDefinition.Create("TestPassB", "Beta"));

            List<Pass> result = asset.Build(renderer: null);

            Assert.That(result, Is.Not.Null,
                "Build should return a non-null list.");
            Assert.That(result.Count, Is.EqualTo(2),
                "Build should return exactly two passes for two definitions.");

            Assert.That(result[0].PassName, Is.EqualTo("Alpha"),
                "First pass should have the InstanceName from its definition.");
            Assert.That(result[0].GetType(), Is.EqualTo(typeof(TestPassA)),
                "First pass should be of type TestPassA.");
            Assert.That(result[0].IsEnabled, Is.True,
                "Newly created passes should be enabled by default.");

            Assert.That(result[1].PassName, Is.EqualTo("Beta"),
                "Second pass should have the InstanceName from its definition.");
            Assert.That(result[1].GetType(), Is.EqualTo(typeof(TestPassB)),
                "Second pass should be of type TestPassB.");
            Assert.That(result[1].IsEnabled, Is.True,
                "Newly created passes should be enabled by default.");

            Object.DestroyImmediate(asset);
        }

        /// <summary>
        /// <see cref="RenderGraphAsset.Build"/> returns an empty list when
        /// <see cref="RenderGraphAsset.Passes"/> is empty.
        /// </summary>
        [Test]
        public void Build_EmptyPassesList_ReturnsEmptyList()
        {
            var asset = ScriptableObject.CreateInstance<RenderGraphAsset>();

            List<Pass> result = asset.Build(renderer: null);

            Assert.That(result, Is.Not.Null,
                "Build should return a non-null list even for empty definitions.");
            Assert.That(result.Count, Is.EqualTo(0),
                "Build should return an empty list when no definitions exist.");

            Object.DestroyImmediate(asset);
        }

        /// <summary>
        /// <see cref="RenderGraphAsset.Build"/> skips definitions whose
        /// <see cref="PassDefinition.InstanceName"/> is <c>null</c> or empty.
        /// </summary>
        [Test]
        public void Build_SkipsDefinitionsWithNullInstanceName()
        {
            var asset = ScriptableObject.CreateInstance<RenderGraphAsset>();
            asset.Passes.Add(PassDefinition.Create("TestPassA", null));
            asset.Passes.Add(PassDefinition.Create("TestPassA", string.Empty));
            asset.Passes.Add(PassDefinition.Create("TestPassB", "Valid"));

            List<Pass> result = asset.Build(renderer: null);

            Assert.That(result.Count, Is.EqualTo(1),
                "Build should skip definitions with null/empty InstanceName.");
            Assert.That(result[0].PassName, Is.EqualTo("Valid"),
                "Only the valid definition should be instantiated.");

            Object.DestroyImmediate(asset);
        }

        #endregion

        #region Build — Slot Connections by Name

        /// <summary>
        /// <see cref="RenderGraphAsset.Build"/> correctly resolves passes by name
        /// when processing <see cref="SlotConnection"/> entries. All referenced
        /// passes appear in the result regardless of connection validity.
        /// </summary>
        [Test]
        public void Build_ConnectsSlots_ByName()
        {
            var asset = ScriptableObject.CreateInstance<RenderGraphAsset>();
            asset.Passes.Add(PassDefinition.Create("TestPassA", "SourcePass"));
            asset.Passes.Add(PassDefinition.Create("TestPassB", "TargetPass"));
            asset.Connections.Add(SlotConnection.Create(
                sourcePass: "SourcePass",
                sourceSlot: "ColorOutput",
                targetPass: "TargetPass",
                targetSlot: "ColorInput"));

            List<Pass> result = asset.Build(renderer: null);

            Assert.That(result, Is.Not.Null,
                "Build should return a non-null list.");
            Assert.That(result.Count, Is.EqualTo(2),
                "Both passes should be instantiated.");

            // Verify the correct pass instances exist by name.
            bool hasSource = result.Exists(p => p.PassName == "SourcePass");
            bool hasTarget = result.Exists(p => p.PassName == "TargetPass");

            Assert.That(hasSource, Is.True,
                "SourcePass should be present in the result.");
            Assert.That(hasTarget, Is.True,
                "TargetPass should be present in the result.");
            Assert.That(result.Find(p => p.PassName == "SourcePass").GetType(),
                Is.EqualTo(typeof(TestPassA)),
                "SourcePass should be TestPassA.");
            Assert.That(result.Find(p => p.PassName == "TargetPass").GetType(),
                Is.EqualTo(typeof(TestPassB)),
                "TargetPass should be TestPassB.");

            Object.DestroyImmediate(asset);
        }

        /// <summary>
        /// <see cref="RenderGraphAsset.Build"/> gracefully handles
        /// <see cref="SlotConnection"/> entries where source or target pass
        /// names do not match any instantiated pass.
        /// </summary>
        [Test]
        public void Build_HandlesMissingConnectionTarget_DoesNotThrow()
        {
            var asset = ScriptableObject.CreateInstance<RenderGraphAsset>();
            asset.Passes.Add(PassDefinition.Create("TestPassA", "OnlyPass"));
            asset.Connections.Add(SlotConnection.Create("OnlyPass", "Out", "GhostPass", "In"));

            List<Pass> result = asset.Build(renderer: null);

            Assert.That(result, Is.Not.Null,
                "Build should not throw when a connection references a non-existent pass.");
            Assert.That(result.Count, Is.EqualTo(1),
                "The valid pass should still be instantiated.");
            Assert.That(result[0].PassName, Is.EqualTo("OnlyPass"),
                "The valid pass should be the only one returned.");

            Object.DestroyImmediate(asset);
        }

        #endregion

        #region Build — Enabled Pass Filtering

        /// <summary>
        /// <see cref="RenderGraphAsset.Build"/> returns only passes whose
        /// <see cref="Pass.IsEnabled"/> is <c>true</c>. Passes that set
        /// <c>IsEnabled = false</c> in their constructor are excluded.
        /// </summary>
        [Test]
        public void Build_ReturnsEnabledPassesOnly()
        {
            var asset = ScriptableObject.CreateInstance<RenderGraphAsset>();
            asset.Passes.Add(PassDefinition.Create("TestPassA", "EnabledAlpha"));
            asset.Passes.Add(PassDefinition.Create("TestPassDisabled", "DisabledPass"));
            asset.Passes.Add(PassDefinition.Create("TestPassB", "EnabledBeta"));

            List<Pass> result = asset.Build(renderer: null);

            Assert.That(result, Is.Not.Null,
                "Build should return a non-null list.");
            Assert.That(result.Count, Is.EqualTo(2),
                "Build should exclude the disabled pass, returning only the two enabled ones.");

            // Verify both enabled passes are present.
            bool hasAlpha = result.Exists(p => p.PassName == "EnabledAlpha");
            bool hasBeta = result.Exists(p => p.PassName == "EnabledBeta");
            bool hasDisabled = result.Exists(p => p.PassName == "DisabledPass");

            Assert.That(hasAlpha, Is.True,
                "EnabledAlpha should be in the result.");
            Assert.That(hasBeta, Is.True,
                "EnabledBeta should be in the result.");
            Assert.That(hasDisabled, Is.False,
                "DisabledPass should NOT be in the result.");

            Object.DestroyImmediate(asset);
        }

        #endregion

        #region Settings

        /// <summary>
        /// <see cref="RenderGraphAsset.Settings"/> can be read and written,
        /// and the <see cref="RenderGraphSettings"/> struct fields are
        /// independently mutable.
        /// </summary>
        [Test]
        public void Settings_CanBeReadAndWritten()
        {
            var asset = ScriptableObject.CreateInstance<RenderGraphAsset>();

            // Default state.
            Assert.That(asset.Settings.SHEvalMode, Is.EqualTo(default(SHEvalMode)),
                "Default SHEvalMode should be PerVertex (= 0).");
            Assert.That(asset.Settings.AllowHDR, Is.False,
                "Default AllowHDR should be false.");

            // Write new values.
            var newSettings = new RenderGraphSettings
            {
                SHEvalMode = SHEvalMode.PerPixel,
                AllowHDR = true,
            };
            asset.Settings = newSettings;

            Assert.That(asset.Settings.SHEvalMode, Is.EqualTo(SHEvalMode.PerPixel),
                "SHEvalMode should reflect the written value.");
            Assert.That(asset.Settings.AllowHDR, Is.True,
                "AllowHDR should reflect the written value.");

            Object.DestroyImmediate(asset);
        }

        #endregion
    }
}
