#ifndef HNRP_SURFACE_DATA_INCLUDED
#define HNRP_SURFACE_DATA_INCLUDED

struct NormalData
{
    float3 normalWS;
    float3 normalTS;
    float4 tangentWS;
    float sgn;
    float3 bitangentWS;
    float3x3 tbn;
};

float3 GetSfNormalWS(float3 rawNormalWS)
{
    return SafeNormalize(rawNormalWS);
}

float4 GetSfTangentWS(float4 rawTangentWS)
{
    return float4(SafeNormalize(rawTangentWS.xyz), rawTangentWS.w);
}

float3 GetSfBitangentWS(float3 sfNormalWS, float4 sfTangentWS)
{
    return sfTangentWS.w * cross(sfNormalWS.xyz, sfTangentWS.xyz);
}

float3 GetNormalSGN(float4 tangentWS)
{
#if defined(_NORMALMAP)
    return tangentWS.w;
#else
    return 0.0;
#endif
}

float3x3 GetNormalTBN(float3 sfNormalWS, float4 sfTangentWS, float3 sfBitangentWS)
{
#if defined(_NORMALMAP)
    return float3x3(sfTangentWS.xyz, sfBitangentWS.xyz, sfNormalWS.xyz);
#else
    return k_identity3x3;
#endif
}

float3 GetNormalWS(float3 sfNormalWS, float3 normalTS, float3x3 tbn)
{
#if defined(_NORMALMAP)
    return TransformTangentToWorld(normalTS, tbn);
#else
    return sfNormalWS;
#endif
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