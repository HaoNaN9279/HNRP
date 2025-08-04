#ifndef HNRP_FORWARD_INPUT_INCLUDED
#define HNRP_FORWARD_INPUT_INCLUDED

#include "../UnityInput.hlsl"
#include "../Attributes.hlsl"
#include "../VertexInput.hlsl"
#include "../Varyings.hlsl"
#include "../PackedVaryings.hlsl"

#if defined(_BASEMAP)
TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
#endif

#if defined(_NORMALMAP)
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
#endif

#if defined(_MASKMAP)
TEXTURE2D(_MaskMap);
SAMPLER(sampler_MaskMap);
#endif

#if defined(_EMISSIONMAP)
TEXTURE2D(_EmissionMap);
SAMPLER(sampler_EmissionMap);
#endif

CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
float4 _BaseMap_TexelSize;
float4 _BaseMap_MipInfo;
half4 _BaseColor;
// half4 _SpecColor;
half4 _EmissionColor;
half _Cutoff;

#if defined(_BASEMAP)
half _AlphaRemapMin;
half _AlphaRemapMax;
#endif

#if defined(_MASKMAP)
half _MetallicRemapMin;
half _MetallicRemapMax;
half _SmoothnessRemapMin;
half _SmoothnessRemapMax;
half _AORemapMin;
half _AORemapMax;
#else
half _Metallic;
half _Smoothness;
#endif

#if defined(_NORMALMAP)
half _NormalScale;
#endif
CBUFFER_END

#if defined(_NORMALMAP)
    #undef USE_TANGENT_WS_VARYING
    #define USE_TANGENT_WS_VARYING 1
#endif

#endif