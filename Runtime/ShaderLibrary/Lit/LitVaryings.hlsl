#ifndef HNRP_LIT_VARYINGS_INCLUDED
#define HNRP_LIT_VARYINGS_INCLUDED

#include "../Core/PackedVaryings.hlsl"
#include "../Lighting/GI.hlsl"

struct LitVaryings
{
    float4 positionCS;
    float3 positionWS;
    float3 normalWS;
    float4 tangentWS;
    float2 uv0;
    float2 uv1;
    float2 uv2;
    float2 uv3;
    float4 color;
    float2 staticLightmapUV;
    float3 vertexSH;
};

void BuildPackVaryings(LitVaryings litVaryings, out PackedVaryings packedVaryings)
{
    ZERO_INITIALIZE(PackedVaryings, packedVaryings);
    int pointer = 0;

    packedVaryings.positionCS = litVaryings.positionCS;

    SET_VARYINGS_POSITION_WS(packedVaryings, litVaryings.positionWS);
    SET_VARYINGS_NORMAL_WS(packedVaryings, litVaryings.normalWS);
    SET_VARYINGS_TANGENT_WS(packedVaryings, litVaryings.tangentWS);
    SET_VARYINGS_UV0(packedVaryings, litVaryings.uv0);
    SET_VARYINGS_UV1(packedVaryings, litVaryings.uv1);
    SET_VARYINGS_UV2(packedVaryings, litVaryings.uv2);
    SET_VARYINGS_UV3(packedVaryings, litVaryings.uv3);
    SET_VARYINGS_COLOR(packedVaryings, litVaryings.color);
    SET_VARYINGS_STATIC_LIGHTMAP_UV(packedVaryings, litVaryings.staticLightmapUV);
    SET_VARYINGS_VERTEX_SH(packedVaryings, litVaryings.vertexSH);
}

void BuildUnpackVaryings(PackedVaryings packedVaryings, out LitVaryings litVaryings)
{
    ZERO_INITIALIZE(LitVaryings, litVaryings);
    int pointer = VARYINGS_FLOAT_COUNT - 1;

    GET_VARYINGS_VERTEX_SH(packedVaryings, litVaryings.vertexSH);
    GET_VARYINGS_STATIC_LIGHTMAP_UV(packedVaryings, litVaryings.staticLightmapUV);
    GET_VARYINGS_COLOR(packedVaryings, litVaryings.color);
    GET_VARYINGS_UV3(packedVaryings, litVaryings.uv3);
    GET_VARYINGS_UV2(packedVaryings, litVaryings.uv2);
    GET_VARYINGS_UV1(packedVaryings, litVaryings.uv1);
    GET_VARYINGS_UV0(packedVaryings, litVaryings.uv0);
    GET_VARYINGS_TANGENT_WS(packedVaryings, litVaryings.tangentWS);
    GET_VARYINGS_NORMAL_WS(packedVaryings, litVaryings.normalWS);
    GET_VARYINGS_POSITION_WS(packedVaryings, litVaryings.positionWS);

    litVaryings.positionCS = packedVaryings.positionCS;
}

void BuildLitVaryings(VertexInput vertexInput, out LitVaryings litVaryings)
{
    ZERO_INITIALIZE(LitVaryings, litVaryings);

    litVaryings.positionWS = TransformObjectToWorld(vertexInput.positionOS);
    litVaryings.normalWS = TransformObjectToWorldDir(vertexInput.normalOS);
    litVaryings.tangentWS = float4(TransformObjectToWorldDir(vertexInput.tangentOS.xyz), vertexInput.tangentOS.w * GetOddNegativeScale());
    litVaryings.uv0 = vertexInput.uv0;
    litVaryings.uv1 = vertexInput.uv1;
    litVaryings.uv2 = vertexInput.uv2;
    litVaryings.uv3 = vertexInput.uv3;
    litVaryings.color = vertexInput.color;
    litVaryings.staticLightmapUV = vertexInput.staticLightmapUV.xy * unity_LightmapST.xy + unity_LightmapST.zw;
    litVaryings.vertexSH = SampleSHVertex(litVaryings.normalWS);
    litVaryings.positionCS = TransformObjectToHClip(vertexInput.positionOS);
}

#endif