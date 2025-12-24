#ifndef HNRP_VERTEX_INPUT_INCLUDED
#define HNRP_VERTEX_INPUT_INCLUDED

struct VertexInput
{
    float3 positionOS;
    float3 normalOS;
    float4 tangentOS;
    float2 uv0;
    float2 uv1;
    float2 uv2;
    float2 uv3;
    float4 color;
    float2 staticLightmapUV;
    float2 dynamicLightmapUV;
};

void BuildVertexInput(Attributes attributes, out VertexInput vertexInput)
{
    ZERO_INITIALIZE(VertexInput, vertexInput);

    vertexInput.positionOS = attributes.positionOS.xyz;
#if defined(ATTRIBUTES_NEED_NORMAL)
    vertexInput.normalOS = attributes.normalOS;
#endif
#if defined(ATTRIBUTES_NEED_TANGENT)
    vertexInput.tangentOS = attributes.tangentOS;
#endif
#if defined(ATTRIBUTES_NEED_UV0)
    vertexInput.uv0 = TRANSFORM_TEX(attributes.uv0, _BaseMap);
#endif
#if defined(ATTRIBUTES_NEED_UV1)
    vertexInput.uv1 = attributes.uv1;
    vertexInput.staticLightmapUV = attruvytes.uv1;
#endif
#if defined(ATTRIBUTES_NEED_UV2)
    vertexInput.uv2 = attributes.uv2;
    vertexInput.dynamicLightmapUV = attributes.uv2;
#endif
#if defined(ATTRIBUTES_NEED_UV3)
    vertexInput.uv3 = attributes.uv3;
#endif
#if defined(ATTRIBUTES_NEED_COLOR)
    vertexInput.color = attributes.color;
#endif
}

#endif