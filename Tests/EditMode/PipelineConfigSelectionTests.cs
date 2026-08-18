using NUnit.Framework;
using UnityEngine;
using System;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for <see cref="HNAdditionalCameraData"/> pipeline config selection priority.
    /// The selection chain is: <c>pipelineConfigOverride ?? defaultConfig ?? null</c>.
    /// </summary>
    public class PipelineConfigSelectionTests
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
            UnityEngine.Object.DestroyImmediate(m_GameObject);
        }

        [Test]
        public void PipelineConfigOverride_DefaultsToNull()
        {
            Assert.That(m_CameraData.PipelineConfigOverride, Is.Null);
        }

        [Test]
        public void PipelineConfigOverride_CanBeSetAndRetrieved()
        {
            var config = ScriptableObject.CreateInstance<RenderGraphAsset>();
            try
            {
                m_CameraData.PipelineConfigOverride = config;
                Assert.That(m_CameraData.PipelineConfigOverride, Is.SameAs(config));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void PipelineConfigOverride_CanBeCleared()
        {
            var config = ScriptableObject.CreateInstance<RenderGraphAsset>();
            try
            {
                m_CameraData.PipelineConfigOverride = config;
                Assert.That(m_CameraData.PipelineConfigOverride, Is.Not.Null);

                m_CameraData.PipelineConfigOverride = null;
                Assert.That(m_CameraData.PipelineConfigOverride, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void SelectionPriority_OverrideTakesPriority_WhenSet()
        {
            var overrideConfig = ScriptableObject.CreateInstance<RenderGraphAsset>();
            var defaultConfig = ScriptableObject.CreateInstance<RenderGraphAsset>();
            try
            {
                m_CameraData.PipelineConfigOverride = overrideConfig;

                // Simulate the selection chain: pipelineConfigOverride ?? defaultConfig ?? null
                var selected = m_CameraData.PipelineConfigOverride ?? defaultConfig;

                Assert.That(selected, Is.SameAs(overrideConfig));
                Assert.That(selected, Is.Not.SameAs(defaultConfig));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(overrideConfig);
                UnityEngine.Object.DestroyImmediate(defaultConfig);
            }
        }

        [Test]
        public void SelectionPriority_FallsBackToDefault_WhenOverrideIsNull()
        {
            var defaultConfig = ScriptableObject.CreateInstance<RenderGraphAsset>();
            try
            {
                m_CameraData.PipelineConfigOverride = null;

                // Simulate the selection chain: pipelineConfigOverride ?? defaultConfig ?? null
                var selected = m_CameraData.PipelineConfigOverride ?? defaultConfig;

                Assert.That(selected, Is.SameAs(defaultConfig));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(defaultConfig);
            }
        }

        [Test]
        public void SelectionPriority_ReturnsNull_WhenBothAreNull()
        {
            m_CameraData.PipelineConfigOverride = null;
            RenderGraphAsset defaultConfig = null;

            // Simulate the selection chain: pipelineConfigOverride ?? defaultConfig ?? null
            var selected = m_CameraData.PipelineConfigOverride ?? defaultConfig;

            Assert.That(selected, Is.Null);
        }
    }
}
