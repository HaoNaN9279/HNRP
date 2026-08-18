// <copyright file="ClusterCullingReflectionProbePass.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    /// <summary>
    /// Runs cluster-based reflection probe culling via compute shader and outputs
    /// a reflection probe atlas, a mask buffer, and a probe data buffer for
    /// downstream passes (e.g. forward / deferred shading).
    /// </summary>
    /// <remarks>
    /// <para><b>New Pass system</b> (ADR-002, ADR-011):
    /// Inherits from <see cref="Pass"/> instead of the legacy <see cref="PassBase"/>.
    /// Uses name-based <see cref="TextureSlot"/> and <see cref="ComputeBufferSlot"/>
    /// outputs for downstream connections instead of index-based slot registration.
    /// </para>
    /// <para>
    /// The compute shader is accessed via
    /// <see cref="CameraContext.RuntimeResources"/>.<see cref="HNRenderPipelineRuntimeResources.clusterCullingReflectionProbeCS"/>.
    /// </para>
    /// </remarks>
    [Pass("Cluster Culling Probe")]
    public sealed class ClusterCullingReflectionProbePass : Pass
    {
        // ── Slots ──

        /// <summary>
        /// Gets the output texture slot for the reflection probe atlas
        /// (<see cref="TextureSlot"/>, <see cref="SlotDirection.Output"/>).
        /// Downstream passes read this atlas for reflection probe sampling.
        /// </summary>
        public TextureSlot? ReflectionProbeAtlasSlot { get; private set; }

        /// <summary>
        /// Gets the output compute buffer slot for the cluster culling
        /// reflection probe mask buffer
        /// (<see cref="ComputeBufferSlot"/>, <see cref="SlotDirection.Output"/>).
        /// </summary>
        public ComputeBufferSlot? ClusterCullingReflectionProbeMaskBufferSlot { get; private set; }

        /// <summary>
        /// Gets the output compute buffer slot for the cluster culling
        /// reflection probe data buffer
        /// (<see cref="ComputeBufferSlot"/>, <see cref="SlotDirection.Output"/>).
        /// </summary>
        public ComputeBufferSlot? ClusterCullingReflectionProbeDatasBufferSlot { get; private set; }

        // ── Camera context ──

        private CameraContext? m_Context;
        private ComputeShader? m_ComputeShader;

        // ── Constants (mirrored from legacy ClusterCullingReflectionProbePass) ──

        private const int MaxReflectionProbesOnScreen = 64;
        private const int ReflectionProbeAtlasSize = 4096;
        private const GraphicsFormat ReflectionProbeAtlasFormat = GraphicsFormat.B10G11R11_UFloatPack32;
        private const TextureDimension ReflectionProbeAtlasDimension = TextureDimension.Tex2D;
        private const FilterMode ReflectionProbeAtlasFilterMode = FilterMode.Trilinear;
        private const TextureWrapMode ReflectionProbeAtlasWrapMode = TextureWrapMode.Clamp;
        private const int MaxClusterMaskWords = 4096 * 4;
        private const string ClusterCullingKernelName = "ClusterCullingReflectionProbeCS";

        // ── Constructor ──

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterCullingReflectionProbePass"/> class.
        /// </summary>
        /// <param name="passName">
        /// The instance name of this pass. Must be non-null and unique within the render graph.
        /// </param>
        public ClusterCullingReflectionProbePass(string passName)
            : base(passName)
        {
        }

        // ── Lifecycle ──

        /// <inheritdoc />
        public override void SetupSlots()
        {
            ReflectionProbeAtlasSlot = new TextureSlot("reflectionProbeAtlas", SlotDirection.Output);
            RegisterSlot(ReflectionProbeAtlasSlot);
            ClusterCullingReflectionProbeMaskBufferSlot = new ComputeBufferSlot(
                "clusterCullingReflectionProbeMaskBuffer", SlotDirection.Output);
            RegisterSlot(ClusterCullingReflectionProbeMaskBufferSlot);
            ClusterCullingReflectionProbeDatasBufferSlot = new ComputeBufferSlot(
                "clusterCullingReflectionProbeDatasBuffer", SlotDirection.Output);
            RegisterSlot(ClusterCullingReflectionProbeDatasBufferSlot);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Stores the camera context and resolves the cluster culling compute
        /// shader from <see cref="CameraContext.RuntimeResources"/>.
        /// </remarks>
        public override void Initialize(CameraContext context)
        {
            m_Context = context;

            if (context.RuntimeResources != null)
            {
                m_ComputeShader = context.RuntimeResources.clusterCullingReflectionProbeCS;
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// Creates the reflection probe atlas (texture), mask buffer, and data buffer
        /// as render graph resources. Records a render function that dispatches the
        /// cluster culling compute shader to populate the mask and data buffers.
        ///
        /// The render function sets compute shader parameters including:
        /// <list type="bullet">
        ///   <item>Mask buffer (output)</item>
        ///   <item>Data buffer (output)</item>
        ///   <item>Culling parameters (cluster size, Z scale/offset, words per cluster)</item>
        ///   <item>Camera matrices (clip-to-view, view-to-clip, clip-to-world)</item>
        /// </list>
        /// </remarks>
        public override void Record(RenderGraph renderGraph)
        {
            if (m_ComputeShader == null)
            {
                Debug.LogError(
                    "Cluster Culling Reflection Probe Compute Shader is null. " +
                    "Ensure HNRenderPipelineRuntimeResources is assigned in the pipeline asset.");
                return;
            }

            if (m_Context == null)
            {
                Debug.LogError("CameraContext is null. Initialize must be called before Record.");
                return;
            }

            using (var builder = renderGraph.AddRenderPass<ClusterCullingReflectionProbePassData>(
                PassName, out var passData))
            {
                builder.AllowPassCulling(false);

                // ── Output: reflection probe atlas ──

                var atlasDesc = new TextureDesc(
                    ReflectionProbeAtlasSize,
                    ReflectionProbeAtlasSize,
                    false, false)
                {
                    name = "_ReflectionProbeAtlas",
                    colorFormat = ReflectionProbeAtlasFormat,
                    dimension = ReflectionProbeAtlasDimension,
                    useMipMap = true,
                    autoGenerateMips = false,
                    filterMode = ReflectionProbeAtlasFilterMode,
                    wrapMode = ReflectionProbeAtlasWrapMode,
                };

                TextureHandle atlasHandle = renderGraph.CreateTexture(atlasDesc);
                passData.reflectionProbeAtlas = builder.WriteTexture(atlasHandle);

                // ── Output: mask buffer ──

                ComputeBufferHandle maskHandle = renderGraph.CreateComputeBuffer(
                    new ComputeBufferDesc(
                        MaxClusterMaskWords,
                        sizeof(uint))
                    { name = "Cluster Culling Reflection Probe Mask Buffer" });

                passData.clusterCullingReflectionProbeMaskBuffer = builder.WriteComputeBuffer(maskHandle);

                // ── Output: data buffer ──

                ComputeBufferHandle datasHandle = renderGraph.CreateComputeBuffer(
                    new ComputeBufferDesc(
                        MaxReflectionProbesOnScreen,
                        UnsafeUtility.SizeOf<ClusterCullingReflectionProbeDatas>())
                    { name = "Cluster Culling Reflection Probe Datas Buffer" });

                passData.clusterCullingReflectionProbeDatasBuffer = builder.WriteComputeBuffer(datasHandle);

                // ── Publish real render graph handles to output slots ──

                ReflectionProbeAtlasSlot!.SetHandle(atlasHandle);
                ClusterCullingReflectionProbeMaskBufferSlot!.SetHandle(maskHandle);
                ClusterCullingReflectionProbeDatasBufferSlot!.SetHandle(datasHandle);

                // ── Compute shader setup ──

                passData.clusterCullingReflectionProbeCS = m_ComputeShader;
                passData.clusterCullingKernel = m_ComputeShader.FindKernel(ClusterCullingKernelName);

                Camera camera = m_Context.Camera;
                int2 screenResolution = math.int2(camera.pixelWidth, camera.pixelHeight);
                int3 clusterSize = GetClusterSize(screenResolution);
                int clusterCount = clusterSize.x * clusterSize.y * clusterSize.z;
                float2 clusterZScaleOffset = GetClusterZScaleOffset(
                    clusterSize, camera.orthographic,
                    camera.nearClipPlane, camera.farClipPlane);

                int itemsPerCluster = MaxReflectionProbesOnScreen;
                int wordsPerCluster = (itemsPerCluster + 31) / 32 + 1;

                Matrix4x4 clipToView = camera.projectionMatrix;
                Matrix4x4 viewToClip = camera.projectionMatrix.inverse;
                Matrix4x4 clipToWorld = (camera.worldToCameraMatrix * camera.projectionMatrix).inverse;

                // ── Render function ──

                builder.SetRenderFunc(
                    (ClusterCullingReflectionProbePassData data, RenderGraphContext ctx) =>
                    {
                        ctx.cmd.SetComputeBufferParam(
                            data.clusterCullingReflectionProbeCS,
                            data.clusterCullingKernel,
                            PropertyIDs.clusterCullingReflectionProbeMaskBuffer,
                            data.clusterCullingReflectionProbeMaskBuffer);
                        ctx.cmd.SetComputeBufferParam(
                            data.clusterCullingReflectionProbeCS,
                            data.clusterCullingKernel,
                            PropertyIDs.reflectionProbeDatas4CSBuffer,
                            data.clusterCullingReflectionProbeDatasBuffer);

                        ctx.cmd.SetComputeVectorParam(
                            data.clusterCullingReflectionProbeCS,
                            PropertyIDs.cullingParams0,
                            new Vector4(
                                clusterZScaleOffset.x,
                                clusterZScaleOffset.y,
                                wordsPerCluster,
                                camera.orthographic ? 1.0f : 0.0f));
                        ctx.cmd.SetComputeVectorParam(
                            data.clusterCullingReflectionProbeCS,
                            PropertyIDs.cullingParams1,
                            new Vector4(clusterSize.x, clusterSize.y, clusterSize.z, 0));

                        ctx.cmd.SetComputeMatrixParam(
                            data.clusterCullingReflectionProbeCS,
                            PropertyIDs.cullingClipToViewMatrix,
                            clipToView);
                        ctx.cmd.SetComputeMatrixParam(
                            data.clusterCullingReflectionProbeCS,
                            PropertyIDs.cullingViewToClipMatrix,
                            viewToClip);
                        ctx.cmd.SetComputeMatrixParam(
                            data.clusterCullingReflectionProbeCS,
                            PropertyIDs.cullingClipToWorldMatrix,
                            clipToWorld);

                        int threadGroup = (clusterCount + 63) / 64;
                        int threadGroupY = (threadGroup + clusterSize.y - 1) / clusterSize.y;
                        ctx.cmd.DispatchCompute(
                            data.clusterCullingReflectionProbeCS,
                            data.clusterCullingKernel,
                            clusterSize.y,
                            threadGroupY,
                            1);
                    });
            }
        }

        /// <inheritdoc />
        public override void Cleanup()
        {
            // No disposable resources held by this pass.
            m_ComputeShader = null;
            m_Context = null;
        }

        // ── Cluster size computation ──

        private const int ClusterMinTileSize = 8;
        private const int ClusterMaxZSlice = 128;
        private const int ClusterMinZSlice = 16;

        private static int3 GetClusterSize(int2 screenResolution)
        {
            int2 clusterSizeXY = new int2(1, 1);
            int sliceCount = ClusterMinZSlice;
            int tileWidth = ClusterMinTileSize >> 1;
            do
            {
                tileWidth <<= 1;
                clusterSizeXY = (screenResolution + tileWidth - 1) / tileWidth;
                int tileCountPerSlice = clusterSizeXY.x * clusterSizeXY.y;
                sliceCount = MaxClusterMaskWords / tileCountPerSlice - 1;
            }
            while (sliceCount < ClusterMinZSlice || sliceCount > ClusterMaxZSlice);

            return new int3(clusterSizeXY.x, clusterSizeXY.y, sliceCount);
        }

        private static float2 GetClusterZScaleOffset(
            int3 clusterSize, bool isOrthographic,
            float nearClipPlane, float farClipPlane)
        {
            float2 result;
            if (isOrthographic)
            {
                result.x = (float)clusterSize.z / (farClipPlane - nearClipPlane);
                result.y = -nearClipPlane * result.x;
            }
            else
            {
                result.x = (float)clusterSize.z / (math.log2(farClipPlane) - math.log2(nearClipPlane));
                result.y = -math.log2(nearClipPlane) * result.x;
            }

            return result;
        }

        // ── Pass data ──

        /// <summary>
        /// Render graph pass data container for
        /// <see cref="ClusterCullingReflectionProbePass"/>.
        /// </summary>
        private sealed class ClusterCullingReflectionProbePassData
        {
            /// <summary>
            /// The reflection probe atlas texture handle.
            /// </summary>
            public TextureHandle reflectionProbeAtlas;

            /// <summary>
            /// The cluster culling mask buffer handle.
            /// </summary>
            public ComputeBufferHandle clusterCullingReflectionProbeMaskBuffer;

            /// <summary>
            /// The cluster culling probe data buffer handle.
            /// </summary>
            public ComputeBufferHandle clusterCullingReflectionProbeDatasBuffer;

            /// <summary>
            /// The cluster culling compute shader.
            /// </summary>
            public ComputeShader clusterCullingReflectionProbeCS;

            /// <summary>
            /// The kernel index for the culling dispatch.
            /// </summary>
            public int clusterCullingKernel;
        }

        // ── Property IDs ──

        /// <summary>
        /// Shader property identifiers for cluster culling reflection probe
        /// compute shader parameters.
        /// Mirrors the shader property IDs used by the cluster culling
        /// reflection probe compute shader.
        /// </summary>
        public static class PropertyIDs
        {
            /// <summary>
            /// Reflection probe atlas texture.
            /// Value: <c>_ReflectionProbeAtlas</c>.
            /// </summary>
            public static readonly int reflectionProbeAtlas =
                Shader.PropertyToID("_ReflectionProbeAtlas");

            /// <summary>
            /// Cluster culling reflection probe mask buffer (RWStructuredBuffer).
            /// Value: <c>_ClusterCullingReflectionProbeMaskBuffer</c>.
            /// </summary>
            public static readonly int clusterCullingReflectionProbeMaskBuffer =
                Shader.PropertyToID("_ClusterCullingReflectionProbeMaskBuffer");

            /// <summary>
            /// Cluster culling reflection probe data buffer (RWStructuredBuffer).
            /// Value: <c>_ClusterCullingReflectionProbeDatasBuffer</c>.
            /// </summary>
            public static readonly int clusterCullingReflectionProbeDatasBuffer =
                Shader.PropertyToID("_ClusterCullingReflectionProbeDatasBuffer");

            /// <summary>
            /// Culling params 0: x=z scale, y=z offset, z=wordsPerCluster, w=isOrthographic.
            /// Value: <c>_ClusterCullingReflectionProbeParams0</c>.
            /// </summary>
            public static readonly int cullingParams0 =
                Shader.PropertyToID("_ClusterCullingReflectionProbeParams0");

            /// <summary>
            /// Culling params 1: xyz=clusterSize, w=probeCount.
            /// Value: <c>_ClusterCullingReflectionProbeParams1</c>.
            /// </summary>
            public static readonly int cullingParams1 =
                Shader.PropertyToID("_ClusterCullingReflectionProbeParams1");

            /// <summary>
            /// Clip-to-view matrix.
            /// Value: <c>_ClusterCullingReflectionProbeClipToView</c>.
            /// </summary>
            public static readonly int cullingClipToViewMatrix =
                Shader.PropertyToID("_ClusterCullingReflectionProbeClipToView");

            /// <summary>
            /// View-to-clip matrix.
            /// Value: <c>_ClusterCullingReflectionProbeViewToClip</c>.
            /// </summary>
            public static readonly int cullingViewToClipMatrix =
                Shader.PropertyToID("_ClusterCullingReflectionProbeViewToClip");

            /// <summary>
            /// Clip-to-world matrix.
            /// Value: <c>_ClusterCullingReflectionProbeClipToWorld</c>.
            /// </summary>
            public static readonly int cullingClipToWorldMatrix =
                Shader.PropertyToID("_ClusterCullingReflectionProbeClipToWorld");

            /// <summary>
            /// Cluster culling reflection probe params buffer (structured buffer for probe parameters).
            /// Value: <c>_ClusterCullingReflectionProbeParamsBuffer</c>.
            /// </summary>
            public static readonly int clusterCullingReflectionProbeParamsBuffer =
                Shader.PropertyToID("_ClusterCullingReflectionProbeParamsBuffer");

            /// <summary>
            /// Reflection probe data for compute shader buffer.
            /// Value: <c>_ClusterCullingReflectionProbeDatas4CSBuffer</c>.
            /// </summary>
            public static readonly int reflectionProbeDatas4CSBuffer =
                Shader.PropertyToID("_ClusterCullingReflectionProbeDatas4CSBuffer");
        }

        // ── Cluster culling data structures (moved from legacy ClusterCullingReflectionProbePass) ──
    }

    /// <summary>
    /// Per-probe data for compute shader culling.
    /// Each element holds the world-space bound center and extents for a single reflection probe.
    /// </summary>
    [Serializable]
    public struct ReflectionProbeData4CS
    {
        /// <summary>
        /// The world-space bound center of the reflection probe.
        /// </summary>
        public float3 boundCenter;

        /// <summary>
        /// The world-space bound extents of the reflection probe.
        /// </summary>
        public float3 boundExtents;
    }

    /// <summary>
    /// Per-probe rendering data passed to the shader after culling.
    /// Mirrors the legacy <c>ClusterCullingReflectionProbeDatas</c> struct.
    /// </summary>
    [Serializable]
    unsafe public struct ClusterCullingReflectionProbeDatas
    {
        /// <summary>
        /// The maximum corner of the probe bounding box in world space.
        /// </summary>
        public Vector3 boxMax;

        /// <summary>
        /// The blend distance for cross-fading between probes.
        /// </summary>
        public float blendDistance;

        /// <summary>
        /// The minimum corner of the probe bounding box in world space.
        /// </summary>
        public Vector3 boxMin;

        /// <summary>
        /// The importance weight of this probe.
        /// </summary>
        public float importance;

        /// <summary>
        /// The world-space position of the reflection probe.
        /// </summary>
        public Vector3 positionWS;

        /// <summary>
        /// The intensity multiplier for this probe's contribution.
        /// </summary>
        public float intensity;

        /// <summary>
        /// The scale and offset for sampling the probe cubemap.
        /// </summary>
        public Vector4 scaleOffset;
    }

    /// <summary>
    /// Cluster culling parameters passed to the compute shader.
    /// Mirrors the legacy <c>ClusterCullingReflectionProbeParams</c> struct.
    /// </summary>
    [Serializable]
    unsafe public struct ClusterCullingReflectionProbeParams
    {
        /// <summary>
        /// The cluster dimensions in screen space (XY).
        /// </summary>
        public Vector2 clusterSizeXY;

        /// <summary>
        /// The cluster Z scale and offset for depth slicing.
        /// </summary>
        public Vector2 clusterZScaleOffset;

        /// <summary>
        /// The number of 32-bit words per cluster in the mask buffer.
        /// </summary>
        public int wordsPerCluster;

        /// <summary>
        /// The total number of reflection probes.
        /// </summary>
        public int reflectionProbeCount;

        /// <summary>
        /// Unused padding (field 0).
        /// </summary>
        public float unused0;

        /// <summary>
        /// Unused padding (field 1).
        /// </summary>
        public float unused1;
    }
}
