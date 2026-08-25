using NUnit.Framework;
using UnityEngine;
using System;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for <see cref="HNAdditionalCameraData"/> render graph view index selection.
    /// The selection chain uses the view index to select from the pipeline asset's view blocks.
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
        public void RenderGraphViewIndex_DefaultsToZero()
        {
            Assert.That(m_CameraData.RenderGraphViewIndex, Is.EqualTo(0));
        }

        [Test]
        public void RenderGraphViewIndex_CanBeSetAndRetrieved()
        {
            m_CameraData.RenderGraphViewIndex = 2;
            Assert.That(m_CameraData.RenderGraphViewIndex, Is.EqualTo(2));
        }

        [Test]
        public void RenderGraphViewIndex_CanBeResetToZero()
        {
            m_CameraData.RenderGraphViewIndex = 2;
            Assert.That(m_CameraData.RenderGraphViewIndex, Is.Not.EqualTo(0));

            m_CameraData.RenderGraphViewIndex = 0;
            Assert.That(m_CameraData.RenderGraphViewIndex, Is.EqualTo(0));
        }
    }
}
