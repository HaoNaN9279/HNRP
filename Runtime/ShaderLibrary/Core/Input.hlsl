#ifndef HNRP_INPUT_INCLUDED
#define HNRP_INPUT_INCLUDED

#include "./UnityInput.hlsl"

#define MAX_DIRECTIONAL_LIGHT_ON_SCREEN (16)
#define MAX_LOCAL_LIGHT_ON_SCREEN (512)
#define MAX_REFLECTION_PROBES_ON_SCREEN (64)
#define REFLECTION_PROBE_ATLAS_MIP_COUNT (8)

// Light Data Structure
struct LightData
{
    float3 positionWS;
    uint lightType;
    float3 color;
    float __unused__0;
    float4 attenuation;
    float3 directionWS;
    bool __unused__1;
};

// Light Data Buffer
StructuredBuffer<LightData> _LightDatas;

// Reflection Probes Data
GLOBAL_CBUFFER_START(ReflectionProbeVariablesGlobal, b2)
    float4 _ReflectionProbeData0[MAX_REFLECTION_PROBES_ON_SCREEN]; // x,y,z: boxMax, w: blendDistance
    float4 _ReflectionProbeData1[MAX_REFLECTION_PROBES_ON_SCREEN]; // x,y,z: boxMin, w: importance
    float4 _ReflectionProbeData2[MAX_REFLECTION_PROBES_ON_SCREEN]; // x,y,z: positionWS, w: intensity
    float4 _ReflectionProbeData3[MAX_REFLECTION_PROBES_ON_SCREEN]; // x,y,z,w: scaleOffset
CBUFFER_END


// Reflection Probes Atlas
TEXTURE2D(_ReflectionProbeAtlas);
SAMPLER(sampler_ReflectionProbeAtlas);

#if FORWARD_PLUS

#define MAX_LIGHTS_PER_TILE MAX_LOCAL_LIGHT_ON_SCREEN
#define MAX_ZBIN_VEC4S 1024
#define MAX_TILE_VEC4S 4096

GLOBAL_CBUFFER_START(ForwardPlusVariablesGlobal, b1)
    float4 _ForwardPlusParams0;
    float4 _ForwardPlusParams1;
    float4 _ForwardPlusParams2;
CBUFFER_END

CBUFFER_START(FP_ZBinBuffer)
    float4 _ForwardPlusZBinsBuffer[MAX_ZBIN_VEC4S];
CBUFFER_END
CBUFFER_START(FP_TileBuffer)
    float4 _ForwardPlusTileMasksBuffer[MAX_TILE_VEC4S];
CBUFFER_END

#define FP_ZBIN_SCALE (_ForwardPlusParams0.x)
#define FP_ZBIN_OFFSET (_ForwardPlusParams0.y)
#define FP_PROBES_BEGIN ((uint)_ForwardPlusParams0.z)
#define FP_DIRECTIONAL_LIGHTS_COUNT ((uint)_ForwardPlusParams0.w)
#define FP_TILE_SCALE ((float2)_ForwardPlusParams1.xy)
#define FP_TILE_COUNT_X ((uint)_ForwardPlusParams1.z)
#define FP_WORDS_PER_TILE ((uint)_ForwardPlusParams1.w)
#define FP_ZBIN_COUNT ((uint)_ForwardPlusParams2.x)
#define FP_TILE_COUNT ((uint)_ForwardPlusParams2.y)

#endif

#endif