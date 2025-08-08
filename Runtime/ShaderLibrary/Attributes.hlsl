#ifndef HNRP_ATTRIBUTES_INCLUDED
#define HNRP_ATTRIBUTES_INCLUDED

struct Attributes
{
    float4 positionOS : POSITION;
#if defined(ATTRIBUTES_NEED_NORMAL)
    float3 normalOS : NORMAL;
#endif
#if defined(ATTRIBUTES_NEED_TANGENT)
    float4 tangentOS : TANGENT;
#endif
#if defined(ATTRIBUTES_NEED_UV0)
    float2 uv0 : TEXCOORD0;
#endif
#if defined(ATTRIBUTES_NEED_UV1)
    float2 uv1 : TEXCOORD1;
#endif
#if defined(ATTRIBUTES_NEED_UV2)
    float2 uv2 : TEXCOORD2;
#endif
#if defined(ATTRIBUTES_NEED_UV3)
    float2 uv3 : TEXCOORD3;
#endif
#if defined(ATTRIBUTES_NEED_COLOR)
    float4 color : COLOR;
#endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

#endif