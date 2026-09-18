// <copyright file="DrawShadowPass.Selection.cs" company="HN">
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
        // ── 选灯 ──

        /// <summary>
        /// 收集本帧参与阴影的光源：方向光 main light 优先，其余按强度降序；
        /// 方向光 ≤4、本地光 ≤32。
        /// </summary>
        private void BuildSelectedLights()
        {
            selectedLights.Clear();

            NativeArray<VisibleLight> visibleLights = cameraContext.VisibleLights;
            if (!visibleLights.IsCreated || visibleLights.Length == 0)
            {
                return;
            }

            int mainLightIndex = cameraContext.MainLightIndex;
            int maxLightCount = Mathf.Min(
                visibleLights.Length,
                HNRenderPipelineAsset.MAX_DIRECTIONAL_LIGHT_ON_SCREEN
                + HNRenderPipelineAsset.MAX_LOCAL_LIGHT_ON_SCREEN);

            int directionalCount = 0;
            int localCount = 0;

            // main light 优先。
            if (mainLightIndex >= 0 && mainLightIndex < maxLightCount
                && TryAddShadowLight(visibleLights, mainLightIndex, ref directionalCount, ref localCount))
            {
            }

            for (int i = 0; i < maxLightCount; i++)
            {
                if (i == mainLightIndex)
                {
                    continue;
                }

                TryAddShadowLight(visibleLights, i, ref directionalCount, ref localCount);
            }
        }

        private bool TryAddShadowLight(
            NativeArray<VisibleLight> visibleLights,
            int index,
            ref int directionalCount,
            ref int localCount)
        {
            VisibleLight visibleLight = visibleLights[index];
            Light light = visibleLight.light;
            if (light == null)
            {
                return false;
            }

            // 先按光源类型过滤（无需组件查询），跳过不支持阴影的类型。
            LightType type = visibleLight.lightType;
            if (type != LightType.Directional && type != LightType.Point && type != LightType.Spot)
            {
                return false;
            }

            if (!light.TryGetComponent(out HNAdditionalLightData additionalLightData)
                || !additionalLightData.EnableShadow)
            {
                return false;
            }

            if (type == LightType.Directional)
            {
                if (directionalCount >= MaxDirectionalShadowLights)
                {
                    return false;
                }
                directionalCount++;
            }
            else if (localCount >= MaxLocalShadowLights)
            {
                return false;
            }
            else
            {
                localCount++;
            }

            // 方向光可覆盖相机的级联设置（级数 / 分割 / 更新模式）；否则沿用相机设置。
            // 非方向光不参与级联，其 cascadeSettings 仅用于更新模式判定，取相机设置。
            bool isDirectional = type == LightType.Directional;
            ShadowCameraSettings cascadeSettings = frameShadowCameraSettings;
            if (isDirectional && additionalLightData.OverrideCameraShadowSettings)
            {
                cascadeSettings = additionalLightData.ShadowSettings;
                cascadeSettings.EnsureValid();
            }

            int cascadeCount = isDirectional
                ? Mathf.Clamp((int)cascadeSettings.CascadeCount, 1, MaxMapPerLight)
                : 0;

            // 分辨率先按有效级联级数收敛（级数越大，单张 map 允许的分辨率越低），
            // 再收敛到 atlas slice 分辨率以内
            ResolutionType clampedResolution = CascadeShadowUtils.ClampResolution(
                cascadeSettings.CascadeCount, additionalLightData.CascadeResolution);
            int resolution = HNRenderPipelineUtils.ClampShadowResolution(
                (int)clampedResolution, allocatedResolution);

            selectedLights.Add(new SelectedLight
            {
                lightIndex = index,
                lightType = type,
                resolution = resolution,
                cascadeCount = cascadeCount,
                cascadeSettings = cascadeSettings,
                cascadeHash = isDirectional
                    ? ComputeCascadeHash(cascadeCount, cascadeSettings.CascadeSplits, cascadeSettings.CascadeSplitGroups)
                    : 0,
                // 方向光槽位（0 起）对应 _ShadowCameraDatas 下标；其他类型无意义。
                cameraDataIndex = isDirectional ? directionalCount - 1 : -1,
                lightLocalToWorld = visibleLight.localToWorldMatrix,
                lightRange = visibleLight.range,
                lightSpotAngle = visibleLight.spotAngle,
                lightInstanceId = light.GetInstanceID(),
                soft = additionalLightData.IsSoftShadow,
                penumbraRadius = additionalLightData.PenumbraRadius,
            });

            return true;
        }

        /// <summary>
        /// 预扫本帧是否存在软阴影 light，决定 atlas 是否采用矩图格式。
        /// 须在 <see cref="EnsureResources"/> 之前调用（atlas 格式依赖该结果）。
        /// </summary>
        private void DetectSoftShadowMode()
        {
            frameSoftShadowMode = false;

            if (!softShadowEnabled)
            {
                return;
            }

            // 矩图依赖 compute shader 生成 mip；缺失时退回硬阴影，避免采样未初始化的 mip。
            if (cameraContext.RuntimeResources == null
                || cameraContext.RuntimeResources.shadowMipmapCS == null)
            {
                return;
            }

            NativeArray<VisibleLight> visibleLights = cameraContext.VisibleLights;
            if (!visibleLights.IsCreated || visibleLights.Length == 0)
            {
                return;
            }

            int maxLightCount = Mathf.Min(
                visibleLights.Length,
                HNRenderPipelineAsset.MAX_DIRECTIONAL_LIGHT_ON_SCREEN
                + HNRenderPipelineAsset.MAX_LOCAL_LIGHT_ON_SCREEN);

            for (int i = 0; i < maxLightCount; i++)
            {
                VisibleLight visibleLight = visibleLights[i];
                Light light = visibleLight.light;
                if (light == null)
                {
                    continue;
                }

                LightType type = visibleLight.lightType;
                if (type != LightType.Directional && type != LightType.Point && type != LightType.Spot)
                {
                    continue;
                }

                if (!light.TryGetComponent(out HNAdditionalLightData additionalLightData)
                    || !additionalLightData.IsSoftShadow)
                {
                    continue;
                }

                frameSoftShadowMode = true;
                return;
            }
        }

        // ── 驻留集 diff ──

        /// <summary>
        /// 同步驻留表与分配器：新增 map 分配、消失 map 释放、重分配结果回写。
        /// </summary>
        private void UpdateResidentMaps()
        {
            mipUpdateCommands.Clear();

            // 标记本帧仍需要的 map；缺失或分辨率变化的重新分配。
            for (int i = 0; i < selectedLights.Count; i++)
            {
                SelectedLight selected = selectedLights[i];
                int mapCount = GetMapCount(selected);
                for (int sub = 0; sub < mapCount; sub++)
                {
                    uint mapIndex = EncodeMapIndex(selected.lightIndex, sub);
                    if (mapIndex >= (uint)residentMaps.Length)
                    {
                        continue;
                    }

                    ResidentMap resident = residentMaps[mapIndex];
                    if (resident == null)
                    {
                        AllocateMap(mapIndex, selected);
                    }
                    else if (resident.resolution != selected.resolution)
                    {
                        textureAllocator.Release(mapIndex);
                        AllocateMap(mapIndex, selected);
                    }
                    else
                    {
                        resident.neededThisFrame = true;
                    }
                }
            }

            // 释放本帧不再需要的 map（只遍历活跃项，O(驻留数)）。
            for (int i = 0; i < activeResidentMaps.Count; )
            {
                uint mapIndex = activeResidentMaps[i];
                ResidentMap resident = residentMaps[mapIndex];
                if (resident != null && resident.neededThisFrame)
                {
                    resident.neededThisFrame = false;
                    i++;
                    continue;
                }

                textureAllocator.Release(mapIndex);
                residentMaps[mapIndex] = null;
                activeResidentMaps[i] = activeResidentMaps[activeResidentMaps.Count - 1];
                activeResidentMaps.RemoveAt(activeResidentMaps.Count - 1);
            }
        }

        private void AllocateMap(uint mapIndex, SelectedLight selected)
        {
            allocateResults.Clear();
            textureAllocator.Allocate(ref allocateResults, mapIndex, selected.resolution);

            // 处理被本次分配重分配（搬移）的既有 map：更新驻留位置。
            // Graphics.CopyTexture 不允许在同一 texture 的同一 element/mip 内拷贝（即使源与目标
            // 区域不同也会报「identical source and destination element」），而 atlas 通常只有
            // 单 slice，因此同 slice 内搬移无法拷贝，改为标记强制重绘，在新位置重建内容。
            // 仅跨 slice 搬移才用 CopyTexture 保留原内容。
            foreach (KeyValuePair<uint, TextureAllocatorResult> pair in allocateResults)
            {
                if (pair.Key == mapIndex || !pair.Value.IsReorg)
                {
                    continue;
                }

                if (!TryGetResidentMap(pair.Key, out ResidentMap moved))
                {
                    continue;
                }

                moved.allocation = pair.Value;

                if (pair.Value.OldSliceIndex == pair.Value.SliceIndex)
                {
                    // 位置未变的搬移是空操作，无需重绘；确已移动的强制下一帧重绘。
                    if (pair.Value.OldScaleOffset != pair.Value.ScaleOffset)
                    {
                        moved.hasSignature = false;
                        moved.lastUpdateFrame = -1;
                    }
                }
                else
                {
                    copyCommands.Add(new ShadowCopyCommand
                    {
                        fromSlice = pair.Value.OldSliceIndex,
                        fromScaleOffset = pair.Value.OldScaleOffset,
                        toSlice = pair.Value.SliceIndex,
                        toScaleOffset = pair.Value.ScaleOffset,
                    });

                    // 跨 slice 搬移只拷贝 level 0，矩图 mip 链需在拷贝后重建。
                    if (frameSoftShadowMode && moved.soft)
                    {
                        mipUpdateCommands.Add(new ShadowMipUpdateCommand
                        {
                            allocation = pair.Value,
                            resolution = moved.resolution,
                        });
                    }
                }
            }

            if (allocateResults.TryGetValue(mapIndex, out TextureAllocatorResult result))
            {
                if (residentMaps[mapIndex] == null)
                {
                    activeResidentMaps.Add(mapIndex);
                }

                residentMaps[mapIndex] = new ResidentMap
                {
                    lightIndex = selected.lightIndex,
                    subIndex = (int)(mapIndex & 0xFu),
                    lightType = selected.lightType,
                    resolution = selected.resolution,
                    allocation = result,
                    hasSignature = false,
                    lastUpdateFrame = -1,
                    neededThisFrame = true,
                    soft = selected.soft,
                };
            }
            else
            {
                // 分配失败：清除旧记录，避免残留已释放的分配被后续读取。
                residentMaps[mapIndex] = null;
            }
        }

        private static int GetMapCount(SelectedLight selected)
        {
            switch (selected.lightType)
            {
                case LightType.Point:
                    return 6;
                case LightType.Spot:
                    return 1;
                default:
                    return Mathf.Clamp(selected.cascadeCount, 1, MaxMapPerLight);
            }
        }

        private static uint EncodeMapIndex(int lightIndex, int subIndex)
        {
            return ((uint)lightIndex << 4) | (uint)subIndex;
        }

        /// <summary>
        /// 打包光源类型 / cascade 级数 / 方向光槽位到单个 uint（供 shader 直读，避免逐像素分支与解码）。
        /// </summary>
        /// <param name="lightType">光源类型。</param>
        /// <param name="cascadeCount">方向光 cascade 级数（非方向光为 0）。</param>
        /// <returns>bit0..7=lightType，bit8..15=cascadeCount/returns>
        private static uint PackLightTypeAndCascadeCount(LightType lightType, int cascadeCount)
        {
            // 与 <see cref="LightData.lightType"/> 的 shader 约定一致：1=方向 2=点 3=聚光。
            uint type = lightType switch
            {
                LightType.Directional => 1u,
                LightType.Point => 2u,
                LightType.Spot => 3u,
                _ => 0u,
            };
            uint cascades = ((uint)Mathf.Clamp(cascadeCount, 0, 0xFF)) << 8;
            return type | cascades;
        }

        /// <summary>
        /// 按 mapIndex 取驻留记录；越界或未驻留时返回 <c>false</c>。
        /// </summary>
        private bool TryGetResidentMap(uint mapIndex, out ResidentMap resident)
        {
            if (residentMaps != null && mapIndex < (uint)residentMaps.Length)
            {
                resident = residentMaps[mapIndex];
                return resident != null;
            }

            resident = null;
            return false;
        }
    }
}
