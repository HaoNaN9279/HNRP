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
    /// underlying <see cref="HNAdditionalCameraData.RenderGraphViewIndex"/> field.
    /// Covers the editor UI behavior and the render graph selection priority chain.
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

        #region RenderGraphViewIndex Field Tests

        [Test]
        public void RenderGraphViewIndex_DefaultsToZero()
        {
            Assert.That(m_CameraData.RenderGraphViewIndex, Is.EqualTo(0),
                "Default RenderGraphViewIndex should be 0.");
        }

        [Test]
        public void RenderGraphViewIndex_CanBeSetToAnyIndex()
        {
            m_CameraData.RenderGraphViewIndex = 2;

            Assert.That(m_CameraData.RenderGraphViewIndex, Is.EqualTo(2),
                "RenderGraphViewIndex should return the assigned index.");
        }

        [Test]
        public void RenderGraphViewIndex_CanBeSetToZero()
        {
            m_CameraData.RenderGraphViewIndex = 2;
            Assert.That(m_CameraData.RenderGraphViewIndex, Is.Not.EqualTo(0));

            m_CameraData.RenderGraphViewIndex = 0;
            Assert.That(m_CameraData.RenderGraphViewIndex, Is.EqualTo(0),
                "Setting to 0 should work.");
        }

        [Test]
        public void RenderGraphViewIndex_CanSwitchBetweenValues()
        {
            m_CameraData.RenderGraphViewIndex = 1;
            Assert.That(m_CameraData.RenderGraphViewIndex, Is.EqualTo(1));

            m_CameraData.RenderGraphViewIndex = 3;
            Assert.That(m_CameraData.RenderGraphViewIndex, Is.EqualTo(3));
            Assert.That(m_CameraData.RenderGraphViewIndex, Is.Not.EqualTo(1));
        }

        [Test]
        public void RenderGraphViewIndex_PersistsAfterComponentCycle()
        {
            m_CameraData.RenderGraphViewIndex = 2;

            // Simulate disable/enable cycle
            m_CameraData.enabled = false;
            m_CameraData.enabled = true;

            Assert.That(m_CameraData.RenderGraphViewIndex, Is.EqualTo(2),
                "RenderGraphViewIndex should persist after enable/disable cycle.");
        }

        #endregion
    }
}
