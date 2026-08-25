// <copyright file="RealtimeProbeRenderer.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    /// <summary>
    /// Renders realtime reflection probes before all main cameras.
    /// Collects visible realtime probes from camera culling results, decides which
    /// cubemap faces to render this frame from each probe's
    /// <see cref="ReflectionProbe.timeSlicingMode"/> and
    /// <see cref="ReflectionProbe.refreshMode"/>, and renders each face through the
    /// normal per-camera pipeline using <see cref="HNRenderPipelineAsset.DefaultReflectionRenderGraph"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Faces are rendered with pooled cameras owned by <see cref="RealtimeProbeCameraPool"/>;
    /// the pool also records which probe faces were already rendered this frame so
    /// probes visible to multiple cameras are rendered only once.
    /// </para>
    /// <para>
    /// Reflection probes themselves are not rendered inside the probe pass — the
    /// Reflection render graph template contains no cluster-culling probe pass.
    /// </para>
    /// </remarks>
    public sealed class RealtimeProbeRenderer : IDisposable
    {
        /// <summary>
        /// The camera pool used for face rendering and per-frame dedup.
        /// </summary>
        private readonly RealtimeProbeCameraPool m_Pool;

        /// <summary>
        /// Realtime probes visible this frame, keyed by probe instance id.
        /// Cleared by <see cref="BeginFrame"/>.
        /// </summary>
        private readonly Dictionary<int, ReflectionProbe> m_Requests = new();

        /// <summary>
        /// Per-probe face progress for
        /// <see cref="ReflectionProbeTimeSlicingMode.IndividualFaces"/>.
        /// </summary>
        private readonly Dictionary<int, int> m_FaceProgress = new();

        /// <summary>
        /// Probes already initialized for <see cref="ReflectionProbeRefreshMode.OnAwake"/>.
        /// </summary>
        private readonly HashSet<int> m_InitializedProbes = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="RealtimeProbeRenderer"/> class.
        /// </summary>
        /// <param name="pool">The camera pool used for face rendering.</param>
        public RealtimeProbeRenderer(RealtimeProbeCameraPool pool)
        {
            m_Pool = pool;
        }

        /// <summary>
        /// Gets the number of collected realtime probes pending render this frame.
        /// </summary>
        public int PendingProbeCount => m_Requests.Count;

        /// <summary>
        /// Starts a new frame: clears the previous frame's requests and the pool's
        /// rendered-face set.
        /// </summary>
        public void BeginFrame()
        {
            m_Pool.BeginFrame();
            m_Requests.Clear();
        }

        /// <summary>
        /// Collects realtime probes from a camera's visible reflection probe culling
        /// results. Duplicate probes (visible to multiple cameras) are collected once.
        /// </summary>
        /// <param name="visibleProbes">Visible reflection probes from a camera's
        /// culling results.</param>
        public void CollectRealtimeProbes(NativeArray<VisibleReflectionProbe> visibleProbes)
        {
            for (int i = 0; i < visibleProbes.Length; i++)
            {
                CollectRealtimeProbe(
                    RealtimeProbeRenderUtils.GetProbeInstanceId(visibleProbes[i]));
            }
        }

        /// <summary>
        /// Collects a single realtime probe by instance id. No-op for invalid ids,
        /// unknown objects, or non-realtime probes.
        /// </summary>
        /// <param name="probeInstanceId">The reflection probe instance id.</param>
        public void CollectRealtimeProbe(int probeInstanceId)
        {
            if (probeInstanceId == 0 || m_Requests.ContainsKey(probeInstanceId))
            {
                return;
            }

            var probe = UnityEngine.Resources.InstanceIDToObject(probeInstanceId) as ReflectionProbe;
            if (probe == null || !RealtimeProbeRenderUtils.IsRealtimeProbe(probe))
            {
                return;
            }

            m_Requests.Add(probeInstanceId, probe);
        }

        /// <summary>
        /// Renders all collected realtime probes. Called before main camera rendering
        /// so probe faces execute first. Each cubemap face is recorded and executed in
        /// its own <c>RecordAndExecute</c> block: the camera matrix set by
        /// <c>SetupCameraProperties</c> is only applied when the block executes, so
        /// every face must execute immediately after its camera is configured —
        /// otherwise a later camera's matrix would leak into this face's passes.
        /// </summary>
        /// <param name="context">The scriptable render context for the frame.</param>
        /// <param name="renderGraph">The render graph to record probe passes into.</param>
        /// <param name="parameters">The render graph parameters for this frame.</param>
        /// <param name="asset">The pipeline asset providing the reflection render graph
        /// and runtime resources.</param>
        /// <param name="deferredDispose">List receiving per-face camera contexts whose
        /// disposal is deferred until after render graph execution.</param>
        public void RenderProbes(
            ScriptableRenderContext context,
            RenderGraph renderGraph,
            in RenderGraphParameters parameters,
            HNRenderPipelineAsset asset)
        {
            if (m_Requests.Count == 0 || asset == null || asset.reflectionRenderGraphViewBlock == null)
            {
                return;
            }

            foreach (KeyValuePair<int, ReflectionProbe> request in m_Requests)
            {
                RenderProbe(context, renderGraph, parameters, asset, request.Value);
            }
        }

        /// <summary>
        /// Returns whether the given probe should be rendered this frame, honoring
        /// its <see cref="ReflectionProbe.refreshMode"/>.
        /// </summary>
        /// <param name="probe">The reflection probe to test.</param>
        /// <returns>
        /// <c>true</c> for <see cref="ReflectionProbeRefreshMode.EveryFrame"/>,
        /// <c>true</c> only until initialized for
        /// <see cref="ReflectionProbeRefreshMode.OnAwake"/>, and <c>false</c> for
        /// <see cref="ReflectionProbeRefreshMode.ViaScripting"/>.
        /// </returns>
        public bool ShouldRenderThisFrame(ReflectionProbe probe)
        {
            if (probe == null)
            {
                return false;
            }

            switch (probe.refreshMode)
            {
                case ReflectionProbeRefreshMode.OnAwake:
                    return !m_InitializedProbes.Contains(probe.GetInstanceID());

                case ReflectionProbeRefreshMode.ViaScripting:
                    return false;

                case ReflectionProbeRefreshMode.EveryFrame:
                default:
                    return true;
            }
        }

        /// <summary>
        /// Marks a probe as initialized so <see cref="ReflectionProbeRefreshMode.OnAwake"/>
        /// probes stop rendering.
        /// </summary>
        /// <param name="probe">The probe that finished its initial render.</param>
        public void MarkInitialized(ReflectionProbe probe)
        {
            if (probe == null)
            {
                return;
            }

            m_InitializedProbes.Add(probe.GetInstanceID());
        }

        /// <summary>
        /// Returns whether the given probe was already initialized.
        /// </summary>
        /// <param name="probe">The reflection probe to test.</param>
        /// <returns><c>true</c> if the probe finished its initial render.</returns>
        public bool IsInitialized(ReflectionProbe probe)
        {
            return probe != null && m_InitializedProbes.Contains(probe.GetInstanceID());
        }

        /// <summary>
        /// Ends the frame for the renderer and its pool.
        /// </summary>
        public void EndFrame()
        {
            m_Pool.EndFrame();
        }

        /// <summary>
        /// Disposes the renderer and its camera pool.
        /// </summary>
        public void Dispose()
        {
            m_Pool.Dispose();
            m_Requests.Clear();
            m_FaceProgress.Clear();
            m_InitializedProbes.Clear();
        }

        // ── Rendering ──

        private void RenderProbe(
            ScriptableRenderContext context,
            RenderGraph renderGraph,
            in RenderGraphParameters parameters,
            HNRenderPipelineAsset asset,
            ReflectionProbe probe)
        {
            if (!ShouldRenderThisFrame(probe))
            {
                return;
            }

            int probeId = probe.GetInstanceID();
            int[] faces = GetFacesForProbe(probe, probeId);

            foreach (int face in faces)
            {
                if (m_Pool.IsFaceRendered(probeId, face))
                {
                    continue;
                }

                RenderFace(context, renderGraph, parameters, asset, probe, face);
                m_Pool.MarkFaceRendered(probeId, face);
            }

            if (probe.refreshMode == ReflectionProbeRefreshMode.OnAwake)
            {
                MarkInitialized(probe);
            }
            else if (faces.Length > 0 &&
                     probe.timeSlicingMode == ReflectionProbeTimeSlicingMode.IndividualFaces)
            {
                int progress = GetFaceProgress(probeId);
                m_FaceProgress[probeId] = RealtimeProbeRenderUtils.AdvanceIndividualFace(progress);
            }
        }

        private int[] GetFacesForProbe(ReflectionProbe probe, int probeId)
        {
            if (probe.refreshMode == ReflectionProbeRefreshMode.OnAwake)
            {
                // OnAwake renders all faces once.
                return RealtimeProbeRenderUtils.AllFaces;
            }

            return RealtimeProbeRenderUtils.GetFacesToRender(
                probe.timeSlicingMode,
                probeId,
                Time.frameCount,
                GetFaceProgress(probeId));
        }

        private int GetFaceProgress(int probeId)
        {
            return m_FaceProgress.TryGetValue(probeId, out int progress) ? progress : 0;
        }

        private void RenderFace(
            ScriptableRenderContext context,
            RenderGraph renderGraph,
            in RenderGraphParameters parameters,
            HNRenderPipelineAsset asset,
            ReflectionProbe probe,
            int face)
        {
            Camera camera = m_Pool.GetCamera();
            RenderTexture target = GetProbeTarget(probe);
            ConfigureCamera(camera, probe, face, target);

            int probeId = probe.GetInstanceID();
            var customTargetHandle = m_Pool.GetOrCreateFaceHandle(probeId, face, target);
            var cameraContext = new CameraContext(camera, context)
            {
                RuntimeResources = asset.runtimeResources,
                TargetFace = (CubemapFace)face,
                TargetDepthSlice = 0,
                Flip = false,
                CustomTargetRTHandle = customTargetHandle,
            };

            bool gotParams = camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParams);
            if (gotParams)
            {
                cameraContext.CullingResults = context.Cull(ref cullingParams);
                cameraContext.HasCullingResults = true;
                cameraContext.VisibleLights = new NativeArray<VisibleLight>(
                    cameraContext.CullingResults.visibleLights, Allocator.TempJob);
                // VisibleReflectionProbes intentionally not populated: the Reflection
                // render graph template has no reflection probe consumer.
            }

            // The face records and executes in its own RecordAndExecute block so the
            // camera matrix configured below is the active one when the passes run.
            using (renderGraph.RecordAndExecute(parameters))
            {
                SetupCameraProperties(context, camera);

                // Push this face camera's global shader constants into the
                // ShaderVariablesGlobal cbuffer. The render graph commands are
                // submitted after this camera block, so the per-face matrices are
                // the active ones for the face's draws (SetupCameraProperties alone
                // would leave the LAST camera's matrix as global state).
                var globalConstantBuffer = new GlobalConstantBuffer();
                GlobalConstantBufferUtility.FillFromCamera(
                    camera,
                    renderIntoTexture: true,
                    ref globalConstantBuffer);
                ConstantBuffer.PushGlobal(
                    parameters.commandBuffer,
                    globalConstantBuffer,
                    GlobalPropertyIDs.ShaderVariablesGlobal);

                var renderer = new CameraRenderer(cameraContext);
                renderer.Build(asset.reflectionRenderGraphViewBlock.GetRenderGraphObject());
                renderer.Render(renderGraph, context);
            }

            m_Pool.ReturnCamera(camera);
            cameraContext.Dispose();
        }

        private static void SetupCameraProperties(ScriptableRenderContext context, Camera camera)
        {
            var cmd = CommandBufferPool.Get("CameraSetup");
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);

            context.SetupCameraProperties(camera);
        }

        private static void ConfigureCamera(
            Camera camera,
            ReflectionProbe probe,
            int face,
            RenderTexture target)
        {
            camera.transform.position = probe.transform.position;
            camera.transform.rotation = RealtimeProbeRenderUtils.GetFaceRotation(face);
            camera.cameraType = CameraType.Reflection;
            camera.targetTexture = target;
            camera.nearClipPlane = probe.nearClipPlane;
            camera.farClipPlane = probe.farClipPlane;
            camera.cullingMask = probe.cullingMask;
            camera.clearFlags = (CameraClearFlags)probe.clearFlags;
            camera.backgroundColor = probe.backgroundColor;
            camera.fieldOfView = 90f;
            camera.aspect = 1f;
            camera.ResetProjectionMatrix();
        }

        private static RenderTexture GetProbeTarget(ReflectionProbe probe)
        {
            if (probe.realtimeTexture != null)
            {
                return probe.realtimeTexture;
            }

            var descriptor = new RenderTextureDescriptor(
                probe.resolution,
                probe.resolution,
                probe.hdr ? RenderTextureFormat.RGB111110Float : RenderTextureFormat.ARGB32,
                0)
            {
                dimension = TextureDimension.Cube,
                useMipMap = true,
                autoGenerateMips = true,
            };

            var rt = new RenderTexture(descriptor)
            {
                name = "RealtimeProbeRT_" + probe.name,
            };

            probe.realtimeTexture = rt;
            return rt;
        }
    }
}
