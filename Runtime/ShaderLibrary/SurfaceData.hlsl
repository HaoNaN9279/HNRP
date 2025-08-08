#ifndef HNRP_SURFACE_DATA_INCLUDED
#define HNRP_SURFACE_DATA_INCLUDED

struct SurfaceData
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

struct NormalData
{
    float3 normalWS;
    float3 normalTS;
    float4 tangentWS;
    float sgn;
    float3 bitangentWS;
    float3x3 tbn;
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

void InitializeSurfaceData(float2 uv, out SurfaceData surfaceData)
{
    ZERO_INITIALIZE(SurfaceData, surfaceData);

    float4 albedoAlpha = GetAlbedoAlpha(uv);
    float3 normalTS = GetNormalTS(uv);
    float4 masks = GetMasks(uv);
    float3 emission = GetEmission(uv);

    surfaceData.albedo = albedoAlpha.rgb;
    surfaceData.alpha = albedoAlpha.a;
    surfaceData.normalTS = normalTS;
    surfaceData.smoothness = masks.x;
    surfaceData.metallic = masks.y;
    surfaceData.occlusion = masks.z;
    surfaceData.emission = emission;
}

NormalData GetNormalData(float3 normalWS, float4 tangentWS, float3 normalTS)
{
    NormalData normalData;
    ZERO_INITIALIZE(NormalData, normalData);

    normalWS = SafeNormalize(normalWS);
    tangentWS.xyz = SafeNormalize(tangentWS.xyz);
#if defined(_NORMALMAP)
    float sgn = tangentWS.w;
    float3 bitangent = sgn * cross(normalWS.xyz, tangentWS.xyz);
    float3x3 tbn = float3x3(tangentWS.xyz, bitangent.xyz, normalWS.xyz);
    normalData.normalWS = TransformTangentToWorld(normalTS, tbn);
    normalData.sgn = sgn;
    normalData.bitangentWS = bitangent;
    normalData.tbn = tbn;
#else
    normalData.normalWS = normalWS;
    normalData.sgn = 0;
    normalData.bitangentWS = float3(1.0, 0.0, 0.0);
    normalData.tbn = k_identity3x3;
#endif
    normalData.normalWS = NormalizeNormalPerPixel(normalData.normalWS);
    normalData.normalTS = normalTS;
    normalData.tangentWS = tangentWS;

    return normalData;
}

#endif