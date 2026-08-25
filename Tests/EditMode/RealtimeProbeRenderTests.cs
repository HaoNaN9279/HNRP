// <copyright file="RealtimeProbeRenderTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for realtime reflection probe rendering logic:
    /// <see cref="RealtimeProbeRenderUtils"/> (face scheduling, mode filtering) and
    /// <see cref="RealtimeProbeRenderer"/> (collection, refresh mode gating, dedup).
    /// </summary>
    public sealed class RealtimeProbeRenderTests
    {
        #region GetFacesToRender — time slicing strategies

        /// <summary>
        /// Verifies that <see cref="RealtimeProbeRenderUtils.GetFacesToRender"/>
        /// returns all six faces on the probe's phase frame for
        /// <see cref="ReflectionProbeTimeSlicingMode.AllFacesAtOnce"/>.
        /// </summary>
        [Test]
        public void AllFacesAtOnce_ReturnsAllFaces_OnPhaseFrame()
        {
            const int probeId = 3;
            const int frameCount = 3; // (3 + 3) % 6 == 0

            int[] faces = RealtimeProbeRenderUtils.GetFacesToRender(
                ReflectionProbeTimeSlicingMode.AllFacesAtOnce, probeId, frameCount, 0);

            Assert.That(faces, Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5 }),
                "AllFacesAtOnce should render all six faces on the phase frame.");
        }

        /// <summary>
        /// Verifies that <see cref="ReflectionProbeTimeSlicingMode.AllFacesAtOnce"/>
        /// returns no faces on non-phase frames.
        /// </summary>
        [Test]
        public void AllFacesAtOnce_ReturnsEmpty_OnOffPhaseFrames()
        {
            const int probeId = 3;
            const int frameCount = 4; // (4 + 3) % 6 != 0

            int[] faces = RealtimeProbeRenderUtils.GetFacesToRender(
                ReflectionProbeTimeSlicingMode.AllFacesAtOnce, probeId, frameCount, 0);

            Assert.That(faces, Is.Empty,
                "AllFacesAtOnce should render nothing on off-phase frames.");
        }

        /// <summary>
        /// Verifies that <see cref="ReflectionProbeTimeSlicingMode.IndividualFaces"/>
        /// renders exactly one face and rotates across calls.
        /// </summary>
        [Test]
        public void IndividualFaces_ReturnsOneFace_RotatesEachCall()
        {
            int[] first = RealtimeProbeRenderUtils.GetFacesToRender(
                ReflectionProbeTimeSlicingMode.IndividualFaces, 1, 0, 0);
            int[] second = RealtimeProbeRenderUtils.GetFacesToRender(
                ReflectionProbeTimeSlicingMode.IndividualFaces, 1, 0, 1);
            int[] wrapped = RealtimeProbeRenderUtils.GetFacesToRender(
                ReflectionProbeTimeSlicingMode.IndividualFaces, 1, 0, 6);

            Assert.That(first, Is.EqualTo(new[] { 0 }), "First call renders face 0.");
            Assert.That(second, Is.EqualTo(new[] { 1 }), "Second call renders face 1.");
            Assert.That(wrapped, Is.EqualTo(new[] { 0 }), "Face index should wrap after six faces.");
        }

        /// <summary>
        /// Verifies that <see cref="ReflectionProbeTimeSlicingMode.NoTimeSlicing"/>
        /// returns all six faces every frame.
        /// </summary>
        [Test]
        public void NoTimeSlicing_ReturnsAllFaces_EveryFrame()
        {
            int[] faces = RealtimeProbeRenderUtils.GetFacesToRender(
                ReflectionProbeTimeSlicingMode.NoTimeSlicing, 1, 123, 0);

            Assert.That(faces, Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5 }),
                "NoTimeSlicing should render all six faces every frame.");
        }

        #endregion

        #region IsRealtimeProbe

        /// <summary>
        /// Verifies <see cref="RealtimeProbeRenderUtils.IsRealtimeProbe"/> returns
        /// <c>true</c> only for probes in <see cref="ReflectionProbeMode.Realtime"/>.
        /// </summary>
        [Test]
        public void IsRealtimeProbe_TrueForRealtimeMode()
        {
            var go = new GameObject("RealtimeProbe");
            var probe = go.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;

            try
            {
                Assert.That(RealtimeProbeRenderUtils.IsRealtimeProbe(probe), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Verifies that baked/custom probes are not treated as realtime.
        /// </summary>
        [Test]
        public void IsRealtimeProbe_FalseForNonRealtimeModes()
        {
            var go = new GameObject("BakedProbe");
            var probe = go.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;

            try
            {
                Assert.That(RealtimeProbeRenderUtils.IsRealtimeProbe(probe), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        #endregion

        #region GetProbeInstanceId

        /// <summary>
        /// Verifies that a default <see cref="VisibleReflectionProbe"/> (no instance)
        /// yields instance id 0.
        /// </summary>
        [Test]
        public void GetProbeInstanceId_DefaultProbe_ReturnsZero()
        {
            var probe = default(VisibleReflectionProbe);

            Assert.That(RealtimeProbeRenderUtils.GetProbeInstanceId(probe), Is.EqualTo(0),
                "A default VisibleReflectionProbe should have instance id 0.");
        }

        #endregion

        #region CollectRealtimeProbes

        /// <summary>
        /// Verifies that collecting an invalid instance id is a no-op.
        /// </summary>
        [Test]
        public void CollectRealtimeProbe_ZeroInstanceId_Ignored()
        {
            using var pool = new RealtimeProbeCameraPool();
            using var renderer = new RealtimeProbeRenderer(pool);

            renderer.CollectRealtimeProbe(0);

            Assert.That(renderer.PendingProbeCount, Is.Zero,
                "Instance id 0 must be ignored during collection.");
        }

        /// <summary>
        /// Verifies that a realtime probe is collected by instance id.
        /// </summary>
        [Test]
        public void CollectRealtimeProbe_RealtimeProbe_Collected()
        {
            using var pool = new RealtimeProbeCameraPool();
            using var renderer = new RealtimeProbeRenderer(pool);

            var go = new GameObject("Probe");
            var probe = go.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;

            try
            {
                renderer.CollectRealtimeProbe(probe.GetInstanceID());

                Assert.That(renderer.PendingProbeCount, Is.EqualTo(1),
                    "A realtime probe should be collected.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Verifies that a baked probe is not collected.
        /// </summary>
        [Test]
        public void CollectRealtimeProbe_BakedProbe_Ignored()
        {
            using var pool = new RealtimeProbeCameraPool();
            using var renderer = new RealtimeProbeRenderer(pool);

            var go = new GameObject("Probe");
            var probe = go.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;

            try
            {
                renderer.CollectRealtimeProbe(probe.GetInstanceID());

                Assert.That(renderer.PendingProbeCount, Is.Zero,
                    "A baked probe must not be collected.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Verifies that the same probe instance id is collected only once.
        /// </summary>
        [Test]
        public void CollectRealtimeProbe_DuplicateInstanceId_Deduplicated()
        {
            using var pool = new RealtimeProbeCameraPool();
            using var renderer = new RealtimeProbeRenderer(pool);

            var go = new GameObject("Probe");
            var probe = go.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;

            try
            {
                renderer.CollectRealtimeProbe(probe.GetInstanceID());
                renderer.CollectRealtimeProbe(probe.GetInstanceID());

                Assert.That(renderer.PendingProbeCount, Is.EqualTo(1),
                    "Duplicate collection must be deduplicated by instance id.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Verifies that <see cref="RealtimeProbeRenderer.BeginFrame"/> clears
        /// collected requests.
        /// </summary>
        [Test]
        public void BeginFrame_ClearsPendingRequests()
        {
            using var pool = new RealtimeProbeCameraPool();
            using var renderer = new RealtimeProbeRenderer(pool);

            var go = new GameObject("Probe");
            var probe = go.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;

            try
            {
                renderer.CollectRealtimeProbe(probe.GetInstanceID());
                Assert.That(renderer.PendingProbeCount, Is.EqualTo(1));

                renderer.BeginFrame();

                Assert.That(renderer.PendingProbeCount, Is.Zero,
                    "BeginFrame should clear collected requests.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        #endregion

        #region ShouldRenderThisFrame — refresh modes

        /// <summary>
        /// Verifies that <see cref="ReflectionProbeRefreshMode.EveryFrame"/> probes
        /// always render this frame.
        /// </summary>
        [Test]
        public void RefreshModeEveryFrame_RendersEveryFrame()
        {
            using var pool = new RealtimeProbeCameraPool();
            using var renderer = new RealtimeProbeRenderer(pool);

            var go = new GameObject("Probe");
            var probe = go.AddComponent<ReflectionProbe>();
            probe.refreshMode = ReflectionProbeRefreshMode.EveryFrame;

            try
            {
                Assert.That(renderer.ShouldRenderThisFrame(probe), Is.True);
                Assert.That(renderer.ShouldRenderThisFrame(probe), Is.True,
                    "EveryFrame probes should render every frame.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Verifies that <see cref="ReflectionProbeRefreshMode.OnAwake"/> probes
        /// render once, then are skipped until re-initialized.
        /// </summary>
        [Test]
        public void RefreshModeOnAwake_RendersOnce_ThenSkips()
        {
            using var pool = new RealtimeProbeCameraPool();
            using var renderer = new RealtimeProbeRenderer(pool);

            var go = new GameObject("Probe");
            var probe = go.AddComponent<ReflectionProbe>();
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;

            try
            {
                Assert.That(renderer.ShouldRenderThisFrame(probe), Is.True,
                    "OnAwake probes should render on first sight.");

                renderer.MarkInitialized(probe);

                Assert.That(renderer.ShouldRenderThisFrame(probe), Is.False,
                    "OnAwake probes should be skipped after first render.");
                Assert.That(renderer.IsInitialized(probe), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Verifies that <see cref="ReflectionProbeRefreshMode.ViaScripting"/> probes
        /// are never auto-rendered.
        /// </summary>
        [Test]
        public void RefreshModeViaScripting_NeverRenders()
        {
            using var pool = new RealtimeProbeCameraPool();
            using var renderer = new RealtimeProbeRenderer(pool);

            var go = new GameObject("Probe");
            var probe = go.AddComponent<ReflectionProbe>();
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;

            try
            {
                Assert.That(renderer.ShouldRenderThisFrame(probe), Is.False,
                    "ViaScripting probes must never be auto-rendered.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        #endregion
    }
}
