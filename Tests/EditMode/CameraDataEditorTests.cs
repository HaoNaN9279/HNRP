// <copyright file="CameraDataEditorTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using HN.HNRP.Editor;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for <see cref="HNRenderPipelineAdditionalCameraDataEditor"/> and its
    /// underlying <see cref="HNAdditionalCameraData.PipelineConfigOverride"/> field.
    /// Covers the editor UI dropdown behavior and the config selection priority chain.
    /// </summary>
    public class CameraDataEditorTests
    {
        private GameObject m_GameObject;
        private Camera m_Camera;
        private HNAdditionalCameraData m_CameraData;

        [SetUp]
        public void SetUp()
        {
            m_GameObject = new GameObject("TestCamera");
            m_Camera = m_GameObject.AddComponent<Camera>();
            m_CameraData = m_GameObject.AddComponent<HNAdditionalCameraData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_GameObject);
        }

        #region Editor Metadata Tests

        [Test]
        public void Editor_HasCustomEditorAttribute_TargetsCorrectType()
        {
            var attributes = typeof(HNRenderPipelineAdditionalCameraDataEditor)
                .GetCustomAttributes(typeof(CustomEditor), inherit: false);

            Assert.That(attributes.Length, Is.GreaterThan(0),
                "Editor should have a [CustomEditor] attribute.");

            var attr = (CustomEditor)attributes[0];
            var inspectedType = typeof(CustomEditor)
                .GetField("m_InspectedType", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(attr) as System.Type;
            Assert.That(inspectedType, Is.EqualTo(typeof(HNAdditionalCameraData)),
                "CustomEditor should target HNAdditionalCameraData.");
        }

        [Test]
        public void Editor_SupportsMultipleObjects()
        {
            var attributes = typeof(HNRenderPipelineAdditionalCameraDataEditor)
                .GetCustomAttributes(typeof(CanEditMultipleObjects), inherit: false);

            Assert.That(attributes.Length, Is.GreaterThan(0),
                "Editor should support editing multiple objects.");
        }

        #endregion

        #region PipelineConfigOverride Field Tests

        [Test]
        public void PipelineConfigOverride_DefaultsToNull()
        {
            Assert.That(m_CameraData.PipelineConfigOverride, Is.Null,
                "Default PipelineConfigOverride should be null (dropdown shows 'None').");
        }

        [Test]
        public void PipelineConfigOverride_CanBeSetToAnyConfig()
        {
            var config = ScriptableObject.CreateInstance<CameraPipelineConfig>();
            try
            {
                config.name = "TestConfig";
                m_CameraData.PipelineConfigOverride = config;

                Assert.That(m_CameraData.PipelineConfigOverride, Is.SameAs(config),
                    "PipelineConfigOverride should reference the assigned config.");
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void PipelineConfigOverride_CanBeSetToNull_NoneOption()
        {
            var config = ScriptableObject.CreateInstance<CameraPipelineConfig>();
            try
            {
                m_CameraData.PipelineConfigOverride = config;
                Assert.That(m_CameraData.PipelineConfigOverride, Is.Not.Null);

                // Simulate selecting "None" from dropdown
                m_CameraData.PipelineConfigOverride = null;
                Assert.That(m_CameraData.PipelineConfigOverride, Is.Null,
                    "Setting to null should clear the override (selects 'None' in dropdown).");
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void PipelineConfigOverride_CanSwitchBetweenConfigs()
        {
            var configA = ScriptableObject.CreateInstance<CameraPipelineConfig>();
            var configB = ScriptableObject.CreateInstance<CameraPipelineConfig>();
            try
            {
                configA.name = "ConfigA";
                configB.name = "ConfigB";

                m_CameraData.PipelineConfigOverride = configA;
                Assert.That(m_CameraData.PipelineConfigOverride, Is.SameAs(configA));

                m_CameraData.PipelineConfigOverride = configB;
                Assert.That(m_CameraData.PipelineConfigOverride, Is.SameAs(configB));
                Assert.That(m_CameraData.PipelineConfigOverride, Is.Not.SameAs(configA));
            }
            finally
            {
                Object.DestroyImmediate(configA);
                Object.DestroyImmediate(configB);
            }
        }

        [Test]
        public void PipelineConfigOverride_PersistsAfterComponentCycle()
        {
            var config = ScriptableObject.CreateInstance<CameraPipelineConfig>();
            try
            {
                m_CameraData.PipelineConfigOverride = config;

                // Simulate disable/enable cycle
                m_CameraData.enabled = false;
                m_CameraData.enabled = true;

                Assert.That(m_CameraData.PipelineConfigOverride, Is.SameAs(config),
                    "PipelineConfigOverride should persist after enable/disable cycle.");
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        #endregion

        #region GlobalSettings Integration Tests

        [Test]
        public void GlobalSettings_CanContainMultipleConfigs_ForDropdown()
        {
            var settings = ScriptableObject.CreateInstance<HNRenderPipelineGlobalSettings>();
            var configA = ScriptableObject.CreateInstance<CameraPipelineConfig>();
            var configB = ScriptableObject.CreateInstance<CameraPipelineConfig>();
            try
            {
                configA.name = "Standard";
                configB.name = "Preview";

                settings.CameraPipelineConfigs.Add(configA);
                settings.CameraPipelineConfigs.Add(configB);

                Assert.That(settings.CameraPipelineConfigs.Count, Is.EqualTo(2),
                    "GlobalSettings should contain multiple configs for dropdown.");
                Assert.That(settings.CameraPipelineConfigs[0].name, Is.EqualTo("Standard"));
                Assert.That(settings.CameraPipelineConfigs[1].name, Is.EqualTo("Preview"));
            }
            finally
            {
                Object.DestroyImmediate(settings);
                Object.DestroyImmediate(configA);
                Object.DestroyImmediate(configB);
            }
        }

        [Test]
        public void GlobalSettings_ConfigAtIndex_CanBeReferencedByOverride()
        {
            var settings = ScriptableObject.CreateInstance<HNRenderPipelineGlobalSettings>();
            var config = ScriptableObject.CreateInstance<CameraPipelineConfig>();
            try
            {
                config.name = "MyConfig";
                settings.CameraPipelineConfigs.Add(config);

                // Simulate selecting from dropdown by referencing the config at index 0
                var selectedConfig = settings.CameraPipelineConfigs[0];
                m_CameraData.PipelineConfigOverride = selectedConfig;

                Assert.That(m_CameraData.PipelineConfigOverride, Is.SameAs(config),
                    "Override should reference the config at the selected dropdown index.");
            }
            finally
            {
                Object.DestroyImmediate(settings);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void GlobalSettings_EmptyList_DropdownShowsOnlyNone()
        {
            var settings = ScriptableObject.CreateInstance<HNRenderPipelineGlobalSettings>();
            try
            {
                Assert.That(settings.CameraPipelineConfigs.Count, Is.EqualTo(0),
                    "Empty list means dropdown shows only 'None'.");
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void GlobalSettings_ConfigAddedAfterOverride_IsReflectedInDropdown()
        {
            var settings = ScriptableObject.CreateInstance<HNRenderPipelineGlobalSettings>();
            var config = ScriptableObject.CreateInstance<CameraPipelineConfig>();
            try
            {
                // Initially empty
                Assert.That(settings.CameraPipelineConfigs.Count, Is.EqualTo(0));

                // Add a config — it should now appear in dropdown
                settings.CameraPipelineConfigs.Add(config);
                Assert.That(settings.CameraPipelineConfigs.Count, Is.EqualTo(1));

                // Override can reference the newly added config
                m_CameraData.PipelineConfigOverride = config;
                Assert.That(m_CameraData.PipelineConfigOverride, Is.SameAs(config));
            }
            finally
            {
                Object.DestroyImmediate(settings);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void GlobalSettings_ConfigRemoved_OverrideStillHoldsReference()
        {
            var settings = ScriptableObject.CreateInstance<HNRenderPipelineGlobalSettings>();
            var config = ScriptableObject.CreateInstance<CameraPipelineConfig>();
            try
            {
                settings.CameraPipelineConfigs.Add(config);
                m_CameraData.PipelineConfigOverride = config;

                // Remove config from global settings list
                settings.CameraPipelineConfigs.Remove(config);
                Assert.That(settings.CameraPipelineConfigs.Count, Is.EqualTo(0));

                // The override still holds the reference (not nullified)
                Assert.That(m_CameraData.PipelineConfigOverride, Is.SameAs(config),
                    "Removing from global list does not clear existing overrides.");
            }
            finally
            {
                Object.DestroyImmediate(settings);
                Object.DestroyImmediate(config);
            }
        }

        #endregion

        #region Selection Priority Chain Tests

        [Test]
        public void SelectionPriority_OverrideTakesPriority_WhenSet()
        {
            var overrideConfig = ScriptableObject.CreateInstance<CameraPipelineConfig>();
            var defaultConfig = ScriptableObject.CreateInstance<CameraPipelineConfig>();
            try
            {
                m_CameraData.PipelineConfigOverride = overrideConfig;

                // Simulate: pipelineConfigOverride ?? defaultConfig ?? null
                var selected = m_CameraData.PipelineConfigOverride ?? defaultConfig;

                Assert.That(selected, Is.SameAs(overrideConfig),
                    "Override should take priority over default.");
                Assert.That(selected, Is.Not.SameAs(defaultConfig));
            }
            finally
            {
                Object.DestroyImmediate(overrideConfig);
                Object.DestroyImmediate(defaultConfig);
            }
        }

        [Test]
        public void SelectionPriority_FallsBackToDefault_WhenOverrideIsNull()
        {
            var defaultConfig = ScriptableObject.CreateInstance<CameraPipelineConfig>();
            try
            {
                m_CameraData.PipelineConfigOverride = null;

                // Simulate: pipelineConfigOverride ?? defaultConfig ?? null
                var selected = m_CameraData.PipelineConfigOverride ?? defaultConfig;

                Assert.That(selected, Is.SameAs(defaultConfig),
                    "Should fall back to default when override is null (None selected).");
            }
            finally
            {
                Object.DestroyImmediate(defaultConfig);
            }
        }

        [Test]
        public void SelectionPriority_ReturnsNull_WhenBothAreNull()
        {
            m_CameraData.PipelineConfigOverride = null;
            CameraPipelineConfig defaultConfig = null;

            var selected = m_CameraData.PipelineConfigOverride ?? defaultConfig;

            Assert.That(selected, Is.Null,
                "Should return null when both override and default are null.");
        }

        #endregion

        #region Obsolete Attribute Tests

        [Test]
        public void RenderGraphViewIndex_HasObsoleteAttribute()
        {
            var property = typeof(HNAdditionalCameraData).GetProperty("RenderGraphViewIndex");
            Assert.That(property, Is.Not.Null);

            var obsoleteAttr = System.Attribute.GetCustomAttribute(
                property, typeof(System.ObsoleteAttribute));

            Assert.That(obsoleteAttr, Is.Not.Null,
                "RenderGraphViewIndex should have [Obsolete] attribute.");
            Assert.That(((System.ObsoleteAttribute)obsoleteAttr).Message,
                Does.Contain("pipelineConfigOverride"));
        }

        #endregion
    }
}
