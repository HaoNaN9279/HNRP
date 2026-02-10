#ifndef HNRP_INPUT_INCLUDED
#define HNRP_INPUT_INCLUDED

#include "./UnityInput.hlsl"

#define MAX_DIRECTIONAL_LIGHT_ON_SCREEN (16)
#define MAX_LOCAL_LIGHT_ON_SCREEN (512)

#define MAX_REFLECTION_PROBE_MASK_WORDS (16384)
#define MAX_REFLECTION_PROBES_ON_SCREEN (64)
#define REFLECTION_PROBE_ATLAS_MIP_COUNT (7)
#define REFLECTION_PROBE_ATLAS_TEXEL_PADDING (2)
#define REFLECTION_PROBE_ATLAS_SIZE (4096)

// Light Data Structure
struct LightData
{
    float3 positionWS;
    uint lightType;
    float3 color;
    float range;
    float4 attenuation;
    float3 directionWS;
    uint renderingLayerMask;
};

// Light Data Buffer
StructuredBuffer<LightData> _LightDatasBuffer;

#if CLUSTER_CULLING_REFLECTION_PROBE

struct ClusterCullingReflectionProbeDatas
{
    float3 boxMax;
    float blendDistance;
    float3 boxMin;
    float importance;
    float3 positionWS;
    float intensity;
    float4 scaleOffset;
};

StructuredBuffer<ClusterCullingReflectionProbeDatas> _ClusterCullingReflectionProbeDatasBuffer;

GLOBAL_CBUFFER_START(_ClusterCullingReflectionProbeParamsBuffer, b2)
    float2 _ClusterCullingReflectionProbeClusterSizeXY;
    float2 _ClusterCullingReflectionProbeClusterZScaleOffset;
    int _ClusterCullingReflectionProbeWordsPerCluster;
    int _ClusterCullingReflectionProbeReflectionProbeCount;
    float _ClusterCullingReflectionProbeUnused0;
    float _ClusterCullingReflectionProbeUnused1;
CBUFFER_END

#define _CLUSTER_CULLING_REFLECTION_PROBE_XY_SCALE (_ClusterCullingReflectionProbeClusterSizeXY.xy)
#define _CLUSTER_CULLING_REFLECTION_PROBE_Z_SCALE (_ClusterCullingReflectionProbeClusterZScaleOffset.x)
#define _CLUSTER_CULLING_REFLECTION_PROBE_Z_OFFSET (_ClusterCullingReflectionProbeClusterZScaleOffset.y)
#define _CLUSTER_CULLING_REFLECTION_PROBE_WORDS_PER_CLUSTER (_ClusterCullingReflectionProbeWordsPerCluster)
#define _CLUSTER_CULLING_REFLECTION_PROBE_COUNT (_ClusterCullingReflectionProbeReflectionProbeCount)

StructuredBuffer<uint> _ClusterCullingReflectionProbeMaskBuffer;

TEXTURE2D(_ReflectionProbeAtlas);
SAMPLER(sampler_ReflectionProbeAtlas);

#endif

#if CLUSTER_CULLING_LIGHT

GLOBAL_CBUFFER_START(_ClusterCullingLightParamsBuffer, b3)
    float2 _ClusterCullingLightClusterSize;
    float2 _ClusterCullingLightClusterZScaleOffset;
    int _ClusterCullingLightWordsPerCluster;
    int _ClusterCullingLightDirectionalLightCount;
    int _ClusterCullingLightLocalLightCount;
    float _ClusterCullingLightUnused;
CBUFFER_END

#define _CLUSTER_CULLING_LIGHT_XY_SCALE (_ClusterCullingLightClusterSize)
#define _CLUSTER_CULLING_LIGHT_Z_SCALE (_ClusterCullingLightClusterZScaleOffset.x)
#define _CLUSTER_CULLING_LIGHT_Z_OFFSET (_ClusterCullingLightClusterZScaleOffset.y)
#define _CLUSTER_CULLING_LIGHT_WORDS_PER_CLUSTER (_ClusterCullingLightWordsPerCluster)
#define _CLUSTER_CULLING_LIGHT_DIRECTIONAL_LIGHT_COUNT (_ClusterCullingLightDirectionalLightCount)
#define _CLUSTER_CULLING_LIGHT_LOCAL_LIGHT_COUNT (_ClusterCullingLightLocalLightCount)

StructuredBuffer<uint> _ClusterCullingLightMaskBuffer;

#endif

// #if FORWARD_PLUS

// #define MAX_LIGHTS_PER_TILE MAX_LOCAL_LIGHT_ON_SCREEN
// #define MAX_ZBIN_VEC4S 1024
// #define MAX_TILE_VEC4S 4096

// GLOBAL_CBUFFER_START(ForwardPlusVariablesGlobal, b1)
//     float4 _ForwardPlusParams0;
//     float4 _ForwardPlusParams1;
//     float4 _ForwardPlusParams2;
// CBUFFER_END

// CBUFFER_START(FP_ZBinBuffer)
//     float4 _ForwardPlusZBinsBuffer[MAX_ZBIN_VEC4S];
// CBUFFER_END
// CBUFFER_START(FP_TileBuffer)
//     float4 _ForwardPlusTileMasksBuffer[MAX_TILE_VEC4S];
// CBUFFER_END

// #define FP_ZBIN_SCALE (_ForwardPlusParams0.x)
// #define FP_ZBIN_OFFSET (_ForwardPlusParams0.y)
// #define FP_PROBES_BEGIN ((uint)_ForwardPlusParams0.z)
// #define FP_DIRECTIONAL_LIGHTS_COUNT ((uint)_ForwardPlusParams0.w)
// #define FP_TILE_SCALE ((float2)_ForwardPlusParams1.xy)
// #define FP_TILE_COUNT_X ((uint)_ForwardPlusParams1.z)
// #define FP_WORDS_PER_TILE ((uint)_ForwardPlusParams1.w)
// #define FP_ZBIN_COUNT ((uint)_ForwardPlusParams2.x)
// #define FP_TILE_COUNT ((uint)_ForwardPlusParams2.y)

// #endif

#endif