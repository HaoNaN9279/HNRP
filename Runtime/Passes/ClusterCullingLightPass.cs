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
    public class ClusterCullingLightPass : PassBase
    {
        public override void OnCreate(HNRenderGraphBase hnRenderGraph, string passName)
        {
            base.OnCreate(hnRenderGraph, passName);
 
            clusterCullingLightMaskBufferIndex = hnRenderGraph.RegistAndGetComputeBufferHandleIndex();

#if UNITY_EDITOR
            clusterCullingLightCS = AssetDatabase.LoadAssetAtPath<ComputeShader>(HNRenderPipelineGlobalSettings.HNRenderPipelinePath + CLUSTER_CULLING_CS_PATH);
#endif
        }

        public override void Record(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            if(clusterCullingLightCS == null)
            {
                Debug.LogError("Cluster Culling Light Compute Shader is Null.");
                return;
            }

            using (var builder = renderGraph.AddRenderPass<ClusterCullingLightPassData>($"{name}({PassName})", out var passData))
            {
                builder.AllowPassCulling(false);

                passData.clusterCullingLightMaskBuffer = builder.WriteComputeBuffer(renderGraph.CreateComputeBuffer(
                    new ComputeBufferDesc(
                        MAX_CLUSTER_MASK_WORDS,
                        sizeof(uint)
                    ) { name = "Cluster Culling Light Mask Buffer" }
                ));
                renderingData.GraphData.computeBufferHandles.Add(passData.clusterCullingLightMaskBuffer);

                // 读取传入的light datas buffer
                var computeBufferHandles = renderingData.GraphData.computeBufferHandles;
                passData.lightDatasBuffer = builder.ReadComputeBuffer(computeBufferHandles[lightDatasBufferIndex]);

                // 获取当前帧所有可见的light数量
                catchedLightCount = Math.Min(renderingData.visibleLights.Length, MAX_LIGHT_ON_SCREEN);
                int directionalLightCount = 0, localLightCount = 0;
                for(int i = 0; i < catchedLightCount; i++)
                {
                    var light = renderingData.visibleLights[i];
                    if(light.lightType == LightType.Directional)
                    {
                        directionalLightCount++;
                    }
                    if(light.lightType == LightType.Point || light.lightType == LightType.Spot)
                    {
                        localLightCount++;
                    }
                }
                
                if(directionalLightCount > 0)
                    directionalLightCount -= 1;

                // 单个Cluster中可见光的最大数量
                int itemsPerCluster = MAX_LIGHT_ON_SCREEN;

                // 单个cluster中所需的words
                int wordsPerCluster = (itemsPerCluster + 31) / 32 + 1/* 1 for header */;

                Camera camera = renderingData.Camera;
                HNAdditionalCameraData cameraData = renderingData.CameraData;
                int2 screenResolution = math.int2(camera.pixelWidth, camera.pixelHeight);

                // 计算当前帧三个方向的cluster数量
                int3 clusterSize = GetClusterSize(screenResolution);

                // 当前帧总cluster的数量
                int clusterCount = clusterSize.x * clusterSize.y * clusterSize.z;

                // 计算cluster Z方向的scaleOffset
                float2 clusterZScaleOffset = GetClusterZScaleOffset(clusterSize, camera.orthographic, camera.nearClipPlane, camera.farClipPlane);
            
                // 更新当前帧渲染需要的cluster数据
                UpdateClusterCullingLightParams(passData, clusterSize, clusterZScaleOffset, wordsPerCluster, directionalLightCount, localLightCount);

                // 获取计算cluster culling所需的矩阵
                GetCameraMatrix(camera);

                // 获取cluster所需的compute shader
                passData.clusterCullingLightCS = clusterCullingLightCS;
                passData.clusterCullingLightKernel = passData.clusterCullingLightCS.FindKernel(CLUSTER_CULLING_CS_KERNEL_NAME);

                builder.SetRenderFunc(
                    (ClusterCullingLightPassData data, RenderGraphContext ctx) =>
                    {
                        ctx.cmd.EnableShaderKeyword(GlobalKeywords.clusterCullingLight);

                        ctx.cmd.SetComputeBufferParam(data.clusterCullingLightCS, data.clusterCullingLightKernel, PropertyIDs.clusterCullingLightMaskBuffer, data.clusterCullingLightMaskBuffer);
                        ctx.cmd.SetComputeVectorParam(data.clusterCullingLightCS, PropertyIDs.cullingParams0, new Vector4(clusterZScaleOffset.x, clusterZScaleOffset.y, wordsPerCluster, camera.orthographic ? 1.0f : 0.0f));
                        ctx.cmd.SetComputeVectorParam(data.clusterCullingLightCS, PropertyIDs.cullingParams1, new Vector4(clusterSize.x, clusterSize.y, clusterSize.z, catchedLightCount));
                        ctx.cmd.SetComputeMatrixParam(data.clusterCullingLightCS, PropertyIDs.cullingClipToViewMatrix, clipToView);
                        ctx.cmd.SetComputeMatrixParam(data.clusterCullingLightCS, PropertyIDs.cullingViewToClipMatrix, viewToClip);
                        ctx.cmd.SetComputeMatrixParam(data.clusterCullingLightCS, PropertyIDs.cullingClipToWorldMatrix, clipToWorld);
                        int threadGroup = (clusterCount + 63) / 64;
                        int threadGroupY = (threadGroup + clusterSize.y - 1) / clusterSize.y;
                        ctx.cmd.SetComputeBufferParam(data.clusterCullingLightCS, data.clusterCullingLightKernel, BuildLightDataPass.PropertyIDs.lightDatasBuffer, data.lightDatasBuffer);
                        ctx.cmd.DispatchCompute(data.clusterCullingLightCS, data.clusterCullingLightKernel, clusterSize.y, threadGroupY, 1);

                        ConstantBuffer.PushGlobal(ctx.cmd, data.clusterCullingLightParams, PropertyIDs.clusterCullingLightParamsBuffer);
                    }
                );
            }
        }

        public override void Cleanup()
        {
            
        }


        [SerializeField]
        public int lightDatasBufferIndex = -1;

        [SerializeField]
        public int clusterCullingLightMaskBufferIndex = -1;

        [SerializeField]
        public int clusterCullingLightParamsBufferIndex = -1;

        /// <summary>
        /// 计算当前帧三个方向的cluster数量
        /// </summary>
        /// <param name="screenResolution"></param>
        /// <returns></returns>
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
        /// 更新当前帧渲染需要的cluster数据
        /// </summary>
        /// <param name="passData"></param>
        /// <param name="clusterSize"></param>
        /// <param name="clusterZScaleOffset"></param>
        /// <param name="wordsPerCluster"></param>
        private void UpdateClusterCullingLightParams(ClusterCullingLightPassData passData, int3 clusterSize, float2 clusterZScaleOffset, int wordsPerCluster, int directionalLightCount, int localLightCount)
        {
            passData.clusterCullingLightParams.clusterSize = new Vector2(clusterSize.x, clusterSize.y);
            passData.clusterCullingLightParams.clusterZScaleOffset = new Vector2(clusterZScaleOffset.x, clusterZScaleOffset.y);
            passData.clusterCullingLightParams.wordsPerCluster = wordsPerCluster;
            passData.clusterCullingLightParams.directionalLightCount = directionalLightCount;
            passData.clusterCullingLightParams.localLightCount = localLightCount;
            passData.clusterCullingLightParams.unused = 0;
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
        private ComputeShader clusterCullingLightCS;

        private int catchedLightCount = 0;

        private Matrix4x4 clipToView, viewToClip, clipToWorld;

        // 当前帧需要处理的local light的数据
        private NativeArray<VisibleLight> visibleLights;


        public const string PassName = "Cluster Culling Light Pass";

        private const string CLUSTER_CULLING_CS_PATH = "Runtime/ShaderLibrary/ComputeShaders/ClusterCullingLightCS.compute";
        private const int MAX_CLUSTER_MASK_WORDS = 4096 * 4;
        private const int MAX_LIGHT_ON_SCREEN = HNRenderPipelineAsset.MAX_DIRECTIONAL_LIGHT_ON_SCREEN + HNRenderPipelineAsset.MAX_LOCAL_LIGHT_ON_SCREEN;
        private const int CLUSTER_MIN_Z_SLIZE = 16;
        private const int CLUSTER_MAX_Z_SLICE = 128;
        private const string CLUSTER_CULLING_CS_KERNEL_NAME = "ClusterCullingLightCS";


        public class ClusterCullingLightPassData
        {
            // light datas
            public ComputeBufferHandle lightDatasBuffer;

            // cluster culling计算出的mask buffer
            public ComputeBufferHandle clusterCullingLightMaskBuffer;

            // cluster culling light渲染所需的global数据
            public ClusterCullingLightParams clusterCullingLightParams;

            // 计算cluster culling的compute shader
            public ComputeShader clusterCullingLightCS;

            // 计算cluster culling的compute shader的kernel
            public int clusterCullingLightKernel;
        }


        unsafe public struct ClusterCullingLightParams
        {
            public Vector2 clusterSize;
            public Vector2 clusterZScaleOffset;
            public int wordsPerCluster;
            public int directionalLightCount;
            public int localLightCount;
            public float unused;
        }


        public static class PropertyIDs
        {
            public static readonly int clusterCullingLightMaskBuffer = Shader.PropertyToID("_ClusterCullingLightMaskBuffer");
            public static readonly int clusterCullingLightParamsBuffer = Shader.PropertyToID("_ClusterCullingLightParamsBuffer");
            // x:z scale y:z offset z:wordsPerCluster w:isOrthographic
            public static readonly int cullingParams0 = Shader.PropertyToID("_ClusterCullingLightParams0");
            // xyz:clusterSize w:probeCount
            public static readonly int cullingParams1 = Shader.PropertyToID("_ClusterCullingLightParams1");
            public static readonly int cullingClipToViewMatrix = Shader.PropertyToID("_ClusterCullingLightClipToView");
            public static readonly int cullingViewToClipMatrix = Shader.PropertyToID("_ClusterCullingLightViewToClip");
            public static readonly int cullingClipToWorldMatrix = Shader.PropertyToID("_ClusterCullingLightClipToWorld");
        }
    }
}
