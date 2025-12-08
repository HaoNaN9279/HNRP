using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    [Serializable]
    public class ForwardPlusLightCullingPass : PassBase
    {
        public override void Initialize(HNRenderGraphBase hnRenderGraph, string passName)
        {
            base.Initialize(hnRenderGraph, passName);

            if(forwardPlusLightCulling == null)
            {
                forwardPlusLightCulling = new ForwardPlusLightCulling();
            }
            forwardPlusLightCulling.Initialize();

            forwardPlusZBinsBufferIndex = hnRenderGraph.RegistAndGetComputeBufferHandleIndex();
            forwardPlusTileMasksBufferIndex = hnRenderGraph.RegistAndGetComputeBufferHandleIndex();
        }

        public override void Record(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            if(forwardPlusLightCulling == null)
            {
                return;
            }
            
            using (var builder = renderGraph.AddRenderPass<ForwardPlusLightCullingPassData>($"{name}({PassName})", out var passData))
            {                
                passData.forwardPlusZBinsBuffer = renderGraph.CreateComputeBuffer(new ComputeBufferDesc(ClusterCulling.maxZBinWords, UnsafeUtility.SizeOf<float4>()) { name = "Forward Plus Z-Bin Buffer" });
                renderingData.GraphData.computeBufferHandles.Add(passData.forwardPlusZBinsBuffer);
                passData.forwardPlusTileMasksBuffer = renderGraph.CreateComputeBuffer(new ComputeBufferDesc(ClusterCulling.maxTileWords, UnsafeUtility.SizeOf<float4>()) { name = "Forward Plus Tile Buffer" });
                renderingData.GraphData.computeBufferHandles.Add(passData.forwardPlusTileMasksBuffer);

                forwardPlusLightCulling.PrepareLightData(ref renderingData);

                builder.SetRenderFunc(
                    (ForwardPlusLightCullingPassData data, RenderGraphContext ctx) =>
                    {
                        forwardPlusLightCulling.cullingHandle.Complete();

                        ctx.cmd.SetBufferData(passData.forwardPlusZBinsBuffer, forwardPlusLightCulling.zBins);
                        ctx.cmd.SetGlobalConstantBuffer(passData.forwardPlusZBinsBuffer, PropertyIDs.forwardPlusZBinsBuffer, 0, ClusterCulling.maxZBinWords * 4);
                        ctx.cmd.SetBufferData(passData.forwardPlusTileMasksBuffer, forwardPlusLightCulling.tileMasks);
                        ctx.cmd.SetGlobalConstantBuffer(passData.forwardPlusTileMasksBuffer, PropertyIDs.forwardPlusTileMasksBuffer, 0, ClusterCulling.maxTileWords * 4);

                        ctx.cmd.SetGlobalVector(PropertyIDs.forwardPlusParams0, forwardPlusLightCulling.params0);
                        ctx.cmd.SetGlobalVector(PropertyIDs.forwardPlusParams1, forwardPlusLightCulling.params1);
                        ctx.cmd.SetGlobalVector(PropertyIDs.forwardPlusParams2, forwardPlusLightCulling.params2);
                    }
                );
            }
        }

        public override void EndRecord()
        {
            if(forwardPlusLightCulling != null)
            {
                forwardPlusLightCulling.Cleanup();
            }
        }


        [SerializeField]
        public int forwardPlusZBinsBufferIndex = -1;

        [SerializeField]
        public int forwardPlusTileMasksBufferIndex = -1;

        private ForwardPlusLightCulling forwardPlusLightCulling;


        public const string PassName = "Forward Plus Light Culling";


        public class ForwardPlusLightCullingPassData
        {
            public ComputeBufferHandle forwardPlusZBinsBuffer;
            public ComputeBufferHandle forwardPlusTileMasksBuffer;
        }



        public static class PropertyIDs
        {
            public static readonly int forwardPlusZBinsBuffer = Shader.PropertyToID("_ForwardPlusZBinsBuffer");
            public static readonly int forwardPlusTileMasksBuffer = Shader.PropertyToID("_ForwardPlusTileMasksBuffer");
            public static readonly int forwardPlusParams0 = Shader.PropertyToID("_ForwardPlusParams0");
            public static readonly int forwardPlusParams1 = Shader.PropertyToID("_ForwardPlusParams1");
            public static readonly int forwardPlusParams2 = Shader.PropertyToID("_ForwardPlusParams2");
        }
    }
}
