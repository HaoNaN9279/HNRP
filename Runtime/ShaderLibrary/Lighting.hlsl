#ifndef HNRP_LIGHTING_INCLUDED
#define HNRP_LIGHTING_INCLUDED

struct DirectLightingData
{
    float3 diffuse;
    float3 specular;
};

struct IndirectLightingData
{
    float3 diffuse;
    float3 specular;
};

DirectLightingData DirectLightingPBR(BRDFData brdfData, BSDFCommonData bsdfCommonData, Light light)
{
    DirectLightingData directLightingData;
    ZERO_INITIALIZE(DirectLightingData, directLightingData);

    float3 radiance = light.color * (light.shadowAttenuation * bsdfCommonData.saturateNdotL);
    directLightingData.diffuse = brdfData.diffuse * radiance;
    directLightingData.specular = brdfData.specular * DirectBRDFSpecular(brdfData, bsdfCommonData);

    return directLightingData;
}

IndirectLightingData IndirectLightingPBR(BRDFData brdfData, BSDFCommonData bsdfCommonData, float3 bakedGI, float3 positionWS)
{
    IndirectLightingData indirectLightingData;
    ZERO_INITIALIZE(IndirectLightingData, indirectLightingData);

    indirectLightingData.diffuse = bakedGI * brdfData.diffuse;
    float3 envReflection = GlossyEnvironmentReflection(bsdfCommonData.refViewDirectionWS, positionWS, brdfData.perceptualRoughness, 1.0, float2(0.0, 0.0));
    float3 envSpecular = EnvironmentBRDFSpecular(brdfData, bsdfCommonData);
    indirectLightingData.specular = envReflection * envSpecular;

    return indirectLightingData;
}

#endif