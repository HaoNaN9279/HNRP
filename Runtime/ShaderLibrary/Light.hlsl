#ifndef HNRP_LIGHT_INCLUDED
#define HNRP_LIGHT_INCLUDED

struct Light
{
    float3 color;
    float3 direction;
    float shadowAttenuation;
};

Light GetMainLight()
{
    Light light;
    light.color = _MainLightColor.rgb;
    light.direction = _MainLightPosition.xyz;
    light.shadowAttenuation = 1.0;

    return light;
}

#endif