#ifndef HNRP_VERTEX_INPUT_INCLUDED
#define HNRP_VERTEX_INPUT_INCLUDED

struct VertexInput
{
    float3 positionOS;
    float3 normalOS;
    float4 tangentOS;
    float2 uv0;
};

VertexInput BuildVertexInput(Attributes attributes)
{
    VertexInput vertexInput;
    ZERO_INITIALIZE(VertexInput, vertexInput);

    vertexInput.positionOS = attributes.positionOS.xyz;
    vertexInput.normalOS = attributes.normalOS;
    vertexInput.tangentOS = attributes.tangentOS;
    vertexInput.uv0 = attributes.texcoord;

    return vertexInput;
}

#endif