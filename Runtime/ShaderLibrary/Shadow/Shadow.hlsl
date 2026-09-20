#ifndef HNRP_SHADOW_INCLUDED
#define HNRP_SHADOW_INCLUDED

#include "../Common/Common.hlsl"
#include "../Core/Input.hlsl"

#ifdef HN_DEBUG_PER_PIXEL
/// 每像素调试：本像素最近一次 soft shadow PCSS 采样实际使用的滤波半径（texel）。
/// 由 <see>SamplePcss</see> 写入，供 LitDebug 的 ShadowFilterRadius 通道读取。
float _HNRPShadowDebugPcss;
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

/// 取某方向光第 k 级的远边界（沿相机视轴深度）。
/// 先整段选出 0..3 或 4..7 对应的 float4，再用 k & 3 索引：
/// 三元表达式两侧都会被求值，直接写 cascadeSplits1[k - 4] 会产生负下标。
float GetCascadeSplit(ShadowCameraData cameraData, int k)
{
    float4 splits = k < 4
        ? cameraData.cascadeSplits0
        : cameraData.cascadeSplits1;
    return splits[k & 3];
}

/// 方向光 cascade 选级：按片段沿相机视轴的深度与各级 splits 判定。
/// 透视与正交相机共用同一判据——C# 侧 CalculateFrustumCorners 对两种投影
/// 都在「距相机 distance 的平面」上取角点，故 split 深度即 -viewPos.z。
/// 级间衔接：在本级远端最后 blendPercentage（占分割距离比例）的过渡区内，
/// 用 smoothstep 求权重，再以每像素噪声在「本级 / 下一级」间二选一，
/// 每像素仍只采样一级 cascade，采样总数不变。
/// 返回 -1 表示超出最远级（或级数非法），调用方应视为无阴影。
int GetDirectionalCascadeIndex(
    int cascadeCount,
    int cameraDataIndex,
    float3 positionWS,
    float blendPercentage,
    float noise)
{
    if (cascadeCount <= 0)
    {
        return -1;
    }

    float3 positionVS = mul(UNITY_MATRIX_V, float4(positionWS, 1.0)).xyz;
    float viewDepth = -positionVS.z;

    ShadowCameraData cameraData = _ShadowCameraDatas[cameraDataIndex];

    int baseCascade = -1;
    [loop]
    for (int k = 0; k < cascadeCount; k++)
    {
        if (viewDepth < GetCascadeSplit(cameraData, k))
        {
            baseCascade = k;
            break;
        }
    }

    if (baseCascade < 0)
    {
        return -1;
    }

    // 级间衔接：本级远端最后一段与下一级混合；最后一级没有下一级可混。
    if (blendPercentage > 0.0 && baseCascade < cascadeCount - 1)
    {
        float far = GetCascadeSplit(cameraData, baseCascade);
        if (far > 0.0)
        {
            float blendStart = far * (1.0 - saturate(blendPercentage));
            if (viewDepth > blendStart)
            {
                // 越接近交界权重越高（0 → 1），由噪声决定本像素实际采样哪一级。
                float weight = smoothstep(blendStart, far, viewDepth);
                if (noise < weight)
                {
                    return baseCascade + 1;
                }
            }
        }
    }

    return baseCascade;
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

// ── PCSS 旋转泊松采样 ──

/// 16 点泊松盘（单位圆，Bridson 算法生成，最小间距 ≈ 0.356）。
/// 配合每像素旋转，用较少采样获得去带状的低差异软阴影核。
static const float2 HN_PoissonDisk16[16] =
{
    float2(-0.294818f, -0.008245f),
    float2(-0.630358f, -0.142079f),
    float2(-0.469005f, -0.513162f),
    float2(-0.332239f, -0.841568f),
    float2( 0.037677f, -0.987452f),
    float2( 0.039828f, -0.535887f),
    float2( 0.046544f, -0.119872f),
    float2( 0.411117f, -0.256146f),
    float2( 0.999987f, -0.005137f),
    float2( 0.496805f,  0.153325f),
    float2( 0.376699f,  0.539086f),
    float2( 0.313545f,  0.921518f),
    float2( 0.083827f,  0.251791f),
    float2(-0.106788f,  0.779910f),
    float2(-0.330317f,  0.481360f),
    float2(-0.642503f,  0.282557f),
};

/// 旋转泊松盘采样点（每像素随机旋转，打散采样去相关带来的带状伪影）。
float2 RotatePoissonSample(float2 sample, float angle)
{
    float sinA, cosA;
    sincos(angle, sinA, cosA);
    return float2(
        sample.x * cosA - sample.y * sinA,
        sample.x * sinA + sample.y * cosA);
}

/// PCSS Step1：blocker 搜索。
/// 返回 false 表示搜索核内无遮挡样本（接收者完全受光，可直接早退）。
bool SearchBlocker(
    float2 atlasUV,
    float4 scaleOffset,
    int sliceIndex,
    float searchRadiusTexels,
    float texel,
    float rotation,
    int sampleCount,
    float receiverDepth,
    out float avgBlockerDepth)
{
    avgBlockerDepth = 0.0;
    float blockerSum = 0.0;
    int blockerCount = 0;

    // 采样限制在本 map 的 atlas 区域内，避免软核越界读到其它 light 的槽位。
    float2 minUV = scaleOffset.zw + 0.5 * texel;
    float2 maxUV = scaleOffset.zw + scaleOffset.xy - 0.5 * texel;

    [loop]
    for (int i = 0; i < sampleCount; i++)
    {
        float2 offset = RotatePoissonSample(HN_PoissonDisk16[i], rotation)
            * (searchRadiusTexels * texel);
        float2 uv = clamp(atlasUV + offset, minUV, maxUV);

        float storedDepth = SAMPLE_TEXTURE2D_ARRAY_LOD(
            _ShadowMapArray, sampler_PointClamp, uv, sliceIndex, 0.0).x;

        if (receiverDepth > storedDepth)
        {
            blockerSum += storedDepth;
            blockerCount++;
        }
    }

    if (blockerCount == 0)
    {
        return false;
    }

    avgBlockerDepth = blockerSum / (float)blockerCount;
    return true;
}

/// 传统 PCSS 软阴影可见性（半影随接收者-遮挡物距离变化）。
/// <para>
/// Step1 blocker 搜索：在最大半影核（光源尺寸 = 2^PenumbraRadius texel）内
/// 用旋转泊松盘采样，取遮挡样本平均深度作为 blocker 深度；
/// Step2 由「接收者-遮挡物分离」求动态滤波半径（contact hardening）；
/// Step3 在该半径的旋转泊松盘内做 PCF，得到可见性。
/// 接触处遮挡物就在接收者近旁 → 分离小 → 半径小（锐利）；远离遮挡物 → 半径增大（模糊）。
/// </para>
/// <param name="receiverDepth">已减深度偏置的接收者归一化线性深度。</param>
/// <param name="penumbraRadius">光源尺寸，以 texel 半径的 log2 表示（0–9）。</param>
/// <param name="screenUV">归一化屏幕 UV，用于每像素旋转随机（静态，避免无 TAA 时闪烁）。</param>
float SamplePcss(
    float2 atlasUV,
    float4 scaleOffset,
    int sliceIndex,
    int resolution,
    int lightType,
    float receiverDepth,
    float penumbraRadius,
    float2 screenUV,
    float blockerSampleCount,
    float filterSampleCount)
{
    float texel = 1.0 / max((float)resolution, 1.0);
    float maxRadiusTexels = min(exp2(penumbraRadius), _ShadowMaxFilterRadius);

    // 每像素旋转角：使用 CoreRP InterleavedGradientNoise（静态，无需 TAA）。
    float rotation = InterleavedGradientNoise(screenUV * _ScreenParams.xy, 0) * TWO_PI;

    // ── Step1：blocker 搜索（搜索核 = 光源尺寸 = 最大半影半径）──
    float avgBlockerDepth;
    if (!SearchBlocker(
            atlasUV, scaleOffset, sliceIndex, maxRadiusTexels, texel,
            rotation, (int)blockerSampleCount, receiverDepth, avgBlockerDepth))
    {
        return 1.0;
    }

    // ── Step2：半影估计 → 滤波半径（contact hardening）──
    // 方向光：sep × 2^r / resolution；点 / 聚光：PCSS 原始距离比。
    float separation = max(receiverDepth - avgBlockerDepth, 0.0);
    float filterRadiusTexels = (lightType == 1)
        ? separation * maxRadiusTexels
        : separation / max(avgBlockerDepth, 1e-4) * maxRadiusTexels;

    filterRadiusTexels = clamp(
        filterRadiusTexels * _ShadowSoftness,
        _ShadowMinFilterRadius,
        maxRadiusTexels);

    // ── Step3：半影核内 PCF（旋转泊松盘，逐 texel 深度比较）──
    float2 minUV = scaleOffset.zw + 0.5 * texel;
    float2 maxUV = scaleOffset.zw + scaleOffset.xy - 0.5 * texel;

    float visible = 0.0;
    [loop]
    for (int i = 0; i < (int)filterSampleCount; i++)
    {
        float2 offset = RotatePoissonSample(HN_PoissonDisk16[i], rotation)
            * (filterRadiusTexels * texel);
        float2 uv = clamp(atlasUV + offset, minUV, maxUV);

        float storedDepth = SAMPLE_TEXTURE2D_ARRAY_LOD(
            _ShadowMapArray, sampler_PointClamp, uv, sliceIndex, 0.0).x;

        visible += receiverDepth <= storedDepth ? 1.0 : 0.0;
    }

#ifdef HN_DEBUG_PER_PIXEL
    // 每像素调试：记录本像素实际使用的 PCSS 滤波半径（texel）。
    _HNRPShadowDebugPcss = filterRadiusTexels;
#endif

    return visible / max(filterSampleCount, 1.0);
}

/// 硬阴影手工比较（R16 深度图无硬件比较采样器）：3x3 双线性采样。
float SampleShadowHard(float2 atlasUV, float4 scaleOffset, int sliceIndex, float biasedDepth)
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
                _ShadowMapArray, sampler_LinearClamp, uv, sliceIndex, 0.0).x;
            sum += biasedDepth <= storedDepth ? 1.0 : 0.0;
        }
    }

    return sum / 9.0;
}

float SampleShadowMap(ShadowLightData lightData, uint mapIndex, float3 positionWS, float2 screenUV)
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

    // 统一深度图采样：Soft 走 PCSS，Hard 走点采样 PCF。深度偏置在此统一施加。
    int lightType = GetShadowLightType(lightData);
    float receiverDepth = GetReceiverLinearDepth(lightData, lightType, positionWS, shadowCoord.z);
    float biasedDepth = max(receiverDepth - _ShadowDepthBias, 0.0);
    float attenuation = mapData.shadowParams.y > 0.5
        ? SamplePcss(
            uv, scaleOffset, sliceIndex, lightData.resolution, lightType,
            biasedDepth, mapData.shadowParams.w, screenUV,
            _ShadowBlockerSampleCount, _ShadowFilterSampleCount)
        : SampleShadowHard(uv, scaleOffset, sliceIndex, biasedDepth);

    return lerp(1.0, attenuation, saturate(mapData.shadowParams.x));
}
#endif

/// 计算某光源对世界坐标点的阴影衰减（1 = 无阴影）。
/// <param name="screenUV">归一化屏幕 UV，供软阴影每像素旋转抖动使用。</param>
float GetShadowAttenuation(uint lightIndex, float3 positionWS, float3 lightDirectionWS, float2 screenUV)
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
        // 静态每像素噪声：用于级间衔接在相邻两级间二选一（无 TAA 时不闪烁）。
        float cascadeNoise = InterleavedGradientNoise(screenUV * _ScreenParams.xy, 0);
        int cascadeIndex = GetDirectionalCascadeIndex(
            GetShadowCascadeCount(lightData),
            lightData.cameraDataIndex,
            positionWS,
            _ShadowCascadeBlend,
            cascadeNoise);
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

    return SampleShadowMap(lightData, mapIndex, positionWS, screenUV);
#else
    return 1.0;
#endif
}

#endif
