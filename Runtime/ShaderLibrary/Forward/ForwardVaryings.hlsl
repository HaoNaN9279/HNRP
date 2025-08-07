#ifndef HNRP_FORWARD_VARYINGS_INCLUDED
#define HNRP_FORWARD_VARYINGS_INCLUDED

#include "../PackageRegistry.hlsl"

// positionWS
#if defined(USE_POSITION_WS_VARYING)
    #define VARYINGS_FLOAT_COUNT_POSITION_WS 3
    #define SET_VARYINGS_POSITION_WS(packedVaryings, value) SetVaryingsFloat3(packedVaryings, value, pointer);
    #define GET_VARYINGS_POSITION_WS(packedVaryings, value) GetVaryingsFloat3(packedVaryings, value, pointer);
#else
    #define VARYINGS_FLOAT_COUNT_POSITION_WS 0
    #define SET_VARYINGS_POSITION_WS(packedVaryings, value)
    #define GET_VARYINGS_POSITION_WS(packedVaryings, value)
#endif

// normalWS
#define VARYINGS_FLOAT_COUNT_NORMAL_WS 3
#define SET_VARYINGS_NORMAL_WS(packedVaryings, value) SetVaryingsFloat3(packedVaryings, value, pointer);
#define GET_VARYINGS_NORMAL_WS(packedVaryings, value) GetVaryingsFloat3(packedVaryings, value, pointer);

// tangentWS
#if defined(USE_TANGENT_WS_VARYING)
    #define VARYINGS_FLOAT_COUNT_TANGENT_WS 4
    #define SET_VARYINGS_TANGENT_WS(packedVaryings, value) SetVaryingsFloat4(packedVaryings, value, pointer);
    #define GET_VARYINGS_TANGENT_WS(packedVaryings, value) GetVaryingsFloat4(packedVaryings, value, pointer);
#else
    #define VARYINGS_FLOAT_COUNT_TANGENT_WS 0
    #define SET_VARYINGS_TANGENT_WS(packedVaryings, value)
    #define GET_VARYINGS_TANGENT_WS(packedVaryings, value)
#endif

// uv0
#define VARYINGS_FLOAT_COUNT_UV0 2
#define SET_VARYINGS_UV0(packedVaryings, value) SetVaryingsFloat2(packedVaryings, value, pointer);
#define GET_VARYINGS_UV0(packedVaryings, value) GetVaryingsFloat2(packedVaryings, value, pointer);

#define VARYINGS_FLOAT_COUNT (VARYINGS_FLOAT_COUNT_POSITION_WS + VARYINGS_FLOAT_COUNT_NORMAL_WS + VARYINGS_FLOAT_COUNT_TANGENT_WS + VARYINGS_FLOAT_COUNT_UV0 + VARYINGS_FLOAT_COUNT_PACKAGES)

#include "../PackedVaryings.hlsl"

PackedVaryings ForwardBuildPackedVaryings(Varyings varyings)
{
    PackedVaryings packedVaryings;
    ZERO_INITIALIZE(PackedVaryings, packedVaryings);
    int pointer = 0;

    packedVaryings.positionCS = varyings.positionCS;

    SET_VARYINGS_POSITION_WS(packedVaryings, varyings.positionWS);
    SET_VARYINGS_NORMAL_WS(packedVaryings, varyings.normalWS);
    SET_VARYINGS_TANGENT_WS(packedVaryings, varyings.tangentWS);
    SET_VARYINGS_UV0(packedVaryings, varyings.uv0);

    return packedVaryings;
}

Varyings ForwardBuildUnpackedVaryings(PackedVaryings packedVaryings)
{
    Varyings varyings;
    ZERO_INITIALIZE(Varyings, varyings);
    int pointer = VARYINGS_FLOAT_COUNT - 1;

    GET_VARYINGS_UV0(packedVaryings, varyings.uv0);
    GET_VARYINGS_TANGENT_WS(packedVaryings, varyings.tangentWS);
    GET_VARYINGS_NORMAL_WS(packedVaryings, varyings.normalWS);
    GET_VARYINGS_POSITION_WS(packedVaryings, varyings.positionWS);

    return varyings;
}

#endif