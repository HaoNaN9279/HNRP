#ifndef HNRP_FORWARD_INPUT_INCLUDED
#define HNRP_FORWARD_INPUT_INCLUDED

#if defined(_NORMALMAP)
    #undef USE_TANGENT_WS_VARYING
    #define USE_TANGENT_WS_VARYING 1
#endif

#undef USE_POSITION_WS_VARYING
#define USE_POSITION_WS_VARYING 1

#include "../UnityInput.hlsl"
#include "../Attributes.hlsl"
#include "../VertexInput.hlsl"
#include "../Varyings.hlsl"

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
float4 _BaseColor;
// float4 _SpecColor;
float4 _EmissionColor;
float _Cutoff;

#if defined(_BASEMAP)
float _AlphaRemapMin;
float _AlphaRemapMax;
#endif

#if defined(_MASKMAP)
float _MetallicRemapMin;
float _MetallicRemapMax;
float _SmoothnessRemapMin;
float _SmoothnessRemapMax;
float _AORemapMin;
float _AORemapMax;
#else
float _Metallic;
float _Smoothness;
#endif

#if defined(_NORMALMAP)
float _NormalScale;
#endif
CBUFFER_END

#endif