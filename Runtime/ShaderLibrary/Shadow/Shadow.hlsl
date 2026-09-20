#ifndef HNRP_SHADOW_INCLUDED
#define HNRP_SHADOW_INCLUDED

#include "../Common/Common.hlsl"
#include "../Core/Input.hlsl"

#ifdef HN_DEBUG_PER_PIXEL
/// 每像素调试：本像素最近一次 soft shadow VSSM 采样实际使用的滤波 mip。
/// 由 <see>SampleVssmMoments</see> 写入，供 LitDebug 的 ShadowMip 通道读取。
float _HNRPShadowDebugMip;
#endif

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
// 布局按 16 字节对齐：共 8 个 float。
GLOBAL_CBUFFER_START(_ShadowMapParamsBuffer, b1)
    float    _ShadowSliceResolution;          // atlas 单 slice 分辨率
    float    _ShadowMinVariance;              // VSSM 最小方差（按接收者深度平方缩放的相对量）
    float    _ShadowLightLeakBias;            // VSSM 漏光偏置
    float    _ShadowMaxMipLevel;              // 矩图最大 mip 层级（= 9）
    float    _ShadowMomentDepthBias;          // 矩图手工比较的接收者深度偏置
    float    _ShadowBlockerMinOcc;            // 遮挡分量占比下限（防反解发散 / 无遮挡判据）
    float    _ShadowBlockerOccThreshold;      // 分层 blocker 搜索的遮挡占比阈值
    float    _ShadowMinVarianceDepthFloor;    // 最小方差深度缩放的深度下限
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

/// VSSM 最小方差：随接收者深度平方缩放。
/// 深度是归一化量（世界深度 ≈ 归一化深度 × 级联深度范围），
/// 固定的归一化方差在近 / 远级联（深度范围不同）下对应不同的世界尺度；
/// 按深度平方缩放后，深度容差的相对尺度在各段级联间一致。
float ShadowMinVariance(float receiverDepth)
{
    float scale = max(receiverDepth, _ShadowMinVarianceDepthFloor);
    return _ShadowMinVariance * scale * scale;
}

/// 由半影核矩解出「遮挡分量」的占比与平均深度。
/// <para>
/// 核内深度建模为两个分量：遮挡分量 zOcc（权重 occ）与非遮挡分量 vLit（权重 1−occ）：
/// <code>
/// μ   = occ·zOcc + (1−occ)·vLit
/// Var = occ·(1−occ)·(zOcc − vLit)²
/// </code>
/// 把 vLit 视为已知量即可精确反解（消去 zOcc）：
/// <code>
/// B = vLit − μ   ⇒   occ = B² / (B² + Var)   ⇒   zOcc = vLit − B / occ
/// </code>
/// vLit 取「非遮挡样本」所在深度 = <c>max(接收者深度, 核均值)</c>：
/// <list type="bullet">
/// <item>μ ≤ 接收者深度：非遮挡样本落在接收面自身上（接收面写入阴影图）→ vLit = 接收者深度，
/// 此时反解精确；</item>
/// <item>μ &gt; 接收者深度：核均值比接收者更远离光源，说明核内混入了被 <c>ShadowClear</c> 清成
/// 远平面的空纹素，或仅仅是足迹内的深度起伏 → 取 B = 0 ⇒ occ = 下限 ⇒ 判「无遮挡」。</item>
/// </list>
/// 用 <c>max</c> 保证 B ≥ 0 是刻意的：若允许 B &lt; 0，核均值因足迹内深度起伏略高于接收者深度时
/// 会反解出「≈ 全遮挡」，把受光面整片染黑。故此处一律退化为「无遮挡」（只会偏亮，不会偏黑）。
/// occ 下限 <c>_ShadowBlockerMinOcc</c> 保证 occ→0 时 zOcc 不发散。
/// </para>
/// <remarks>
/// 不用 Chebyshev 的「接收者比核均值更近光 ⇒ 完全受光」硬早退：
/// 模糊核是多表面混合，核均值不代表单一表面，硬早退会把整块核抹成受光（核宽度漏光）；
/// 且它使 occ 恒为 0，进而令 separation 塌到 0（半影在硬 / 软之间二值跳变）。
/// </remarks>
void DecodeBlockerMoments(float2 moments, float receiverDepth, out float occ, out float zOcc)
{
    float variance = max(moments.y - moments.x * moments.x, ShadowMinVariance(receiverDepth));
    float vLit = max(receiverDepth, moments.x);
    float b = vLit - moments.x;
    float b2 = b * b;
    occ = clamp(b2 / max(b2 + variance, 1e-8), _ShadowBlockerMinOcc, 1.0);
    zOcc = clamp(vLit - b / occ, 0.0, receiverDepth);
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

/// PCSS+VSSM 软阴影可见性（半影随接收者-遮挡物距离变化）。
/// <para>
/// 全程走 mip 采样：<see cref="SampleShadowMoments"/> 用 bilinear 混合周围像素，
/// 再按 frac 在相邻两级 mip 间插值。
/// Step1 分层 blocker 搜索：在 mip 链上由细到粗找到**最小的、遮挡占比达阈值的核**，
/// 该核代表「离接收者最近的遮挡面」，其遮挡分量平均深度即 blocker 深度
/// （<see cref="DecodeBlockerMoments"/>，只取被遮挡部分的均值）；
/// Step2 由「接收者-遮挡物分离」求动态滤波半径（contact hardening，上限 2^PenumbraRadius texel）；
/// Step3 在该半径对应 mip 上取矩，用半影核内的遮挡占比求可见性。
/// 接触处遮挡物就在接收者近旁 → 分离小 → 半径→1 texel（锐利）；远离遮挡物 → 半径增大（模糊）。
/// </para>
/// <remarks>
/// 不用单级「最大半影核均值」估计 blocker：核越大，其混合均值越被投射者深处与
/// 空纹素拉偏，blocker 深度会退化为「投射者几何高度」而与接收者-遮挡物距离无关
/// （半影近似常数、无 contact hardening）；且遮挡占比小时反解发散。
/// </remarks>
float SampleVssmMoments(
    float2 atlasUV,
    float4 scaleOffset,
    int sliceIndex,
    int resolution,
    int lightType,
    float receiverDepth,
    float penumbraRadius)
{
    float maxMip = min(log2((float)resolution), _ShadowMaxMipLevel);
    float maxBlurMip = clamp(penumbraRadius, 0.0, maxMip);
    float maxRadiusTexels = exp2(maxBlurMip);

    // ── PCSS Step 1：blocker search（分层，只取被遮挡部分的平均深度）──
    // 粗级先判「有无遮挡」：最大半影核内都没有遮挡样本 ⇒ 接收者完全受光，直接返回。
    float2 coarseMoments = SampleShadowMoments(atlasUV, scaleOffset, sliceIndex, maxBlurMip, maxMip);
    float coarseOcc, coarseBlocker;
    DecodeBlockerMoments(coarseMoments, receiverDepth, coarseOcc, coarseBlocker);

    if (coarseOcc <= _ShadowBlockerMinOcc)
    {
        return 1.0;
    }

    // 遮挡占比随核尺寸单调不减（mip 链是盒滤波金字塔，足迹单调增），
    // 故可二分找到「最小的、遮挡占比达 _ShadowBlockerOccThreshold 的核尺寸」。
    // 取最小核 ⇒ 取离接收者最近的遮挡面 ⇒ 半影随接收者-遮挡物距离变化。
    int lo = 0;
    int hi = (int)maxBlurMip;
    float avgBlocker = coarseBlocker;

    [loop]
    while (lo <= hi)
    {
        int mid = (lo + hi) >> 1;
        float2 moments = SampleShadowMoments(atlasUV, scaleOffset, sliceIndex, (float)mid, maxMip);
        float occ, zOcc;
        DecodeBlockerMoments(moments, receiverDepth, occ, zOcc);

        if (occ >= _ShadowBlockerOccThreshold)
        {
            avgBlocker = zOcc;
            hi = mid - 1;
        }
        else
        {
            lo = mid + 1;
        }
    }

    // ── PCSS Step 2：半影尺寸 → 滤波半径（contact hardening） ──
    // 方向光：penumbra = tanθ × 世界分离 / texel世界尺寸；取 tanθ = 2^r/resolution 后得 sep × 2^r。
    // 点/聚光：PCSS 原始距离比。
    float separation = max(receiverDepth - avgBlocker, 0.0);
    float filterRadiusTexels = (lightType == 1)
        ? separation * maxRadiusTexels
        : separation / max(avgBlocker, 1e-4) * maxRadiusTexels;

    filterRadiusTexels = clamp(filterRadiusTexels, 1.0, maxRadiusTexels);

    // ── PCSS Step 3：半影核内的「遮挡占比」即可见性 ──
    // 滤波核取到半影尺寸后，核内深度只有「遮挡物」与「非遮挡面」两类，
    // 遮挡占比即该像素被挡住的份额 ⇒ 可见性 = 1 − occ。
    // 注意：光栅化用的是同一套核采样路径（bilinear + 相邻两级 mip 插值）。
    float filterMip = clamp(log2(filterRadiusTexels), 0.0, maxMip);

    float2 filtered = SampleShadowMoments(atlasUV, scaleOffset, sliceIndex, filterMip, maxMip);
    float filterOcc, filterBlocker;   // filterBlocker 仅供反解输出，此处只用占比
    DecodeBlockerMoments(filtered, receiverDepth, filterOcc, filterBlocker);

    // 漏光偏置：压低受光（与旧的 Chebyshev 路径语义一致）。
    float result = saturate(((1.0 - filterOcc) - _ShadowLightLeakBias)
        / max(1.0 - _ShadowLightLeakBias, 1e-4));
#ifdef HN_DEBUG_PER_PIXEL
    // 每像素调试：记录本像素实际使用的 soft shadow 滤波 mip。
    _HNRPShadowDebugMip = filterMip;
#endif
    return result;
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
            uv, scaleOffset, sliceIndex, lightData.resolution, lightType,
            biasedDepth, mapData.shadowParams.w)
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
