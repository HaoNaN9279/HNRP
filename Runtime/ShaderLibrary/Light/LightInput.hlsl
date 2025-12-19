#ifndef HNRP_LIGHT_INPUT_INCLUDED
#define HNRP_LIGHT_INPUT_INCLUDED

struct LightData
{
    float3 positionWS;
    uint lightType;
    float3 color;
    float __unused__0;
    float4 attenuation;
    float3 directionWS;
    bool __unused__1;
};

StructuredBuffer<LightData> _LightDatas;

#endif