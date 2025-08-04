#ifndef HNRP_FORWARD_PASS_INCLUDED
#define HNRP_FORWARD_PASS_INCLUDED

#include "../Common.hlsl"

#include "ForwardInput.hlsl"
#include "ForwardLighting.hlsl"

PackedVaryings vert (Attributes attributes)
{
    VertexInput vertexInput;
    Varyings varyings;
    PackedVaryings packedVaryings;

    ZERO_INITIALIZE(VertexInput, vertexInput);
    ZERO_INITIALIZE(Varyings, varyings);
    ZERO_INITIALIZE(PackedVaryings, packedVaryings);

    vertexInput = BuildVertexInput(attributes);
    varyings = BuildVaryings(vertexInput);
    packedVaryings = BuildPackedVaryings(varyings);

    return packedVaryings;
}

void frag
(
    PackedVaryings packedVaryings,
    out float4 outColor : SV_Target
)
{
    Varyings varyings;

    ZERO_INITIALIZE(Varyings, varyings);

    varyings = BuildUnpackedVaryings(packedVaryings);

    float test = varyings.normalWS.x;
    float3 test3 = varyings.normalWS;//float3(test, test, test);
    outColor = float4(test3, 1);
}

#endif