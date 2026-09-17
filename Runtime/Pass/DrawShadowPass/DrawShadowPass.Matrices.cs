// <copyright file="DrawShadowPass.Matrices.cs" company="HN">
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
        /// 计算第 <paramref name="sub"/> 张 map 的视图 / 投影矩阵与裁剪数据。
        /// </summary>
        /// <param name="selected">本 map 所属的光源。</param>
        /// <param name="sub">map 在光源内的子索引（cascade / 面）。</param>
        /// <param name="view">输出的光源视图矩阵。</param>
        /// <param name="proj">输出的光源投影矩阵。</param>
        /// <param name="splitData">
        /// 输出的裁剪数据，供 <c>DrawShadows</c> 二次裁剪投射者。
        /// 方向光返回 <c>default</c>：其剔除体积由 <see cref="GetDirectionalGroupSplitData"/> 按组提供。
        /// </param>
        private bool ComputeMatrices(
            SelectedLight selected,
            int sub,
            out Matrix4x4 view,
            out Matrix4x4 proj,
            out ShadowSplitData splitData)
        {
            view = Matrix4x4.identity;
            proj = Matrix4x4.identity;
            splitData = default;

            CullingResults cullingResults = cameraContext.CullingResults;
            switch (selected.lightType)
            {
                case LightType.Directional:
                    return ComputeDirectionalMatrices(selected, sub, out view, out proj);

                case LightType.Spot:
                    return cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(
                        selected.lightIndex, out view, out proj, out splitData);

                case LightType.Point:
                {
                    if (!cullingResults.ComputePointShadowMatricesAndCullingPrimitives(
                            selected.lightIndex, (CubemapFace)sub, PointLightFovBias,
                            out view, out proj, out splitData))
                    {
                        return false;
                    }

                    // 对齐 URP：翻转 view 的第三行，修正点光面与聚光面法线偏置不一致。
                    view.m10 = -view.m10;
                    view.m11 = -view.m11;
                    view.m12 = -view.m12;
                    view.m13 = -view.m13;
                    return true;
                }

                default:
                    return false;
            }
        }

        /// <summary>
        /// 手写方向光级联矩阵：用相机子视锥拟合球体，再绕光方向建正交投影。
        /// 使用世界空间 split，支持任意级数。
        /// 透视与正交相机共用同一split 语义 —<c>CalculateFrustumCorners</c>
        /// 对两种投影都在「距相机 <c>distance</c> 的平面」上取角点
        /// </summary>
        private bool ComputeDirectionalMatrices(
            SelectedLight selected,
            int cascadeIndex,
            out Matrix4x4 view,
            out Matrix4x4 proj)
        {
            view = Matrix4x4.identity;
            proj = Matrix4x4.identity;

            Camera camera = cameraContext.Camera;
            List<float> splits = selected.cascadeSettings.CascadeSplits;
            if (camera == null || splits == null || cascadeIndex >= selected.cascadeCount)
            {
                return false;
            }

            float near = cascadeIndex == 0
                ? camera.nearClipPlane
                : splits[cascadeIndex - 1];
            float far = splits[Mathf.Min(cascadeIndex, splits.Count - 1)];
            if (far <= near)
            {
                far = near + 1f;
            }

            Vector3[] nearCorners = nearCornersScratch;
            Vector3[] farCorners = farCornersScratch;
            camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), near, camera.stereoActiveEye, nearCorners);
            camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), far, camera.stereoActiveEye, farCorners);

            for (int i = 0; i < 4; i++)
            {
                nearCorners[i] = camera.transform.TransformPoint(nearCorners[i]);
                farCorners[i] = camera.transform.TransformPoint(farCorners[i]);
            }

            Vector3 center = Vector3.zero;
            for (int i = 0; i < 4; i++)
            {
                center += nearCorners[i] + farCorners[i];
            }
            center /= 8f;

            float radius = 0f;
            for (int i = 0; i < 4; i++)
            {
                radius = Mathf.Max(radius, (nearCorners[i] - center).magnitude);
                radius = Mathf.Max(radius, (farCorners[i] - center).magnitude);
            }
            radius = Mathf.Max(radius, 0.001f);

            Vector3 lightForward = selected.lightLocalToWorld.GetColumn(2);
            Quaternion lightRotation = Quaternion.LookRotation(lightForward, Vector3.up);
            Vector3 viewPosition = center - lightForward * (radius + shadowNearPlaneOffset);

            // Unity 视图空间沿 -Z 观察（OpenGL 约定），而 TRS(...).inverse 得到的是沿 +Z
            // 的灯光局部空间；必须左乘 Z 翻转，否则正交投影会打反 z 符号，
            // 使投影深度落在 [0,1] 之外——caster 被裁掉、采样恒返回「无阴影」。
            Matrix4x4 lightToWorld = Matrix4x4.TRS(viewPosition, lightRotation, Vector3.one);
            view = Matrix4x4.Scale(new Vector3(1f, 1f, -1f)) * lightToWorld.inverse;
            proj = Matrix4x4.Ortho(-radius, radius, -radius, radius, 0f, 2f * radius + shadowNearPlaneOffset);

            // 选级不依赖裁剪球：shader 用相splits 与片段视轴深度直接判定（Shadow.hlsl）
            return true;
        }

        /// <summary>
        /// 取某方向光某组的引擎剔除结果：组内各级共享一次计算，帧内缓存。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 引擎的方向光剔除接口只支持 1/2/4 级，故 8 级按分组交界合并为 ≤4 组后按组索取；
        /// 每级的手写正交矩阵仍按本级 splits 拟合，只有「从裁剪结果中取哪些投射者」
        /// 这一步使用组级体积（组体积是该组所有级联范围的超集，故不会漏投影）。
        /// </para>
        /// <para>
        /// 分割比例的分母必须与 Cull 期实际生效的 <c>shadowDistance</c> 一致
        /// （见 <see cref="CameraContext.ShadowDistance"/>），否则引擎算出的绝对边界
        /// 与本项目 splits 错位，远端级联会取不到投射者。
        /// </para>
        /// </remarks>
        /// <param name="selected">所属方向光。</param>
        /// <param name="groupIndex">组下标（0 起）。</param>
        /// <returns>该组的剔除结果；引擎计算失败时返回 <c>default</c> 且对应 ready 标记为 <c>false</c>。</returns>
        private ShadowSplitData GetDirectionalGroupSplitData(SelectedLight selected, int groupIndex)
        {
            if (groupIndex < 0 || groupIndex >= groupSplitDataScratch.Length)
            {
                return default;
            }

            if (groupSplitDataReady[groupIndex])
            {
                return groupSplitDataScratch[groupIndex];
            }

            float shadowDistance = ResolveFrameShadowDistance(selected);
            if (shadowDistance <= 0f)
            {
                return default;
            }

            // 空组已被分组逻辑排除，此处恒有 [1, MaxGroupCount] 个组。
            int splitCount = Mathf.Clamp(cascadeGroupsScratch.Count, 1, ShadowCameraSettings.MaxGroupCount);
            Vector3 ratios = CascadeShadowGrouping.BuildSplitRatio(
                selected.cascadeSettings.CascadeSplits,
                cascadeGroupsScratch,
                shadowDistance);

            if (cameraContext.CullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                    selected.lightIndex,
                    groupIndex,
                    splitCount,
                    ratios,
                    selected.resolution,
                    shadowNearPlaneOffset,
                    out _,
                    out _,
                    out ShadowSplitData splitData))
            {
                // 不做跨级联混合裁剪，避免组体积被进一步收缩而漏掉组内其它级的投射者。
                splitData.shadowCascadeBlendCullingFactor = 1.0f;
                groupSplitDataScratch[groupIndex] = splitData;
                groupSplitDataReady[groupIndex] = true;
            }

            return groupSplitDataScratch[groupIndex];
        }

        /// <summary>
        /// 取本帧用于归一化分割比例的阴影距离；缺失时退回该光自身的最远 splits。
        /// </summary>
        private float ResolveFrameShadowDistance(SelectedLight selected)
        {
            float shadowDistance = cameraContext.ShadowDistance;
            if (shadowDistance > 0f)
            {
                return shadowDistance;
            }

            List<float> splits = selected.cascadeSettings.CascadeSplits;
            return splits != null && splits.Count > 0 ? splits[splits.Count - 1] : 0f;
        }

        /// <summary>
        /// 构建「世界 → 阴影图坐标」矩阵（含 xy 的 [0,1] remap，不含 atlas 偏移）。
        /// </summary>
        /// <remarks>
        /// 深度与绘制侧严格同源：绘制用 GL.GetGPUProjectionMatrix(proj, true)，
        /// 它包含平台的 z 映射（reversed-Z 平台 near=1 / far=0）与渲染到纹理所需的 Y 翻转；
        /// 采样用 GL.GetGPUProjectionMatrix(proj, false)，保留同一 z 映射但不做 Y 翻转，
        /// 使采样的 UV（左下原点）与渲染结果对齐。这里只补上裁剪空间 xy 的 [-1,1] → [0,1] 映射。
        /// </remarks>
        private static Matrix4x4 GetShadowTransform(Matrix4x4 proj, Matrix4x4 view)
        {
            Matrix4x4 worldToShadow = GL.GetGPUProjectionMatrix(proj, false) * view;
            Matrix4x4 textureScaleAndBias = Matrix4x4.identity;
            textureScaleAndBias.m00 = 0.5f;
            textureScaleAndBias.m11 = 0.5f;
            textureScaleAndBias.m03 = 0.5f;
            textureScaleAndBias.m13 = 0.5f;
            return textureScaleAndBias * worldToShadow;
        }

        /// <summary>
        /// 把光源投影矩阵转换为「渲染阴影图」用的 GPU 投影矩阵。
        /// </summary>
        /// <remarks>
        /// renderIntoTexture: true 会按平台追加 z 映射与 Y 翻转：D3D 上渲染到纹理必须翻转 Y
        /// </remarks>
        private static Matrix4x4 GetShadowDrawProjection(Matrix4x4 proj)
        {
            return GL.GetGPUProjectionMatrix(proj, true);
        }
    }
}
