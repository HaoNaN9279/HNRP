// <copyright file="HNRenderPipelineAssetTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using NUnit.Framework;
using UnityEngine;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for <see cref="HNRenderPipelineAsset"/> — verifies that
    /// <see cref="RenderGraphViewBlock"/> fields serialize and can be assigned
    /// for each camera type (Game, SceneView, Preview, Reflection).
    /// </summary>
    public class HNRenderPipelineAssetTests
    {
        [Test]
        public void GameViewRenderGraphViewBlock_IsNotNull()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();

            try
            {
                Assert.That(asset.gameViewRenderGraphViewBlock, Is.Not.Null,
                    "New asset should have a GameViewRenderGraphViewBlock.");
                Assert.That(asset.gameViewRenderGraphViewBlock, Is.InstanceOf<GameViewRenderGraphViewBlock>(),
                    "gameViewRenderGraphViewBlock should be a GameViewRenderGraphViewBlock.");
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void ReflectionRenderGraphViewBlock_IsNotNull()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();

            try
            {
                Assert.That(asset.reflectionRenderGraphViewBlock, Is.Not.Null,
                    "New asset should have a ReflectionRenderGraphViewBlock.");
                Assert.That(asset.reflectionRenderGraphViewBlock, Is.InstanceOf<ReflectionRenderGraphViewBlock>(),
                    "reflectionRenderGraphViewBlock should be a ReflectionRenderGraphViewBlock.");
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

#if UNITY_EDITOR
        [Test]
        public void SceneViewRenderGraphViewBlock_IsNotNull()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();

            try
            {
                Assert.That(asset.sceneViewRenderGraphViewBlock, Is.Not.Null,
                    "New asset should have a SceneViewRenderGraphViewBlock.");
                Assert.That(asset.sceneViewRenderGraphViewBlock, Is.InstanceOf<SceneViewRenderGraphViewBlock>(),
                    "sceneViewRenderGraphViewBlock should be a SceneViewRenderGraphViewBlock.");
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void PreviewRenderGraphViewBlock_IsNotNull()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();

            try
            {
                Assert.That(asset.previewRenderGraphViewBlock, Is.Not.Null,
                    "New asset should have a PreviewRenderGraphViewBlock.");
                Assert.That(asset.previewRenderGraphViewBlock, Is.InstanceOf<PreviewRenderGraphViewBlock>(),
                    "previewRenderGraphViewBlock should be a PreviewRenderGraphViewBlock.");
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }
#endif

        [Test]
        public void ViewBlocks_AreIndependent()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();

            try
            {
                Assert.That(asset.gameViewRenderGraphViewBlock, Is.Not.SameAs(asset.reflectionRenderGraphViewBlock),
                    "Game and Reflection view blocks should be independent.");
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void ViewBlocks_ContainDefaultViews()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();

            try
            {
                Assert.That(asset.gameViewRenderGraphViewBlock.RenderGraphViews.Count, Is.GreaterThan(0),
                    "GameViewRenderGraphViewBlock should contain at least one default view.");
                Assert.That(asset.reflectionRenderGraphViewBlock.RenderGraphViews.Count, Is.GreaterThan(0),
                    "ReflectionRenderGraphViewBlock should contain at least one default view.");
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }
    }
}
