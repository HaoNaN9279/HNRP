#ifndef HNRP_SURFACE_DATA_INCLUDED
#define HNRP_SURFACE_DATA_INCLUDED

struct SurfaceData
{
    float3 normalWS;
    float3 normalTS;
    float4 tangentWS;
    float sgn;
    float3 bitangentWS;
    float3x3 tbn;
};

float3 GetNormalTS(float2 uv)
{
#if defined(_NORMALMAP)
    float4 n = float4(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv));
    return UnpackNormalScale(n, _NormalScale);
#else
    return float3(0.0, 0.0, 1.0);
#endif
}

SurfaceData GetSurfaceData(float3 normalWS, float4 tangentWS, float3 normalTS)
{
    SurfaceData surfaceData;
    ZERO_INITIALIZE(SurfaceData, surfaceData);

    normalWS = SafeNormalize(normalWS);
    tangentWS.xyz = SafeNormalize(tangentWS.xyz);
#if defined(_NORMALMAP)
    float sgn = tangentWS.w;
    float3 bitangent = sgn * cross(normalWS.xyz, tangentWS.xyz);
    float3x3 tbn = float3x3(tangentWS.xyz, bitangent.xyz, normalWS.xyz);
    surfaceData.normalWS = TransformTangentToWorld(normalTS, tbn);
    surfaceData.sgn = sgn;
    surfaceData.bitangentWS = bitangent;
    surfaceData.tbn = tbn;
#else
    surfaceData.normalWS = normalWS;
    surfaceData.sgn = 0;
    surfaceData.bitangentWS = float3(1.0, 0.0, 0.0);
    surfaceData.tbn = k_identity3x3;
#endif
    surfaceData.normalWS = NormalizeNormalPerPixel(surfaceData.normalWS);
    surfaceData.normalTS = normalTS;
    surfaceData.tangentWS = tangentWS;

    return surfaceData;
}

#endif