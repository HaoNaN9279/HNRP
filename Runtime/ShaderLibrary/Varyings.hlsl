#ifndef HNRP_VARYINGS_INCLUDED
#define HNRP_VARYINGS_INCLUDED

struct Varyings
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

Varyings BuildVaryings(VertexInput vertexInput)
{
    Varyings varyings;
    ZERO_INITIALIZE(Varyings, varyings);

    varyings.positionWS = TransformObjectToWorld(vertexInput.positionOS);
    varyings.normalWS = TransformObjectToWorldDir(vertexInput.normalOS);
    varyings.tangentWS = float4(TransformObjectToWorldDir(vertexInput.tangentOS.xyz), vertexInput.tangentOS.w * GetOddNegativeScale());
    varyings.uv0 = vertexInput.uv0;
    varyings.uv1 = vertexInput.uv1;
    varyings.uv2 = vertexInput.uv2;
    varyings.uv3 = vertexInput.uv3;
    varyings.color = vertexInput.color;
    varyings.staticLightmapUV = vertexInput.staticLightmapUV.xy * unity_LightmapST.xy + unity_LightmapST.zw;
    varyings.vertexSH = SampleSHVertex(varyings.normalWS);
    varyings.positionCS = TransformObjectToHClip(vertexInput.positionOS);

    return varyings;
}

#endif