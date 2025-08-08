#ifndef HNRP_LIT_LIGHTING_INCLUDED
#define HNRP_LIT_LIGHTING_INCLUDED

struct LightingInputData
{
    float3 positionWS;
    float3 viewDirectionWS;
    Light mainLight;
    float2 mainUV;
    NormalData normalData;
    float3 bakedGI;
};

struct LightingData
{
    DirectLightingData directLight;
    IndirectLightingData indirectLight;
};

struct LightingOutputData
{
    float3 lightingColor;
};

SurfaceData BuildSurfaceData(Varyings varyings)
{
    SurfaceData surfaceData;
    InitializeSurfaceData(varyings.uv0, surfaceData);

    return surfaceData;
}

LightingInputData BuildLightingInputData(Varyings varyings, SurfaceData surfaceData)
{
    LightingInputData lightingInputData;
    ZERO_INITIALIZE(LightingInputData, lightingInputData);

    lightingInputData.positionWS = varyings.positionWS;

    float3 viewDirectionWS = GetViewDirectionWS(varyings.positionWS);
    lightingInputData.viewDirectionWS = viewDirectionWS;

    Light mainLight = GetMainLight();
    lightingInputData.mainLight = mainLight;

    lightingInputData.mainUV = varyings.uv0;

    NormalData normalData = GetNormalData(varyings.normalWS, varyings.tangentWS, surfaceData.normalTS);
    lightingInputData.normalData = normalData;

    float3 bakedGI = SAMPLE_GI(varyings.staticLightmapUV, varyings.dynamicLightmapUV, varyings.vertexSH, normalData.normalWS);
    lightingInputData.bakedGI = bakedGI;

    return lightingInputData;
}

BRDFData BuildBRDFData(SurfaceData surfaceData)
{
    BRDFData brdfData;
    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.smoothness, surfaceData.alpha, brdfData);
    
    return brdfData;
}

BSDFCommonData BuildBSDFCommonData(LightingInputData lightingInputData)
{
    BSDFCommonData bsdfCommonData;
    InitializeBSDFCommonData(lightingInputData.normalData.normalWS, lightingInputData.viewDirectionWS, lightingInputData.mainLight, bsdfCommonData);

    return bsdfCommonData;
}

LightingData BuildLightingData(LightingInputData lightingInputData, BRDFData brdfData, BSDFCommonData bsdfCommonData)
{
    LightingData lightingData;
    ZERO_INITIALIZE(LightingData, lightingData);

    lightingData.directLight = DirectLightingPBR(brdfData, bsdfCommonData, lightingInputData.mainLight);
    lightingData.indirectLight = IndirectLightingPBR(brdfData, bsdfCommonData, lightingInputData.bakedGI, lightingInputData.positionWS);

    return lightingData;
}

LightingOutputData BuildLightingOutputData(SurfaceData surfaceData, LightingData lightingData)
{
    LightingOutputData lightingOutputData;
    ZERO_INITIALIZE(LightingOutputData, lightingOutputData);

    lightingOutputData.lightingColor.rgb = lightingData.directLight.diffuse.rgb + lightingData.directLight.specular.rgb + lightingData.indirectLight.diffuse.rgb + lightingData.indirectLight.specular.rgb;
    lightingOutputData.lightingColor.rgb += surfaceData.emission.rgb;

    return lightingOutputData;
}

#endif