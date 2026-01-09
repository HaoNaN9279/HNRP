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
                passData.forwardPlusZBinsBuffer = renderGraph.CreateComputeBuffer(
                    new ComputeBufferDesc(
                        ClusterCulling.maxZBinWords, 
                        UnsafeUtility.SizeOf<float4>()
                    ) { name = "Forward Plus Z-Bin Buffer" }
                );
                renderingData.GraphData.computeBufferHandles.Add(passData.forwardPlusZBinsBuffer);
                passData.forwardPlusTileMasksBuffer = renderGraph.CreateComputeBuffer(
                    new ComputeBufferDesc(
                        ClusterCulling.maxTileWords, 
                        UnsafeUtility.SizeOf<float4>()
                    ) { name = "Forward Plus Tile Buffer" }
                );
                renderingData.GraphData.computeBufferHandles.Add(passData.forwardPlusTileMasksBuffer);

                forwardPlusLightCulling.PrepareLightData(ref renderingData);

                builder.WriteComputeBuffer(passData.forwardPlusZBinsBuffer);
                builder.WriteComputeBuffer(passData.forwardPlusTileMasksBuffer);

                builder.SetRenderFunc(
                    (ForwardPlusLightCullingPassData data, RenderGraphContext ctx) =>
                    {
/*
    job的Complete命令的调用应该在Record阶段还是在设置渲染命令阶段，取决于job用到的native数据
    当native数据可能会被后面的pass访问时，在当前pass中依赖该native数据的job应该在Record阶段调用Complete。
    当job计算量较大，需要更多时间来执行，想尽可能晚的调用Complete时，需要确保该pass所依赖的所有native数据
    不会再被后续的pass访问，然后就可以将Complete的调用放在设置渲染命令阶段。
*/
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

        public override void EndRecord()
        {
        }

        public override void Dispose()
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
