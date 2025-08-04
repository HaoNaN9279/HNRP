#ifndef HNRP_VARYINGS_INCLUDED
#define HNRP_VARYINGS_INCLUDED

struct Varyings
{
    float3 normalWS;
#if defined(USE_TANGENT_WS_VARYING)
    float4 tangentWS;
#endif
    float2 uv0;
    float4 positionCS;
};

Varyings BuildVaryings(VertexInput vertexInput)
{
    Varyings varyings;
    ZERO_INITIALIZE(Varyings, varyings);

    varyings.normalWS = TransformObjectToWorldDir(vertexInput.normalOS);
#if defined(USE_TANGENT_WS_VARYING)
    varyings.tangentWS = half4(TransformObjectToWorldDir(attribute.tangentOS.xyz), vertexInput.tangentOS.w * GetOddNegativeScale());
#endif
    varyings.uv0 = vertexInput.uv0;
    varyings.positionCS = TransformObjectToHClip(vertexInput.positionOS);

    return varyings;
}

#endif