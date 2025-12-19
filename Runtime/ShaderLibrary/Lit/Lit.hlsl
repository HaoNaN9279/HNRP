#ifndef HNRP_FORWARD_PASS_INCLUDED
#define HNRP_FORWARD_PASS_INCLUDED

#include "../Lit/LitInput.hlsl"
#include "../Lit/LitSurfaceData.hlsl"
#include "../Lit/LitVaryings.hlsl"
#include "../Lit/LitLighting.hlsl"

PackedVaryings vertMain(Attributes attributes)
{
    UNITY_SETUP_INSTANCE_ID(attributes);

    VertexInput vertexInput;
    ZERO_INITIALIZE(VertexInput, vertexInput);
    vertexInput = BuildVertexInput(attributes);

    Varyings varyings;
    ZERO_INITIALIZE(Varyings, varyings);
    varyings = BuildVaryings(vertexInput);
    
    PackedVaryings packedVaryings;
    ZERO_INITIALIZE(PackedVaryings, packedVaryings);
    packedVaryings = ForwardBuildPackVaryings(varyings);

    UNITY_TRANSFER_INSTANCE_ID(attributes, packedVaryings);

    return packedVaryings;
}

float4 fragMain(PackedVaryings packedVaryings)
{
    UNITY_SETUP_INSTANCE_ID(packedVaryings);

    Varyings varyings;
    ZERO_INITIALIZE(Varyings, varyings);
    varyings = ForwardBuildUnpackVaryings(packedVaryings);

    LitSurfaceData litSurfaceData;
    ZERO_INITIALIZE(LitSurfaceData, litSurfaceData);
    litSurfaceData = BuildLitSurfaceData(varyings);

    LightingInputData lightingInputData;
    ZERO_INITIALIZE(LightingInputData, lightingInputData);
    lightingInputData = BuildLightingInputData(varyings, litSurfaceData);

    BRDFData brdfData;
    ZERO_INITIALIZE(BRDFData, brdfData);
    brdfData = BuildBRDFData(litSurfaceData);

    BSDFCommonData bsdfCommonData;
    ZERO_INITIALIZE(BSDFCommonData, bsdfCommonData);
    bsdfCommonData = BuildBSDFCommonData(lightingInputData);

    LightingData lightingData;
    ZERO_INITIALIZE(LightingData, lightingData);
    lightingData = BuildLightingData(lightingInputData, brdfData, bsdfCommonData);

    LightingOutputData lightingOutputData;
    ZERO_INITIALIZE(LightingOutputData, lightingOutputData);
    lightingOutputData = BuildLightingOutputData(litSurfaceData, lightingData);

// #if defined(USE_UV0_VARYING)
//     float test = 1;
// #else
//     float test = 0;
// #endif
    // float test = GetAdditionalLightsCount() - 2;
    float3 test3 = abs(_LightDatas[1].positionWS); /* float3(test, test, test) */;
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