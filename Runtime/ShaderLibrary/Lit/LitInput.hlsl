#ifndef HNRP_LIT_INPUT_INCLUDED
#define HNRP_LIT_INPUT_INCLUDED

#define ATTRIBUTES_NEED_NORMAL
#define USE_POSITION_WS_VARYING
#define USE_NORMAL_WS_VARYING

#if defined(_BASEMAP) || defined(_NORMALMAP) || defined(_MASKMAP) || defined(_EMISSIONMAP)
    #define ATTRIBUTES_NEED_UV0
    #define USE_UV0_VARYING
#endif

#if defined(_NORMALMAP)
    #define ATTRIBUTES_NEED_TANGENT
    #define USE_TANGENT_WS_VARYING
#endif

#if defined(LIGHTMAP_ON)
    #define ATTRIBUTES_NEED_UV1
    #define USE_STATIC_LIGHTMAP_UV_VARYING
#endif

#if defined(EVALUATE_SH_MIXED) || defined(EVALUATE_SH_VERTEX)
    #define USE_VERTEX_SH_VARYING
#endif

#include "../Common/Common.hlsl"
#include "../Core/UnityInput.hlsl"
#include "../Light/LightInput.hlsl"
#include "../ClusterCulling/ForwardPlusInput.hlsl"

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


#ifdef UNITY_DOTS_INSTANCING_ENABLED

UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _EmissionColor)
    UNITY_DOTS_INSTANCED_PROP(float, _Cutoff)
#if defined(_BASEMAP)
    UNITY_DOTS_INSTANCED_PROP(float, _AlphaRemapMin)
    UNITY_DOTS_INSTANCED_PROP(float, _AlphaRemapMax)
#endif
#if defined(_MASKMAP)
    UNITY_DOTS_INSTANCED_PROP(float, _MetallicRemapMin)
    UNITY_DOTS_INSTANCED_PROP(float, _MetallicRemapMax)
    UNITY_DOTS_INSTANCED_PROP(float, _SmoothnessRemapMin)
    UNITY_DOTS_INSTANCED_PROP(float, _SmoothnessRemapMax)
    UNITY_DOTS_INSTANCED_PROP(float, _AORemapMin)
    UNITY_DOTS_INSTANCED_PROP(float, _AORemapMax)
#else
    UNITY_DOTS_INSTANCED_PROP(float, _Metallic)
    UNITY_DOTS_INSTANCED_PROP(float, _Smoothness)
#endif
#if defined(_NORMALMAP)
    UNITY_DOTS_INSTANCED_PROP(float, _NormalScale)
#endif
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)


static float4 unity_DOTS_Sampled_BaseColor;
static float4 unity_DOTS_Sampled_EmissionColor;
static float unity_DOTS_Sampled_Cutoff;
#if defined(_BASEMAP)
static float unity_DOTS_Sampled_AlphaRemapMin;
static float unity_DOTS_Sampled_AlphaRemapMax;
#endif
#if defined(_MASKMAP)
static float unity_DOTS_Sampled_MetallicRemapMin;
static float unity_DOTS_Sampled_MetallicRemapMax;
static float unity_DOTS_Sampled_SmoothnessRemapMin;
static float unity_DOTS_Sampled_SmoothnessRemapMax;
static float unity_DOTS_Sampled_AORemapMin;
static float unity_DOTS_Sampled_AORemapMax;
#else
static float unity_DOTS_Sampled_Metallic;
static float unity_DOTS_Sampled_Smoothness;
#endif
#if defined(_NORMALMAP)
static float unity_DOTS_Sampled_NormalScale;
#endif


void SetupDOTSLitMaterialPropertyCaches()
{
    unity_DOTS_Sampled_BaseColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor);
    unity_DOTS_Sampled_EmissionColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _EmissionColor);
    unity_DOTS_Sampled_Cutoff = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _Cutoff);
#if defined(_BASEMAP)
    unity_DOTS_Sampled_AlphaRemapMin = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _AlphaRemapMin);
    unity_DOTS_Sampled_AlphaRemapMax = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _AlphaRemapMax);
#endif
#if defined(_MASKMAP)
    unity_DOTS_Sampled_MetallicRemapMin = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _MetallicRemapMin);
    unity_DOTS_Sampled_MetallicRemapMax = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _MetallicRemapMax);
    unity_DOTS_Sampled_SmoothnessRemapMin = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _SmoothnessRemapMin);
    unity_DOTS_Sampled_SmoothnessRemapMax = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _SmoothnessRemapMax);
    unity_DOTS_Sampled_AORemapMin = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _AORemapMin);
    unity_DOTS_Sampled_AORemapMax = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _AORemapMax);
#else
    unity_DOTS_Sampled_Metallic = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _Metallic);
    unity_DOTS_Sampled_Smoothness = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _Smoothness);
#endif
#if defined(_NORMALMAP)
    unity_DOTS_Sampled_NormalScale = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _NormalScale);
#endif
}

#undef UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES
#define UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES() SetupDOTSLitMaterialPropertyCaches()

    #define _BaseColor unity_DOTS_Sampled_BaseColor
    #define _EmissionColor unity_DOTS_Sampled_EmissionColor
    #define _Cutoff unity_DOTS_Sampled_Cutoff
#if defined(_BASEMAP)
    #define _AlphaRemapMin unity_DOTS_Sampled_AlphaRemapMin
    #define _AlphaRemapMax unity_DOTS_Sampled_AlphaRemapMax
#endif
#if defined(_MASKMAP)
    #define _MetallicRemapMin unity_DOTS_Sampled_MetallicRemapMin
    #define _MetallicRemapMax unity_DOTS_Sampled_MetallicRemapMax
    #define _SmoothnessRemapMin unity_DOTS_Sampled_SmoothnessRemapMin
    #define _SmoothnessRemapMax unity_DOTS_Sampled_SmoothnessRemapMax
    #define _AORemapMin unity_DOTS_Sampled_AORemapMin
    #define _AORemapMax unity_DOTS_Sampled_AORemapMax
#else
    #define _Metallic unity_DOTS_Sampled_Metallic
    #define _Smoothness unity_DOTS_Sampled_Smoothness
#endif
#if defined(_NORMALMAP)
    #define _NormalScale unity_DOTS_Sampled_NormalScale
#endif

#endif

#include "../Common/CommonFunctions.hlsl"

#include "../Core/Attributes.hlsl"
#include "../Core/VertexInput.hlsl"
#include "../Core/Varyings.hlsl"
#include "../Core/SurfaceData.hlsl"

#endif