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

DirectLightingData LightingPBR(BRDFData brdfData, BSDFCommonData bsdfCommonData, Light light)
{
    DirectLightingData directLightingData;
    ZERO_INITIALIZE(DirectLightingData, directLightingData);

    float3 radiance = light.color * (light.shadowAttenuation * bsdfCommonData.saturateNdotL);
    directLightingData.diffuse = brdfData.diffuse * radiance;

    float d = Sq(bsdfCommonData.saturateNdotH) * brdfData.roughness2MinusOne + 1.00001f;
    float specularTerm = brdfData.roughness2 / (Sq(d) * max(0.1, Sq(bsdfCommonData.saturateLdotH)) * brdfData.normalizationTerm);
    directLightingData.specular = brdfData.specular * specularTerm;

    return directLightingData;
}

#endif