// <copyright file="CameraContextTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="CameraContext"/>.
    /// Verifies construction, property storage, and resource disposal.
    /// </summary>
    public sealed class CameraContextTests
    {
        /// <summary>
        /// Creates a test <see cref="Camera"/> attached to a new <see cref="GameObject"/>.
        /// The caller is responsible for destroying the GameObject.
        /// </summary>
        private static Camera CreateTestCamera()
        {
            var go = new GameObject("TestCamera");
            return go.AddComponent<Camera>();
        }

        /// <summary>
        /// Verifies that the constructor creates a valid context with the expected camera
        /// and a pooled command buffer.
        /// </summary>
        [Test]
        public void Context_CreatedForCamera()
        {
            var camera = CreateTestCamera();
            try
            {
                var context = new CameraContext(camera, new ScriptableRenderContext());

                Assert.That(context, Is.Not.Null);
                Assert.That(context.Camera, Is.SameAs(camera));
                Assert.That(context.Cmd, Is.Not.Null,
                    "Constructor should allocate a command buffer from the pool.");
                Assert.That(context.Cmd.name, Is.EqualTo("CameraContext"),
                    "Command buffer should use the expected pool name.");

                context.Dispose();
            }
            finally
            {
                Object.DestroyImmediate(camera.gameObject);
            }
        }

        /// <summary>
        /// Verifies that <see cref="CameraContext.CullingResults"/> can be stored and
        /// retrieved without exceptions.
        /// </summary>
        [Test]
        public void Context_StoresCullingResults()
        {
            var camera = CreateTestCamera();
            try
            {
                var context = new CameraContext(camera, new ScriptableRenderContext());

                // CullingResults is a value type — assign and verify no exception.
                context.CullingResults = new CullingResults();

                Assert.DoesNotThrow(() =>
                {
                    var _ = context.CullingResults;
                }, "Reading CullingResults after assignment should succeed.");

                context.Dispose();
            }
            finally
            {
                Object.DestroyImmediate(camera.gameObject);
            }
        }

        /// <summary>
        /// Verifies that <see cref="CameraContext.Dispose"/> releases the command buffer
        /// back to the pool and nulls the reference. Also verifies idempotency.
        /// </summary>
        [Test]
        public void Context_DisposeReleasesCmd()
        {
            var camera = CreateTestCamera();
            try
            {
                var context = new CameraContext(camera, new ScriptableRenderContext());

                Assert.That(context.Cmd, Is.Not.Null,
                    "Cmd should be allocated before Dispose.");

                context.Dispose();

                Assert.That(context.Cmd, Is.Null,
                    "Cmd should be null after Dispose releases it back to the pool.");

                // Idempotence: calling Dispose again should not throw.
                Assert.DoesNotThrow(() => context.Dispose(),
                    "Dispose should be safe to call multiple times.");
            }
            finally
            {
                Object.DestroyImmediate(camera.gameObject);
            }
        }
    }
}
