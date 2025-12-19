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

struct BSDFCommonData
{
    float NdotL;
    float saturateNdotL;
    float3 nH;   // normalized HalfDir
    float NdotH;
    float saturateNdotH;
    float LdotH;
    float saturateLdotH;
    float3 refViewDirectionWS;
    float NdotV;
    float saturateNdotV;
    float fresnelTerm;
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

void InitializeBSDFCommonData(float3 normalWS, float3 viewDirectionWS, Light light, out BSDFCommonData bsdfCommonData)
{
    ZERO_INITIALIZE(BSDFCommonData, bsdfCommonData);

    float NdotL = dot(normalWS, light.directionWS);
    float saturateNdotL = saturate(NdotL);
    float3 halfDir = SafeNormalize(light.directionWS + viewDirectionWS);
    float NdotH = dot(normalWS, halfDir);
    float saturateNdotH = saturate(NdotH);
    float LdotH = dot(light.directionWS, halfDir);
    float saturateLdotH = saturate(LdotH);
    float3 refViewDirectionWS = reflect(-viewDirectionWS, normalWS);
    float NdotV = dot(normalWS, viewDirectionWS);
    float saturateNdotV = saturate(NdotV);
    float fresnelTerm = Pow4(1.0 - saturateNdotV);

    bsdfCommonData.NdotL = NdotL;
    bsdfCommonData.saturateNdotL = saturateNdotL;
    bsdfCommonData.nH = halfDir;
    bsdfCommonData.NdotH = NdotH;
    bsdfCommonData.saturateNdotH = saturateNdotH;
    bsdfCommonData.LdotH = LdotH;
    bsdfCommonData.saturateLdotH = saturateLdotH;
    bsdfCommonData.refViewDirectionWS = refViewDirectionWS;
    bsdfCommonData.NdotV = NdotV;
    bsdfCommonData.saturateNdotV = saturateNdotV;
    bsdfCommonData.fresnelTerm = fresnelTerm;
}

float DirectBRDFSpecular(BRDFData brdfData, BSDFCommonData bsdfCommonData)
{
    float d = Sq(bsdfCommonData.saturateNdotH) * brdfData.roughness2MinusOne + 1.00001f;
    float specularTerm = brdfData.roughness2 / (Sq(d) * max(0.1, Sq(bsdfCommonData.saturateLdotH)) * brdfData.normalizationTerm);

    return specularTerm;
}

float3 EnvironmentBRDFSpecular(BRDFData brdfData, BSDFCommonData bsdfCommonData)
{
    float surfaceReduction = 1.0 / (brdfData.roughness2 + 1.0);
    float3 specularTerm = float3(surfaceReduction * lerp(brdfData.specular, brdfData.grazingTerm, bsdfCommonData.fresnelTerm));

    return specularTerm;
}


#endif