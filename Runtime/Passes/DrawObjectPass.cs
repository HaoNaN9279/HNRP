using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace HN.HNRP
{
    [Serializable]
    public class DrawObjectPass : PassBase
    {
        public override void OnCreate(HNRenderGraphBase hnRenderGraph, string passName)
        {
            base.OnCreate(hnRenderGraph, passName);

            colorTargetSlot = new TexturePassSlot(hnRenderGraph, PassSlotType.ReadWrite);
            depthTargetSlot = new TexturePassSlot(hnRenderGraph, PassSlotType.ReadWrite);
            rendererListSlot = new RendererListPassSlot(hnRenderGraph, PassSlotType.ReadOnly);
            lightDatasBufferSlot = new ComputeBufferPassSlot(hnRenderGraph, PassSlotType.ReadOnly);
            reflectionProbeAtlasSlot = new TexturePassSlot(hnRenderGraph, PassSlotType.ReadOnly);
            clusterCullingReflectionProbeMaskBufferSlot = new ComputeBufferPassSlot(hnRenderGraph, PassSlotType.ReadOnly);
            clusterCullingReflectionProbeDatasBufferSlot = new ComputeBufferPassSlot(hnRenderGraph, PassSlotType.ReadOnly);
            clusterCullingLightMaskBufferSlot = new ComputeBufferPassSlot(hnRenderGraph, PassSlotType.ReadOnly);
        }

        public override void Record(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            if(!colorTargetSlot.IsConnected || !depthTargetSlot.IsConnected)
            {
                return;
            }

            using (var builder = renderGraph.AddRenderPass<DrawObjectPassData>($"{name}({PassName})", out var passData))
            {
                builder.AllowRendererListCulling(false);
                
                var graphObject = renderingData.GraphObject;
                passData.colorTarget = builder.UseColorBuffer(graphObject.GetTextureHandle(colorTargetSlot), 0);
                var depthTargetHandle = graphObject.GetTextureHandle(depthTargetSlot);
                if (depthTargetHandle.IsValid())
                {
                    passData.depthTarget = builder.UseDepthBuffer(depthTargetHandle, DepthAccess.ReadWrite);
                }

                if(lightDatasBufferSlot.IsConnected)
                {
                    passData.lightDatasBuffer = builder.ReadComputeBuffer(graphObject.GetComputeBufferHandle(lightDatasBufferSlot));
                }

                if(reflectionProbeAtlasSlot.IsConnected && clusterCullingReflectionProbeMaskBufferSlot.IsConnected && clusterCullingReflectionProbeDatasBufferSlot.IsConnected)
                {
                    passData.reflectionProbeAtlas = builder.ReadTexture(graphObject.GetTextureHandle(reflectionProbeAtlasSlot));
                    passData.clusterCullingReflectionProbeMaskBuffer = builder.ReadComputeBuffer(graphObject.GetComputeBufferHandle(clusterCullingReflectionProbeMaskBufferSlot));
                    passData.clusterCullingReflectionProbeDatasBuffer = builder.ReadComputeBuffer(graphObject.GetComputeBufferHandle(clusterCullingReflectionProbeDatasBufferSlot));
                }

                if(clusterCullingLightMaskBufferSlot.IsConnected)
                {
                    passData.clusterCullingLightMaskBuffer = builder.ReadComputeBuffer(graphObject.GetComputeBufferHandle(clusterCullingLightMaskBufferSlot));
                }

                RendererListDesc rendererListDesc = HNRenderPipelineUtils.GetOpaqueRendererListDesc(ShaderPassNames.AllForwardNames, renderingData.CullingResults, renderingData.Camera, renderingLayerMask);
                passData.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(rendererListDesc));

                builder.SetRenderFunc(
                    (DrawObjectPassData data, RenderGraphContext ctx) =>
                    {
                        if(reflectionProbeAtlasSlot.IsConnected && clusterCullingReflectionProbeMaskBufferSlot.IsConnected && clusterCullingReflectionProbeDatasBufferSlot.IsConnected)
                        {
                            ctx.cmd.EnableShaderKeyword(GlobalKeywords.clusterCullingReflectionProbe);
                            ctx.cmd.SetGlobalTexture(ClusterCullingReflectionProbePass.PropertyIDs.reflectionProbeAtlas, data.reflectionProbeAtlas);
                            ctx.cmd.SetGlobalBuffer(ClusterCullingReflectionProbePass.PropertyIDs.clusterCullingReflectionProbeMaskBuffer, data.clusterCullingReflectionProbeMaskBuffer);
                            ctx.cmd.SetGlobalBuffer(ClusterCullingReflectionProbePass.PropertyIDs.clusterCullingReflectionProbeDatasBuffer, data.clusterCullingReflectionProbeDatasBuffer);
                        }

                        if(clusterCullingLightMaskBufferSlot.IsConnected)
                        {
                            ctx.cmd.EnableShaderKeyword(GlobalKeywords.clusterCullingLight);
                            ctx.cmd.SetGlobalBuffer(ClusterCullingLightPass.PropertyIDs.clusterCullingLightMaskBuffer, data.clusterCullingLightMaskBuffer);
                        }

                        ctx.cmd.SetGlobalBuffer(BuildLightDataPass.PropertyIDs.lightDatasBuffer, passData.lightDatasBuffer);

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
        public TexturePassSlot colorTargetSlot;

        [SerializeField]
        public TexturePassSlot depthTargetSlot;

        [SerializeField]
        public RendererListPassSlot rendererListSlot;

        [SerializeField]
        public ComputeBufferPassSlot lightDatasBufferSlot;

        [SerializeField]
        public TexturePassSlot reflectionProbeAtlasSlot;

        [SerializeField]
        public ComputeBufferPassSlot clusterCullingReflectionProbeMaskBufferSlot;

        [SerializeField]
        public ComputeBufferPassSlot clusterCullingReflectionProbeDatasBufferSlot;

        [SerializeField]
        public ComputeBufferPassSlot clusterCullingLightMaskBufferSlot;


        public const string PassName = "Draw Object Pass";


        public class DrawObjectPassData : PassData
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

