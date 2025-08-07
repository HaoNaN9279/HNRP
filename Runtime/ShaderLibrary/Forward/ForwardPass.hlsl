#ifndef HNRP_FORWARD_PASS_INCLUDED
#define HNRP_FORWARD_PASS_INCLUDED

#include "../Common.hlsl"

#include "ForwardInput.hlsl"
#include "ForwardVaryings.hlsl"
#include "ForwardLighting.hlsl"

PackedVaryings vertMain(Attributes attributes)
{
    VertexInput vertexInput;
    ZERO_INITIALIZE(VertexInput, vertexInput);
    vertexInput = BuildVertexInput(attributes);

    Varyings varyings;
    ZERO_INITIALIZE(Varyings, varyings);
    varyings = BuildVaryings(vertexInput);
    
    PackedVaryings packedVaryings;
    ZERO_INITIALIZE(PackedVaryings, packedVaryings);
    packedVaryings = ForwardBuildPackedVaryings(varyings);

    return packedVaryings;
}

float4 fragMain(PackedVaryings packedVaryings)
{
    Varyings varyings;
    ZERO_INITIALIZE(Varyings, varyings);
    varyings = ForwardBuildUnpackedVaryings(packedVaryings);

    SurfaceData surfaceData;
    ZERO_INITIALIZE(SurfaceData, surfaceData);
    surfaceData = BuildSurfaceData(varyings);

    LightingInputData lightingInputData;
    ZERO_INITIALIZE(LightingInputData, lightingInputData);
    lightingInputData = BuildLightingInputData(varyings, surfaceData);

    BRDFData brdfData;
    ZERO_INITIALIZE(BRDFData, brdfData);
    brdfData = BuildBRDFData(surfaceData);

    BSDFCommonData bsdfCommonData;
    ZERO_INITIALIZE(BSDFCommonData, bsdfCommonData);
    bsdfCommonData = BuildBSDFCommonData(lightingInputData);

    LightingData lightingData;
    ZERO_INITIALIZE(LightingData, lightingData);
    lightingData = BuildLightingData(lightingInputData, brdfData, bsdfCommonData);

    LightingOutputData lightingOutputData;
    ZERO_INITIALIZE(LightingOutputData, lightingOutputData);
    lightingOutputData = BuildLightingOutputData(lightingData);

    float test = VARYINGS_FLOAT_COUNT_TANGENT_WS - 3;
    float3 test3 = varyings.positionWS.xyz;//float3(test, test, test);
    // float4 outColor = float4(test3.x, test3.y, test3.z, 1);
    
    float4 outColor = float4(lightingOutputData.lightingColor.r, lightingOutputData.lightingColor.g, lightingOutputData.lightingColor.b, 1);
    return outColor;
}

void frag
(
    PackedVaryings packedVaryings,
    out float4 outColor : SV_Target
)
{
    outColor = fragMain(packedVaryings);
}

PackedVaryings vert (Attributes attributes)
{
    PackedVaryings packedVaryings = vertMain(attributes);
    return packedVaryings;
}

#endif