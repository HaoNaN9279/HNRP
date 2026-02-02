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
        public override void OnCreate(HNRenderGraphBase hnRenderGraph, string passName)
        {
            base.OnCreate(hnRenderGraph, passName);

            forwardPlusZBinsBufferIndex = hnRenderGraph.RegistAndGetComputeBufferHandleIndex();
            forwardPlusTileMasksBufferIndex = hnRenderGraph.RegistAndGetComputeBufferHandleIndex();
        }

        public override void Record(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            if(forwardPlusLightCulling == null)
            {
                forwardPlusLightCulling = new ForwardPlusLightCulling();
            }
            forwardPlusLightCulling.Initialize();
            
            using (var builder = renderGraph.AddRenderPass<ForwardPlusLightCullingPassData>($"{name}({PassName})", out var passData))
            {                
                passData.forwardPlusZBinsBuffer = builder.WriteComputeBuffer(renderGraph.CreateComputeBuffer(
                    new ComputeBufferDesc(
                        ClusterCulling.maxZBinWords, 
                        UnsafeUtility.SizeOf<float4>()
                    ) { name = "Forward Plus Z-Bin Buffer" }
                ));
                renderingData.GraphData.computeBufferHandles.Add(passData.forwardPlusZBinsBuffer);
                passData.forwardPlusTileMasksBuffer = builder.WriteComputeBuffer(renderGraph.CreateComputeBuffer(
                    new ComputeBufferDesc(
                        ClusterCulling.maxTileWords, 
                        UnsafeUtility.SizeOf<float4>()
                    ) { name = "Forward Plus Tile Buffer" }
                )); 
                renderingData.GraphData.computeBufferHandles.Add(passData.forwardPlusTileMasksBuffer);

                forwardPlusLightCulling.PrepareLightData(ref renderingData);

                builder.SetRenderFunc(
                    (ForwardPlusLightCullingPassData data, RenderGraphContext ctx) =>
                    {
                        forwardPlusLightCulling.cullingHandle.Complete();

                        ctx.cmd.EnableShaderKeyword(GlobalKeywords.forwardPlus);

                        ctx.cmd.SetBufferData(passData.forwardPlusZBinsBuffer, forwardPlusLightCulling.zBins);
                        ctx.cmd.SetBufferData(passData.forwardPlusTileMasksBuffer, forwardPlusLightCulling.tileMasks);

                        globalConstantBuffer._ForwardPlusParams0 = forwardPlusLightCulling.params0;
                        globalConstantBuffer._ForwardPlusParams1 = forwardPlusLightCulling.params1;
                        globalConstantBuffer._ForwardPlusParams2 = forwardPlusLightCulling.params2;
                        ConstantBuffer.PushGlobal(ctx.cmd, globalConstantBuffer, PropertyIDs.forwardPlusGlobalConstantBuffer);
                    }
                );
            }
        }

        public override void Cleanup()
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
        private ForwardPlusGlobalConstantBuffer globalConstantBuffer = default;


        public const string PassName = "Forward Plus Light Culling";


        public class ForwardPlusLightCullingPassData
        {
            public ComputeBufferHandle forwardPlusZBinsBuffer;
            public ComputeBufferHandle forwardPlusTileMasksBuffer;
        }



        public static class PropertyIDs
        {
            public static readonly int forwardPlusGlobalConstantBuffer = Shader.PropertyToID("ForwardPlusVariablesGlobal");
        }


        public struct ForwardPlusGlobalConstantBuffer
        {
            public Vector4 _ForwardPlusParams0;
            public Vector4 _ForwardPlusParams1;
            public Vector4 _ForwardPlusParams2;
        }
    }
}
