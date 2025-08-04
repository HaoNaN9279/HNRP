#ifndef HNRP_PACKED_VARYINGS_INCLUDED
#define HNRP_PACKED_VARYINGS_INCLUDED

#include "PackageRegistry.hlsl"

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

// #define VARYINGS_FLOAT_COUNT (VARYINGS_FLOAT_COUNT_NORMAL_WS + VARYINGS_FLOAT_COUNT_TANGENT_WS + VARYINGS_FLOAT_COUNT_UV0 + VARYINGS_FLOAT_COUNT_PACKAGES)
#define VARYINGS_FLOAT_COUNT 9


struct PackedVaryings
{
    float4 positionCS : SV_POSITION;
    
#if (VARYINGS_FLOAT_COUNT == 1)
    float packBuffer0[1] : TEXCOORD0;
#elif (VARYINGS_FLOAT_COUNT == 2)
    float2 packBuffer0 : TEXCOORD0;
#elif (VARYINGS_FLOAT_COUNT == 3)
    float3 packBuffer0 : TEXCOORD0;
#elif (VARYINGS_FLOAT_COUNT >= 4)
    float4 packBuffer0 : TEXCOORD0;
#endif

#if (VARYINGS_FLOAT_COUNT == 5)
    float packBuffer1[1] : TEXCOORD1;
#elif (VARYINGS_FLOAT_COUNT == 6)
    float2 packBuffer1 : TEXCOORD1;
#elif (VARYINGS_FLOAT_COUNT == 7)
    float3 packBuffer1 : TEXCOORD1;
#elif (VARYINGS_FLOAT_COUNT >= 8)
    float4 packBuffer1 : TEXCOORD1;
#endif

#if (VARYINGS_FLOAT_COUNT == 9)
    float packBuffer2[1] : TEXCOORD2;
#elif (VARYINGS_FLOAT_COUNT == 10)
    float2 packBuffer2 : TEXCOORD2;
#elif (VARYINGS_FLOAT_COUNT == 11)
    float3 packBuffer2 : TEXCOORD2;
#elif (VARYINGS_FLOAT_COUNT >= 12)
    float4 packBuffer2 : TEXCOORD2;
#endif

#if (VARYINGS_FLOAT_COUNT == 13)
    float packBuffer3[1] : TEXCOORD3;
#elif (VARYINGS_FLOAT_COUNT == 14)
    float2 packBuffer3 : TEXCOORD3;
#elif (VARYINGS_FLOAT_COUNT == 15)
    float3 packBuffer3 : TEXCOORD3;
#elif (VARYINGS_FLOAT_COUNT >= 16)
    float4 packBuffer3 : TEXCOORD3;
#endif

#if (VARYINGS_FLOAT_COUNT == 17)
    float packBuffer4[1] : TEXCOORD4;
#elif (VARYINGS_FLOAT_COUNT == 18)
    float2 packBuffer4 : TEXCOORD4;
#elif (VARYINGS_FLOAT_COUNT == 19)
    float3 packBuffer4 : TEXCOORD4;
#elif (VARYINGS_FLOAT_COUNT >= 20)
    float4 packBuffer4 : TEXCOORD4;
#endif

#if (VARYINGS_FLOAT_COUNT == 21)
    float packBuffer5[1] : TEXCOORD5;
#elif (VARYINGS_FLOAT_COUNT == 22)
    float2 packBuffer5 : TEXCOORD5;
#elif (VARYINGS_FLOAT_COUNT == 23)
    float3 packBuffer5 : TEXCOORD5;
#elif (VARYINGS_FLOAT_COUNT >= 24)
    float4 packBuffer5 : TEXCOORD5;
#endif

#if (VARYINGS_FLOAT_COUNT == 25)
    float packBuffer6[1] : TEXCOORD6;
#elif (VARYINGS_FLOAT_COUNT == 26)
    float2 packBuffer6 : TEXCOORD6;
#elif (VARYINGS_FLOAT_COUNT == 27)
    float3 packBuffer6 : TEXCOORD6;
#elif (VARYINGS_FLOAT_COUNT >= 28)
    float4 packBuffer6 : TEXCOORD6;
#endif

#if (VARYINGS_FLOAT_COUNT == 29)
    float packBuffer7[1] : TEXCOORD7;
#elif (VARYINGS_FLOAT_COUNT == 30)
    float2 packBuffer7 : TEXCOORD7;
#elif (VARYINGS_FLOAT_COUNT == 31)
    float3 packBuffer7 : TEXCOORD7;
#elif (VARYINGS_FLOAT_COUNT >= 32)
    float4 packBuffer7 : TEXCOORD7;
#endif
};

void SetVaryingsFloat(inout PackedVaryings packedVaryings, float value, inout int pointer)
{
#define SET_FLOAT(i, j) \
    if(pointer == (i * 4 + j)) \
        packedVaryings.packBuffer##i[j] = value;

#if (VARYINGS_FLOAT_COUNT >= 1)
    SET_FLOAT(0, 0)
#endif
#if (VARYINGS_FLOAT_COUNT >= 2)
    SET_FLOAT(0, 1)
#endif
#if (VARYINGS_FLOAT_COUNT >= 3)
    SET_FLOAT(0, 2)
#endif
#if (VARYINGS_FLOAT_COUNT >= 4)
    SET_FLOAT(0, 3)
#endif

#if (VARYINGS_FLOAT_COUNT >= 5)
    SET_FLOAT(1, 0)
#endif
#if (VARYINGS_FLOAT_COUNT >= 6)
    SET_FLOAT(1, 1)
#endif
#if (VARYINGS_FLOAT_COUNT >= 7)
    SET_FLOAT(1, 2)
#endif
#if (VARYINGS_FLOAT_COUNT >= 8)
    SET_FLOAT(1, 3)
#endif

#if (VARYINGS_FLOAT_COUNT >= 9)
    SET_FLOAT(2, 0)
#endif
#if (VARYINGS_FLOAT_COUNT >= 10)
    SET_FLOAT(2, 1)
#endif
#if (VARYINGS_FLOAT_COUNT >= 11)
    SET_FLOAT(2, 2)
#endif
#if (VARYINGS_FLOAT_COUNT >= 12)
    SET_FLOAT(2, 3)
#endif

#if (VARYINGS_FLOAT_COUNT >= 13)
    SET_FLOAT(3, 0)
#endif
#if (VARYINGS_FLOAT_COUNT >= 14)
    SET_FLOAT(3, 1)
#endif
#if (VARYINGS_FLOAT_COUNT >= 15)
    SET_FLOAT(3, 2)
#endif
#if (VARYINGS_FLOAT_COUNT >= 16)
    SET_FLOAT(3, 3)
#endif

#if (VARYINGS_FLOAT_COUNT >= 17)
    SET_FLOAT(4, 0)
#endif
#if (VARYINGS_FLOAT_COUNT >= 18)
    SET_FLOAT(4, 1)
#endif
#if (VARYINGS_FLOAT_COUNT >= 19)
    SET_FLOAT(4, 2)
#endif
#if (VARYINGS_FLOAT_COUNT >= 20)
    SET_FLOAT(4, 3)
#endif

#if (VARYINGS_FLOAT_COUNT >= 21)
    SET_FLOAT(5, 0)
#endif
#if (VARYINGS_FLOAT_COUNT >= 22)
    SET_FLOAT(5, 1)
#endif
#if (VARYINGS_FLOAT_COUNT >= 23)
    SET_FLOAT(5, 2)
#endif
#if (VARYINGS_FLOAT_COUNT >= 24)
    SET_FLOAT(5, 3)
#endif

#if (VARYINGS_FLOAT_COUNT >= 25)
    SET_FLOAT(6, 0)
#endif
#if (VARYINGS_FLOAT_COUNT >= 26)
    SET_FLOAT(6, 1)
#endif
#if (VARYINGS_FLOAT_COUNT >= 27)
    SET_FLOAT(6, 2)
#endif
#if (VARYINGS_FLOAT_COUNT >= 28)
    SET_FLOAT(6, 3)
#endif

#if (VARYINGS_FLOAT_COUNT >= 29)
    SET_FLOAT(7, 0)
#endif
#if (VARYINGS_FLOAT_COUNT >= 30)
    SET_FLOAT(7, 1)
#endif
#if (VARYINGS_FLOAT_COUNT >= 31)
    SET_FLOAT(7, 2)
#endif
#if (VARYINGS_FLOAT_COUNT >= 32)
    SET_FLOAT(7, 3)
#endif

    pointer++;
}

void SetVaryingsFloat2(inout PackedVaryings packedVaryings, float2 value, inout int pointer)
{
    SetVaryingsFloat(packedVaryings, value.x, pointer);
    SetVaryingsFloat(packedVaryings, value.y, pointer);
}

void SetVaryingsFloat3(inout PackedVaryings packedVaryings, float3 value, inout int pointer)
{
    SetVaryingsFloat(packedVaryings, value.x, pointer);
    SetVaryingsFloat(packedVaryings, value.y, pointer);
    SetVaryingsFloat(packedVaryings, value.z, pointer);
}

void SetVaryingsFloat4(inout PackedVaryings packedVaryings, float4 value, inout int pointer)
{
    SetVaryingsFloat(packedVaryings, value.x, pointer);
    SetVaryingsFloat(packedVaryings, value.y, pointer);
    SetVaryingsFloat(packedVaryings, value.z, pointer);
    SetVaryingsFloat(packedVaryings, value.w, pointer);
}

void GetVaryingsFloat(PackedVaryings packedVaryings, inout float value, inout int pointer)
{
    value = 0;

#define GET_FLOAT(i, j) \
    if(pointer == (i * 4 + j)) \
        value = packedVaryings.packBuffer##i[j];

#if (VARYINGS_FLOAT_COUNT >= 1)
    GET_FLOAT(0, 0)
#endif
#if (VARYINGS_FLOAT_COUNT >= 2)
    GET_FLOAT(0, 1)
#endif
#if (VARYINGS_FLOAT_COUNT >= 3)
    GET_FLOAT(0, 2)
#endif
#if (VARYINGS_FLOAT_COUNT >= 4)
    GET_FLOAT(0, 3)
#endif

#if (VARYINGS_FLOAT_COUNT >= 5)
    GET_FLOAT(1, 0)
#endif
#if (VARYINGS_FLOAT_COUNT >= 6)
    GET_FLOAT(1, 1)
#endif
#if (VARYINGS_FLOAT_COUNT >= 7)
    GET_FLOAT(1, 2)
#endif
#if (VARYINGS_FLOAT_COUNT >= 8)
    GET_FLOAT(1, 3)
#endif

#if (VARYINGS_FLOAT_COUNT >= 9)
    GET_FLOAT(2, 0)
#endif
#if (VARYINGS_FLOAT_COUNT >= 10)
    GET_FLOAT(2, 1)
#endif
#if (VARYINGS_FLOAT_COUNT >= 11)
    GET_FLOAT(2, 2)
#endif
#if (VARYINGS_FLOAT_COUNT >= 12)
    GET_FLOAT(2, 3)
#endif

#if (VARYINGS_FLOAT_COUNT >= 13)
    GET_FLOAT(3, 0)
#endif
#if (VARYINGS_FLOAT_COUNT >= 14)
    GET_FLOAT(3, 1)
#endif
#if (VARYINGS_FLOAT_COUNT >= 15)
    GET_FLOAT(3, 2)
#endif
#if (VARYINGS_FLOAT_COUNT >= 16)
    GET_FLOAT(3, 3)
#endif

#if (VARYINGS_FLOAT_COUNT >= 17)
    GET_FLOAT(4, 0)
#endif
#if (VARYINGS_FLOAT_COUNT >= 18)
    GET_FLOAT(4, 1)
#endif
#if (VARYINGS_FLOAT_COUNT >= 19)
    GET_FLOAT(4, 2)
#endif
#if (VARYINGS_FLOAT_COUNT >= 20)
    GET_FLOAT(4, 3)
#endif

#if (VARYINGS_FLOAT_COUNT >= 21)
    GET_FLOAT(5, 0)
#endif
#if (VARYINGS_FLOAT_COUNT >= 22)
    GET_FLOAT(5, 1)
#endif
#if (VARYINGS_FLOAT_COUNT >= 23)
    GET_FLOAT(5, 2)
#endif
#if (VARYINGS_FLOAT_COUNT >= 24)
    GET_FLOAT(5, 3)
#endif

#if (VARYINGS_FLOAT_COUNT >= 25)
    GET_FLOAT(6, 0)
#endif
#if (VARYINGS_FLOAT_COUNT >= 26)
    GET_FLOAT(6, 1)
#endif
#if (VARYINGS_FLOAT_COUNT >= 27)
    GET_FLOAT(6, 2)
#endif
#if (VARYINGS_FLOAT_COUNT >= 28)
    GET_FLOAT(6, 3)
#endif

#if (VARYINGS_FLOAT_COUNT >= 29)
    GET_FLOAT(7, 0)
#endif
#if (VARYINGS_FLOAT_COUNT >= 30)
    GET_FLOAT(7, 1)
#endif
#if (VARYINGS_FLOAT_COUNT >= 31)
    GET_FLOAT(7, 2)
#endif
#if (VARYINGS_FLOAT_COUNT >= 32)
    GET_FLOAT(7, 3)
#endif

    pointer--;
}

void GetVaryingsFloat2(PackedVaryings packedVaryings, inout float2 value, inout int pointer)
{
    GetVaryingsFloat(packedVaryings, value.y, pointer);
    GetVaryingsFloat(packedVaryings, value.x, pointer);
}

void GetVaryingsFloat3(PackedVaryings packedVaryings, inout float3 value, inout int pointer)
{
    GetVaryingsFloat(packedVaryings, value.z, pointer);
    GetVaryingsFloat(packedVaryings, value.y, pointer);
    GetVaryingsFloat(packedVaryings, value.x, pointer);
}

void GetVaryingsFloat4(PackedVaryings packedVaryings, inout float4 value, inout int pointer)
{
    GetVaryingsFloat(packedVaryings, value.w, pointer);
    GetVaryingsFloat(packedVaryings, value.z, pointer);
    GetVaryingsFloat(packedVaryings, value.y, pointer);
    GetVaryingsFloat(packedVaryings, value.x, pointer);
}

PackedVaryings BuildPackedVaryings(Varyings varyings)
{
    PackedVaryings packedVaryings;
    ZERO_INITIALIZE(PackedVaryings, packedVaryings);
    int pointer = 0;

    packedVaryings.positionCS = varyings.positionCS;

    SET_VARYINGS_NORMAL_WS(packedVaryings, varyings.normalWS);
    SET_VARYINGS_TANGENT_WS(packedVaryings, varyings.tangentWS);
    SET_VARYINGS_UV0(packedVaryings, varyings.uv0);

    SetVaryingsFloat(packedVaryings, varyings.normalWS.x, pointer);
    SetVaryingsFloat(packedVaryings, varyings.normalWS.y, pointer);
    SetVaryingsFloat(packedVaryings, varyings.normalWS.z, pointer);
    // SetVaryingsFloat(packedVaryings, varyings.tangentWS.x, pointer);
    // SetVaryingsFloat(packedVaryings, varyings.tangentWS.y, pointer);
    // SetVaryingsFloat(packedVaryings, varyings.tangentWS.z, pointer);
    // SetVaryingsFloat(packedVaryings, varyings.tangentWS.w, pointer);
    SetVaryingsFloat(packedVaryings, varyings.uv0.x, pointer);
    SetVaryingsFloat(packedVaryings, varyings.uv0.y, pointer);

    return packedVaryings;
}

Varyings BuildUnpackedVaryings(PackedVaryings packedVaryings)
{
    Varyings varyings;
    ZERO_INITIALIZE(Varyings, varyings);
    int pointer = VARYINGS_FLOAT_COUNT;

    GET_VARYINGS_UV0(packedVaryings, varyings.uv0);
    GET_VARYINGS_TANGENT_WS(packedVaryings, varyings.tangentWS);
    GET_VARYINGS_NORMAL_WS(packedVaryings, varyings.normalWS);

    return varyings;
}

#endif