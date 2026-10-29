#ifndef HNRP_UNLIT_VARYINGS_INCLUDED
#define HNRP_UNLIT_VARYINGS_INCLUDED

#include "../Core/PackedVaryings.hlsl"

struct UnlitVaryings
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
    // 包 varyings 字段（组合头注入）。
    HNRP_PKG_VARYINGS_FIELDS
};

void BuildPackVaryings(UnlitVaryings unlitVaryings, out PackedVaryings packedVaryings)
{
    ZERO_INITIALIZE(PackedVaryings, packedVaryings);
    int pointer = 0;

    packedVaryings.positionCS = unlitVaryings.positionCS;

    SET_VARYINGS_POSITION_WS(packedVaryings, unlitVaryings.positionWS);
    SET_VARYINGS_NORMAL_WS(packedVaryings, unlitVaryings.normalWS);
    SET_VARYINGS_TANGENT_WS(packedVaryings, unlitVaryings.tangentWS);
    SET_VARYINGS_UV0(packedVaryings, unlitVaryings.uv0);
    SET_VARYINGS_UV1(packedVaryings, unlitVaryings.uv1);
    SET_VARYINGS_UV2(packedVaryings, unlitVaryings.uv2);
    SET_VARYINGS_UV3(packedVaryings, unlitVaryings.uv3);
    SET_VARYINGS_COLOR(packedVaryings, unlitVaryings.color);

    // 包 varyings 打包（组合头注入，追加在基底字段之后）。
    HNRP_PKG_SET_VARYINGS(packedVaryings, unlitVaryings, pointer);
}

void BuildUnpackVaryings(PackedVaryings packedVaryings, out UnlitVaryings unlitVaryings)
{
    ZERO_INITIALIZE(UnlitVaryings, unlitVaryings);
    int pointer = VARYINGS_FLOAT_COUNT - 1;

    // 包 varyings 解包（组合头注入，从高端先行，与 SET 镜像）。
    HNRP_PKG_GET_VARYINGS(packedVaryings, unlitVaryings, pointer);

    GET_VARYINGS_COLOR(packedVaryings, unlitVaryings.color);
    GET_VARYINGS_UV3(packedVaryings, unlitVaryings.uv3);
    GET_VARYINGS_UV2(packedVaryings, unlitVaryings.uv2);
    GET_VARYINGS_UV1(packedVaryings, unlitVaryings.uv1);
    GET_VARYINGS_UV0(packedVaryings, unlitVaryings.uv0);
    GET_VARYINGS_TANGENT_WS(packedVaryings, unlitVaryings.tangentWS);
    GET_VARYINGS_NORMAL_WS(packedVaryings, unlitVaryings.normalWS);
    GET_VARYINGS_POSITION_WS(packedVaryings, unlitVaryings.positionWS);

    unlitVaryings.positionCS = packedVaryings.positionCS;
}

void BuildUnlitVaryings(VertexInput vertexInput, out UnlitVaryings unlitVaryings)
{
    ZERO_INITIALIZE(UnlitVaryings, unlitVaryings);

    unlitVaryings.positionWS = TransformObjectToWorld(vertexInput.positionOS);
    unlitVaryings.normalWS = TransformObjectToWorldDir(vertexInput.normalOS);
    unlitVaryings.tangentWS = float4(TransformObjectToWorldDir(vertexInput.tangentOS.xyz), vertexInput.tangentOS.w * GetOddNegativeScale());
    unlitVaryings.uv0 = vertexInput.uv0;
    unlitVaryings.uv1 = vertexInput.uv1;
    unlitVaryings.uv2 = vertexInput.uv2;
    unlitVaryings.uv3 = vertexInput.uv3;
    unlitVaryings.color = vertexInput.color;
    unlitVaryings.positionCS = TransformObjectToHClip(vertexInput.positionOS);
}

#endif
