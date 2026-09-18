#ifndef HNRP_SHADOW_INCLUDED
#define HNRP_SHADOW_INCLUDED

#include "../Common/Common.hlsl"
#include "../Core/Input.hlsl"

// ── GPU 数据布局（须与 C# DrawShadowPass.cs 一致）──

/// 按 light 的阴影元数据（两级查表第一级）。
struct ShadowLightData
{
    float4 lightPosRange;        // xyz=光源世界位置，w=1/range（方向光为 0）
    int    lightIndex;           // = buffer 下标
    int    resolution;           // 单张 map 分辨率；0 = 该 light 无阴影
    uint   blockDatas0;          // map 0..3 的 atlas 位置，每张 8 位 = (slice << 6) | blockId
    uint   blockDatas1;          // map 4..7 的 atlas 位置
    uint   typeAndCascadeCount;  // bit0..7=lightType, bit8..15=cascadeCount
    int    cameraDataIndex;      // 方向光：_ShadowCameraDatas 下标；其他类型无意义
};

/// 每方向光阴影参数（按方向光槽位索引）：方向光 cascade 的各级远边界（沿相机视轴深度）。
struct ShadowCameraData
{
    float4 cascadeSplits0;       // cascade 0..3
    float4 cascadeSplits1;       // cascade 4..7
};

/// 按 atlas 槽的阴影数据（两级查表第二级）。buffer 下标 = atlas 位置字段。
struct ShadowMapData
{
    float4   shadowParams;   // x=strength, y=soft(0/1), z=cascadeIndex/faceIndex, w=penumbraRadius
    float4x4 worldToShadow;  // 世界 → 阴影裁剪（含 [0,1] remap，不含 atlas offset）
};

// 阴影全局标量常量缓冲（b1）。
// 布局按 16 字节对齐：标量行占 1 个寄存器；未来新增标量可接管 padding。
GLOBAL_CBUFFER_START(_ShadowMapParamsBuffer, b1)
    float    _ShadowSliceResolution;   // atlas 单 slice 分辨率
    float    _ShadowMinVariance;       // VSSM 最小方差
    float    _ShadowLightLeakBias;     // VSSM 漏光偏置
    float    _ShadowMaxMipLevel;       // 矩图最大 mip 层级（= 9）
    float    _ShadowMomentDepthBias;   // 矩图手工比较的接收者深度偏置
    float    _ShadowBlockerSearchMip;  // VSSM blocker search 采样 mip
    float2   _ShadowMapParamsPadding;  // 16 字节对齐占位（无数据语义）
CBUFFER_END

#define _SHADOW_SLICE_RESOLUTION (_ShadowSliceResolution)

#if defined(SHADOW_MAP)
StructuredBuffer<ShadowLightData> _ShadowLightDatas;
StructuredBuffer<ShadowMapData> _ShadowMapDatas;
StructuredBuffer<ShadowCameraData> _ShadowCameraDatas;
// 阴影图集为单张 RG16F 矩图（R=线性深度，G=深度²），无硬件比较采样器。
// 采样器名绑定纹理自身设置（Bilinear / Clamp），避免与 core 全局 sampler_LinearClamp 重定义。
TEXTURE2D_ARRAY(_ShadowMapArray);
SAMPLER(sampler_ShadowMapArray);
#endif

// ── 两级查表 ──

/// 取该 light 第 k 张 map 的 8 位 atlas 位置（同时是 ShadowMapData 下标）。
uint GetShadowMapIndex(ShadowLightData lightData, int k)
{
    uint word = k < 4 ? lightData.blockDatas0 : lightData.blockDatas1;
    return (word >> ((k & 3) * 8)) & 0xFFu;
}

/// 光源类型（1=方向 2=点 3=聚光，与 _LightDatasBuffer.lightType 约定一致）。
int GetShadowLightType(ShadowLightData lightData)
{
    return (int)(lightData.typeAndCascadeCount & 0xFFu);
}

/// 方向光有效 cascade 数（C# 已算好，直接读，避免逐像素 8 次循环）。
int GetShadowCascadeCount(ShadowLightData lightData)
{
    return (int)((lightData.typeAndCascadeCount >> 8) & 0xFFu);
}

#if defined(SHADOW_MAP)
/// 由 atlas 位置字段解出采样所需的 scaleOffset 与 slice。
void DecodeShadowPosition(uint field, int resolution, out float4 scaleOffset, out int sliceIndex)
{
    uint blockId = field & 0x3Fu;
    sliceIndex = int(field >> 6);

    float perAxis = max(1.0, _ShadowSliceResolution / 512.0);
    float scale = resolution / _ShadowSliceResolution;

    uint xId = 0u;
    uint yId = 0u;
    [unroll]
    for (int i = 0; i < 3; i++)
    {
        xId |= ((blockId >> (i * 2)) & 1u) << i;
        yId |= ((blockId >> (i * 2 + 1)) & 1u) << i;
    }

    scaleOffset = float4(scale, scale, xId / perAxis, yId / perAxis);
}

/// 方向光 cascade 选级：按片段沿相机视轴的深度与各级 splits 判定。
/// 透视与正交相机共用同一判据——C# 侧 CalculateFrustumCorners 对两种投影
/// 都在「距相机 distance 的平面」上取角点，故 split 深度即 -viewPos.z。
/// 返回 -1 表示超出最远级（或级数非法），调用方应视为无阴影。
int GetDirectionalCascadeIndex(int cascadeCount, int cameraDataIndex, float3 positionWS)
{
    if (cascadeCount <= 0)
    {
        return -1;
    }

    float3 positionVS = mul(UNITY_MATRIX_V, float4(positionWS, 1.0)).xyz;
    float viewDepth = -positionVS.z;

    ShadowCameraData cameraData = _ShadowCameraDatas[cameraDataIndex];

    [loop]
    for (int k = 0; k < cascadeCount; k++)
    {
        // 先整段选出 0..3 或 4..7 对应的 float4，再用 k & 3 索引：
        // 三元表达式两侧都会被求值，直接写 cascadeSplits1[k - 4] 会产生负下标。
        float4 splits = k < 4
            ? cameraData.cascadeSplits0
            : cameraData.cascadeSplits1;
        float split = splits[k & 3];

        if (viewDepth < split)
        {
            return k;
        }
    }

    return -1;
}

/// 归一化线性深度（0=近光，1=远光），与投射者写入端（LitShadowCaster）严格同式。
float GetReceiverLinearDepth(ShadowLightData lightData, int lightType, float3 positionWS, float clipZ)
{
    float linearDepth;
    if (lightType == 2 || lightType == 3) // Point / Spot：透视，用距离 / range
    {
        linearDepth = length(positionWS - lightData.lightPosRange.xyz) * lightData.lightPosRange.w;
    }
    else
    {
        linearDepth = clipZ;
#if UNITY_REVERSED_Z
        linearDepth = 1.0 - linearDepth;
#endif
    }

    return saturate(linearDepth);
}

/// Chebyshev 单边上界：返回 P(深度 >= 接收者)（1 = 受光）。
float ChebyshevUpperBound(float2 moments, float receiverDepth)
{
    float variance = max(moments.y - moments.x * moments.x, _ShadowMinVariance);
    float d = receiverDepth - moments.x;
    float p = variance / (variance + d * d);
    p = saturate((p - _ShadowLightLeakBias) / max(1.0 - _ShadowLightLeakBias, 1e-4));
    return max(p, receiverDepth <= moments.x ? 1.0 : 0.0);
}

/// 取任意滤波半径（以 mip 层级表示）的深度均值 / 深度平方均值。
/// 区域归一化边界与 mip 无关，仅半 texel 内缩随 mip 变化；
/// 相邻两级 mip 线性插值（类 Trilinear），支持小数 mip。
float2 SampleShadowMoments(float2 atlasUV, float4 scaleOffset, int sliceIndex, float mip, float maxMip)
{
    float level = clamp(mip, 0.0, maxMip);
    float atlasResAtMip = _ShadowSliceResolution / exp2(level);
    float2 inset = 0.5 / max(atlasResAtMip, 1.0);
    float2 uv = clamp(atlasUV, scaleOffset.zw + inset, scaleOffset.zw + scaleOffset.xy - inset);

    float level0 = floor(level);
    float frac = level - level0;
    float2 m0 = SAMPLE_TEXTURE2D_ARRAY_LOD(
        _ShadowMapArray, sampler_ShadowMapArray, uv, sliceIndex, level0).xy;
    if (frac <= 0.0)
    {
        return m0;
    }

    float2 m1 = SAMPLE_TEXTURE2D_ARRAY_LOD(
        _ShadowMapArray, sampler_ShadowMapArray, uv, sliceIndex, min(level0 + 1.0, maxMip)).xy;
    return lerp(m0, m1, frac);
}

/// VSSM 软阴影可见性：blocker search → penumbra 估计 → 可变半径滤波，全部经矩均值完成。
float SampleVssmMoments(
    float2 atlasUV,
    float4 scaleOffset,
    int sliceIndex,
    int resolution,
    float receiverDepth,
    float penumbraRadius)
{
    float maxMip = min(log2((float)resolution), _ShadowMaxMipLevel);

    // Step 1：blocker search（以较低 mip 近似核内均值）。
    float searchMip = clamp(_ShadowBlockerSearchMip, 0.0, maxMip);
    float2 moments = SampleShadowMoments(atlasUV, scaleOffset, sliceIndex, searchMip, maxMip);
    float pLit = ChebyshevUpperBound(moments, receiverDepth);
    float pOcc = 1.0 - pLit;

    // Step 2：VSSM 假设 z_unocc = receiverDepth，反解遮挡物平均深度。
    float avgBlocker = moments.x;
    if (pOcc > 1e-4)
    {
        avgBlocker = (moments.x - pLit * receiverDepth) / pOcc;
    }

    avgBlocker = clamp(avgBlocker, 0.0, max(receiverDepth, 0.0));

    // Step 3：Penumbra 尺寸 → 滤波 mip（PenumbraRadius 为光源尺寸的 log2 texel 半径）。
    float lightRadiusTexels = exp2(max(penumbraRadius, 0.0));
    float penumbraTexels = (receiverDepth - avgBlocker) / max(avgBlocker, 1e-4) * lightRadiusTexels;
    float filterMip = clamp(log2(max(penumbraTexels, 1.0)), 0.0, maxMip);

    float2 filtered = SampleShadowMoments(atlasUV, scaleOffset, sliceIndex, filterMip, maxMip);
    return ChebyshevUpperBound(filtered, receiverDepth);
}

/// 硬阴影手工比较（矩图无硬件比较采样器）：3x3 点采样。
float SampleMomentsHard(float2 atlasUV, float4 scaleOffset, int sliceIndex, float biasedDepth)
{
    float texel = 1.0 / _ShadowSliceResolution;
    float2 minUV = scaleOffset.zw + 0.5 * texel;
    float2 maxUV = scaleOffset.zw + scaleOffset.xy - 0.5 * texel;

    float sum = 0.0;
    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            float2 uv = clamp(atlasUV + float2(x, y) * texel, minUV, maxUV);
            float storedDepth = SAMPLE_TEXTURE2D_ARRAY_LOD(
                _ShadowMapArray, sampler_ShadowMapArray, uv, sliceIndex, 0.0).x;
            sum += biasedDepth <= storedDepth ? 1.0 : 0.0;
        }
    }

    return sum / 9.0;
}

float SampleShadowMap(ShadowLightData lightData, uint mapIndex, float3 positionWS)
{
    ShadowMapData mapData = _ShadowMapDatas[mapIndex];

    float4 scaleOffset;
    int sliceIndex;
    DecodeShadowPosition(mapIndex, lightData.resolution, scaleOffset, sliceIndex);

    float4 shadowCoord = mul(mapData.worldToShadow, float4(positionWS, 1.0));
    if (shadowCoord.w <= 0.0)
    {
        return 1.0;
    }
    shadowCoord.xyz /= shadowCoord.w;
    
    if (shadowCoord.z <= 0.0 || shadowCoord.z >= 1.0)
    {
        return 1.0;
    }

    float2 uv = shadowCoord.xy * scaleOffset.xy + scaleOffset.zw;

    // 统一矩图采样：Soft 走 VSSM，Hard 走点采样 PCF。深度偏置在此统一施加。
    int lightType = GetShadowLightType(lightData);
    float receiverDepth = GetReceiverLinearDepth(lightData, lightType, positionWS, shadowCoord.z);
    float biasedDepth = max(receiverDepth - _ShadowMomentDepthBias, 0.0);
    float attenuation = mapData.shadowParams.y > 0.5
        ? SampleVssmMoments(
            uv, scaleOffset, sliceIndex, lightData.resolution, biasedDepth, mapData.shadowParams.w)
        : SampleMomentsHard(uv, scaleOffset, sliceIndex, biasedDepth);

    return lerp(1.0, attenuation, saturate(mapData.shadowParams.x));
}
#endif

/// 计算某光源对世界坐标点的阴影衰减（1 = 无阴影）。
float GetShadowAttenuation(uint lightIndex, float3 positionWS, float3 lightDirectionWS)
{
#if defined(SHADOW_MAP)
    ShadowLightData lightData = _ShadowLightDatas[lightIndex];
    if (lightData.resolution <= 0)
    {
        return 1.0;
    }

    int lightType = GetShadowLightType(lightData);
    uint mapIndex = 0u;

    if (lightType == 2 /* Point */)
    {
        int face = CubeMapFaceID(-lightDirectionWS);
        mapIndex = GetShadowMapIndex(lightData, face);
    }
    else if (lightType == 1 /* Directional */)
    {
        int cascadeIndex = GetDirectionalCascadeIndex(
            GetShadowCascadeCount(lightData), lightData.cameraDataIndex, positionWS);
        if (cascadeIndex < 0)
        {
            return 1.0;
        }

        mapIndex = GetShadowMapIndex(lightData, cascadeIndex);
    }
    else /* Spot */
    {
        mapIndex = GetShadowMapIndex(lightData, 0);
    }

    return SampleShadowMap(lightData, mapIndex, positionWS);
#else
    return 1.0;
#endif
}

#endif
