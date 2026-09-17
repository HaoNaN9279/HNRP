// <copyright file="DrawShadowPass.Redraw.cs" company="HN">
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
        /// <summary>
        /// 判定某张 map 本帧是否需要重绘（新增 / 参数变化 / 更新模式）。
        /// </summary>
        private bool ShouldRedrawMap(SelectedLight selected, ResidentMap resident, int sub)
        {
            LightSignature signature = ComputeSignature(selected, sub, resident.resolution);
            bool redraw;

            if (!resident.hasSignature || !resident.signature.Equals(signature))
            {
                redraw = true;
            }
            else
            {
                switch (selected.cascadeSettings.ShadowUpdateMode)
                {
                    case ShadowUpdateModeType.OnDemand:
                        redraw = pendingOnDemandRequests.Contains(selected.lightInstanceId);
                        break;

                    case ShadowUpdateModeType.Custom:
                        // 相位由用户按级配置（而非按光源实例 id 推导）：同 interval 的各级
                        // 可通过配置不同相位错开到不同帧更新，避免在同一帧集中重绘造成卡顿尖峰。
                        redraw = CascadeShadowUtils.ShouldUpdateOnFrame(
                            Time.frameCount,
                            GetTimeSlice(selected.cascadeSettings, sub),
                            GetPhaseFrame(selected.cascadeSettings, sub));
                        break;

                    default:
                        redraw = true;
                        break;
                }
            }

            // 刚分配的 map 一定重绘。
            if (resident.lastUpdateFrame < 0)
            {
                redraw = true;
            }

            if (redraw)
            {
                resident.signature = signature;
                resident.hasSignature = true;
                resident.lastUpdateFrame = Time.frameCount;
            }

            return redraw;
        }

        private LightSignature ComputeSignature(SelectedLight selected, int sub, int resolution)
        {
            LightSignature signature = default;
            signature.lightMatrix = selected.lightLocalToWorld;
            signature.lightRange = selected.lightRange;
            signature.spotAngle = selected.lightSpotAngle;
            signature.resolution = resolution;
            signature.subIndex = sub;

            // 方向光 cascade 依赖相机与级数 / 分割，任一变化都需重绘并重算矩阵。
            if (selected.lightType == LightType.Directional)
            {
                signature.cameraView = cachedCameraViewMatrix;
                signature.cameraProj = cachedCameraProjMatrix;
                signature.cascadeHash = selected.cascadeHash;
            }

            return signature;
        }

        /// <summary>
        /// 计算方向光 cascade 级数、分割与分组交界的哈希，用于签名比较与缓存失效判断。
        /// 分组交界影响每级所用的剔除体积（可能间接影响可见阴影），必须纳入哈希。
        /// </summary>
        private static int ComputeCascadeHash(int cascadeCount, List<float> splits, List<int> splitGroups)
        {
            int hash = cascadeCount * 397;
            if (splits != null)
            {
                for (int i = 0; i < splits.Count; i++)
                {
                    hash = (hash * 31) + splits[i].GetHashCode();
                }
            }

            if (splitGroups != null)
            {
                for (int i = 0; i < splitGroups.Count; i++)
                {
                    hash = (hash * 31) + splitGroups[i];
                }
            }

            return hash;
        }

        private int GetTimeSlice(ShadowCameraSettings settings, int sub)
        {
            List<int> slices = settings.CascadeTimeSlices;
            if (slices == null || slices.Count == 0)
            {
                return 0;
            }

            int index = Mathf.Clamp(sub, 0, slices.Count - 1);
            return slices[index];
        }

        /// <summary>
        /// 取某级 cascade 在 Custom 更新模式下配置的相位帧。
        /// </summary>
        /// <param name="settings">该光生效的级联设置。</param>
        /// <param name="sub">map 在光源内的子索引（cascade / 面）。</param>
        /// <returns>该级的相位帧；未配置时为 <c>0</c>。</returns>
        private int GetPhaseFrame(ShadowCameraSettings settings, int sub)
        {
            List<int> phases = settings.CascadePhaseFrames;
            if (phases == null || phases.Count == 0)
            {
                return 0;
            }

            int index = Mathf.Clamp(sub, 0, phases.Count - 1);
            return phases[index];
        }
    }
}
