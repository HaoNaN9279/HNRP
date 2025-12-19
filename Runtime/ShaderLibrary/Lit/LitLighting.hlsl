#ifndef HNRP_LIT_LIGHTING_INCLUDED
#define HNRP_LIT_LIGHTING_INCLUDED

#include "../Light/Light.hlsl"
#include "../Lighting/Lighting.hlsl"
#include "../Lighting/BRDF.hlsl"

struct LightingInputData
{
    float3 positionWS;
    float3 viewDirectionWS;
    Light mainLight;
    float2 mainUV;
    SurfaceData surfaceData;
    float3 bakedGI;
    float2 normalizedScreenSpaceUV;
};

struct LightingData
{
    DirectLightingData mainDirectLight;
    DirectLightingData additionalDirectLight;
    IndirectLightingData indirectLight;
};

struct LightingOutputData
{
    float3 lightingColor;
};

LitSurfaceData BuildLitSurfaceData(Varyings varyings)
{
    LitSurfaceData litSurfaceData;
    InitializeLitSurfaceData(varyings.uv0, litSurfaceData);

    return litSurfaceData;
}

LightingInputData BuildLightingInputData(Varyings varyings, LitSurfaceData litSurfaceData)
{
    LightingInputData lightingInputData;
    ZERO_INITIALIZE(LightingInputData, lightingInputData);

    lightingInputData.positionWS = varyings.positionWS;

    float3 viewDirectionWS = GetViewDirectionWS(varyings.positionWS);
    lightingInputData.viewDirectionWS = viewDirectionWS;

    Light mainLight = GetMainLight();
    lightingInputData.mainLight = mainLight;

    lightingInputData.mainUV = varyings.uv0;

    SurfaceData surfaceData = GetSurfaceData(varyings.normalWS, varyings.tangentWS, litSurfaceData.normalTS);
    lightingInputData.surfaceData = surfaceData;

    float3 bakedGI = SAMPLE_GI(varyings.staticLightmapUV, varyings.vertexSH, surfaceData.normalWS);
    lightingInputData.bakedGI = bakedGI;

    float2 normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(varyings.positionCS);
    lightingInputData.normalizedScreenSpaceUV;

    return lightingInputData;
}

BRDFData BuildBRDFData(LitSurfaceData litSurfaceData)
{
    BRDFData brdfData;
    InitializeBRDFData(litSurfaceData.albedo, litSurfaceData.metallic, litSurfaceData.smoothness, litSurfaceData.alpha, brdfData);
    
    return brdfData;
}

BSDFCommonData BuildBSDFCommonData(LightingInputData lightingInputData)
{
    BSDFCommonData bsdfCommonData;
    InitializeBSDFCommonData(lightingInputData.surfaceData.normalWS, lightingInputData.viewDirectionWS, lightingInputData.mainLight, bsdfCommonData);

    return bsdfCommonData;
}

LightingData BuildLightingData(LightingInputData lightingInputData, BRDFData brdfData, BSDFCommonData bsdfCommonData)
{
    LightingData lightingData;
    ZERO_INITIALIZE(LightingData, lightingData);

    float3 mainLightRadiance = DirectLightingDiffuseRadiance(lightingInputData.mainLight, bsdfCommonData.saturateNdotL);
    float mainLightSpecularTerm = DirectBRDFSpecular(brdfData, bsdfCommonData);
    lightingData.mainDirectLight = DirectLightingPBR(brdfData.diffuse, mainLightRadiance, brdfData.specular, mainLightSpecularTerm);

#if FORWARD_PLUS
    for(uint lightIndex = 0; lightIndex < FP_DIRECTIONAL_LIGHTS_COUNT; lightIndex++)
    {
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
        
        Light light = GetAdditionalLight(lightIndex, lightingInputData.positionWS);
        float saturateNdotL = saturate(dot(lightingInputData.surfaceData.normalWS, light.directionWS));
        float3 diffuseRadiance = DirectLightingDiffuseRadiance(light, saturateNdotL);
        float lightSpecularTerm = DirectBRDFSpecular(brdfData, bsdfCommonData);
        float3 specularRadiance = DirectLightingSpecularRadiance(light, lightSpecularTerm);
        DirectLightingData additionalDirectLight = DirectLightingPBR(brdfData.diffuse, diffuseRadiance, brdfData.specular, specularRadiance);
        lightingData.additionalDirectLight.diffuse += additionalDirectLight.diffuse;
        lightingData.additionalDirectLight.specular += additionalDirectLight.specular;
    }
#endif

    int lightCount = GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(lightCount)
        Light light = GetAdditionalLight(lightIndex, lightingInputData.positionWS);
        float saturateNdotL = saturate(dot(lightingInputData.surfaceData.normalWS, light.directionWS));
        float3 diffuseRadiance = DirectLightingDiffuseRadiance(light, saturateNdotL);
        float lightSpecularTerm = DirectBRDFSpecular(brdfData, bsdfCommonData);
        float3 specularRadiance = DirectLightingSpecularRadiance(light, lightSpecularTerm);
        DirectLightingData additionalDirectLight = DirectLightingPBR(brdfData.diffuse, diffuseRadiance, brdfData.specular, specularRadiance);
        lightingData.additionalDirectLight.diffuse += additionalDirectLight.diffuse;
        lightingData.additionalDirectLight.specular += additionalDirectLight.specular;
    LIGHT_LOOP_END

    float3 envSpecular = EnvironmentBRDFSpecular(brdfData, bsdfCommonData);
    float3 envReflection = GlossyEnvironmentReflection(bsdfCommonData.refViewDirectionWS, lightingInputData.positionWS, brdfData.perceptualRoughness, 1.0, float2(0.0, 0.0));
    lightingData.indirectLight = IndirectLightingPBR(brdfData.diffuse, lightingInputData.bakedGI, envSpecular, envReflection);

    return lightingData;
}

LightingOutputData BuildLightingOutputData(LitSurfaceData litSurfaceData, LightingData lightingData)
{
    LightingOutputData lightingOutputData;
    ZERO_INITIALIZE(LightingOutputData, lightingOutputData);

    lightingOutputData.lightingColor.rgb = 
        lightingData.mainDirectLight.diffuse.rgb + 
        lightingData.mainDirectLight.specular.rgb + 
        lightingData.additionalDirectLight.diffuse.rgb +
        lightingData.additionalDirectLight.specular.rgb +
        lightingData.indirectLight.diffuse.rgb + 
        lightingData.indirectLight.specular.rgb
        ;
    lightingOutputData.lightingColor.rgb += litSurfaceData.emission.rgb;

    return lightingOutputData;
}

#endif