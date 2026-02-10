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
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HN.HNRP
{
    [Serializable]
    public class ClusterCullingReflectionProbePass : PassBase
    {
        public override void OnCreate(HNRenderGraphBase hnRenderGraph, string passName)
        {
            base.OnCreate(hnRenderGraph, passName);

            reflectionProbeAtlasIndex = hnRenderGraph.RegistAndGetTextureHandleIndex();
            clusterCullingReflectionProbeMaskBufferIndex = hnRenderGraph.RegistAndGetComputeBufferHandleIndex();
            clusterCullingReflectionProbeDatasBufferIndex = hnRenderGraph.RegistAndGetComputeBufferHandleIndex();

#if UNITY_EDITOR
            clusterCullingReflectionProbeCS = AssetDatabase.LoadAssetAtPath<ComputeShader>(HNRenderPipelineGlobalSettings.HNRenderPipelinePath + CLUSTER_CULLING_CS_PATH);
#endif
        }

        public override void Record(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            if(clusterCullingReflectionProbeCS == null)
            {
                Debug.LogError("Cluster Culling Reflection Probe Computer Shader is Null.");
                return;
            }

            using (var builder = renderGraph.AddRenderPass<ClusterCullingReflectionProbePassData>($"{name}({PassName})", out var passData))
            {
                builder.AllowPassCulling(false);

                // TODO: 渲染ReflectionProbe时的环境反射使用默认ReflectionProbe
                if(renderingData.Camera.cameraType == CameraType.Reflection)
                {
                    return;
                }
                
                // 清空当前帧可见probe的引用
                ClearProbesRef();

                // 更新当前帧可见probe的引用
                UpdateProbeRefs(ref renderingData.visibleReflectionProbes);

                // 从当前帧可见probe的引用中catch需要渲染的probe
                CatcheProbes(passData, ref catchedReflectionProbes);

                // 更新当前帧需要渲染的probe的数据
                UpdateReflectionProbeData(passData);

                // 将当前帧需要更新的probe的texture导入render graph
                ImportProbeTextures(renderGraph, passData);
                
                // 创建当前帧的reflection probe atlas
                passData.reflectionProbeAtlas = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(REFLECTION_PROBE_ATLAS_SIZE, REFLECTION_PROBE_ATLAS_SIZE, false, false)
                {
                    name = REFLECTION_PROBE_ATLAS_NAME,
                    colorFormat = REFLECTION_PROBE_ATLAS_FORMAT,
                    dimension = REFLECTION_PROBE_ATLAS_DIMENSION,
                    useMipMap = true,
                    autoGenerateMips = false,
                    filterMode = REFLECTION_PROBE_ATLAS_FILTER_MODE,
                    wrapMode = REFLECTION_PROBE_ATLAS_WRAP_MODE
                }));
                renderingData.GraphData.textureHandles.Add(passData.reflectionProbeAtlas);

                // 创建当前帧的mask buffer
                passData.clusterCullingReflectionProbeMaskBuffer = builder.WriteComputeBuffer(renderGraph.CreateComputeBuffer(
                    new ComputeBufferDesc(
                        MAX_CLUSTER_MASK_WORDS,
                        sizeof(uint)
                    ) { name = "Cluster Culling Reflection Probe Mask Buffer" }
                ));
                renderingData.GraphData.computeBufferHandles.Add(passData.clusterCullingReflectionProbeMaskBuffer);

                // 创建当前帧的global constant buffer
                passData.clusterCullingReflectionProbeDatasBuffer = builder.WriteComputeBuffer(renderGraph.CreateComputeBuffer(
                    new ComputeBufferDesc(
                        MAX_REFLECTION_PROBES_ON_SCREEN,
                        UnsafeUtility.SizeOf<ClusterCullingReflectionProbeDatas>()
                    ) { name = "Cluster Culling Reflection Probe Datas Buffer" }
                ));
                renderingData.GraphData.computeBufferHandles.Add(passData.clusterCullingReflectionProbeDatasBuffer);

                // 单个cluster中可见probe的最大数量
                int itemsPerCluster = MAX_REFLECTION_PROBES_ON_SCREEN;

                // 单个cluster中所需的words 
                int wordsPerCluster = (itemsPerCluster + 31) / 32 + 1/* 1 for header */;

                Camera camera = renderingData.Camera;
                HNAdditionalCameraData cameraData = renderingData.CameraData;
                int2 screenResolution = math.int2(camera.pixelWidth, camera.pixelHeight);
                
                // 计算当前帧三个方向的cluster数量
                int3 clusterSize = GetClusterSize(screenResolution);

                // 当前帧总cluster的数量
                int clusterCount = clusterSize.x * clusterSize.y * clusterSize.z;
                
                // 计算cluster Z方向的scaleoffset
                float2 clusterZScaleOffset = GetClusterZScaleOffset(clusterSize, camera.orthographic, camera.nearClipPlane, camera.farClipPlane);
                
                // 更新当前帧渲染需要的cluster数据
                UpdateReflectionProbeParams(passData, clusterSize, clusterZScaleOffset, wordsPerCluster);

                // 获取计算cluster culling所需的矩阵
                GetCameraMatrix(camera);

                //获取cluster所需的compute shader
                passData.clusterCullingReflectionProbeCS = clusterCullingReflectionProbeCS;
                passData.clusterCullingKernel = passData.clusterCullingReflectionProbeCS.FindKernel(CLUSTER_CULLING_CS_KERNEL_NAME);
                
                // 获取计算mask所需的probe数据
                GetReflectionProbeDatas4CS();

                // 创建计算mask所需的probe数据的buffer
                passData.reflectionProbeDatas4CSBuffer = builder.WriteComputeBuffer(renderGraph.CreateComputeBuffer(
                    new ComputeBufferDesc(
                        reflectionProbeDatas4CS.Length,
                        UnsafeUtility.SizeOf<ReflectionProbeData4CS>()
                    ) { name = "Cluster Culling Reflection Probe Datas for CS Buffer" }
                ));

                builder.SetRenderFunc(
                    (ClusterCullingReflectionProbePassData data, RenderGraphContext ctx) =>
                    {
                        ctx.cmd.EnableShaderKeyword(GlobalKeywords.clusterCullingReflectionProbe);
                        
                        if(isEmpty)
                        {
                            for(int mipLevel = 0; mipLevel < REFLECTION_PROBE_ATLAS_MIP_COUNT; mipLevel++)
                            {
                                ctx.cmd.SetRenderTarget(data.reflectionProbeAtlas, mipLevel, CubemapFace.Unknown, 0);
                                ctx.cmd.ClearRenderTarget(false, true, Color.black);
                            }
                        }
                        else
                        {
                            for(int i = 0; i < MAX_REFLECTION_PROBES_ON_SCREEN; i++)
                            {
                                if(data.needUpdate[i])
                                {
                                    int texelPadding = REFLECTION_PROBE_ATLAS_TEXEL_PADDING;
                                    Vector4 scaleOffset = GetTextureScaleOffsetWithoutPaddingInAtlas(data.scaleOffset[i]);
                                    Vector2 textureSizeWithoutPadding = GetTextureSizeWithoutpadding(scaleOffset, texelPadding);
                                    
                                    for(int mipLevel = 0; mipLevel < REFLECTION_PROBE_ATLAS_MIP_COUNT; mipLevel++)
                                    {
                                        ctx.cmd.SetRenderTarget(data.reflectionProbeAtlas, mipLevel, CubemapFace.Unknown, 0);
                                        var propertyBlock = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                                        Blitter.BlitCubeToOctahedral2DQuadWithPadding(ctx.cmd, propertyBlock, data.textures[i], textureSizeWithoutPadding, scaleOffset, mipLevel, data.isBilinear[i], texelPadding);
                                        texelPadding *= 2;
                                    }
                                }
                            }

                            ctx.cmd.SetComputeBufferParam(data.clusterCullingReflectionProbeCS, data.clusterCullingKernel, PropertyIDs.clusterCullingReflectionProbeMaskBuffer, data.clusterCullingReflectionProbeMaskBuffer);
                            ctx.cmd.SetBufferData(data.reflectionProbeDatas4CSBuffer, reflectionProbeDatas4CS);
                            ctx.cmd.SetComputeBufferParam(data.clusterCullingReflectionProbeCS, data.clusterCullingKernel, PropertyIDs.reflectionProbeDatas4CSBuffer, data.reflectionProbeDatas4CSBuffer);
                            ctx.cmd.SetComputeVectorParam(data.clusterCullingReflectionProbeCS, PropertyIDs.cullingParams0, new Vector4(clusterZScaleOffset.x, clusterZScaleOffset.y, wordsPerCluster, camera.orthographic ? 1.0f : 0.0f));
                            ctx.cmd.SetComputeVectorParam(data.clusterCullingReflectionProbeCS, PropertyIDs.cullingParams1, new Vector4(clusterSize.x, clusterSize.y, clusterSize.z, catchedProbeCount));
                            ctx.cmd.SetComputeMatrixParam(data.clusterCullingReflectionProbeCS, PropertyIDs.cullingClipToViewMatrix, clipToView);
                            ctx.cmd.SetComputeMatrixParam(data.clusterCullingReflectionProbeCS, PropertyIDs.cullingViewToClipMatrix, viewToClip);
                            ctx.cmd.SetComputeMatrixParam(data.clusterCullingReflectionProbeCS, PropertyIDs.cullingClipToWorldMatrix, clipToWorld);
                            int threadGroup = (clusterCount + 63) / 64;
                            int threadGroupY = (threadGroup + clusterSize.y - 1) / clusterSize.y;
                            ctx.cmd.DispatchCompute(data.clusterCullingReflectionProbeCS, data.clusterCullingKernel, clusterSize.y, threadGroupY, 1);
                        }

                        ctx.cmd.SetBufferData(data.clusterCullingReflectionProbeDatasBuffer, clusterCullingReflectionProbeDatas);
                        ConstantBuffer.PushGlobal(ctx.cmd, data.clusterCullingReflectionProbeParams, PropertyIDs.clusterCullingReflectionProbeParamsBuffer);
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
        }


        /// <summary>
        /// 清空当前帧可见probe的引用
        /// </summary>
        private void ClearProbesRef()
        {
            foreach(var probesDict in refProbes)
            {
                probesDict.Clear();
            }
        }

        /// <summary>
        /// 更新当前帧可见probe的引用
        /// </summary>
        /// <param name="visibleReflectionProbes"></param>
        private void UpdateProbeRefs(ref NativeArray<VisibleReflectionProbe> visibleReflectionProbes)
        {
            var reflectionProbes = visibleReflectionProbes;
            if(reflectionProbes.Length == 0)
            {
                isEmpty = true;
            }
            else
            {
                isEmpty = false;
                for(int i = 0; i < reflectionProbes.Length; i++)
                {
                    var probe = reflectionProbes[i];
                    if(probe.texture == null)
                        continue;
                    
                    var probeData = probe.reflectionProbe.GetHNAdditionalReflectionProbeData();
                    UpdateProbeRef(probe, probeData);
                }
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

        /// <summary>
        /// 从当前帧可见probe的引用中catch需要渲染的probe
        /// </summary>
        /// <param name="passData"></param>
        /// <param name="catchedReflectionProbes"></param>
        private void CatcheProbes(ClusterCullingReflectionProbePassData passData, ref VisibleReflectionProbe[] catchedReflectionProbes)
        {
            catchedProbeCount = 0;
            uint offsetMask = 0;
            uint maxOffsetMask = 0x00FFC000; // 0000 0000 1111 1111 1100 0000 0000 0000
            int maxCount = MAX_REFLECTION_PROBES_ON_SCREEN;
            for(int i = 0; i < refProbes.Length; i++)
            {
                int refIndex = 0;
                var hashes = refProbes[i].Keys.ToList();
                while(refProbes[i].Count > 0 && refIndex < hashes.Count && refIndex < maxCount && offsetMask < maxOffsetMask)
                {
                    int width = 4096 / (int)Mathf.Pow(2, i);
                    GetOffset(offsetMask, out int offsetX, out int offsetY);
                    int4 scaleOffset = new int4(width, width, offsetX, offsetY);
                    if(passData.probeHash[catchedProbeCount] != hashes[refIndex])
                    {
                        passData.scaleOffset[catchedProbeCount] = scaleOffset;
                        passData.needUpdate[catchedProbeCount] = true;
                        passData.probeHash[catchedProbeCount] = hashes[refIndex];
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
                    passData.probe[catchedProbeCount] = refProbes[i][hashes[refIndex]];
                    refIndex++;
                    catchedProbeCount++;
                    offsetMask += (uint)1 << (int)(Mathf.Log(width, 2) * 2 - 2);
                }
            }
            catchedReflectionProbes = passData.probe;

            int dataIndex = catchedProbeCount;
            while(dataIndex < MAX_REFLECTION_PROBES_ON_SCREEN)
            {
                // if(passData.probeHash[dataIndex] != 0u)
                // {
                //     passData.needUpdate[dataIndex] = true;
                // }
                // else
                // {
                    passData.needUpdate[dataIndex] = false;
                // }
                passData.probeHash[dataIndex] = 0u;
                passData.scaleOffset[dataIndex] = new int4(0, 0, 0, 0);
                passData.probe[dataIndex] = default;
                dataIndex++;
            }
        }

        /// <summary>
        /// 将当前帧需要更新的probe的texture导入render graph
        /// </summary>
        /// <param name="renderGraph"></param>
        /// <param name="passData"></param>
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

        /// <summary>
        /// 更新当前帧需要渲染的probe的数据
        /// </summary>
        /// <param name="passData"></param>
        private void UpdateReflectionProbeData(ClusterCullingReflectionProbePassData passData)
        {
            for(int i = 0; i < MAX_REFLECTION_PROBES_ON_SCREEN; i++)
            {
                var probe = passData.probe[i];
                if(probe.texture == null)
                    continue;
                Vector4 scaleOffsetNormalized = GetTextureScaleOffsetWithoutPaddingInAtlas(passData.scaleOffset[i]);
                clusterCullingReflectionProbeDatas[i].boxMax = probe.bounds.max;
                clusterCullingReflectionProbeDatas[i].blendDistance = probe.blendDistance;
                clusterCullingReflectionProbeDatas[i].boxMin = probe.bounds.min;
                clusterCullingReflectionProbeDatas[i].importance = probe.importance;
                clusterCullingReflectionProbeDatas[i].positionWS = new Vector3(probe.localToWorldMatrix.m03, probe.localToWorldMatrix.m13, probe.localToWorldMatrix.m23);
                clusterCullingReflectionProbeDatas[i].intensity = probe.reflectionProbe.intensity;
                clusterCullingReflectionProbeDatas[i].scaleOffset = scaleOffsetNormalized;
            }
        }

        /// <summary>
        /// 计算当前帧三个方向的cluster数量
        /// </summary>
        /// <param name="screenResolution"></param>
        /// <returns></returns>
        private int3 GetClusterSize(int2 screenResolution)
        {
            int2 clusterSizeXY = new int2(1, 1);
            int sliceCount = CLUSTER_MIN_Z_SLICE;
            int tileWidth = CLUSTER_MIN_TILE_SIZE >> 1;
            do
            {
                tileWidth <<= 1;
                clusterSizeXY = (screenResolution + tileWidth - 1) / tileWidth;
                int tileCountPerSlice = clusterSizeXY.x * clusterSizeXY.y;
                sliceCount = MAX_CLUSTER_MASK_WORDS / tileCountPerSlice - 1;
            }
            while(sliceCount < CLUSTER_MIN_Z_SLICE || sliceCount > CLUSTER_MAX_Z_SLICE);
            return new int3(clusterSizeXY.x, clusterSizeXY.y, sliceCount);
        }

        /// <summary>
        /// 计算cluster Z方向的scaleoffset
        /// </summary>
        /// <param name="clusterSize"></param>
        /// <param name="isOrthographic"></param>
        /// <param name="nearClipPlane"></param>
        /// <param name="farClipPlane"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 获取计算mask所需的probe数据
        /// </summary>
        private void GetReflectionProbeDatas4CS()
        {
            for(int i = 0; i < MAX_REFLECTION_PROBES_ON_SCREEN; i++)
            {
                reflectionProbeDatas4CS[i].boundCenter = catchedReflectionProbes[i].bounds.center;
                reflectionProbeDatas4CS[i].boundExtents = catchedReflectionProbes[i].bounds.extents;
            }
        }

        /// <summary>
        /// 更新当前帧渲染需要的cluster数据
        /// </summary>
        /// <param name="passData"></param>
        /// <param name="clusterSize"></param>
        /// <param name="clusterZScaleOffset"></param>
        /// <param name="wordsPerCluster"></param>
        private void UpdateReflectionProbeParams(ClusterCullingReflectionProbePassData passData, int3 clusterSize, float2 clusterZScaleOffset, int wordsPerCluster)
        {
            passData.clusterCullingReflectionProbeParams.clusterSizeXY = new Vector2(clusterSize.x, clusterSize.y);
            passData.clusterCullingReflectionProbeParams.clusterZScaleOffset = new Vector2(clusterZScaleOffset.x, clusterZScaleOffset.y);
            passData.clusterCullingReflectionProbeParams.wordsPerCluster = wordsPerCluster;
            passData.clusterCullingReflectionProbeParams.reflectionProbeCount = catchedProbeCount;
        }

        /// <summary>
        /// 获取计算cluster所需的矩阵
        /// </summary>
        /// <param name="camera"></param>
        private void GetCameraMatrix(Camera camera)
        {
            clipToView = camera.projectionMatrix;
            viewToClip = camera.projectionMatrix.inverse;
            clipToWorld = (camera.worldToCameraMatrix * camera.projectionMatrix).inverse;
        }


        [SerializeField]
        public int reflectionProbeAtlasIndex = -1;

        [SerializeField]
        public int clusterCullingReflectionProbeMaskBufferIndex = -1;

        [SerializeField]
        public int clusterCullingReflectionProbeDatasBufferIndex = -1;


        // 当前帧从剔除结果获取的reflection probe列表
        private Dictionary<uint, VisibleReflectionProbe>[] refProbes = new Dictionary<uint, VisibleReflectionProbe>[5]
        {
            new Dictionary<uint, VisibleReflectionProbe>(),
            new Dictionary<uint, VisibleReflectionProbe>(),
            new Dictionary<uint, VisibleReflectionProbe>(),
            new Dictionary<uint, VisibleReflectionProbe>(),
            new Dictionary<uint, VisibleReflectionProbe>()
        };

        // 计算剔除的Compute Shader
        [SerializeField]
        private ComputeShader clusterCullingReflectionProbeCS;


        // 当前帧是否不存在可见的reflection probe
        private bool isEmpty = false;

        // 按照分辨率排序catch 当前帧需要渲染的reflection probe
        private VisibleReflectionProbe[] catchedReflectionProbes = new VisibleReflectionProbe[MAX_REFLECTION_PROBES_ON_SCREEN];
        
        // 记录当前帧需要渲染的reflection probe数量
        private int catchedProbeCount = 0;

        // 当前帧需要渲染的reflection probe的texture handles
        private RTHandle[] textureRTHandles = new RTHandle[MAX_REFLECTION_PROBES_ON_SCREEN];
        
        // 计算剔除的Compute Shader所需的Reflection Probe的数据
        private ReflectionProbeData4CS[] reflectionProbeDatas4CS = new ReflectionProbeData4CS[MAX_REFLECTION_PROBES_ON_SCREEN];

        // reflection probe渲染所需的global数据
        public ClusterCullingReflectionProbeDatas[] clusterCullingReflectionProbeDatas = new ClusterCullingReflectionProbeDatas[MAX_REFLECTION_PROBES_ON_SCREEN];

        // 计算剔除的Compute Shader所需的矩阵
        private Matrix4x4 clipToView, viewToClip, clipToWorld;
        

        public const string PassName = "Cluster Culling Reflection Probe Pass";

        // 当前帧屏幕可见的Reflection Probe的最大数量 与Input.hlsl中同步
        private const int MAX_REFLECTION_PROBES_ON_SCREEN = 64;
        
        // reflection probe atlas的尺寸 与Input.hlsl中同步
        private const int REFLECTION_PROBE_ATLAS_SIZE = 4096;

        // reflection probe atlas的格式
        private const GraphicsFormat REFLECTION_PROBE_ATLAS_FORMAT = GraphicsFormat.B10G11R11_UFloatPack32;

        // reflection probe atlas的Dimension
        private const TextureDimension REFLECTION_PROBE_ATLAS_DIMENSION = TextureDimension.Tex2D;

        // reflection probe atlas的Filter Mode
        private const FilterMode REFLECTION_PROBE_ATLAS_FILTER_MODE = FilterMode.Trilinear;

        // reflection probe atlas的Wrap Mode
        private const TextureWrapMode REFLECTION_PROBE_ATLAS_WRAP_MODE = TextureWrapMode.Clamp;

        // reflection probe atlas的mip数量 与Input.hlsl中同步
        private const int REFLECTION_PROBE_ATLAS_MIP_COUNT = 7;

        // reflection probe atlas中每张贴图的padding值 与Input.hlsl中同步
        private const int REFLECTION_PROBE_ATLAS_TEXEL_PADDING = 2;

        // reflection probe atlas的名字
        private const string REFLECTION_PROBE_ATLAS_NAME = "_ReflectionProbeAtlas";

        // mask buffer的最大尺寸（1 words = 32 bit）
        private const int MAX_CLUSTER_MASK_WORDS = 4096 * 4;

        // 单个cluster最小尺寸（pixel）
        private const int CLUSTER_MIN_TILE_SIZE = 8;

        // cluster Z方向最大切分数量
        private const int CLUSTER_MAX_Z_SLICE = 128;

        // cluster Z方向最小切分数量
        private const int CLUSTER_MIN_Z_SLICE = 16;

        // cluster culling compute shader中的kernel名
        private const string CLUSTER_CULLING_CS_KERNEL_NAME = "ClusterCullingReflectionProbeCS";

        // cluster culling compute shader的路径
        private const string CLUSTER_CULLING_CS_PATH = "Runtime/ShaderLibrary/ComputeShaders/ClusterCullingReflectionProbeCS.compute";


        public class ClusterCullingReflectionProbePassData
        {
            // reflection probe atlas handle
            public TextureHandle reflectionProbeAtlas;

            // 当前帧渲染的每个probe的hash 没有probe为0
            public uint[] probeHash = new uint[MAX_REFLECTION_PROBES_ON_SCREEN];

            // 当前帧渲染的每个probe的lod0的texture在reflection probe atlas中的实际尺寸 单位：pixel
            public int4[] scaleOffset = new int4[MAX_REFLECTION_PROBES_ON_SCREEN];

            // 当前帧渲染的每个probe是否需要更新 非realtime的probe的texture不需要每帧更新
            public bool[] needUpdate = new bool[MAX_REFLECTION_PROBES_ON_SCREEN];

            // 当前帧渲染的每个probe的数据
            public VisibleReflectionProbe[] probe = new VisibleReflectionProbe[MAX_REFLECTION_PROBES_ON_SCREEN];

            // 当前帧渲染的每个probe的texture是否是Bilinear
            public bool[] isBilinear = new bool[MAX_REFLECTION_PROBES_ON_SCREEN];

            // 当前帧渲染的每个probe的texture handle （需要render graph导入后blit到atlas上）
            public TextureHandle[] textures = new TextureHandle[MAX_REFLECTION_PROBES_ON_SCREEN];

            // reflection probe渲染所需数据
            // xyz:box max w:blend distance
            public Vector4[] reflectionProbeData0 = new Vector4[MAX_REFLECTION_PROBES_ON_SCREEN];

            // reflection probe渲染所需数据
            // xyz:box min w:importance
            public Vector4[] reflectionProbeData1 = new Vector4[MAX_REFLECTION_PROBES_ON_SCREEN];

            // reflection probe渲染所需数据
            // xyz:position w:intensity
            public Vector4[] reflectionProbeData2 = new Vector4[MAX_REFLECTION_PROBES_ON_SCREEN];

            // 计算cluster culling的compute shader
            public ComputeShader clusterCullingReflectionProbeCS;

            // 计算cluster culling的compute shader的kernel
            public int clusterCullingKernel;

            // cluster culling计算出的mask buffer
            public ComputeBufferHandle clusterCullingReflectionProbeMaskBuffer;

            // global constant buffer
            public ComputeBufferHandle clusterCullingReflectionProbeDatasBuffer;

            public ClusterCullingReflectionProbeParams clusterCullingReflectionProbeParams = default;

            // cluster culling计算所需的数据
            public ComputeBufferHandle reflectionProbeDatas4CSBuffer;
        }


        // cluster culling计算所需的数据结构
        [Serializable]
        public struct ReflectionProbeData4CS
        {
            // 当前probe的bound中心 world space
            public float3 boundCenter;

            // 当前probe的bound的extent world space
            public float3 boundExtents;
        }


        // probe渲染所需的global数据
        [Serializable]
        unsafe public struct ClusterCullingReflectionProbeDatas
        {
            public Vector3 boxMax;
            public float blendDistance;
            public Vector3 boxMin;
            public float importance;
            public Vector3 positionWS;
            public float intensity;
            public Vector4 scaleOffset;
        }


        [Serializable]
        unsafe public struct ClusterCullingReflectionProbeParams
        {
            public Vector2 clusterSizeXY;
            public Vector2 clusterZScaleOffset;
            public int wordsPerCluster;
            public int reflectionProbeCount;
            public float unused0;
            public float unused1;
        }


        public static class PropertyIDs
        {
            public static readonly int reflectionProbeAtlas = Shader.PropertyToID("_ReflectionProbeAtlas");
            public static readonly int clusterCullingReflectionProbeDatasBuffer = Shader.PropertyToID("_ClusterCullingReflectionProbeDatasBuffer");
            public static readonly int clusterCullingReflectionProbeMaskBuffer = Shader.PropertyToID("_ClusterCullingReflectionProbeMaskBuffer");
            public static readonly int clusterCullingReflectionProbeParamsBuffer = Shader.PropertyToID("_ClusterCullingReflectionProbeParamsBuffer");
            public static readonly int reflectionProbeDatas4CSBuffer = Shader.PropertyToID("_ClusterCullingReflectionProbeDatas4CSBuffer");
            // x:z scale y:z offset z:wordsPerCluster w:isOrthographic
            public static readonly int cullingParams0 = Shader.PropertyToID("_ClusterCullingReflectionProbeParams0");
            // xyz:clusterSize w:probeCount
            public static readonly int cullingParams1 = Shader.PropertyToID("_ClusterCullingReflectionProbeParams1");
            public static readonly int cullingClipToViewMatrix = Shader.PropertyToID("_ClusterCullingReflectionProbeClipToView");
            public static readonly int cullingViewToClipMatrix = Shader.PropertyToID("_ClusterCullingReflectionProbeViewToClip");
            public static readonly int cullingClipToWorldMatrix = Shader.PropertyToID("_ClusterCullingReflectionProbeClipToWorld");
        }
    }
}
