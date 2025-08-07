#ifndef HNRP_VARYINGS_INCLUDED
#define HNRP_VARYINGS_INCLUDED

struct Varyings
{
    float3 positionWS;
    float3 normalWS;
    float4 tangentWS;
    float2 uv0;
    float4 positionCS;
};

Varyings BuildVaryings(VertexInput vertexInput)
{
    Varyings varyings;
    ZERO_INITIALIZE(Varyings, varyings);

#if defined(USE_POSITION_WS_VARYING)
    varyings.positionWS = TransformObjectToWorld(vertexInput.positionOS);
#endif
    varyings.normalWS = TransformObjectToWorldDir(vertexInput.normalOS);
#if defined(USE_TANGENT_WS_VARYING)
    varyings.tangentWS = float4(TransformObjectToWorldDir(vertexInput.tangentOS.xyz), vertexInput.tangentOS.w * GetOddNegativeScale());
#endif
    varyings.uv0 = vertexInput.uv0;
    varyings.positionCS = TransformObjectToHClip(vertexInput.positionOS);

    return varyings;
}

#endif