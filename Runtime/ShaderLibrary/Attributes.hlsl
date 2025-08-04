#ifndef HNRP_ATTRIBUTES_INCLUDED
#define HNRP_ATTRIBUTES_INCLUDED

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 texcoord : TEXCOORD0;
    // float2 staticLightmapUV : TEXCOORD1;
};

#endif