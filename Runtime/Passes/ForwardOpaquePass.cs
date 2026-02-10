using System;
using System.Collections;
using System.Collections.Generic;
using Palmmedia.ReportGenerator.Core.Parser.Analysis;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace HN.HNRP
{
    [Serializable]
    public class ForwardOpaquePass : PassBase
    {
        public override void Record(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<ForwardOpaquePassData>($"{name}({PassName})", out var passData))
            {
                builder.AllowRendererListCulling(false);
                
                var textureHandles = renderingData.GraphData.textureHandles;
                var computeBufferHandles = renderingData.GraphData.computeBufferHandles;

                passData.colorTarget = builder.UseColorBuffer(textureHandles[colorTargetIndex], 0);
                if (textureHandles[depthTargetIndex].IsValid())
                {
                    passData.depthTarget = builder.UseDepthBuffer(textureHandles[depthTargetIndex], DepthAccess.ReadWrite);
                }

                passData.lightDatasBuffer = builder.ReadComputeBuffer(computeBufferHandles[lightDatasBufferIndex]);

                passData.reflectionProbeAtlas = builder.ReadTexture(textureHandles[reflectionProbeAtlasIndex]);
                passData.clusterCullingReflectionProbeMaskBuffer = builder.ReadComputeBuffer(computeBufferHandles[clusterCullingReflectionProbeMaskBufferIndex]);
                passData.clusterCullingReflectionProbeDatasBuffer = builder.ReadComputeBuffer(computeBufferHandles[clusterCullingReflectionProbeDatasBufferIndex]);

                passData.clusterCullingLightMaskBuffer = builder.ReadComputeBuffer(computeBufferHandles[clusterCullingLightMaskBufferIndex]);

                RendererListDesc rendererListDesc = HNRenderPipelineUtils.GetOpaqueRendererListDesc(ShaderPassNames.AllForwardNames, renderingData.CullingResults, renderingData.Camera, renderingLayerMask);
                passData.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(rendererListDesc));

                builder.SetRenderFunc(
                    (ForwardOpaquePassData data, RenderGraphContext ctx) =>
                    {
                        ctx.cmd.SetGlobalBuffer(BuildLightDataPass.PropertyIDs.lightDatasBuffer, passData.lightDatasBuffer);

                        ctx.cmd.SetGlobalTexture(ClusterCullingReflectionProbePass.PropertyIDs.reflectionProbeAtlas, data.reflectionProbeAtlas);
                        ctx.cmd.SetGlobalBuffer(ClusterCullingReflectionProbePass.PropertyIDs.clusterCullingReflectionProbeMaskBuffer, data.clusterCullingReflectionProbeMaskBuffer);
                        ctx.cmd.SetGlobalBuffer(ClusterCullingReflectionProbePass.PropertyIDs.clusterCullingReflectionProbeDatasBuffer, data.clusterCullingReflectionProbeDatasBuffer);

                        ctx.cmd.SetGlobalBuffer(ClusterCullingLightPass.PropertyIDs.clusterCullingLightMaskBuffer, data.clusterCullingLightMaskBuffer);
                        
                        ctx.cmd.DrawRendererList(data.rendererList);
                    }
                );

            }
        }

        public override void Cleanup()
        {
            
        }


        [SerializeField]
        public uint renderingLayerMask = 0x00000001;

        [SerializeField]
        public int colorTargetIndex = -1;

        [SerializeField]
        public int depthTargetIndex = -1;

        [SerializeField]
        public int lightDatasBufferIndex = -1;

        [SerializeField]
        public int reflectionProbeAtlasIndex = -1;

        [SerializeField]
        public int clusterCullingReflectionProbeMaskBufferIndex = -1;

        [SerializeField]
        public int clusterCullingReflectionProbeDatasBufferIndex = -1;

        [SerializeField]
        public int clusterCullingLightMaskBufferIndex = -1;


        public const string PassName = "Forward Opaque Pass";


        public class ForwardOpaquePassData : PassData
        {
            public TextureHandle colorTarget;
            public TextureHandle depthTarget;
            public ComputeBufferHandle lightDatasBuffer;
            public TextureHandle reflectionProbeAtlas;
            public ComputeBufferHandle clusterCullingReflectionProbeMaskBuffer;
            public ComputeBufferHandle clusterCullingReflectionProbeDatasBuffer;
            public ComputeBufferHandle clusterCullingLightMaskBuffer;
            public ComputeBufferHandle clusterCullingLightParamsBuffer;
            public RendererListHandle rendererList;

        }


        public static class PropertyIDs
        {
        }

    }

}

