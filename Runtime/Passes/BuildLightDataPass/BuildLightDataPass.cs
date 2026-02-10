using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    [Serializable]
    public class BuildLightDataPass : PassBase
    {
        public override void OnCreate(HNRenderGraphBase hnRenderGraph, string passName)
        {
            base.OnCreate(hnRenderGraph, passName);

            lightDatasBufferIndex = hnRenderGraph.RegistAndGetComputeBufferHandleIndex();
        }

        public override void Record(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<BuildLightDataPassData>($"{name}({PassName})", out var passData))
            {
                builder.AllowPassCulling(false);

                passData.lightDatasBuffer = builder.WriteComputeBuffer(renderGraph.CreateComputeBuffer(
                    new ComputeBufferDesc(
                        HNRenderPipelineAsset.MAX_DIRECTIONAL_LIGHT_ON_SCREEN + HNRenderPipelineAsset.MAX_LOCAL_LIGHT_ON_SCREEN, 
                        UnsafeUtility.SizeOf<LightData>()
                    ){ name = "Light Datas Buffer" }
                ));
                renderingData.GraphData.computeBufferHandles.Add(passData.lightDatasBuffer);

                int lightCount = math.min(renderingData.visibleLights.Length, HNRenderPipelineAsset.MAX_DIRECTIONAL_LIGHT_ON_SCREEN + HNRenderPipelineAsset.MAX_LOCAL_LIGHT_ON_SCREEN);

                lightDatas = new NativeArray<LightData>(lightCount, Allocator.TempJob);

                buildLightDataJob = new BuildLightDataJob
                {
                    visibleLights = renderingData.visibleLights,
                    lightDatas = lightDatas,
                };
                var buildLightDataHandle = buildLightDataJob.ScheduleParallel(lightCount, 1, new JobHandle());
                
                builder.SetRenderFunc(
                    (BuildLightDataPassData data, RenderGraphContext ctx) =>
                    {
                        buildLightDataHandle.Complete();
                        ctx.cmd.SetBufferData(passData.lightDatasBuffer, lightDatas);

                        lightDatas.Dispose();
                    }
                );
            }
        }

        public override void Cleanup()
        {
            
        }


        [SerializeField]
        public int lightDatasBufferIndex = -1;

        private BuildLightDataJob buildLightDataJob;
        private NativeArray<LightData> lightDatas;


        public const string PassName = "Build Light Data";


        public class BuildLightDataPassData
        {
            public ComputeBufferHandle lightDatasBuffer;
        }


        public static class PropertyIDs
        {
            public static readonly int lightDatasBuffer = Shader.PropertyToID("_LightDatasBuffer");
        }
    }
}
