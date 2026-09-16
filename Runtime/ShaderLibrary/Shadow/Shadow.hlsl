#ifndef HNRP_SHADOW_INCLUDED
#define HNRP_SHADOW_INCLUDED

#include "../Common/Common.hlsl"
#include "../Core/Input.hlsl"

// ── GPU 数据布局（须与 C# DrawShadowPass.cs 一致）──

/// 按 light 的阴影元数据（两级查表第一级）。
struct ShadowLightData
{
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
    float4   shadowParams;   // x=strength, y=soft, z=cascadeIndex/faceIndex
    float4x4 worldToShadow;  // 世界 → 阴影裁剪（含 [0,1] remap，不含 atlas offset）
};

// 阴影全局标量常量缓冲（b1）。
// 布局按 16 字节对齐：标量行占 1 个寄存器；未来新增标量可接管 padding。
GLOBAL_CBUFFER_START(_ShadowMapParamsBuffer, b1)
    float    _ShadowSliceResolution;   // atlas 单 slice 分辨率
    float3   _ShadowMapParamsPadding;  // 16 字节对齐占位（无数据语义）
CBUFFER_END

#define _SHADOW_SLICE_RESOLUTION (_ShadowSliceResolution)

#if defined(SHADOW_MAP)
StructuredBuffer<ShadowLightData> _ShadowLightDatas;
StructuredBuffer<ShadowMapData> _ShadowMapDatas;
StructuredBuffer<ShadowCameraData> _ShadowCameraDatas;
TEXTURE2D_ARRAY_SHADOW(_ShadowMapArray);
SAMPLER_CMP(sampler_LinearClampCompare);
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

float SampleShadowMap(int resolution, uint mapIndex, float3 positionWS)
{
    ShadowMapData mapData = _ShadowMapDatas[mapIndex];

    float4 scaleOffset;
    int sliceIndex;
    DecodeShadowPosition(mapIndex, resolution, scaleOffset, sliceIndex);

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
    float attenuation = SAMPLE_TEXTURE2D_ARRAY_SHADOW(
        _ShadowMapArray, sampler_LinearClampCompare, float3(uv, shadowCoord.z), sliceIndex);

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

    return SampleShadowMap(lightData.resolution, mapIndex, positionWS);
#else
    return 1.0;
#endif
}

#endif
