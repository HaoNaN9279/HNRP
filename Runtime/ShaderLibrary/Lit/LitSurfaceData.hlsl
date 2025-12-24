#ifndef HNRP_LIT_SURFACE_DATA_INCLUDED
#define HNRP_LIT_SURFACE_DATA_INCLUDED

struct LitSurfaceData
{
    float3 albedo;
    float  alpha;
    // float3 specular;
    float3 normalTS;
    float  smoothness;
    float  metallic;
    float  occlusion;
    float3 emission;
};

float4 GetAlbedoAlpha(float2 uv)
{
#if defined(_BASEMAP)
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
    float3 albedo = baseMap.rgb * _BaseColor.rgb;
    float alpha = RemapFrom01(baseMap.a, _AlphaRemapMin, _AlphaRemapMax) * _BaseColor.a;
#if defined(_ALPHATEST_ON)
    clip(alpha - _Cutoff);
#endif
    return float4(albedo, alpha);
#else
    return _BaseColor;
#endif
}

float3 GetNormalTS(float2 uv)
{
#if defined(_NORMALMAP)
    float4 n = float4(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv));
    return UnpackNormalScale(n, _NormalScale);
#else
    return float3(0.0, 0.0, 1.0);
#endif
}

float4 GetMasks(float2 uv)
{
#if defined(_MASKMAP)
    float4 mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uv);
    float smoothness = RemapFrom01(mask.x, _SmoothnessRemapMin, _SmoothnessRemapMax);
    float metallic = RemapFrom01(mask.y, _MetallicRemapMin, _MetallicRemapMax);
    float AO = RemapFrom01(mask.z, _AORemapMin, _AORemapMax);
    return float4(smoothness, metallic, AO, mask.w);
#else
    return float4(_Smoothness, _Metallic, 1.0, 1.0);
#endif
}

float3 GetEmission(float2 uv)
{
#if defined(_EMISSIONMAP)
    return float4(SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv)).rgb * _EmissionColor.rgb;
#else
    return float3(_EmissionColor.rgb);
#endif
}

void InitializeLitSurfaceData(float2 uv, out LitSurfaceData litSurfaceData)
{
    ZERO_INITIALIZE(LitSurfaceData, litSurfaceData);

    float4 albedoAlpha = GetAlbedoAlpha(uv);
    float3 normalTS = GetNormalTS(uv);
    float4 masks = GetMasks(uv);
    float3 emission = GetEmission(uv);

    litSurfaceData.albedo = albedoAlpha.rgb;
    litSurfaceData.alpha = albedoAlpha.a;
    litSurfaceData.normalTS = normalTS;
    litSurfaceData.smoothness = masks.x;
    litSurfaceData.metallic = masks.y;
    litSurfaceData.occlusion = masks.z;
    litSurfaceData.emission = emission;
}

void BuildLitSurfaceData(LitVaryings litVaryings, out LitSurfaceData litSurfaceData)
{
    InitializeLitSurfaceData(litVaryings.uv0, litSurfaceData);
}


#endif