#ifndef HNRP_BRDF_INCLUDED
#define HNRP_BRDF_INCLUDED

struct BRDFData
{
    float3 albedo;
    float4 kDielectricSpec;
    float oneMinusReflectivity;
    float reflectivity;
    float3 diffuse;
    float3 specular;
    float perceptualRoughness;
    float roughness;
    float roughness2;
    float grazingTerm;
    float normalizationTerm;
    float roughness2MinusOne;
};

float OnMinusReflectivityMetallic(float metallic, float4 kDielectricSpec)
{
    // We'll need oneMinusReflectivity, so
    //   1-reflectivity = 1-lerp(dielectricSpec, 1, metallic) = lerp(1-dielectricSpec, 0, metallic)
    // store (1-dielectricSpec) in kDielectricSpec.a, then
    //   1-reflectivity = lerp(alpha, 0, metallic) = alpha + metallic*(0 - alpha) =
    //                  = alpha - metallic * alpha
    float oneMinusDielectricSpec = kDielectricSpec.a;
    return oneMinusDielectricSpec - metallic * oneMinusDielectricSpec;
}

void InitializeBRDFData(float3 albedo, float metallic, float smoothness, float alpha, out BRDFData brdfData)
{
    ZERO_INITIALIZE(BRDFData, brdfData);

    brdfData.albedo = albedo;

    float4 kDielectricSpec = float4(0.04, 0.04, 0.04, 1.0 - 0.04);
    brdfData.kDielectricSpec = kDielectricSpec;

    float oneMinusReflectivity = OnMinusReflectivityMetallic(metallic, brdfData.kDielectricSpec);
    brdfData.oneMinusReflectivity = oneMinusReflectivity;

    float reflectivity = 1.0 - oneMinusReflectivity;
    brdfData.reflectivity = reflectivity;

    float3 diffuse = albedo * oneMinusReflectivity;
    brdfData.diffuse = diffuse;

    float3 specular = lerp(kDielectricSpec.rgb, albedo, metallic);
    brdfData.specular = specular;

    brdfData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(smoothness);
    brdfData.roughness = max(PerceptualRoughnessToRoughness(brdfData.perceptualRoughness), FLT_MIN);
    brdfData.roughness2 = max(brdfData.roughness * brdfData.roughness, FLT_MIN);
    brdfData.grazingTerm = saturate(smoothness + reflectivity);
    brdfData.normalizationTerm = brdfData.roughness * 4.0 + 2.0;
    brdfData.roughness2MinusOne = brdfData.roughness2 - 1.0;
}

struct BSDFCommonData
{
    float NdotL;
    float saturateNdotL;
    float3 nH;   // normalized HalfDir
    float NdotH;
    float saturateNdotH;
    float LdotH;
    float saturateLdotH;
};

void InitializeBSDFCommonData(float3 normalWS, float3 viewDirectionWS, Light light, out BSDFCommonData bsdfCommonData)
{
    ZERO_INITIALIZE(BSDFCommonData, bsdfCommonData);

    float NdotL = dot(normalWS, light.direction);
    bsdfCommonData.NdotL = NdotL;
    bsdfCommonData.saturateNdotL = saturate(NdotL);
    float3 halfDir = SafeNormalize(light.direction + viewDirectionWS);
    bsdfCommonData.nH = halfDir;
    float NdotH = dot(normalWS, halfDir);
    bsdfCommonData.NdotH = NdotH;
    bsdfCommonData.saturateNdotH = saturate(NdotH);
    float LdotH = dot(light.direction, halfDir);
    bsdfCommonData.LdotH = LdotH;
    bsdfCommonData.saturateLdotH = saturate(LdotH);
}

#endif