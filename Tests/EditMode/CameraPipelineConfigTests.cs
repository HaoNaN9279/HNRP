// <copyright file="CameraPipelineConfigTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using NUnit.Framework;
using UnityEngine;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for <see cref="CameraPipelineConfig"/> — the camera-level
    /// pipeline configuration that maps a camera to a
    /// <see cref="RenderGraphAsset"/> template with optional settings overrides.
    /// </summary>
    public class CameraPipelineConfigTests
    {
        [Test]
        public void Config_ReferencesRenderGraphAsset()
        {
            var config = ScriptableObject.CreateInstance<CameraPipelineConfig>();
            var graph = ScriptableObject.CreateInstance<RenderGraphAsset>();

            try
            {
                Assert.That(config.RenderGraph, Is.Null,
                    "New config should have no render graph assigned.");

                config.RenderGraph = graph;

                Assert.That(config.RenderGraph, Is.SameAs(graph),
                    "RenderGraph should return the assigned asset.");
                Assert.That(config.RenderGraph, Is.InstanceOf<RenderGraphAsset>(),
                    "RenderGraph should be a RenderGraphAsset.");
            }
            finally
            {
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(graph);
            }
        }

        [Test]
        public void Config_RenderGraph_CanBeCleared()
        {
            var config = ScriptableObject.CreateInstance<CameraPipelineConfig>();
            var graph = ScriptableObject.CreateInstance<RenderGraphAsset>();

            try
            {
                config.RenderGraph = graph;
                Assert.That(config.RenderGraph, Is.Not.Null);

                config.RenderGraph = null;
                Assert.That(config.RenderGraph, Is.Null,
                    "RenderGraph should be clearable back to null.");
            }
            finally
            {
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(graph);
            }
        }

        [Test]
        public void SettingsOverride_DefaultsToNotOverridden()
        {
            var config = ScriptableObject.CreateInstance<CameraPipelineConfig>();

            try
            {
                var settings = config.SettingsOverride;

                Assert.That(settings.IsOverridden, Is.False,
                    "Default settings override should not be marked as overridden.");
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void SettingsOverride_MergesCorrectly()
        {
            var config = ScriptableObject.CreateInstance<CameraPipelineConfig>();

            try
            {
                // Start with defaults — no overrides.
                Assert.That(config.SettingsOverride.IsOverridden, Is.False,
                    "Initial settings should be non-overridden.");

                // Override with custom settings.
                var customSettings = new CameraPipelineConfigSettings
                {
                    IsOverridden = true
                };

                config.SettingsOverride = customSettings;

                Assert.That(config.SettingsOverride.IsOverridden, Is.True,
                    "After override, IsOverridden should be true.");

                // Clear the override back to defaults.
                config.SettingsOverride = default;

                Assert.That(config.SettingsOverride.IsOverridden, Is.False,
                    "After resetting to default, IsOverridden should be false.");
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void SettingsOverride_IsIndependentPerConfig()
        {
            var configA = ScriptableObject.CreateInstance<CameraPipelineConfig>();
            var configB = ScriptableObject.CreateInstance<CameraPipelineConfig>();

            try
            {
                // Override only config A.
                configA.SettingsOverride = new CameraPipelineConfigSettings
                {
                    IsOverridden = true
                };

                Assert.That(configA.SettingsOverride.IsOverridden, Is.True,
                    "Config A should have overridden settings.");
                Assert.That(configB.SettingsOverride.IsOverridden, Is.False,
                    "Config B should retain default settings, unaffected by A.");
            }
            finally
            {
                Object.DestroyImmediate(configA);
                Object.DestroyImmediate(configB);
            }
        }

        [Test]
        public void Config_IsScriptableObject()
        {
            var config = ScriptableObject.CreateInstance<CameraPipelineConfig>();

            try
            {
                Assert.That(config, Is.InstanceOf<ScriptableObject>(),
                    "CameraPipelineConfig must be a ScriptableObject.");
                Assert.That(config, Is.InstanceOf<CameraPipelineConfig>(),
                    "CameraPipelineConfig must be of its own type.");
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}
