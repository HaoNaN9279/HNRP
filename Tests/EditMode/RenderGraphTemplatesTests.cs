// <copyright file="RenderGraphTemplatesTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System.Linq;
using NUnit.Framework;
using UnityEngine;
using HN.HNRP;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for <see cref="RenderGraphTemplates"/> — verifies that
    /// <see cref="RenderGraphTemplate.Ensure"/> returns a persistent
    /// (AssetDatabase-backed) <see cref="RenderGraphAsset"/>, caches a single
    /// instance per template, and populates exactly the expected definitions
    /// for the Standard and Preview graphs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="RenderGraphTemplate.Ensure"/> returns a persistent asset —
    /// tests must NOT <see cref="Object.DestroyImmediate"/> it.
    /// </para>
    /// <para>
    /// These tests exercise only the template definitions and the Ensure
    /// mechanism. They do not call <see cref="RenderGraphAsset.Build"/> and do
    /// not depend on <see cref="PassRegistry"/> registration.
    /// </para>
    /// </remarks>
    public sealed class RenderGraphTemplatesTests
    {
        #region Ensure — Non-null & Unique

        /// <summary>
        /// <see cref="RenderGraphTemplates.Standard"/>.Ensure() returns a
        /// non-null asset, and repeated calls return the same cached instance.
        /// </summary>
        [Test]
        public void StandardTemplate_Ensure_ReturnsNonNullAndUnique()
        {
            RenderGraphAsset first = RenderGraphTemplates.Standard.Ensure();
            RenderGraphAsset second = RenderGraphTemplates.Standard.Ensure();

            Assert.That(first, Is.Not.Null,
                "Standard template Ensure() should return a non-null render graph asset.");
            Assert.That(second, Is.SameAs(first),
                "Standard template Ensure() should return the same cached asset on repeated calls.");
        }

        /// <summary>
        /// <see cref="RenderGraphTemplates.Preview"/>.Ensure() returns a
        /// non-null asset, and repeated calls return the same cached instance.
        /// </summary>
        [Test]
        public void PreviewTemplate_Ensure_ReturnsNonNullAndUnique()
        {
            RenderGraphAsset first = RenderGraphTemplates.Preview.Ensure();
            RenderGraphAsset second = RenderGraphTemplates.Preview.Ensure();

            Assert.That(first, Is.Not.Null,
                "Preview template Ensure() should return a non-null render graph asset.");
            Assert.That(second, Is.SameAs(first),
                "Preview template Ensure() should return the same cached asset on repeated calls.");
        }

        #endregion

        #region Ensure — Definition Content

        /// <summary>
        /// The Standard template declares the full 8-pass pipeline (buildLight /
        /// clusterProbe / clusterLight / forwardOpaque / sky / transparency /
        /// wireOverlay / finalBlit), 9 resources, the chained slot connections,
        /// and PerPixel HDR settings.
        /// </summary>
        [Test]
        public void StandardTemplate_HasExpectedDefinition()
        {
            RenderGraphAsset asset = RenderGraphTemplates.Standard.Ensure();

            Assert.That(asset.Passes.Count, Is.EqualTo(8),
                "Standard template should declare exactly 8 passes.");
            Assert.That(asset.Connections.Count, Is.EqualTo(12),
                "Standard template should declare exactly 12 slot connections.");
            Assert.That(asset.Resources.Count, Is.EqualTo(9),
                "Standard template should declare exactly 9 resources.");
            Assert.That(asset.ResourceConnections.Count, Is.EqualTo(14),
                "Standard template should declare exactly 14 resource connections.");

            Assert.That(asset.Settings.SHEvalMode, Is.EqualTo(SHEvalMode.PerPixel),
                "Standard template should use PerPixel SH evaluation.");
            Assert.That(asset.Settings.AllowHDR, Is.True,
                "Standard template should allow HDR render targets.");

            foreach (string passName in new[] { "buildLight", "forwardOpaque", "finalBlit" })
            {
                Assert.That(asset.Passes.Any(p => p.InstanceName == passName), Is.True,
                    $"Standard template should declare a '{passName}' pass.");
            }

            foreach (string resourceName in new[] { "ColorBuffer", "LightDatas", "OpaqueRendererList" })
            {
                Assert.That(asset.Resources.Any(r => r.ResourceName == resourceName), Is.True,
                    $"Standard template should declare a '{resourceName}' resource.");
            }

            Assert.That(
                asset.Connections.Any(c =>
                    c.SourcePass == "forwardOpaque"
                    && c.SourceSlot == "ColorTargetOutput"
                    && c.TargetPass == "sky"
                    && c.TargetSlot == "ColorTarget"),
                Is.True,
                "Standard template should connect forwardOpaque.ColorTargetOutput to sky.ColorTarget.");
        }

        /// <summary>
        /// The Preview template declares the minimal 2-pass pipeline (opaque /
        /// finalBlit), 3 resources (ColorBuffer / DepthBuffer /
        /// OpaqueRendererList), the single chained connection, and PerVertex
        /// non-HDR settings.
        /// </summary>
        [Test]
        public void PreviewTemplate_HasExpectedDefinition()
        {
            RenderGraphAsset asset = RenderGraphTemplates.Preview.Ensure();

            Assert.That(asset.Passes.Count, Is.EqualTo(2),
                "Preview template should declare exactly 2 passes.");
            Assert.That(asset.Connections.Count, Is.EqualTo(1),
                "Preview template should declare exactly 1 slot connection.");
            Assert.That(asset.Resources.Count, Is.EqualTo(3),
                "Preview template should declare exactly 3 resources.");
            Assert.That(asset.ResourceConnections.Count, Is.EqualTo(3),
                "Preview template should declare exactly 3 resource connections.");

            Assert.That(asset.Settings.SHEvalMode, Is.EqualTo(SHEvalMode.PerVertex),
                "Preview template should use PerVertex SH evaluation.");
            Assert.That(asset.Settings.AllowHDR, Is.False,
                "Preview template should not allocate HDR render targets.");

            foreach (string passName in new[] { "opaque", "finalBlit" })
            {
                Assert.That(asset.Passes.Any(p => p.InstanceName == passName), Is.True,
                    $"Preview template should declare a '{passName}' pass.");
            }

            foreach (string resourceName in new[] { "ColorBuffer", "DepthBuffer", "OpaqueRendererList" })
            {
                Assert.That(asset.Resources.Any(r => r.ResourceName == resourceName), Is.True,
                    $"Preview template should declare a '{resourceName}' resource.");
            }
        }

        #endregion

        #region Ensure — Distinctness

        /// <summary>
        /// The Standard and Preview templates resolve to two distinct persistent
        /// assets — they must not share a cached instance.
        /// </summary>
        [Test]
        public void StandardAndPreview_AreDistinct()
        {
            RenderGraphAsset standard = RenderGraphTemplates.Standard.Ensure();
            RenderGraphAsset preview = RenderGraphTemplates.Preview.Ensure();

            Assert.That(standard, Is.Not.SameAs(preview),
                "Standard and Preview templates should resolve to distinct render graph assets.");
        }

        #endregion

#if UNITY_EDITOR
        #region Ensure — Non-Mutating Guard

        /// <summary>
        /// <see cref="RenderGraphTemplate.Ensure"/> must not overwrite an
        /// already-populated template asset (protects user edits). Loads the
        /// existing Standard asset directly, records its pass count, calls
        /// Ensure, and asserts the pass list is unchanged.
        /// </summary>
        [Test]
        public void TemplateDoesNotMutate_ExistingNonEmptyAsset()
        {
            RenderGraphAsset asset =
                AssetDatabase.LoadAssetAtPath<RenderGraphAsset>(RenderGraphTemplates.Standard.AssetPath);
            Assume.That(asset, Is.Not.Null,
                "Standard template asset must already exist for the non-overwrite guard to be testable.");
            Assume.That(asset.Passes.Count, Is.GreaterThan(0),
                "Standard template asset must already be populated for the non-overwrite guard to be testable.");

            int passesBefore = asset.Passes.Count;

            RenderGraphAsset ensured = RenderGraphTemplates.Standard.Ensure();

            Assert.That(ensured, Is.SameAs(asset),
                "Ensure() should return the existing asset rather than replacing it.");
            Assert.That(asset.Passes.Count, Is.EqualTo(passesBefore),
                "Ensure() must not overwrite an already-populated template asset.");
        }

        #endregion
#endif
    }
}
