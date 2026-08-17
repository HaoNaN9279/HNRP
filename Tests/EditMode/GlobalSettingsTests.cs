// <copyright file="GlobalSettingsTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using NUnit.Framework;
using UnityEngine;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for <see cref="HNRenderPipelineGlobalSettings"/> —
    /// specifically the <see cref="HNRenderPipelineGlobalSettings.CameraPipelineConfigs"/> list.
    /// </summary>
    public class GlobalSettingsTests
    {
        [Test]
        public void CameraPipelineConfigs_StartsEmpty()
        {
            var settings = ScriptableObject.CreateInstance<HNRenderPipelineGlobalSettings>();

            try
            {
                Assert.That(settings.CameraPipelineConfigs, Is.Not.Null,
                    "CameraPipelineConfigs list should not be null.");
                Assert.That(settings.CameraPipelineConfigs.Count, Is.EqualTo(0),
                    "New settings should have an empty configs list.");
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void CameraPipelineConfigs_CanAddConfig()
        {
            var settings = ScriptableObject.CreateInstance<HNRenderPipelineGlobalSettings>();
            var config = ScriptableObject.CreateInstance<CameraPipelineConfig>();

            try
            {
                settings.CameraPipelineConfigs.Add(config);

                Assert.That(settings.CameraPipelineConfigs.Count, Is.EqualTo(1),
                    "List should contain one config after adding.");
                Assert.That(settings.CameraPipelineConfigs[0], Is.SameAs(config),
                    "List should contain the added config.");
            }
            finally
            {
                Object.DestroyImmediate(settings);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void CameraPipelineConfigs_CanAddMultipleConfigs()
        {
            var settings = ScriptableObject.CreateInstance<HNRenderPipelineGlobalSettings>();
            var configA = ScriptableObject.CreateInstance<CameraPipelineConfig>();
            var configB = ScriptableObject.CreateInstance<CameraPipelineConfig>();
            var configC = ScriptableObject.CreateInstance<CameraPipelineConfig>();

            try
            {
                settings.CameraPipelineConfigs.Add(configA);
                settings.CameraPipelineConfigs.Add(configB);
                settings.CameraPipelineConfigs.Add(configC);

                Assert.That(settings.CameraPipelineConfigs.Count, Is.EqualTo(3),
                    "List should contain three configs.");
                Assert.That(settings.CameraPipelineConfigs[0], Is.SameAs(configA));
                Assert.That(settings.CameraPipelineConfigs[1], Is.SameAs(configB));
                Assert.That(settings.CameraPipelineConfigs[2], Is.SameAs(configC));
            }
            finally
            {
                Object.DestroyImmediate(settings);
                Object.DestroyImmediate(configA);
                Object.DestroyImmediate(configB);
                Object.DestroyImmediate(configC);
            }
        }

        [Test]
        public void CameraPipelineConfigs_CanRemoveConfig()
        {
            var settings = ScriptableObject.CreateInstance<HNRenderPipelineGlobalSettings>();
            var config = ScriptableObject.CreateInstance<CameraPipelineConfig>();

            try
            {
                settings.CameraPipelineConfigs.Add(config);
                Assert.That(settings.CameraPipelineConfigs.Count, Is.EqualTo(1));

                settings.CameraPipelineConfigs.Remove(config);

                Assert.That(settings.CameraPipelineConfigs.Count, Is.EqualTo(0),
                    "List should be empty after removing the config.");
            }
            finally
            {
                Object.DestroyImmediate(settings);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void CameraPipelineConfigs_CanRemoveByIndex()
        {
            var settings = ScriptableObject.CreateInstance<HNRenderPipelineGlobalSettings>();
            var configA = ScriptableObject.CreateInstance<CameraPipelineConfig>();
            var configB = ScriptableObject.CreateInstance<CameraPipelineConfig>();

            try
            {
                settings.CameraPipelineConfigs.Add(configA);
                settings.CameraPipelineConfigs.Add(configB);
                Assert.That(settings.CameraPipelineConfigs.Count, Is.EqualTo(2));

                settings.CameraPipelineConfigs.RemoveAt(0);

                Assert.That(settings.CameraPipelineConfigs.Count, Is.EqualTo(1),
                    "List should have one config after RemoveAt.");
                Assert.That(settings.CameraPipelineConfigs[0], Is.SameAs(configB),
                    "Remaining config should be the second one.");
            }
            finally
            {
                Object.DestroyImmediate(settings);
                Object.DestroyImmediate(configA);
                Object.DestroyImmediate(configB);
            }
        }

        [Test]
        public void CameraPipelineConfigs_CanClear()
        {
            var settings = ScriptableObject.CreateInstance<HNRenderPipelineGlobalSettings>();
            var config = ScriptableObject.CreateInstance<CameraPipelineConfig>();

            try
            {
                settings.CameraPipelineConfigs.Add(config);
                Assert.That(settings.CameraPipelineConfigs.Count, Is.EqualTo(1));

                settings.CameraPipelineConfigs.Clear();

                Assert.That(settings.CameraPipelineConfigs.Count, Is.EqualTo(0),
                    "List should be empty after Clear.");
            }
            finally
            {
                Object.DestroyImmediate(settings);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void CameraPipelineConfigs_AllowsNullEntries()
        {
            var settings = ScriptableObject.CreateInstance<HNRenderPipelineGlobalSettings>();

            try
            {
                settings.CameraPipelineConfigs.Add(null);

                Assert.That(settings.CameraPipelineConfigs.Count, Is.EqualTo(1),
                    "List should allow null entries.");
                Assert.That(settings.CameraPipelineConfigs[0], Is.Null,
                    "Entry should be null.");
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void CameraPipelineConfigs_IsIndependentPerSettings()
        {
            var settingsA = ScriptableObject.CreateInstance<HNRenderPipelineGlobalSettings>();
            var settingsB = ScriptableObject.CreateInstance<HNRenderPipelineGlobalSettings>();
            var config = ScriptableObject.CreateInstance<CameraPipelineConfig>();

            try
            {
                settingsA.CameraPipelineConfigs.Add(config);

                Assert.That(settingsA.CameraPipelineConfigs.Count, Is.EqualTo(1),
                    "Settings A should have one config.");
                Assert.That(settingsB.CameraPipelineConfigs.Count, Is.EqualTo(0),
                    "Settings B should be unaffected.");
            }
            finally
            {
                Object.DestroyImmediate(settingsA);
                Object.DestroyImmediate(settingsB);
                Object.DestroyImmediate(config);
            }
        }
    }
}
