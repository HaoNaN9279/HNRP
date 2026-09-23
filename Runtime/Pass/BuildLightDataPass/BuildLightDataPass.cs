// <copyright file="BuildLightDataPass.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    /// <summary>
    /// 构建光照数据：把可见光打包进计算缓冲，供前向/簇剔除等 pass 消费。
    /// </summary>
    [Pass(PassNameConst)]
    public sealed class BuildLightDataPass : Pass, IGlobalShaderResource, ILightDatasProvider
    {
        /// <summary>
        /// 用于注册与识别的常量 pass 名。与旧 <see cref="BuildLightDataPass.PassName"/> 一致。
        /// </summary>
        public const string PassNameConst = "Build Light Data";

        // ── 持久资源 ──

        /// <summary>
        /// 光照数据结构化缓冲。持久资源，随本 pass 实例跨帧存活，
        /// 由 <see cref="Cleanup"/> 释放。消费者经显式依赖边直接引用，
        /// 不再经 RenderGraph 资源句柄传递。
        /// </summary>
        private ComputeBuffer lightDatasBuffer;

        /// <inheritdoc />
        public ComputeBuffer LightDatasBuffer => lightDatasBuffer;

        // ── 每帧状态 ──

        private CameraContext cameraContext;
        private NativeArray<VisibleLight> visibleLights;
        private int lightCount;
        private int maxLightCount; 

        private BuildLightDataJob job;
        private NativeArray<uint> renderingLayerMasks;
        private NativeArray<LightData> lightDatas;

        // ── 构造函数 ──

        /// <summary>
        /// 初始化 <see cref="BuildLightDataPass"/> 的新实例。
        /// </summary>
        /// <remarks>
        /// 无参构造仅供 <see cref="RenderGraphAsset"/> 上参数缓存 Pass 的
        /// <c>[SerializeReference]</c> 反序列化使用；实例名随后由序列化数据填充。
        /// </remarks>
        public BuildLightDataPass()
            : base(string.Empty)
        {
        }

        /// <summary>
        /// 初始化 <see cref="BuildLightDataPass"/> 的新实例。
        /// </summary>
        /// <param name="passName">
        /// 本 pass 的实例名。必须非 null 且在渲染图内唯一。
        /// </param>
        public BuildLightDataPass(string passName)
            : base(passName)
        {
        }

        // ── 生命周期 ──

        /// <inheritdoc />
        public override void SetupSlots()
        {
            // 本 pass 无 RenderGraph 内容 slot：光照数据经持久缓冲 + 显式依赖边
            // 传递给消费者（ClusterCullingLight 局部 SRV / ForwardPass 全局绑定）。
        }

        /// <inheritdoc />
        /// <remarks>
        /// 捕获相机专属渲染上下文，以访问 <see cref="CameraContext.VisibleLights"/>
        /// 的每帧可见光数据。最大光源数在初始化期由管线资产常量推导。
        /// </remarks>
        public override void PreRecord(RenderGraphAsset template, CameraContext context)
        {
            cameraContext = context;
            visibleLights = context.VisibleLights;
            maxLightCount = HNRenderPipelineAsset.MAX_DIRECTIONAL_LIGHT_ON_SCREEN
                            + HNRenderPipelineAsset.MAX_LOCAL_LIGHT_ON_SCREEN;
            lightCount = math.min(visibleLights.Length, maxLightCount);
        }

        /// <inheritdoc />
        /// <remarks>
        /// 确保按最大光源数尺寸的持久计算缓冲已分配，调度并行 <see cref="BuildLightDataJob"/>
        /// 把可见光打包进 <see cref="LightData"/> 结构体，并记录一个把完成的 job
        /// 输出上传到 GPU 缓冲的渲染函数。
        /// </remarks>
        public override void Record(RenderGraph renderGraph)
        {
            using (var builder = renderGraph.AddRenderPass<BuildLightDataPassData>(
                PassName, out var passData))
            {
                builder.AllowPassCulling(false);

                var renderingLayerMaskList = new uint[visibleLights.Length];
                for(int i = 0; i < visibleLights.Length; i++)
                {
                    if(visibleLights[i].light.TryGetComponent<HNAdditionalLightData>(out var lightData))
                    {
                        renderingLayerMaskList[i] = lightData.RenderingLayerMask;
                    }
                    else
                    {
                        renderingLayerMaskList[i] = 0;
                    }
                }

                // 持久缓冲：光照数据跨帧复用。消费者经显式依赖边直接引用本 pass
                // 持有的持久 ComputeBuffer（不再经 RenderGraph 句柄传递）。
                HNRenderPipelineUtils.ValidateComputeBuffer(
                    ref lightDatasBuffer,
                    maxLightCount,
                    UnsafeUtility.SizeOf<LightData>());

                renderingLayerMasks = new NativeArray<uint>(renderingLayerMaskList, Allocator.TempJob);
                lightDatas = new NativeArray<LightData>(lightCount, Allocator.TempJob);

                job = new BuildLightDataJob
                {
                    visibleLights = visibleLights,
                    renderingLayerMasks = renderingLayerMasks,
                    lightDatas = lightDatas,
                };

                var jobHandle = job.ScheduleParallel(lightCount, 1, new JobHandle());

                // 把每帧值存到池化 pass 数据上，使渲染函数闭包只捕获 `this`（零分配）。
                passData.jobHandle = jobHandle;
                passData.lightDatas = lightDatas;
                passData.renderingLayerMasks = renderingLayerMasks;

                builder.SetRenderFunc(
                    (BuildLightDataPassData data, RenderGraphContext ctx) =>
                    {
                        data.jobHandle.Complete();

                        if (IsEnabled)
                        {
                            ctx.cmd.SetBufferData(lightDatasBuffer, data.lightDatas);
                        }

                        // 两个 TempJob 原生数组在 job 完成后必须释放，
                        // 否则超过 4 帧生命周期触发 JobTempAlloc 泄漏告警。
                        data.lightDatas.Dispose();
                        data.renderingLayerMasks.Dispose();
                    });
            }
        }

        /// <inheritdoc />
        public override void Cleanup()
        {
            // 释放本 pass 持有的持久光照数据缓冲。
            CoreUtils.SafeRelease(lightDatasBuffer);
            lightDatasBuffer = null;
        }

        /// <inheritdoc />
        /// <remarks>
        /// 把已产出的 <c>_LightDatasBuffer</c> 设为全局；未产出（被禁用 / 未分配）
        /// 时不绑定。本 pass 不控制任何全局 keyword。
        /// </remarks>
        public void BindGlobalShaderResources(CommandBuffer cmd)
        {
            if (IsEnabled && lightDatasBuffer != null)
            {
                cmd.SetGlobalBuffer(
                    PropertyIDs.LightDatasBuffer,
                    lightDatasBuffer);
            }
        }

        // ── Pass data ──

        /// <summary>
        /// <see cref="BuildLightDataPass"/> 的渲染图 pass 数据容器。
        /// </summary>
        private class BuildLightDataPassData
        {
            /// <summary>
            /// 上传打包光照数据前需完成的 job 句柄。
            /// </summary>
            public JobHandle jobHandle;

            /// <summary>
            /// 上传到 GPU 并在渲染函数内释放的打包光照数据数组。
            /// </summary>
            public NativeArray<LightData> lightDatas;

            /// <summary>
            /// job 读取的渲染层掩码数组；job 完成后在渲染函数内释放。
            /// </summary>
            public NativeArray<uint> renderingLayerMasks;
        }

        // ── Property IDs ──

        /// <summary>
        /// 本 pass 与其消费方使用的 shader 属性标识。
        /// </summary>
        public static class PropertyIDs
        {
            /// <summary>
            /// 光照数据结构化缓冲的全局 shader 属性 ID。值：<c>_LightDatasBuffer</c>。
            /// </summary>
            public static readonly int LightDatasBuffer = Shader.PropertyToID("_LightDatasBuffer");
        }
    }
}
