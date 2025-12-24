#ifndef HNRP_LIGHT_INCLUDED
#define HNRP_LIGHT_INCLUDED

#include "../ClusterCulling/ForwardPlusCluster.hlsl"

struct Light
{
    float3 color;
    float3 positionWS;
    float3 directionWS;
    float shadowAttenuation;
    float distanceAttenuation;
};

#if FORWARD_PLUS && defined(LIGHTMAP_ON) && defined(LIGHTMAP_SHADOW_MIXING)
#define FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK if(_AdditionalLightsColor[lightIndex].a > 0.0) continue;
#else
#define FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
#endif

#if FORWARD_PLUS
    #define LIGHT_LOOP_BEGIN(lightCount) { \
    uint lightIndex; \
    ClusterIterator _internal_clusterIterator = ClusterInit(inputData.normalizedScreenSpaceUV, inputData.positionWS, 0); \
    [loop] while (ClusterNext(_internal_clusterIterator, lightIndex)) { \
        lightIndex += FP_DIRECTIONAL_LIGHTS_COUNT; \
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
    #define LIGHT_LOOP_END } }
#else
    #define LIGHT_LOOP_BEGIN(lightCount) \
    for(uint lightIndex = 0u; lightIndex < lightCount; ++lightIndex) { \
        if(lightIndex == _LightConstantData.x) continue;

    #define LIGHT_LOOP_END }
#endif

float3 GetViewDirectionWS(float3 positionWS)
{
    return normalize(GetCameraPositionWS().xyz - positionWS);
}

Light GetMainLight()
{
    uint mainLightIndex = _LightConstantData.x;
    Light light;
    ZERO_INITIALIZE(Light, light);
    light.color = _LightDatas[mainLightIndex].color;
    light.directionWS = _LightDatas[mainLightIndex].directionWS;
    light.shadowAttenuation = 1.0;
    light.distanceAttenuation = 1.0;

    return light;
}

Light GetAdditionalLight(uint lightIndex, float3 positionWS)
{
    Light light;
    light.color = _LightDatas[lightIndex].color;
    light.directionWS = _LightDatas[lightIndex].directionWS;
    light.positionWS = _LightDatas[lightIndex].positionWS;
    light.shadowAttenuation = 1.0;
    uint lightType = asuint(_LightDatas[lightIndex].lightType);
    if(lightType == 1/* Directional light */)
    {
        light.distanceAttenuation = 1.0;
    }
    else if(lightType == 2 /* Point Light */|| lightType == 0 /* Spot Light */)
    {
        float3 lightVector = light.positionWS - positionWS;
        float distanceSqr = max(dot(lightVector, lightVector), FLT_MIN);
        light.directionWS = lightVector * rsqrt(distanceSqr);
        float distanceAttenuation = DistanceAttenuation(distanceSqr, _LightDatas[lightIndex].attenuation.xy);
        light.distanceAttenuation = distanceAttenuation;
        if(lightType == 0 /* Spot Light */)
        {
            float angleAttenuation = AngleAttenuation(_LightDatas[lightIndex].directionWS, light.directionWS, _LightDatas[lightIndex].attenuation.zw);
            light.distanceAttenuation = distanceAttenuation * angleAttenuation;
        }
    }

    return light;
}

int GetAdditionalLightsCount()
{
    return _LightConstantData.y;
}

#endif