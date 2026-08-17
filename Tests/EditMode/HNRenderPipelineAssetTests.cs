// <copyright file="HNRenderPipelineAssetTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using NUnit.Framework;
using UnityEngine;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for <see cref="HNRenderPipelineAsset"/> — verifies that
    /// <see cref="CameraPipelineConfig"/> fields serialize and can be assigned
    /// for each camera type (Game, SceneView, Preview, Reflection).
    /// </summary>
    public class HNRenderPipelineAssetTests
    {
        [Test]
        public void DefaultGameCameraConfig_CanBeAssigned()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            var config = ScriptableObject.CreateInstance<CameraPipelineConfig>();

            try
            {
                Assert.That(asset.DefaultGameCameraConfig, Is.Null,
                    "New asset should have no default Game config.");

                asset.DefaultGameCameraConfig = config;

                Assert.That(asset.DefaultGameCameraConfig, Is.SameAs(config),
                    "DefaultGameCameraConfig should return the assigned config.");
                Assert.That(asset.DefaultGameCameraConfig, Is.InstanceOf<CameraPipelineConfig>(),
                    "DefaultGameCameraConfig should be a CameraPipelineConfig.");
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void DefaultGameCameraConfig_CanBeCleared()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            var config = ScriptableObject.CreateInstance<CameraPipelineConfig>();

            try
            {
                asset.DefaultGameCameraConfig = config;
                Assert.That(asset.DefaultGameCameraConfig, Is.Not.Null);

                asset.DefaultGameCameraConfig = null;
                Assert.That(asset.DefaultGameCameraConfig, Is.Null,
                    "DefaultGameCameraConfig should be clearable back to null.");
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void DefaultReflectionCameraConfig_CanBeAssigned()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            var config = ScriptableObject.CreateInstance<CameraPipelineConfig>();

            try
            {
                Assert.That(asset.DefaultReflectionCameraConfig, Is.Null);

                asset.DefaultReflectionCameraConfig = config;

                Assert.That(asset.DefaultReflectionCameraConfig, Is.SameAs(config));
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void DefaultReflectionCameraConfig_CanBeCleared()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            var config = ScriptableObject.CreateInstance<CameraPipelineConfig>();

            try
            {
                asset.DefaultReflectionCameraConfig = config;
                Assert.That(asset.DefaultReflectionCameraConfig, Is.Not.Null);

                asset.DefaultReflectionCameraConfig = null;
                Assert.That(asset.DefaultReflectionCameraConfig, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(config);
            }
        }

#if UNITY_EDITOR
        [Test]
        public void DefaultSceneViewCameraConfig_CanBeAssigned()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            var config = ScriptableObject.CreateInstance<CameraPipelineConfig>();

            try
            {
                Assert.That(asset.DefaultSceneViewCameraConfig, Is.Null);

                asset.DefaultSceneViewCameraConfig = config;

                Assert.That(asset.DefaultSceneViewCameraConfig, Is.SameAs(config));
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void DefaultPreviewCameraConfig_CanBeAssigned()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            var config = ScriptableObject.CreateInstance<CameraPipelineConfig>();

            try
            {
                Assert.That(asset.DefaultPreviewCameraConfig, Is.Null);

                asset.DefaultPreviewCameraConfig = config;

                Assert.That(asset.DefaultPreviewCameraConfig, Is.SameAs(config));
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void DefaultSceneViewCameraConfig_CanBeCleared()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            var config = ScriptableObject.CreateInstance<CameraPipelineConfig>();

            try
            {
                asset.DefaultSceneViewCameraConfig = config;
                Assert.That(asset.DefaultSceneViewCameraConfig, Is.Not.Null);

                asset.DefaultSceneViewCameraConfig = null;
                Assert.That(asset.DefaultSceneViewCameraConfig, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void DefaultPreviewCameraConfig_CanBeCleared()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            var config = ScriptableObject.CreateInstance<CameraPipelineConfig>();

            try
            {
                asset.DefaultPreviewCameraConfig = config;
                Assert.That(asset.DefaultPreviewCameraConfig, Is.Not.Null);

                asset.DefaultPreviewCameraConfig = null;
                Assert.That(asset.DefaultPreviewCameraConfig, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(config);
            }
        }
#endif

        [Test]
        public void Configs_AreIndependent()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            var gameConfig = ScriptableObject.CreateInstance<CameraPipelineConfig>();
            var reflectionConfig = ScriptableObject.CreateInstance<CameraPipelineConfig>();

            try
            {
                asset.DefaultGameCameraConfig = gameConfig;
                asset.DefaultReflectionCameraConfig = reflectionConfig;

                Assert.That(asset.DefaultGameCameraConfig, Is.SameAs(gameConfig));
                Assert.That(asset.DefaultReflectionCameraConfig, Is.SameAs(reflectionConfig));
                Assert.That(asset.DefaultGameCameraConfig, Is.Not.SameAs(reflectionConfig),
                    "Game and Reflection configs should be independent.");
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(gameConfig);
                Object.DestroyImmediate(reflectionConfig);
            }
        }

        [Test]
        public void Asset_DefaultsToNullConfigs()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();

            try
            {
                Assert.That(asset.DefaultGameCameraConfig, Is.Null,
                    "DefaultGameCameraConfig should default to null.");
                Assert.That(asset.DefaultReflectionCameraConfig, Is.Null,
                    "DefaultReflectionCameraConfig should default to null.");
#if UNITY_EDITOR
                Assert.That(asset.DefaultSceneViewCameraConfig, Is.Null,
                    "DefaultSceneViewCameraConfig should default to null.");
                Assert.That(asset.DefaultPreviewCameraConfig, Is.Null,
                    "DefaultPreviewCameraConfig should default to null.");
#endif
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void Asset_AllConfigs_CanBeAssignedTogether()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            var gameConfig = ScriptableObject.CreateInstance<CameraPipelineConfig>();
            var reflectionConfig = ScriptableObject.CreateInstance<CameraPipelineConfig>();
#if UNITY_EDITOR
            var sceneConfig = ScriptableObject.CreateInstance<CameraPipelineConfig>();
            var previewConfig = ScriptableObject.CreateInstance<CameraPipelineConfig>();
#endif

            try
            {
                asset.DefaultGameCameraConfig = gameConfig;
                asset.DefaultReflectionCameraConfig = reflectionConfig;
#if UNITY_EDITOR
                asset.DefaultSceneViewCameraConfig = sceneConfig;
                asset.DefaultPreviewCameraConfig = previewConfig;
#endif

                Assert.That(asset.DefaultGameCameraConfig, Is.SameAs(gameConfig));
                Assert.That(asset.DefaultReflectionCameraConfig, Is.SameAs(reflectionConfig));
#if UNITY_EDITOR
                Assert.That(asset.DefaultSceneViewCameraConfig, Is.SameAs(sceneConfig));
                Assert.That(asset.DefaultPreviewCameraConfig, Is.SameAs(previewConfig));
#endif
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(gameConfig);
                Object.DestroyImmediate(reflectionConfig);
#if UNITY_EDITOR
                Object.DestroyImmediate(sceneConfig);
                Object.DestroyImmediate(previewConfig);
#endif
            }
        }
    }
}
