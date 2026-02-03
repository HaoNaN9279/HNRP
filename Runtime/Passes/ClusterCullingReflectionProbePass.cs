using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using log4net.Util;
using Unity.Properties;
using log4net.DateFormatter;

namespace HN.HNRP
{
    [Serializable]
    public class ClusterCullingReflectionProbePass : PassBase
    {
        public override void OnCreate(HNRenderGraphBase hnRenderGraph, string passName)
        {
            base.OnCreate(hnRenderGraph, passName);
        }

        public override void Record(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            if(reflectionProbeAtlasRT == null)
            {
                reflectionProbeAtlasRT = new RenderTexture(new RenderTextureDescriptor(REFLECTION_PROBE_ATLAS_SIZE, REFLECTION_PROBE_ATLAS_SIZE, REFLECTION_PROBE_ATLAS_FORMAT))
                {
                    name = REFLECTION_PROBE_ATLAS_NAME,
                    dimension = REFLECTION_PROBE_ATLAS_DIMENSION,
                    volumeDepth = 1,
                    enableRandomWrite = false,
                    useMipMap = true,
                    autoGenerateMips = false,
                    filterMode = REFLECTION_PROBE_ATLAS_FILTER_MODE,
                    wrapMode = REFLECTION_PROBE_ATLAS_WRAP_MODE
                };
            }
            reflectionProbeAtlasHandle = RTHandles.Alloc(reflectionProbeAtlasRT);

            using (var builder = renderGraph.AddRenderPass<ClusterCullingReflectionProbePassData>($"{name}({PassName})", out var passData))
            {
                builder.AllowPassCulling(false);

                // TODO: 渲染ReflectionProbe时的环境反射使用默认ReflectionProbe
                if(renderingData.Camera.cameraType == CameraType.Reflection)
                {
                    return;
                }
                
                ClearProbesRef();
                UpdateProbeRefs(ref renderingData.visibleReflectionProbes);
                CatcheProbes(passData, ref catchedReflectionProbes);
                UpdateReflectionProbeData(passData);
                ImportProbeTextures(renderGraph, passData);
                passData.reflectionProbeAtlas = renderGraph.ImportTexture(reflectionProbeAtlasHandle);

                passData.clusterCullingReflectionProbeMaskBuffer = builder.WriteComputeBuffer(renderGraph.CreateComputeBuffer(
                    new ComputeBufferDesc(
                        MAX_CLUSTER_MASK_WORDS,
                        sizeof(uint)
                    ) { name = "Cluster Culling Reflection Probe Cluster Mask Buffer" }
                ));
                int itemsPerCluster = MAX_REFLECTION_PROBES_ON_SCREEN;
                int wordsPerCluster = (itemsPerCluster + 31) / 32 + 1/* 1 for header */;
                Camera camera = renderingData.Camera;
                HNAdditionalCameraData cameraData = renderingData.CameraData;
                int2 screenResolution = math.int2(camera.pixelWidth, camera.pixelHeight);
                int3 clusterSize = GetClusterSize(screenResolution);
                int clusterCount = clusterSize.x * clusterSize.y * clusterSize.z;
                // Debug.Log($"ClusterSize: x: {clusterSize.x} y: {clusterSize.y} z: {clusterSize.z}.");
                float2 clusterZScaleOffset = GetClusterZScaleOffset(clusterSize, camera.orthographic, camera.nearClipPlane, camera.farClipPlane);
                UpdateReflectionProbeParams(clusterSize, clusterZScaleOffset, wordsPerCluster);
                GetCameraMatrix(camera);
                passData.clusterCullingReflectionProbeCS = renderingData.runtimeResources.shaderResources.clusterCullingReflectionProbeCS;
                passData.clusterCullingKernel = passData.clusterCullingReflectionProbeCS.FindKernel(CLUSTER_CULLING_CS_KERNEL_NAME);
                GetReflectionProbeDataForCS(catchedReflectionProbes);
                passData.reflectionProbeDatasBuffer = builder.WriteComputeBuffer(renderGraph.CreateComputeBuffer(
                    new ComputeBufferDesc(
                        reflectionProbeDatas.Length,
                        UnsafeUtility.SizeOf<ReflectionProbeData>()
                    ) { name = "Reflection Probe Datas Buffer" }
                ));

                builder.SetRenderFunc(
                    (ClusterCullingReflectionProbePassData data, RenderGraphContext ctx) =>
                    {
                        ctx.cmd.EnableShaderKeyword(GlobalKeywords.clusterCullingReflectionProbe);
                        
                        for(int i = 0; i < MAX_REFLECTION_PROBES_ON_SCREEN; i++)
                        {
                            if(data.needUpdate[i])
                            {
                                int texelPadding = REFLECTION_PROBE_ATLAS_TEXEL_PADDING;
                                Vector4 scaleOffset = GetTextureScaleOffsetWithoutPaddingInAtlas(data.scaleOffset[i]);
                                Vector2 textureSizeWithoutPadding = GetTextureSizeWithoutpadding(scaleOffset, texelPadding);
                                
                                for(int mipLevel = 0; mipLevel < REFLECTION_PROBE_ATLAS_MIP_COUNT; mipLevel++)
                                {
                                    texelPadding *= 2;
                                    ctx.cmd.SetRenderTarget(data.reflectionProbeAtlas, mipLevel, CubemapFace.Unknown, 0);
                                    var propertyBlock = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                                    Blitter.BlitCubeToOctahedral2DQuadWithPadding(ctx.cmd, propertyBlock, data.textures[i], textureSizeWithoutPadding, scaleOffset, mipLevel, data.isBilinear[i], texelPadding);
                                }
                            }
                        }

                        ctx.cmd.SetComputeBufferParam(data.clusterCullingReflectionProbeCS, data.clusterCullingKernel, PropertyIDs.clusterCullingReflectionProbeMaskBuffer, data.clusterCullingReflectionProbeMaskBuffer);
                        ctx.cmd.SetBufferData(data.reflectionProbeDatasBuffer, reflectionProbeDatas);
                        ctx.cmd.SetComputeBufferParam(data.clusterCullingReflectionProbeCS, data.clusterCullingKernel, PropertyIDs.reflectionProbeDatasBuffer, data.reflectionProbeDatasBuffer);
                        ctx.cmd.SetComputeVectorParam(data.clusterCullingReflectionProbeCS, PropertyIDs.cullingParams0, new Vector4(clusterZScaleOffset.x, clusterZScaleOffset.y, wordsPerCluster, camera.orthographic ? 1.0f : 0.0f));
                        ctx.cmd.SetComputeVectorParam(data.clusterCullingReflectionProbeCS, PropertyIDs.cullingParams1, new Vector4(clusterSize.x, clusterSize.y, clusterSize.z, catchedProbeCount));
                        ctx.cmd.SetComputeMatrixParam(data.clusterCullingReflectionProbeCS, PropertyIDs.cullingClipToViewMatrix, clipToView);
                        ctx.cmd.SetComputeMatrixParam(data.clusterCullingReflectionProbeCS, PropertyIDs.cullingViewToClipMatrix, viewToClip);
                        ctx.cmd.SetComputeMatrixParam(data.clusterCullingReflectionProbeCS, PropertyIDs.cullingClipToWorldMatrix, clipToWorld);
                        int threadGroup = (clusterCount + 63) / 64;
                        int threadGroupY = (threadGroup + clusterSize.y - 1) / clusterSize.y;
                        ctx.cmd.DispatchCompute(data.clusterCullingReflectionProbeCS, data.clusterCullingKernel, clusterSize.y, threadGroupY, 1);

                        ctx.cmd.SetGlobalBuffer(PropertyIDs.clusterCullingReflectionProbeMaskBuffer, data.clusterCullingReflectionProbeMaskBuffer);
                        ConstantBuffer.PushGlobal(ctx.cmd, globalConstantBuffer, PropertyIDs.reflectionProbeGlobalConstantBuffer);
                        ctx.cmd.SetGlobalTexture(PropertyIDs.reflectionProbeAtlas, data.reflectionProbeAtlas);
                    }
                );
            }
        }

        public override void Cleanup()
        {
            for(int i = 0; i < textureRTHandles.Length; i++)
            {
                if(textureRTHandles[i] != null)
                {
                    RTHandles.Release(textureRTHandles[i]);
                    textureRTHandles[i] = null;
                }
            }

            RTHandles.Release(reflectionProbeAtlasHandle);
        }


        private void ClearProbesRef()
        {
            foreach(var probesDict in refProbes)
            {
                probesDict.Clear();
            }
        }

        private void UpdateProbeRefs(ref NativeArray<VisibleReflectionProbe> visibleReflectionProbes)
        {
            var reflectionProbes = visibleReflectionProbes;
            for(int i = 0; i < reflectionProbes.Length; i++)
            {
                var probe = reflectionProbes[i];
                if(probe.texture == null)
                    continue;
                
                var probeData = probe.reflectionProbe.GetHNAdditionalReflectionProbeData();
                UpdateProbeRef(probe, probeData);
            }
        }

        private void UpdateProbeRef(VisibleReflectionProbe probe, HNAdditionalReflectionProbeData probeData)
        {
            if(probe.texture == null)
                return;
            
            int resolution = probe.texture.width;
            uint probeHash = GetProbeHash(probe, probeData, resolution);
            int index = (int)Math.Log(4096 / resolution, 2);
            refProbes[index].TryAdd(probeHash, probe);
        }

        private uint GetProbeHash(VisibleReflectionProbe probe, HNAdditionalReflectionProbeData probeData, int resolution)
        {
            uint probeCount = (uint)probeData.UpdateCount;
            uint textureID = (uint)probe.texture.GetInstanceID();

            const uint kPrime = 31;
            return (kPrime + (uint)resolution) * textureID + probeCount;
        }

        private void CatcheProbes(ClusterCullingReflectionProbePassData passData, ref VisibleReflectionProbe[] catchedReflectionProbes)
        {
            catchedProbeCount = 0;
            uint offsetMask = 0;
            uint maxOffsetMask = 0x00FFC000; // 0000 0000 1111 1111 1100 0000 0000 0000
            int maxCount = MAX_REFLECTION_PROBES_ON_SCREEN;
            for(int i = 0; i < refProbes.Length; i++)
            {
                int index = 0;
                var hashes = refProbes[i].Keys.ToList();
                while(refProbes[i].Count > 0 && index < hashes.Count && index < maxCount && offsetMask < maxOffsetMask)
                {
                    int width = 4096 / (int)Mathf.Pow(2, i);
                    GetOffset(offsetMask, out int offsetX, out int offsetY);
                    int4 scaleOffset = new int4(width, width, offsetX, offsetY);
                    if(passData.probeHash[catchedProbeCount] != hashes[index])
                    {
                        passData.scaleOffset[catchedProbeCount] = scaleOffset;
                        passData.needUpdate[catchedProbeCount] = true;
                        passData.probeHash[catchedProbeCount] = hashes[index];
                    }
                    else
                    {
                        if(!Int4Equal(passData.scaleOffset[catchedProbeCount], scaleOffset))
                        {
                            passData.scaleOffset[catchedProbeCount] = scaleOffset;
                            passData.needUpdate[catchedProbeCount] = true;
                        }
                        else
                        {
                            passData.needUpdate[catchedProbeCount] = false;
                        }
                    }
                    // 如果probe没变，probe中的参数改变也需要更新
                    passData.probe[catchedProbeCount] = refProbes[i][hashes[index]];
                    index++;
                    catchedProbeCount++;
                    offsetMask += (uint)1 << (int)(Mathf.Log(width, 2) * 2 - 2);
                }
            }
            catchedReflectionProbes = passData.probe;
        }

        private void ImportProbeTextures(RenderGraph renderGraph, ClusterCullingReflectionProbePassData passData)
        {
            for(int i = 0; i < MAX_REFLECTION_PROBES_ON_SCREEN; i++)
            {
                if(passData.needUpdate[i])
                {
                    var reflectionProbe = passData.probe[i].reflectionProbe;
                    var texture = reflectionProbe?.texture;
                    if(texture == null)
                        continue;
                    textureRTHandles[i] = RTHandles.Alloc(texture);
                    passData.isBilinear[i] = texture.filterMode == FilterMode.Bilinear || texture.filterMode == FilterMode.Trilinear;
                    passData.textures[i] = renderGraph.ImportTexture(textureRTHandles[i]);
                }
            }
        }

        private void GetOffset(uint offsetMask, out int offsetX, out int offsetY)
        {
            // 在offsetMask中按位存储当前texture在atlas中的位置
            // Mask中的有效位只有中间的2 * 5 = 10位，2表示x和y，即相邻两位左边表示x，右边表示y；5表示最多支持5种分辨率的reflection probe
            // 将atlas分成四块，四块分别用00, 01, 10, 11表示，分好的每一块又可以再次四分，用低两位00, 01, 10, 11表示，如此递归
            // 最终可以用2 * 5 = 10位表示从最大4096分辨率到256分辨率的reflection probe在atlas中的位置
            // 有效位放在offsetMask的第15到24位，是为了方便计算分辨率
            // 下面的计算是为了将相邻的x和y位拆开，分别计算出offsetX和offsetY

            offsetX = offsetY = 0;
            uint oddBits = 0;
            uint evenBits = 0;
            int oddIndex = 0;
            int evenIndex = 0;
            for(int i = 0; i < 32; i++)
            {
                uint bit = (offsetMask >> i) & 0x1;
                if(i % 2 == 0)
                {
                    evenIndex++;
                    evenBits |= (bit << evenIndex);
                }
                else
                {
                    oddIndex++;
                    oddBits |= (bit << oddIndex);
                }
            }
            offsetX = (int)evenBits;
            offsetY = (int)oddBits;
        }

        private bool Int4Equal(int4 int4A, int4 int4B)
        {
            return int4A.x == int4B.x && int4A.y == int4B.y && int4A.z == int4B.z && int4A.w == int4B.w;
        }

        private Vector4 GetTextureScaleOffsetWithoutPaddingInAtlas(int4 scaleOffset)
        {
            float atlasSize = REFLECTION_PROBE_ATLAS_SIZE;
            float scaleX = scaleOffset.x / atlasSize;
            float scaleY = scaleOffset.y / atlasSize;
            float offsetX = scaleOffset.z / atlasSize;
            float offsetY = scaleOffset.w / atlasSize;
            return new Vector4(scaleX, scaleY, offsetX, offsetY);
        }

        private Vector4 GetTextureScaleOffsetWithPaddingInAtlas(int4 scaleOffset)
        {
            float atlasSize = REFLECTION_PROBE_ATLAS_SIZE;
            float scaleX = (scaleOffset.x - REFLECTION_PROBE_ATLAS_TEXEL_PADDING * 2) / atlasSize;
            float scaleY = (scaleOffset.y - REFLECTION_PROBE_ATLAS_TEXEL_PADDING * 2) / atlasSize;
            float offsetX = (scaleOffset.z + REFLECTION_PROBE_ATLAS_TEXEL_PADDING) / atlasSize;
            float offsetY = (scaleOffset.w + REFLECTION_PROBE_ATLAS_TEXEL_PADDING) / atlasSize;
            return new Vector4(scaleX, scaleY, offsetX, offsetY);
        }

        private Vector2 GetTextureSizeWithoutpadding(Vector4 scaleOffset, int texelPadding)
        {
            float scaleX = scaleOffset.x * REFLECTION_PROBE_ATLAS_SIZE - texelPadding * 2;
            float scaleY = scaleOffset.y * REFLECTION_PROBE_ATLAS_SIZE - texelPadding * 2;
            return new Vector2(scaleX, scaleY);
        }

        unsafe private void UpdateReflectionProbeData(ClusterCullingReflectionProbePassData passData)
        {
            for(int i = 0; i < MAX_REFLECTION_PROBES_ON_SCREEN; i++)
            {
                var probe = passData.probe[i];
                if(probe.texture == null)
                    continue;
                int baseIndex = i * 4;
                globalConstantBuffer.reflectionProbeData0[baseIndex + 0] = probe.bounds.max.x;
                globalConstantBuffer.reflectionProbeData0[baseIndex + 1] = probe.bounds.max.y;
                globalConstantBuffer.reflectionProbeData0[baseIndex + 2] = probe.bounds.max.z;
                globalConstantBuffer.reflectionProbeData0[baseIndex + 3] = probe.blendDistance;
                globalConstantBuffer.reflectionProbeData1[baseIndex + 0] = probe.bounds.min.x;
                globalConstantBuffer.reflectionProbeData1[baseIndex + 1] = probe.bounds.min.y;
                globalConstantBuffer.reflectionProbeData1[baseIndex + 2] = probe.bounds.min.z;
                globalConstantBuffer.reflectionProbeData1[baseIndex + 3] = probe.importance;
                globalConstantBuffer.reflectionProbeData2[baseIndex + 0] = probe.localToWorldMatrix.m03;
                globalConstantBuffer.reflectionProbeData2[baseIndex + 1] = probe.localToWorldMatrix.m13;
                globalConstantBuffer.reflectionProbeData2[baseIndex + 2] = probe.localToWorldMatrix.m23;
                globalConstantBuffer.reflectionProbeData2[baseIndex + 3] = probe.reflectionProbe.intensity;
                Vector4 scaleOffsetNormalized = GetTextureScaleOffsetWithoutPaddingInAtlas(passData.scaleOffset[i]);
                globalConstantBuffer.reflectionProbeData3[baseIndex + 0] = scaleOffsetNormalized.x;
                globalConstantBuffer.reflectionProbeData3[baseIndex + 1] = scaleOffsetNormalized.y;
                globalConstantBuffer.reflectionProbeData3[baseIndex + 2] = scaleOffsetNormalized.z;
                globalConstantBuffer.reflectionProbeData3[baseIndex + 3] = scaleOffsetNormalized.w;
            }
        }

        private int3 GetClusterSize(int2 screenResolution)
        {
            int2 clusterSizeXY = new int2(1, 1);
            int sliceCount = CLUSTER_MIN_Z_SLIZE;
            int tileWidth = 8 >> 1;
            do
            {
                tileWidth <<= 1;
                clusterSizeXY = (screenResolution + tileWidth - 1) / tileWidth;
                int tileCountPerSlice = clusterSizeXY.x * clusterSizeXY.y;
                sliceCount = MAX_CLUSTER_MASK_WORDS / tileCountPerSlice - 1;
            }
            while(sliceCount < CLUSTER_MIN_Z_SLIZE || sliceCount > CLUSTER_MAX_Z_SLICE);
            return new int3(clusterSizeXY.x, clusterSizeXY.y, sliceCount);
        }

        private float2 GetClusterZScaleOffset(int3 clusterSize, bool isOrthographic, float nearClipPlane, float farClipPlane)
        {
            float2 clusterZScaleOffset = new float2(0, 0);
            if(isOrthographic) // 正交相机
            {
                clusterZScaleOffset.x = (float)clusterSize.z / (farClipPlane - nearClipPlane);
                clusterZScaleOffset.y = -nearClipPlane * clusterZScaleOffset.x;
            }
            else // 透视相机
            {
                clusterZScaleOffset.x = (float)clusterSize.z / (math.log2(farClipPlane) - math.log2(nearClipPlane));
                clusterZScaleOffset.y = -math.log2(nearClipPlane) * clusterZScaleOffset.x;
            }
            return clusterZScaleOffset;
        }

        private void GetReflectionProbeDataForCS(VisibleReflectionProbe[] catchedReflectionProbes)
        {
            reflectionProbeDatas = new ReflectionProbeData[MAX_REFLECTION_PROBES_ON_SCREEN];
            for(int i = 0; i < catchedReflectionProbes.Length; i++)
            {
                reflectionProbeDatas[i].boundCenter = catchedReflectionProbes[i].bounds.center;
                reflectionProbeDatas[i].boundExtents = catchedReflectionProbes[i].bounds.extents;
            }
        }

        unsafe private void UpdateReflectionProbeParams(int3 clusterSize, float2 clusterZScaleOffset, int wordsPerCluster)
        {
            globalConstantBuffer.reflectionProbeParam0[0] = clusterSize.x;
            globalConstantBuffer.reflectionProbeParam0[1] = clusterSize.y;
            globalConstantBuffer.reflectionProbeParam0[2] = clusterZScaleOffset.x;
            globalConstantBuffer.reflectionProbeParam0[3] = clusterZScaleOffset.y;
            globalConstantBuffer.reflectionProbeParam1[0] = wordsPerCluster;
        }

        private void GetCameraMatrix(Camera camera)
        {
            clipToView = camera.projectionMatrix;
            viewToClip = camera.projectionMatrix.inverse;
            clipToWorld = (camera.worldToCameraMatrix * camera.projectionMatrix).inverse;
        }


        // 当前帧从剔除结果获取的reflection probe列表
        private Dictionary<uint, VisibleReflectionProbe>[] refProbes = new Dictionary<uint, VisibleReflectionProbe>[5]
        {
            new Dictionary<uint, VisibleReflectionProbe>(),
            new Dictionary<uint, VisibleReflectionProbe>(),
            new Dictionary<uint, VisibleReflectionProbe>(),
            new Dictionary<uint, VisibleReflectionProbe>(),
            new Dictionary<uint, VisibleReflectionProbe>()
        };

        private VisibleReflectionProbe[] catchedReflectionProbes = new VisibleReflectionProbe[MAX_REFLECTION_PROBES_ON_SCREEN];
        private int catchedProbeCount = 0;
        private RTHandle[] textureRTHandles = new RTHandle[MAX_REFLECTION_PROBES_ON_SCREEN];
        private RenderTexture reflectionProbeAtlasRT;
        private RTHandle reflectionProbeAtlasHandle;
        private ReflectionProbeGlobalConstantBuffer globalConstantBuffer = default;
        private ComputeShader clusterCullingReflectionProbeCS;
        private ReflectionProbeData[] reflectionProbeDatas;
        private Matrix4x4 clipToView, viewToClip, clipToWorld;
        

        public const string PassName = "Cluster Culling Reflection Probe Pass";

        private const int MAX_REFLECTION_PROBES_ON_SCREEN = HNRenderPipelineAsset.MAX_REFLECTION_PROBES_ON_SCREEN;
        private const int REFLECTION_PROBE_ATLAS_SIZE = 4096;
        private const RenderTextureFormat REFLECTION_PROBE_ATLAS_FORMAT = RenderTextureFormat.RGB111110Float;
        private const TextureDimension REFLECTION_PROBE_ATLAS_DIMENSION = TextureDimension.Tex2D;
        private const FilterMode REFLECTION_PROBE_ATLAS_FILTER_MODE = FilterMode.Trilinear;
        private const TextureWrapMode REFLECTION_PROBE_ATLAS_WRAP_MODE = TextureWrapMode.Clamp;
        private const int REFLECTION_PROBE_ATLAS_MIP_COUNT = 8;
        private const int REFLECTION_PROBE_ATLAS_TEXEL_PADDING = 2;
        private const string REFLECTION_PROBE_ATLAS_NAME = "_ReflectionProbeAtlas";
        private const int MAX_CLUSTER_MASK_WORDS = 4096 * 4;
        private const int CLUSTER_MIN_TILE_SIZE = 8;
        private const int CLUSTER_MAX_Z_SLICE = 128;
        private const int CLUSTER_MIN_Z_SLIZE = 16;
        private const string CLUSTER_CULLING_CS_KERNEL_NAME = "ClusterCulling";


        public class ClusterCullingReflectionProbePassData
        {
            public TextureHandle reflectionProbeAtlas;
            public uint[] probeHash = new uint[MAX_REFLECTION_PROBES_ON_SCREEN];
            public int4[] scaleOffset = new int4[MAX_REFLECTION_PROBES_ON_SCREEN];
            public bool[] needUpdate = new bool[MAX_REFLECTION_PROBES_ON_SCREEN];
            public VisibleReflectionProbe[] probe = new VisibleReflectionProbe[MAX_REFLECTION_PROBES_ON_SCREEN];
            public bool[] isBilinear = new bool[MAX_REFLECTION_PROBES_ON_SCREEN];
            public TextureHandle[] textures = new TextureHandle[MAX_REFLECTION_PROBES_ON_SCREEN];
            // xyz:box max w:blend distance
            public Vector4[] reflectionProbeData0 = new Vector4[MAX_REFLECTION_PROBES_ON_SCREEN];
            // xyz:box min w:importance
            public Vector4[] reflectionProbeData1 = new Vector4[MAX_REFLECTION_PROBES_ON_SCREEN];
            // xyz:position w:intensity
            public Vector4[] reflectionProbeData2 = new Vector4[MAX_REFLECTION_PROBES_ON_SCREEN];
            public ComputeShader clusterCullingReflectionProbeCS;
            public int clusterCullingKernel;
            public ComputeBufferHandle clusterCullingReflectionProbeMaskBuffer;
            public ComputeBufferHandle reflectionProbeDatasBuffer;
        }

        [Serializable]
        public struct ReflectionProbeData
        {
            public float3 boundCenter;
            public float3 boundExtents;
        }


        public static class PropertyIDs
        {
            public static readonly int reflectionProbeAtlas = Shader.PropertyToID("_ReflectionProbeAtlas");
            public static readonly int reflectionProbeGlobalConstantBuffer = Shader.PropertyToID("ReflectionProbeVariablesGlobal");
            public static readonly int clusterCullingReflectionProbeMaskBuffer = Shader.PropertyToID("_ClusterCullingReflectionProbeMaskBuffer");
            public static readonly int reflectionProbeDatasBuffer = Shader.PropertyToID("_ReflectionProbeDatasBuffer");
            // x:z scale y:z offset z:wordsPerCluster w:isOrthographic
            public static readonly int cullingParams0 = Shader.PropertyToID("_CullingParams0");
            // xyz:clusterSize w:probeCount
            public static readonly int cullingParams1 = Shader.PropertyToID("_CullingParams1");
            public static readonly int cullingClipToViewMatrix = Shader.PropertyToID("_ClipToView");
            public static readonly int cullingViewToClipMatrix = Shader.PropertyToID("_ViewToClip");
            public static readonly int cullingClipToWorldMatrix = Shader.PropertyToID("_ClipToWorld");
        }


        unsafe public struct ReflectionProbeGlobalConstantBuffer
        {
            // xyz: boxMax w: blendDistance
            public fixed float reflectionProbeData0[MAX_REFLECTION_PROBES_ON_SCREEN * 4];
            // xyz: boxMin w: importance
            public fixed float reflectionProbeData1[MAX_REFLECTION_PROBES_ON_SCREEN * 4];
            // xyz: positionWS w: intensity
            public fixed float reflectionProbeData2[MAX_REFLECTION_PROBES_ON_SCREEN * 4];
            // xyzw: scaleOffset
            public fixed float reflectionProbeData3[MAX_REFLECTION_PROBES_ON_SCREEN * 4];
            // xy: X Y scale zw:Z scale offset
            public fixed float reflectionProbeParam0[4];
            // x: words per cluster
            public fixed float reflectionProbeParam1[4];
        }
    }
}
