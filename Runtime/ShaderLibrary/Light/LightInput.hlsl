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

GLOBAL_CBUFFER_START(ReflectionProbeVariablesGlobal, b2)
    float4 _ReflectionProbeData0[MAX_REFLECTION_PROBES_ON_SCREEN]; // x,y,z: boxMax, w: blendDistance
    float4 _ReflectionProbeData1[MAX_REFLECTION_PROBES_ON_SCREEN]; // x,y,z: boxMin, w: importance
    float4 _ReflectionProbeData2[MAX_REFLECTION_PROBES_ON_SCREEN]; // x,y,z: positionWS, w: intensity
    float4 _ReflectionProbeData3[MAX_REFLECTION_PROBES_ON_SCREEN]; // x,y,z,w: scaleOffset
CBUFFER_END

#endif