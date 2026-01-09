using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    [Serializable]
    public class BuildLightDataPass : PassBase
    {
        public override void Initialize(HNRenderGraphBase hnRenderGraph, string passName)
        {
            base.Initialize(hnRenderGraph, passName);

            lightDatasBufferIndex = hnRenderGraph.RegistAndGetComputeBufferHandleIndex();
        }

        public override void Record(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<BuildLightDataPassData>($"{name}({PassName})", out var passData))
            {
                builder.AllowPassCulling(false);

                passData.lightDataBuffer = renderGraph.CreateComputeBuffer(
                    new ComputeBufferDesc(
                        HNRenderPipelineAsset.MAX_DIRECTIONAL_LIGHT_ON_SCREEN + HNRenderPipelineAsset.MAX_LOCAL_LIGHT_ON_SCREEN, 
                        UnsafeUtility.SizeOf<LightData>()
                    ){ name = "Light Data Buffer" }
                );
                renderingData.GraphData.computeBufferHandles.Add(passData.lightDataBuffer);
                
                builder.WriteComputeBuffer(passData.lightDataBuffer);

                int lightCount = math.min(renderingData.visibleLights.Length, HNRenderPipelineAsset.MAX_DIRECTIONAL_LIGHT_ON_SCREEN + HNRenderPipelineAsset.MAX_LOCAL_LIGHT_ON_SCREEN);

                if (lightDatas.IsCreated)
                {
                    lightDatas.Dispose();
                }

                lightDatas = new NativeArray<LightData>(lightCount, Allocator.TempJob);

                createLightDataJob = new CreateLightDataJob
                {
                    visibleLights = renderingData.visibleLights,
                    lightDatas = lightDatas,
                };
                var createLightDataHandle = createLightDataJob.ScheduleParallel(lightCount, 1, new JobHandle());
                createLightDataHandle.Complete();
                
                builder.SetRenderFunc(
                    (BuildLightDataPassData data, RenderGraphContext ctx) =>
                    {
                        ctx.cmd.SetBufferData(passData.lightDataBuffer, lightDatas);
                    }
                );
            }
        }

        public override void EndRecord()
        {
            if (lightDatas.IsCreated)
            {
                lightDatas.Dispose();
            }
            lightDatas = default;
        }

        public override void Dispose()
        {
            
        }


        [SerializeField]
        public int lightDatasBufferIndex = -1;

        private CreateLightDataJob createLightDataJob;
        private NativeArray<LightData> lightDatas;


        public const string PassName = "Set Light Data";


        public class BuildLightDataPassData
        {
            public ComputeBufferHandle lightDataBuffer;
        }


    }
}
