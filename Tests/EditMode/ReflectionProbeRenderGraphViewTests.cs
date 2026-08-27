// <copyright file="ReflectionProbeRenderGraphViewTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using NUnit.Framework;
using UnityEngine;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for per-probe render graph view selection:
    /// <see cref="HNAdditionalReflectionProbeData.RenderGraphViewIndex"/> and
    /// <see cref="ReflectionProbeRenderUtils.SelectReflectionRenderGraph"/>.
    /// </summary>
    public sealed class ReflectionProbeRenderGraphViewTests
    {
        #region RenderGraphViewIndex

        /// <summary>
        /// Verifies a new <see cref="HNAdditionalReflectionProbeData"/> defaults to
        /// render graph view index 0.
        /// </summary>
        [Test]
        public void RenderGraphViewIndex_DefaultsToZero()
        {
            var go = new GameObject("Probe");
            var data = go.AddComponent<HNAdditionalReflectionProbeData>();

            try
            {
                Assert.That(data.RenderGraphViewIndex, Is.Zero,
                    "New probe data should default to render graph view index 0.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Verifies the render graph view index can be set and read back.
        /// </summary>
        [Test]
        public void RenderGraphViewIndex_SetAndGet()
        {
            var go = new GameObject("Probe");
            var data = go.AddComponent<HNAdditionalReflectionProbeData>();

            try
            {
                data.RenderGraphViewIndex = 2;

                Assert.That(data.RenderGraphViewIndex, Is.EqualTo(2),
                    "Set value should round-trip through the property.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        #endregion

        #region SelectReflectionRenderGraph

        /// <summary>
        /// Verifies the probe's render graph view index selects the corresponding
        /// render graph asset from the reflection view block.
        /// </summary>
        [Test]
        public void SelectReflectionRenderGraph_ByIndex()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            var graph0 = ScriptableObject.CreateInstance<RenderGraphAsset>();
            var graph1 = ScriptableObject.CreateInstance<RenderGraphAsset>();

            RenderGraphViewBlock block = asset.reflectionRenderGraphViewBlock;
            string firstKey = new System.Collections.Generic.List<string>(block.RenderGraphViews.Keys)[0];
            block.RenderGraphViews[firstKey] = graph0;
            block.CreateView("SecondView");
            block.RenderGraphViews["SecondView"] = graph1;

            var go = new GameObject("Probe");
            var probe = go.AddComponent<ReflectionProbe>();
            var data = go.AddComponent<HNAdditionalReflectionProbeData>();

            try
            {
                data.RenderGraphViewIndex = 0;
                Assert.That(ReflectionProbeRenderUtils.SelectReflectionRenderGraph(asset, probe),
                    Is.SameAs(graph0), "Index 0 should select the first view.");

                data.RenderGraphViewIndex = 1;
                Assert.That(ReflectionProbeRenderUtils.SelectReflectionRenderGraph(asset, probe),
                    Is.SameAs(graph1), "Index 1 should select the second view.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(asset);
                UnityEngine.Object.DestroyImmediate(graph0);
                UnityEngine.Object.DestroyImmediate(graph1);
            }
        }

        /// <summary>
        /// Verifies an out-of-range index falls back to the first view.
        /// </summary>
        [Test]
        public void SelectReflectionRenderGraph_OutOfRangeIndex_FallsBackToFirst()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            var graph0 = ScriptableObject.CreateInstance<RenderGraphAsset>();

            RenderGraphViewBlock block = asset.reflectionRenderGraphViewBlock;
            string firstKey = new System.Collections.Generic.List<string>(block.RenderGraphViews.Keys)[0];
            block.RenderGraphViews[firstKey] = graph0;

            var go = new GameObject("Probe");
            var probe = go.AddComponent<ReflectionProbe>();
            var data = go.AddComponent<HNAdditionalReflectionProbeData>();

            try
            {
                data.RenderGraphViewIndex = 99;
                Assert.That(ReflectionProbeRenderUtils.SelectReflectionRenderGraph(asset, probe),
                    Is.SameAs(graph0), "Out-of-range index should fall back to the first view.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(asset);
                UnityEngine.Object.DestroyImmediate(graph0);
            }
        }

        /// <summary>
        /// Verifies a null probe falls back to the first view.
        /// </summary>
        [Test]
        public void SelectReflectionRenderGraph_NullProbe_FallsBackToFirst()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            var graph0 = ScriptableObject.CreateInstance<RenderGraphAsset>();

            RenderGraphViewBlock block = asset.reflectionRenderGraphViewBlock;
            string firstKey = new System.Collections.Generic.List<string>(block.RenderGraphViews.Keys)[0];
            block.RenderGraphViews[firstKey] = graph0;

            try
            {
                Assert.That(ReflectionProbeRenderUtils.SelectReflectionRenderGraph(asset, null),
                    Is.SameAs(graph0), "Null probe should fall back to the first view.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
                UnityEngine.Object.DestroyImmediate(graph0);
            }
        }

        #endregion
    }
}
