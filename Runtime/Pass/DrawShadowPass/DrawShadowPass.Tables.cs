// <copyright file="DrawShadowPass.Tables.cs" company="HN">
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
        // ── 表与绘制命令 ──

        /// <summary>
        /// 填充 <c>ShadowLightData</c> / <c>ShadowMapData</c> 两张表，并生成本帧绘制命令。
        /// 并生成本帧绘制命令
        /// </summary>
        private void BuildTablesAndDrawCommands()
        {
            drawCommands.Clear();

            // 清除上一帧写入的 light 条目：光源关闭阴影 / 移出视野后，若残留
            // resolution>0 与旧 blockDatas，着色器会采样已释放的 atlas 槽。
            System.Array.Clear(shadowLightDatasArray, 0, shadowLightDatasArray.Length);

            // 每方向光一套 cascade 级数与分割：上传后 shader 按 light 的槽位索引取用。
            System.Array.Clear(shadowCameraDatasArray, 0, shadowCameraDatasArray.Length);

            for (int i = 0; i < selectedLights.Count; i++)
            {
                SelectedLight selected = selectedLights[i];
                int lightIndex = selected.lightIndex;
                if (lightIndex < 0 || lightIndex >= shadowLightDatasArray.Length)
                {
                    continue;
                }

                ShadowLightData lightData = default;
                lightData.lightIndex = lightIndex;
                lightData.resolution = selected.resolution;
                lightData.typeAndCascadeCount = PackLightTypeAndCascadeCount(
                    selected.lightType, selected.cascadeCount);

                // 光源位置 / 半径倒数：供矩图软阴影在采样端计算接收者线性深度。
                Vector3 lightPosition = selected.lightLocalToWorld.GetColumn(3);
                float invRange = selected.lightType == LightType.Directional
                    ? 0f
                    : 1f / Mathf.Max(selected.lightRange, 0.0001f);
                lightData.lightPosRange = new Vector4(
                    lightPosition.x, lightPosition.y, lightPosition.z, invRange);

                // 方向光：记录本光源的 cascade 参数槽位并写入对应 splits。
                if (selected.lightType == LightType.Directional
                    && selected.cameraDataIndex >= 0
                    && selected.cameraDataIndex < shadowCameraDatasArray.Length)
                {
                    lightData.cameraDataIndex = selected.cameraDataIndex;
                    shadowCameraDatasArray[selected.cameraDataIndex] =
                        BuildShadowCameraData(selected.cascadeSettings, selected.cascadeCount);
                }

                // 方向光：先按级数与分组交界切组，并清空本帧的组级剔除结果缓存。
                // 组级剔除结果只在组内出现需重绘的 cascade 时才向引擎索取（惰性）。
                bool isDirectionalLight = selected.lightType == LightType.Directional;
                if (isDirectionalLight)
                {
                    CascadeShadowGrouping.BuildGroups(
                        selected.cascadeSettings.CascadeCount,
                        selected.cascadeSettings.CascadeSplitGroups,
                        cascadeGroupsScratch);

                    System.Array.Clear(groupSplitDataScratch, 0, groupSplitDataScratch.Length);
                    System.Array.Clear(groupSplitDataReady, 0, groupSplitDataReady.Length);
                }

                int mapCount = GetMapCount(selected);
                bool allAllocated = true;
                for (int sub = 0; sub < mapCount; sub++)
                {
                    uint mapIndex = EncodeMapIndex(lightIndex, sub);
                    if (!TryGetResidentMap(mapIndex, out ResidentMap resident))
                    {
                        allAllocated = false;
                        continue;
                    }

                    uint field = EncodeField(resident.allocation);
                    if (sub < 4)
                    {
                        lightData.blockDatas0 |= field << (sub * 8);
                    }
                    else
                    {
                        lightData.blockDatas1 |= field << ((sub - 4) * 8);
                    }

                    // 先判定是否重绘，再只计算一次矩阵：结果同时供阴影表与绘制命令使用。
                    // 非重绘且已有有效缓存时跳过 ComputeMatrices（签名已覆盖所有影响矩阵的输入）。
                    // Hard/Soft 只影响 shadowParams.y（着色器分支），atlas 内容语义（单通道深度）
                    // 相同，故切换无需强制重绘。
                    bool shouldRedraw = ShouldRedrawMap(selected, resident, sub);
                    float softFlag = selected.soft ? 1f : 0f;
                    bool needMatrices = shouldRedraw
                        || !resident.hasMapData
                        || resident.mapData.shadowParams.x != shadowStrength
                        || resident.mapData.shadowParams.y != softFlag
                        || resident.mapData.shadowParams.w != selected.penumbraRadius;

                    if (needMatrices)
                    {
                        if (ComputeMatrices(selected, sub, out Matrix4x4 view, out Matrix4x4 proj, out ShadowSplitData splitData))
                        {
                            ShadowMapData mapData = default;
                            mapData.shadowParams = new Vector4(
                                shadowStrength, softFlag, sub, selected.penumbraRadius);
                            mapData.worldToShadow = GetShadowTransform(proj, view);
                            resident.mapData = mapData;
                            resident.hasMapData = true;

                            if (shouldRedraw)
                            {
                                ShadowDrawCommand command = new ShadowDrawCommand
                                {
                                    view = view,
                                    proj = proj,
                                    allocation = resident.allocation,
                                    lightIndex = lightIndex,
                                    lightType = selected.lightType,
                                    splitData = splitData,
                                    hasCasters = true,
                                    lightPosRange = lightData.lightPosRange,
                                    isDirectional = selected.lightType == LightType.Directional,
                                };

                                if (isDirectionalLight)
                                {
                                    // 方向光的剔除体积取自引擎的组级结果：组内各级共享一次计算，
                                    // 与本级手写正交矩阵解耦（矩阵仍按本级 splits 拟合）。
                                    int groupIndex = CascadeShadowGrouping.FindGroup(cascadeGroupsScratch, sub);
                                    if (groupIndex >= 0)
                                    {
                                        command.splitData = GetDirectionalGroupSplitData(selected, groupIndex);
                                        command.hasCasters = groupSplitDataReady[groupIndex];
                                    }
                                    else
                                    {
                                        command.hasCasters = false;
                                    }
                                }

                                drawCommands.Add(command);
                            }
                        }
                        else
                        {
                            resident.mapData = default;
                            resident.hasMapData = false;
                        }
                    }

                    if ((int)field < shadowMapDatasArray.Length)
                    {
                        shadowMapDatasArray[field] = resident.mapData;
                    }
                }

                // 任一 map 分配失败则禁用该 light 的阴影，避免着色器读到无效槽
                if (!allAllocated)
                {
                    lightData.resolution = 0;
                }

                shadowLightDatasArray[lightIndex] = lightData;
            }

            shadowGlobalParams._ShadowSliceResolution = allocatedResolution;
            shadowGlobalParams._ShadowDepthBias = shadowDepthBias;
            shadowGlobalParams._ShadowBlockerSampleCount = blockerSampleCount;
            shadowGlobalParams._ShadowFilterSampleCount = filterSampleCount;
            // 存量序列化数据新字段读到 0，按默认值兜底，保证级间衔接默认开启。
            shadowGlobalParams._ShadowCascadeBlend = cascadeBlendPercentage > 0f
                ? cascadeBlendPercentage
                : DefaultCascadeBlendPercentage;
            shadowGlobalParams._ShadowParamUnused0 = 0;
            shadowGlobalParams._ShadowParamUnused1 = 0;
            shadowGlobalParams._ShadowParamUnused2 = 0;

            // OnDemand 请求本帧已消费，清空。
            pendingOnDemandRequests.Clear();
        }
        private static uint EncodeField(TextureAllocatorResult allocation)
        {
            return ((uint)allocation.SliceIndex << 6) | (allocation.BlockId & 0x3Fu);
        }

        /// <summary>
        /// 由某方向光的级联设置构建上传给 shader 的相机阴影参数（各级 cascade 远边界）
        /// shader 侧据此与片段深度比较来选级
        /// </summary>
        private static ShadowCameraData BuildShadowCameraData(ShadowCameraSettings settings, int cascadeCount)
        {
            ShadowCameraData data = default;
            List<float> splits = settings.CascadeSplits;
            if (splits == null)
            {
                return data;
            }

            int count = Mathf.Clamp(cascadeCount, 1, splits.Count);
            for (int i = 0; i < 4; i++)
            {
                if (i < count && i < splits.Count)
                {
                    data.cascadeSplits0[i] = splits[i];
                }

                int index = i + 4;
                if (index < count && index < splits.Count)
                {
                    data.cascadeSplits1[i] = splits[index];
                }
            }

            return data;
        }
    }
}
