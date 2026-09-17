// <copyright file="DrawShadowPass.Types.cs" company="HN">
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
        // ── 数据结构 ──

        private sealed class ResidentMap
        {
            public int lightIndex;
            public int subIndex;
            public LightType lightType;
            public int resolution;
            public TextureAllocatorResult allocation;

            /// <summary>上次重绘帧号；-1 表示刚分配尚未绘制。</summary>
            public int lastUpdateFrame;

            /// <summary>是否已记录参数签名。</summary>
            public bool hasSignature;

            /// <summary>光源 / 相机参数签名，用于检测是否需重绘。</summary>
            public LightSignature signature;

            /// <summary>本 map 最近一次计算的阴影表数据（矩阵派生），未重算时跨帧复用。</summary>
            public ShadowMapData mapData;

            /// <summary>是否已缓存有效的 <see cref="mapData"/>。</summary>
            public bool hasMapData;

            /// <summary>本帧是否仍被需要（O(驻留 释放遍历）/summary>
            public bool neededThisFrame;
        }

        private struct SelectedLight
        {
            public int lightIndex;
            public LightType lightType;
            public int resolution;

            /// <summary>该光源占用的 map 数（方向= 相机级联级数；否则由类型决定）/summary>
            public int cascadeCount;

            /// <summary>该光源生效的级联设置（方向光 override 时为光源自身设置，否则为相机设置）。</summary>
            public ShadowCameraSettings cascadeSettings;

            /// <summary>该光源级联级数与分割的哈希，用于签名比较。</summary>
            public int cascadeHash;

            /// <summary>方向光在 <c>_ShadowCameraDatas</c> 中的槽位；非方向光为 -1。</summary>
            public int cameraDataIndex;

            public Matrix4x4 lightLocalToWorld;
            public float lightRange;
            public float lightSpotAngle;
            public int lightInstanceId;
        }

        private struct ShadowDrawCommand
        {
            public Matrix4x4 view;
            public Matrix4x4 proj;
            public TextureAllocatorResult allocation;

            /// <summary>投射光源在 <c>CullingResults.visibleLights</c> 中的下标（供 <c>DrawShadows</c> 定位投射者集合）。</summary>
            public int lightIndex;

            /// <summary>投射光源类型（决定 <c>DrawShadows</c> 的投影类型）。</summary>
            public LightType lightType;

            /// <summary>本 map 的裁剪数据（供引擎二次裁剪投射者）。</summary>
            public ShadowSplitData splitData;

            /// <summary>
            /// 本 map 是否存在可用投射者。方向光取组级剔除结果失败时为 <c>false</c>：
            /// 仍执行清屏（避免残留上一帧内容），但不提交投射者绘制。
            /// </summary>
            public bool hasCasters;
        }

        private struct ShadowCopyCommand
        {
            public int fromSlice;
            public Vector4 fromScaleOffset;
            public int toSlice;
            public Vector4 toScaleOffset;
        }

        /// <summary>
        /// 光源与相机参数签名，用于检测阴影是否需要重绘。
        /// </summary>
        private struct LightSignature : System.IEquatable<LightSignature>
        {
            public Matrix4x4 lightMatrix;
            public float lightRange;
            public float spotAngle;
            public int resolution;
            public int subIndex;
            public Matrix4x4 cameraView;
            public Matrix4x4 cameraProj;
            public int cascadeHash;

            public bool Equals(LightSignature other)
            {
                return lightMatrix == other.lightMatrix
                    && lightRange == other.lightRange
                    && spotAngle == other.spotAngle
                    && resolution == other.resolution
                    && subIndex == other.subIndex
                    && cameraView == other.cameraView
                    && cameraProj == other.cameraProj
                    && cascadeHash == other.cascadeHash;
            }

            public override bool Equals(object obj)
            {
                return obj is LightSignature other && Equals(other);
            }

            public override int GetHashCode()
            {
                return subIndex ^ resolution;
            }
        }

        private sealed class DrawShadowPassData
        {
            public TextureHandle shadowMap;
            public ComputeBufferHandle lightDatasBuffer;
        }
    }
}
