// <copyright file="RealtimeProbeRenderUtils.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    /// <summary>
    /// Pure helper logic for realtime reflection probe rendering: time-slicing
    /// face scheduling, realtime-mode filtering, and reflection probe lookup from
    /// <see cref="VisibleReflectionProbe"/> culling data.
    /// </summary>
    public static class RealtimeProbeRenderUtils
    {
        /// <summary>
        /// The six cubemap face indices (<c>0..5</c>).
        /// </summary>
        public static readonly int[] AllFaces = { 0, 1, 2, 3, 4, 5 };

        private static readonly int[] s_EmptyFaces = Array.Empty<int>();

        /// <summary>
        /// The <c>m_InstanceId</c> backing field of <see cref="VisibleReflectionProbe"/>.
        /// Unity 2022.3 exposes no public members on this struct; the instance id is
        /// read once via reflection and cached.
        /// </summary>
        private static readonly FieldInfo s_InstanceIdField =
            typeof(VisibleReflectionProbe).GetField(
                "m_InstanceId",
                BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Computes which cubemap faces should be rendered this frame for a probe
        /// according to its time-slicing mode.
        /// </summary>
        /// <param name="mode">The probe's time-slicing mode.</param>
        /// <param name="probeInstanceId">The probe's instance id (phase offset).</param>
        /// <param name="frameCount">The current frame count.</param>
        /// <param name="faceProgress">The current face progress for
        /// <see cref="ReflectionProbeTimeSlicingMode.IndividualFaces"/>.</param>
        /// <returns>Indices of the faces to render this frame (empty when none).</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item><see cref="ReflectionProbeTimeSlicingMode.AllFacesAtOnce"/> —
        /// all six faces once every six frames; the phase is offset by the probe's
        /// instance id so different probes refresh on different frames.</item>
        /// <item><see cref="ReflectionProbeTimeSlicingMode.IndividualFaces"/> —
        /// exactly one face per frame, rotating through 0..5.</item>
        /// <item><see cref="ReflectionProbeTimeSlicingMode.NoTimeSlicing"/> —
        /// all six faces every frame.</item>
        /// </list>
        /// </remarks>
        public static int[] GetFacesToRender(
            ReflectionProbeTimeSlicingMode mode,
            int probeInstanceId,
            int frameCount,
            int faceProgress)
        {
            switch (mode)
            {
                case ReflectionProbeTimeSlicingMode.IndividualFaces:
                    return new[] { Mathf.Abs(faceProgress) % 6 };

                case ReflectionProbeTimeSlicingMode.NoTimeSlicing:
                    return AllFaces;

                case ReflectionProbeTimeSlicingMode.AllFacesAtOnce:
                    int phase = Mathf.Abs(probeInstanceId) % 6;
                    if ((frameCount + phase) % 6 == 0)
                    {
                        return AllFaces;
                    }

                    return s_EmptyFaces;

                default:
                    return s_EmptyFaces;
            }
        }

        /// <summary>
        /// Advances the individual-face progress counter after a face was rendered.
        /// </summary>
        /// <param name="faceProgress">The current progress counter.</param>
        /// <returns>The next progress counter value.</returns>
        public static int AdvanceIndividualFace(int faceProgress)
        {
            return faceProgress + 1;
        }

        /// <summary>
        /// Returns whether the given probe is a realtime probe
        /// (<see cref="ReflectionProbeMode.Realtime"/>).
        /// </summary>
        /// <param name="probe">The reflection probe to inspect.</param>
        /// <returns><c>true</c> if the probe is set to realtime mode.</returns>
        public static bool IsRealtimeProbe(ReflectionProbe probe)
        {
            return probe != null && probe.mode == ReflectionProbeMode.Realtime;
        }

        /// <summary>
        /// Reads the probe instance id from a <see cref="VisibleReflectionProbe"/>
        /// culling entry. Unity 2022.3 exposes no public field on this struct, so the
        /// private <c>m_InstanceId</c> field is read via cached reflection.
        /// </summary>
        /// <param name="visibleProbe">The visible reflection probe entry.</param>
        /// <returns>The reflection probe instance id, or <c>0</c> when unavailable.</returns>
        public static int GetProbeInstanceId(in VisibleReflectionProbe visibleProbe)
        {
            if (s_InstanceIdField == null)
            {
                return 0;
            }

            return (int)s_InstanceIdField.GetValue(visibleProbe);
        }

        /// <summary>
        /// Gets the <see cref="ReflectionProbe"/> component from a
        /// <see cref="VisibleReflectionProbe"/> culling entry.
        /// </summary>
        /// <param name="visibleProbe">The visible reflection probe entry.</param>
        /// <returns>The probe component, or <c>null</c> when not resolvable.</returns>
        public static ReflectionProbe GetReflectionProbe(in VisibleReflectionProbe visibleProbe)
        {
            int instanceId = GetProbeInstanceId(visibleProbe);
            if (instanceId == 0)
            {
                return null;
            }

            return UnityEngine.Resources.InstanceIDToObject(instanceId) as ReflectionProbe;
        }

        /// <summary>
        /// Gets the world-space rotation for a cubemap face index
        /// (<c>0=+X, 1=-X, 2=+Y, 3=-Y, 4=+Z, 5=-Z</c>) using the OpenGL-style
        /// cubemap face convention used by Unity.
        /// </summary>
        /// <param name="face">The cubemap face index in <c>0..5</c>.</param>
        /// <returns>The camera rotation for that face.</returns>
        public static Quaternion GetFaceRotation(int face)
        {
            int index = Mathf.Clamp(face, 0, 5);
            return s_FaceRotations[index];
        }

        private static readonly Quaternion[] s_FaceRotations =
        {
            Quaternion.LookRotation(Vector3.right, Vector3.down),     // +X
            Quaternion.LookRotation(Vector3.left, Vector3.down),      // -X
            Quaternion.LookRotation(Vector3.up, Vector3.forward),     // +Y
            Quaternion.LookRotation(Vector3.down, Vector3.back),      // -Y
            Quaternion.LookRotation(Vector3.forward, Vector3.down),   // +Z
            Quaternion.LookRotation(Vector3.back, Vector3.down),      // -Z
        };
    }
}
