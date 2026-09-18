// <copyright file="DrawShadowPass.Resource.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public sealed partial class DrawShadowPass
    {
        // ── 资源 ──

        /// <summary>
        /// 释放阴影图集。
        /// </summary>
        private void ReleaseAtlas()
        {
            shadowAtlas?.Release();
            shadowAtlas = null;
        }

        /// <summary>
        /// 确保持久资源（atlas、分配器、表缓冲）已按当前参数分配。
        /// </summary>
        private void EnsureResources()
        {
            if (residentMaps == null)
            {
                residentMaps = new ResidentMap[ResidentMapCapacity];
            }

            if (shadowAtlas == null
                || allocatedResolution != sliceResolution
                || allocatedSliceCount != sliceCount
                || allocatedSoftMode != frameSoftShadowMode)
            {
                ReleaseAtlas();

                int resolution = Mathf.Max(512, Mathf.ClosestPowerOfTwo(sliceResolution));
                int slices = Mathf.Clamp(sliceCount, 1, 4);

                // 单一 RG16F 矩图 atlas：R=归一化线性深度，G=深度²。
                // 无深度附件：投射者用 BlendOp Min 直接写入最近深度的矩（z 与 z² 同单调，逐通道 min 正确）。
                // 仅软阴影需要 mip 链（硬阴影只手采样 mip0），故 mip 只在软模式分配以省显存。
                // 手动 mip（autoGenerateMips=false），由 ShadowMipmapCS 逐区域更新。
                shadowAtlas = RTHandles.Alloc(
                    resolution,
                    resolution,
                    slices: slices,
                    depthBufferBits: DepthBits.None,
                    colorFormat: GraphicsFormat.R16G16_SFloat,
                    filterMode: FilterMode.Bilinear,
                    wrapMode: TextureWrapMode.Clamp,
                    dimension: TextureDimension.Tex2DArray,
                    useMipMap: frameSoftShadowMode,
                    autoGenerateMips: false,
                    isShadowMap: false,
                    enableRandomWrite: true,
                    name: "HN Shadow Atlas");

                allocatedResolution = resolution;
                allocatedSliceCount = slices;
                allocatedSoftMode = frameSoftShadowMode;

                textureAllocator = new TextureAllocator(resolution, 512, 4096, slices);
                System.Array.Clear(residentMaps, 0, residentMaps.Length);
                activeResidentMaps.Clear();

                // atlas 已重建，旧 slice / 偏移的搬移命令全部失效，避免残留命令访问无效区域。
                copyCommands.Clear();
                mipUpdateCommands.Clear();

                // 整图初始化为远矩 (1,1)（无深度附件）。
                // 命令延迟到 render func 内经 RenderGraph 的命令缓冲提交，避免在录制期
                // 私自申请 CommandBuffer 并即时提交到 ScriptableRenderContext。
                needsAtlasClear = true;
            }

            int maxLightCount = HNRenderPipelineAsset.MAX_DIRECTIONAL_LIGHT_ON_SCREEN
                                + HNRenderPipelineAsset.MAX_LOCAL_LIGHT_ON_SCREEN;

            if (shadowLightDatasArray == null || lightDataCapacity != maxLightCount)
            {
                lightDataCapacity = maxLightCount;
                shadowLightDatasArray = new ShadowLightData[maxLightCount];
                HNRenderPipelineUtils.ValidateComputeBuffer(
                    ref shadowLightDatasBuffer, maxLightCount, System.Runtime.InteropServices.Marshal.SizeOf<ShadowLightData>());
            }

            int maxMapSlots = (allocatedResolution / 512) * (allocatedResolution / 512) * allocatedSliceCount;
            if (shadowMapDatasArray == null || mapDataCapacity != maxMapSlots)
            {
                mapDataCapacity = maxMapSlots;
                shadowMapDatasArray = new ShadowMapData[maxMapSlots];
                HNRenderPipelineUtils.ValidateComputeBuffer(
                    ref shadowMapDatasBuffer, maxMapSlots, System.Runtime.InteropServices.Marshal.SizeOf<ShadowMapData>());
            }

            if (shadowCameraDatasArray == null)
            {
                shadowCameraDatasArray = new ShadowCameraData[MaxDirectionalShadowLights];
                HNRenderPipelineUtils.ValidateComputeBuffer(
                    ref shadowCameraDatasBuffer,
                    MaxDirectionalShadowLights,
                    System.Runtime.InteropServices.Marshal.SizeOf<ShadowCameraData>());
            }

            if (mipRegionsArray == null)
            {
                // 每帧待更新区域数上限 = 驻留 map 上限；缓冲容量按此一次性分配。
                mipRegionsArray = new ShadowMipRegion[ResidentMapCapacity];
                HNRenderPipelineUtils.ValidateComputeBuffer(
                    ref mipRegionsBuffer,
                    ResidentMapCapacity,
                    System.Runtime.InteropServices.Marshal.SizeOf<ShadowMipRegion>());
            }
        }
    }
}
