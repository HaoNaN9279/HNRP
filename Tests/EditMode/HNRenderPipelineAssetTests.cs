// <copyright file="HNRenderPipelineAssetTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using NUnit.Framework;
using UnityEngine;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for <see cref="HNRenderPipelineAsset"/> — verifies that
    /// <see cref="RenderGraphAsset"/> fields serialize and can be assigned
    /// for each camera type (Game, SceneView, Preview, Reflection).
    /// </summary>
    public class HNRenderPipelineAssetTests
    {
        [Test]
        public void DefaultGameRenderGraph_CanBeAssigned()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            var renderGraph = ScriptableObject.CreateInstance<RenderGraphAsset>();

            try
            {
                Assert.That(asset.DefaultGameRenderGraph, Is.Null,
                    "New asset should have no default Game render graph.");

                asset.DefaultGameRenderGraph = renderGraph;

                Assert.That(asset.DefaultGameRenderGraph, Is.SameAs(renderGraph),
                    "DefaultGameRenderGraph should return the assigned render graph.");
                Assert.That(asset.DefaultGameRenderGraph, Is.InstanceOf<RenderGraphAsset>(),
                    "DefaultGameRenderGraph should be a RenderGraphAsset.");
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(renderGraph);
            }
        }

        [Test]
        public void DefaultGameRenderGraph_CanBeCleared()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            var renderGraph = ScriptableObject.CreateInstance<RenderGraphAsset>();

            try
            {
                asset.DefaultGameRenderGraph = renderGraph;
                Assert.That(asset.DefaultGameRenderGraph, Is.Not.Null);

                asset.DefaultGameRenderGraph = null;
                Assert.That(asset.DefaultGameRenderGraph, Is.Null,
                    "DefaultGameRenderGraph should be clearable back to null.");
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(renderGraph);
            }
        }

        [Test]
        public void DefaultReflectionRenderGraph_CanBeAssigned()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            var renderGraph = ScriptableObject.CreateInstance<RenderGraphAsset>();

            try
            {
                Assert.That(asset.DefaultReflectionRenderGraph, Is.Null);

                asset.DefaultReflectionRenderGraph = renderGraph;

                Assert.That(asset.DefaultReflectionRenderGraph, Is.SameAs(renderGraph));
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(renderGraph);
            }
        }

        [Test]
        public void DefaultReflectionRenderGraph_CanBeCleared()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            var renderGraph = ScriptableObject.CreateInstance<RenderGraphAsset>();

            try
            {
                asset.DefaultReflectionRenderGraph = renderGraph;
                Assert.That(asset.DefaultReflectionRenderGraph, Is.Not.Null);

                asset.DefaultReflectionRenderGraph = null;
                Assert.That(asset.DefaultReflectionRenderGraph, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(renderGraph);
            }
        }

#if UNITY_EDITOR
        [Test]
        public void DefaultSceneViewRenderGraph_CanBeAssigned()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            var renderGraph = ScriptableObject.CreateInstance<RenderGraphAsset>();

            try
            {
                Assert.That(asset.DefaultSceneViewRenderGraph, Is.Null);

                asset.DefaultSceneViewRenderGraph = renderGraph;

                Assert.That(asset.DefaultSceneViewRenderGraph, Is.SameAs(renderGraph));
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(renderGraph);
            }
        }

        [Test]
        public void DefaultPreviewRenderGraph_CanBeAssigned()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            var renderGraph = ScriptableObject.CreateInstance<RenderGraphAsset>();

            try
            {
                Assert.That(asset.DefaultPreviewRenderGraph, Is.Null);

                asset.DefaultPreviewRenderGraph = renderGraph;

                Assert.That(asset.DefaultPreviewRenderGraph, Is.SameAs(renderGraph));
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(renderGraph);
            }
        }

        [Test]
        public void DefaultSceneViewRenderGraph_CanBeCleared()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            var renderGraph = ScriptableObject.CreateInstance<RenderGraphAsset>();

            try
            {
                asset.DefaultSceneViewRenderGraph = renderGraph;
                Assert.That(asset.DefaultSceneViewRenderGraph, Is.Not.Null);

                asset.DefaultSceneViewRenderGraph = null;
                Assert.That(asset.DefaultSceneViewRenderGraph, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(renderGraph);
            }
        }

        [Test]
        public void DefaultPreviewRenderGraph_CanBeCleared()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            var renderGraph = ScriptableObject.CreateInstance<RenderGraphAsset>();

            try
            {
                asset.DefaultPreviewRenderGraph = renderGraph;
                Assert.That(asset.DefaultPreviewRenderGraph, Is.Not.Null);

                asset.DefaultPreviewRenderGraph = null;
                Assert.That(asset.DefaultPreviewRenderGraph, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(renderGraph);
            }
        }
#endif

        [Test]
        public void RenderGraphs_AreIndependent()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            var gameRenderGraph = ScriptableObject.CreateInstance<RenderGraphAsset>();
            var reflectionRenderGraph = ScriptableObject.CreateInstance<RenderGraphAsset>();

            try
            {
                asset.DefaultGameRenderGraph = gameRenderGraph;
                asset.DefaultReflectionRenderGraph = reflectionRenderGraph;

                Assert.That(asset.DefaultGameRenderGraph, Is.SameAs(gameRenderGraph));
                Assert.That(asset.DefaultReflectionRenderGraph, Is.SameAs(reflectionRenderGraph));
                Assert.That(asset.DefaultGameRenderGraph, Is.Not.SameAs(reflectionRenderGraph),
                    "Game and Reflection render graphs should be independent.");
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(gameRenderGraph);
                Object.DestroyImmediate(reflectionRenderGraph);
            }
        }

        [Test]
        public void Asset_DefaultsToNullRenderGraphs()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();

            try
            {
                Assert.That(asset.DefaultGameRenderGraph, Is.Null,
                    "DefaultGameRenderGraph should default to null.");
                Assert.That(asset.DefaultReflectionRenderGraph, Is.Null,
                    "DefaultReflectionRenderGraph should default to null.");
#if UNITY_EDITOR
                Assert.That(asset.DefaultSceneViewRenderGraph, Is.Null,
                    "DefaultSceneViewRenderGraph should default to null.");
                Assert.That(asset.DefaultPreviewRenderGraph, Is.Null,
                    "DefaultPreviewRenderGraph should default to null.");
#endif
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void Asset_AllRenderGraphs_CanBeAssignedTogether()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            var gameRenderGraph = ScriptableObject.CreateInstance<RenderGraphAsset>();
            var reflectionRenderGraph = ScriptableObject.CreateInstance<RenderGraphAsset>();
#if UNITY_EDITOR
            var sceneRenderGraph = ScriptableObject.CreateInstance<RenderGraphAsset>();
            var previewRenderGraph = ScriptableObject.CreateInstance<RenderGraphAsset>();
#endif

            try
            {
                asset.DefaultGameRenderGraph = gameRenderGraph;
                asset.DefaultReflectionRenderGraph = reflectionRenderGraph;
#if UNITY_EDITOR
                asset.DefaultSceneViewRenderGraph = sceneRenderGraph;
                asset.DefaultPreviewRenderGraph = previewRenderGraph;
#endif

                Assert.That(asset.DefaultGameRenderGraph, Is.SameAs(gameRenderGraph));
                Assert.That(asset.DefaultReflectionRenderGraph, Is.SameAs(reflectionRenderGraph));
#if UNITY_EDITOR
                Assert.That(asset.DefaultSceneViewRenderGraph, Is.SameAs(sceneRenderGraph));
                Assert.That(asset.DefaultPreviewRenderGraph, Is.SameAs(previewRenderGraph));
#endif
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(gameRenderGraph);
                Object.DestroyImmediate(reflectionRenderGraph);
#if UNITY_EDITOR
                Object.DestroyImmediate(sceneRenderGraph);
                Object.DestroyImmediate(previewRenderGraph);
#endif
            }
        }
    }
}
