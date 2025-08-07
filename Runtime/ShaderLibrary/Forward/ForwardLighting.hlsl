#ifndef HNRP_FORWARD_LIGHTING_INCLUDED
#define HNRP_FORWARD_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonShadow.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/AreaLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/BSDF.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SphericalHarmonics.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/MetaPass.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"

#include "../SurfaceData.hlsl"
#include "../Light.hlsl"
#include "../BRDF.hlsl"
#include "../GI.hlsl"
#include "../Shadow.hlsl"
#include "../Lighting.hlsl"

struct LightingInputData
{
    float3 viewDirectionWS;
    Light mainLight;
    float2 uv0;
    NormalData normalData;
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

    float3 viewDirectionWS = GetViewDirectionWS(varyings.positionWS);
    lightingInputData.viewDirectionWS = viewDirectionWS;

    Light mainLight = GetMainLight();
    lightingInputData.mainLight = mainLight;

    NormalData normalData = GetNormalData(varyings.normalWS, varyings.tangentWS, surfaceData.normalTS);
    lightingInputData.normalData = normalData;

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

    lightingData.directLight = LightingPBR(brdfData, bsdfCommonData, lightingInputData.mainLight);

    return lightingData;
}

LightingOutputData BuildLightingOutputData(LightingData lightingData)
{
    LightingOutputData lightingOutputData;
    ZERO_INITIALIZE(LightingOutputData, lightingOutputData);

    lightingOutputData.lightingColor.rgb = lightingData.directLight.diffuse.rgb + lightingData.directLight.specular.rgb + lightingData.indirectLight.diffuse.rgb + lightingData.indirectLight.specular.rgb;

    return lightingOutputData;
}

#endif