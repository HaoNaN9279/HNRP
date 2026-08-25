// <copyright file="RealtimeProbeCameraPool.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    /// <summary>
    /// Pool of <see cref="Camera"/> instances used to render realtime reflection
    /// probe cubemap faces. Cameras are reused across frames instead of being
    /// created per render, and the pool records which probe faces were already
    /// rendered this frame so overlapping cameras do not render a probe twice.
    /// Also manages <see cref="RTHandle"/> instances for probe cubemap faces so
    /// they are not re-allocated every frame.
    /// </summary>
    public sealed class RealtimeProbeCameraPool : IDisposable
    {
        /// <summary>
        /// Cameras currently idle and available for reuse.
        /// </summary>
        private readonly List<Camera> m_FreeCameras = new();

        /// <summary>
        /// Probe faces already rendered this frame. Keyed by
        /// <c>probeInstanceId * 6 + face</c>.
        /// </summary>
        private readonly HashSet<int> m_RenderedFaces = new();

        /// <summary>
        /// Cached RTHandles for probe cubemap faces, keyed by
        /// <c>probeInstanceId * 6 + face</c>. Handles persist across frames
        /// and are only released on <see cref="Dispose"/>.
        /// </summary>
        private readonly Dictionary<int, RTHandle> m_ProbeFaceHandles = new();

        /// <summary>
        /// Probe textures rendered this frame. Keyed by probe instance id.
        /// Cleared by <see cref="BeginFrame"/>.
        /// </summary>
        private readonly Dictionary<int, Texture> m_RenderedProbeTextures = new();

        /// <summary>
        /// Gets a camera from the pool, creating one when the pool is empty.
        /// The caller must return it via <see cref="ReturnCamera"/> after use.
        /// </summary>
        /// <returns>A camera for rendering a probe face.</returns>
        public Camera GetCamera()
        {
            if (m_FreeCameras.Count > 0)
            {
                Camera pooled = m_FreeCameras[m_FreeCameras.Count - 1];
                m_FreeCameras.RemoveAt(m_FreeCameras.Count - 1);
                return pooled;
            }

            return CreateCamera();
        }

        /// <summary>
        /// Returns a camera to the pool for later reuse.
        /// </summary>
        /// <param name="camera">The camera to return. Null is ignored.</param>
        public void ReturnCamera(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            m_FreeCameras.Add(camera);
        }

        /// <summary>
        /// Returns whether the given probe face was already rendered this frame.
        /// </summary>
        /// <param name="probeInstanceId">The reflection probe instance id.</param>
        /// <param name="face">The cubemap face index (<c>0..5</c>).</param>
        /// <returns><c>true</c> if the face was already rendered this frame.</returns>
        public bool IsFaceRendered(int probeInstanceId, int face)
        {
            return m_RenderedFaces.Contains(Encode(probeInstanceId, face));
        }

        /// <summary>
        /// Marks the given probe face as rendered this frame so later requests skip it.
        /// </summary>
        /// <param name="probeInstanceId">The reflection probe instance id.</param>
        /// <param name="face">The cubemap face index (<c>0..5</c>).</param>
        public void MarkFaceRendered(int probeInstanceId, int face)
        {
            m_RenderedFaces.Add(Encode(probeInstanceId, face));
        }

        /// <summary>
        /// Gets or creates a cached <see cref="RTHandle"/> for a specific probe
        /// cubemap face. The handle wraps a <see cref="RenderTargetIdentifier"/>
        /// pointing at the given face of the cubemap. Handles are reused across
        /// frames and released on <see cref="Dispose"/>.
        /// </summary>
        /// <param name="probeInstanceId">The probe instance id.</param>
        /// <param name="face">The cubemap face index (0..5).</param>
        /// <param name="cubemap">The cubemap render texture.</param>
        /// <returns>The cached RTHandle for this probe face.</returns>
        public RTHandle GetOrCreateFaceHandle(int probeInstanceId, int face, RenderTexture cubemap)
        {
            int key = Encode(probeInstanceId, face);
            if (m_ProbeFaceHandles.TryGetValue(key, out RTHandle existing))
            {
                return existing;
            }
            
            var targetId = new RenderTargetIdentifier(cubemap, 0, (CubemapFace)face, 0);
            var handle = RTHandles.Alloc(targetId, "RealtimeProbeFace" + key);
            m_ProbeFaceHandles[key] = handle;
            return handle;
        }

        /// <summary>
        /// Starts a new frame: clears the previous frame's rendered-face set
        /// and rendered probe textures.
        /// </summary>
        public void BeginFrame()
        {
            m_RenderedFaces.Clear();
            m_RenderedProbeTextures.Clear();
        }

        /// <summary>
        /// Ends the frame. Retained as an extension point; cameras are returned by
        /// the caller and the rendered set is cleared by <see cref="BeginFrame"/>.
        /// </summary>
        public void EndFrame()
        {
        }

        /// <summary>
        /// Destroys all pooled cameras, releases all cached RTHandles,
        /// and clears all state.
        /// </summary>
        public void Dispose()
        {
            foreach (Camera camera in m_FreeCameras)
            {
                if (camera != null)
                {
                    UnityEngine.Object.DestroyImmediate(camera.gameObject);
                }
            }

            m_FreeCameras.Clear();

            foreach (RTHandle handle in m_ProbeFaceHandles.Values)
            {
                handle?.Release();
            }

            m_ProbeFaceHandles.Clear();
            m_RenderedFaces.Clear();
            m_RenderedProbeTextures.Clear();
        }

        private static int Encode(int probeInstanceId, int face)
        {
            return probeInstanceId * 6 + face;
        }

        private static Camera CreateCamera()
        {
            var go = new GameObject("RealtimeProbeCamera");
            go.hideFlags = HideFlags.HideAndDontSave;
            Camera camera = go.AddComponent<Camera>();
            camera.cameraType = CameraType.Reflection;
            camera.enabled = false;
            return camera;
        }
    }
}
