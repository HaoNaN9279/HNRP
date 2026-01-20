#ifndef HNRP_BRDF_INCLUDED
#define HNRP_BRDF_INCLUDED

struct PreBRDFData
{
    float3 sfNormalWS;
    float4 sfTangentWS;
    float3 sfBitangentWS;
    float sgn;
    float3x3 tbn;
    float4 kDielectricSpec;
    float3 viewDirectionWS;
};

struct BRDFData
{
    float3 normalTS;
    float3 normalWS;
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
    float3 refViewDirectionWS;
    float NdotV;
    float saturateNdotV;
    float fresnelTerm;
};

struct BRDFLightingData
{
    float NdotL;
    float saturateNdotL;
    float3 nH;   // normalized HalfDir
    float NdotH;
    float saturateNdotH;
    float LdotH;
    float saturateLdotH;
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

void InitializePreBRDFData(float3 rawNormalWS, float4 rawTangentWS, float3 positionWS, out PreBRDFData preBRDFData)
{
    ZERO_INITIALIZE(PreBRDFData, preBRDFData);

    float4 kDielectricSpec = float4(0.04, 0.04, 0.04, 1.0 - 0.04);
    preBRDFData.kDielectricSpec = kDielectricSpec;
    float3 viewDirectionWS = GetViewDirectionWS(positionWS);
    preBRDFData.viewDirectionWS = viewDirectionWS;
    preBRDFData.sfNormalWS = GetSfNormalWS(rawNormalWS);
    preBRDFData.sfTangentWS = GetSfTangentWS(rawTangentWS);
    preBRDFData.sfBitangentWS = GetSfBitangentWS(preBRDFData.sfNormalWS, preBRDFData.sfTangentWS);
    preBRDFData.sgn = GetNormalSGN(preBRDFData.sfTangentWS);
    preBRDFData.tbn = GetNormalTBN(preBRDFData.sfNormalWS, preBRDFData.sfTangentWS, preBRDFData.sfBitangentWS);
}

void InitializeBRDFData(float3 albedo, float3 normalTS, float smoothness, float metallic, PreBRDFData preBRDFData, out BRDFData brdfData)
{
    ZERO_INITIALIZE(BRDFData, brdfData);
    
    brdfData.normalTS = normalTS;
    brdfData.normalWS = GetNormalWS(preBRDFData.sfNormalWS, brdfData.normalTS, preBRDFData.tbn);

    float oneMinusReflectivity = OnMinusReflectivityMetallic(metallic, preBRDFData.kDielectricSpec);
    brdfData.oneMinusReflectivity = oneMinusReflectivity;

    float reflectivity = 1.0 - oneMinusReflectivity;
    brdfData.reflectivity = reflectivity;

    float3 diffuse = albedo * oneMinusReflectivity;
    brdfData.diffuse = diffuse;

    float3 specular = lerp(preBRDFData.kDielectricSpec.rgb, albedo, metallic);
    brdfData.specular = specular;

    brdfData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(smoothness);

    brdfData.roughness = max(PerceptualRoughnessToRoughness(brdfData.perceptualRoughness), FLT_MIN);

    brdfData.roughness2 = max(brdfData.roughness * brdfData.roughness, FLT_MIN);

    brdfData.grazingTerm = saturate(smoothness + reflectivity);

    brdfData.normalizationTerm = brdfData.roughness * 4.0 + 2.0;

    brdfData.roughness2MinusOne = brdfData.roughness2 - 1.0;
    
    float3 refViewDirectionWS = reflect(-preBRDFData.viewDirectionWS, brdfData.normalWS);
    brdfData.refViewDirectionWS = refViewDirectionWS;

    float NdotV = dot(brdfData.normalWS, preBRDFData.viewDirectionWS);
    brdfData.NdotV = NdotV;

    float saturateNdotV = saturate(NdotV);
    brdfData.saturateNdotV = saturateNdotV;

    float fresnelTerm = Pow4(1.0 - saturateNdotV);
    brdfData.fresnelTerm = fresnelTerm;
}

void InitializeBRDFLightingData(float3 normalWS, float3 viewDirectionWS, Light light, out BRDFLightingData brdfLightingData)
{
    ZERO_INITIALIZE(BRDFLightingData, brdfLightingData);

    float NdotL = dot(normalWS, light.directionWS);
    float saturateNdotL = saturate(NdotL);
    float3 halfDir = SafeNormalize(light.directionWS + viewDirectionWS);
    float NdotH = dot(normalWS, halfDir);
    float saturateNdotH = saturate(NdotH);
    float LdotH = dot(light.directionWS, halfDir);
    float saturateLdotH = saturate(LdotH);

    brdfLightingData.NdotL = NdotL;
    brdfLightingData.saturateNdotL = saturateNdotL;
    brdfLightingData.nH = halfDir;
    brdfLightingData.NdotH = NdotH;
    brdfLightingData.saturateNdotH = saturateNdotH;
    brdfLightingData.LdotH = LdotH;
    brdfLightingData.saturateLdotH = saturateLdotH;
}

float DirectBRDFSpecular(BRDFData brdfData, BRDFLightingData brdfLightingData)
{
    float d = Sq(brdfLightingData.saturateNdotH) * brdfData.roughness2MinusOne + 1.00001f;
    float specularTerm = brdfData.roughness2 / (Sq(d) * max(0.1, Sq(brdfLightingData.saturateLdotH)) * brdfData.normalizationTerm);

    return specularTerm;
}

float3 EnvironmentBRDFSpecular(BRDFData brdfData, BRDFLightingData brdfLightingData)
{
    float surfaceReduction = 1.0 / (brdfData.roughness2 + 1.0);
    float3 specularTerm = float3(surfaceReduction * lerp(brdfData.specular, brdfData.grazingTerm, brdfData.fresnelTerm));

    return specularTerm;
}


#endif