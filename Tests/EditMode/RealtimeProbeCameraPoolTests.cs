// <copyright file="RealtimeProbeCameraPoolTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using NUnit.Framework;
using UnityEngine;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for <see cref="RealtimeProbeCameraPool"/>: camera reuse, per-frame
    /// rendered-face tracking, and lifecycle.
    /// </summary>
    public sealed class RealtimeProbeCameraPoolTests
    {
        #region GetCamera

        /// <summary>
        /// Verifies that <see cref="RealtimeProbeCameraPool.GetCamera"/> returns a
        /// usable <see cref="Camera"/> instance.
        /// </summary>
        [Test]
        public void GetCamera_ReturnsCameraInstance()
        {
            using var pool = new RealtimeProbeCameraPool();

            Camera cam = pool.GetCamera();

            try
            {
                Assert.That(cam, Is.Not.Null, "GetCamera should return a camera.");
            }
            finally
            {
                
            }
        }

        /// <summary>
        /// Verifies that pooled cameras are of type <see cref="CameraType.Reflection"/>.
        /// </summary>
        [Test]
        public void GetCamera_ReturnsReflectionCameraType()
        {
            using var pool = new RealtimeProbeCameraPool();

            Camera cam = pool.GetCamera();

            try
            {
                Assert.That(cam.cameraType, Is.EqualTo(CameraType.Reflection),
                    "Pooled probe cameras must be Reflection type.");
            }
            finally
            {
                
            }
        }

        /// <summary>
        /// Verifies that pooled cameras are inactive (never render automatically).
        /// </summary>
        [Test]
        public void GetCamera_ReturnsDisabledCamera()
        {
            using var pool = new RealtimeProbeCameraPool();

            Camera cam = pool.GetCamera();

            try
            {
                Assert.That(cam.enabled, Is.False,
                    "Pooled probe cameras should be disabled; the render graph drives rendering.");
            }
            finally
            {
                
            }
        }

        #endregion

        #region Rendered-face tracking

        /// <summary>
        /// Verifies <see cref="RealtimeProbeCameraPool.IsFaceRendered"/> /
        /// <see cref="RealtimeProbeCameraPool.MarkFaceRendered"/> round-trip.
        /// </summary>
        [Test]
        public void IsFaceRendered_MarkFaceRendered_RoundTrip()
        {
            using var pool = new RealtimeProbeCameraPool();

            const int probeId = 42;
            const int face = 3;

            Assert.That(pool.IsFaceRendered(probeId, face), Is.False,
                "A face should not be marked rendered before MarkFaceRendered.");

            pool.MarkFaceRendered(probeId, face);

            Assert.That(pool.IsFaceRendered(probeId, face), Is.True,
                "A face should be marked rendered after MarkFaceRendered.");
        }

        /// <summary>
        /// Verifies that different faces of the same probe are tracked independently.
        /// </summary>
        [Test]
        public void MarkFaceRendered_SameProbeDifferentFaces_Independent()
        {
            using var pool = new RealtimeProbeCameraPool();

            const int probeId = 7;

            pool.MarkFaceRendered(probeId, 0);

            Assert.That(pool.IsFaceRendered(probeId, 0), Is.True);
            Assert.That(pool.IsFaceRendered(probeId, 1), Is.False,
                "Rendering one face must not mark another face of the same probe.");
        }

        /// <summary>
        /// Verifies that <see cref="RealtimeProbeCameraPool.BeginFrame"/> clears the
        /// rendered-face set so faces render again next frame.
        /// </summary>
        [Test]
        public void BeginFrame_ClearsRenderedFaces()
        {
            using var pool = new RealtimeProbeCameraPool();

            const int probeId = 5;
            const int face = 2;

            pool.MarkFaceRendered(probeId, face);
            Assert.That(pool.IsFaceRendered(probeId, face), Is.True);

            pool.BeginFrame();

            Assert.That(pool.IsFaceRendered(probeId, face), Is.False,
                "BeginFrame should clear the per-frame rendered set.");
        }

        #endregion

        #region Dispose

        /// <summary>
        /// Verifies that <see cref="RealtimeProbeCameraPool.Dispose"/> destroys all
        /// pooled camera game objects.
        /// </summary>
        [Test]
        public void Dispose_DestroysCameraGameObjects()
        {
            var pool = new RealtimeProbeCameraPool();

            Camera cam = pool.GetCamera();

            pool.Dispose();

            Assert.That(cam == null, Is.True,
                "Dispose should destroy pooled camera game objects.");
        }

        #endregion
    }
}
