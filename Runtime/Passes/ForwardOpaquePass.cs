using System;
using System.Collections;
using System.Collections.Generic;
using Palmmedia.ReportGenerator.Core.Parser.Analysis;
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

                passData.forwardPlusZBinsBuffer = builder.ReadComputeBuffer(computeBufferHandles[forwardPlusZBinsBufferIndex]);
                passData.forwardPlusTileMasksBuffer = builder.ReadComputeBuffer(computeBufferHandles[forwardPlusTileMasksBufferIndex]);

                RendererListDesc rendererListDesc = HNRenderPipelineUtils.GetOpaqueRendererListDesc(ShaderPassNames.AllForwardNames, renderingData.CullingResults, renderingData.Camera, renderingLayerMask);
                passData.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(rendererListDesc));

                builder.SetRenderFunc(
                    (ForwardOpaquePassData data, RenderGraphContext ctx) =>
                    {
                        ctx.cmd.SetGlobalConstantBuffer(passData.forwardPlusZBinsBuffer, PropertyIDs.forwardPlusZBinsBuffer, 0, ClusterCulling.maxZBinWords * 4);
                        ctx.cmd.SetGlobalConstantBuffer(passData.forwardPlusTileMasksBuffer, PropertyIDs.forwardPlusTileMasksBuffer, 0, ClusterCulling.maxTileWords * 4);
                        
                        ctx.cmd.DrawRendererList(data.rendererList);
                    }
                );

            }
        }

        public override void EndRecord()
        {
            
        }

        public override void Dispose()
        {
            
        }


        [SerializeField]
        public uint renderingLayerMask = 0x00000001;

        [SerializeField]
        public int colorTargetIndex = -1;

        [SerializeField]
        public int depthTargetIndex = -1;

        [SerializeField]
        public int forwardPlusZBinsBufferIndex = -1;

        [SerializeField]
        public int forwardPlusTileMasksBufferIndex = -1;

        [SerializeField]
        public int lightDatasBufferIndex = -1;

        public const string PassName = "Forward Opaque Pass";


        public class ForwardOpaquePassData : PassData
        {
            public TextureHandle colorTarget;
            public TextureHandle depthTarget;
            public ComputeBufferHandle forwardPlusZBinsBuffer;
            public ComputeBufferHandle forwardPlusTileMasksBuffer;
            public ComputeBufferHandle lightDatasBuffer;
            public RendererListHandle rendererList;

        }


        public static class PropertyIDs
        {
            public static readonly int forwardPlusZBinsBuffer = Shader.PropertyToID("_ForwardPlusZBinsBuffer");
            public static readonly int forwardPlusTileMasksBuffer = Shader.PropertyToID("_ForwardPlusTileMasksBuffer");
        }

    }

}

