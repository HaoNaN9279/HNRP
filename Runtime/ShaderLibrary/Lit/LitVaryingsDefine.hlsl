#ifndef HNRP_LIT_VARYINGS_DEFINE_INCLUDED
#define HNRP_LIT_VARYINGS_DEFINE_INCLUDED

#include "../Core/PackageRegistry.hlsl"

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
#if defined(USE_NORMAL_WS_VARYING)
    #define VARYINGS_FLOAT_COUNT_NORMAL_WS 3
    #define SET_VARYINGS_NORMAL_WS(packedVaryings, value) SetVaryingsFloat3(packedVaryings, value, pointer);
    #define GET_VARYINGS_NORMAL_WS(packedVaryings, value) GetVaryingsFloat3(packedVaryings, value, pointer);
#else
    #define VARYINGS_FLOAT_COUNT_NORMAL_WS 0
    #define SET_VARYINGS_NORMAL_WS(packedVaryings, value)
    #define GET_VARYINGS_NORMAL_WS(packedVaryings, value)
#endif

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
#if defined(USE_UV0_VARYING)
    #define VARYINGS_FLOAT_COUNT_UV0 2
    #define SET_VARYINGS_UV0(packedVaryings, value) SetVaryingsFloat2(packedVaryings, value, pointer);
    #define GET_VARYINGS_UV0(packedVaryings, value) GetVaryingsFloat2(packedVaryings, value, pointer);
#else
    #define VARYINGS_FLOAT_COUNT_UV0 0
    #define SET_VARYINGS_UV0(packedVaryings, value)
    #define GET_VARYINGS_UV0(packedVaryings, value)
#endif

// uv1
#if defined(USE_UV1_VARYING)
    #define VARYINGS_FLOAT_COUNT_UV1 2
    #define SET_VARYINGS_UV1(packedVaryings, value) SetVaryingsFloat2(packedVaryings, value, pointer);
    #define GET_VARYINGS_UV1(packedVaryings, value) GetVaryingsFloat2(packedVaryings, value, pointer);
#else
    #define VARYINGS_FLOAT_COUNT_UV1 0
    #define SET_VARYINGS_UV1(packedVaryings, value)
    #define GET_VARYINGS_UV1(packedVaryings, value)
#endif

// uv2
#if defined(USE_UV2_VARYING)
    #define VARYINGS_FLOAT_COUNT_UV2 2
    #define SET_VARYINGS_UV2(packedVaryings, value) SetVaryingsFloat2(packedVaryings, value, pointer);
    #define GET_VARYINGS_UV2(packedVaryings, value) GetVaryingsFloat2(packedVaryings, value, pointer);
#else
    #define VARYINGS_FLOAT_COUNT_UV2 0
    #define SET_VARYINGS_UV2(packedVaryings, value)
    #define GET_VARYINGS_UV2(packedVaryings, value)
#endif

// uv3
#if defined(USE_UV3_VARYING)
    #define VARYINGS_FLOAT_COUNT_UV3 2
    #define SET_VARYINGS_UV3(packedVaryings, value) SetVaryingsFloat2(packedVaryings, value, pointer);
    #define GET_VARYINGS_UV3(packedVaryings, value) GetVaryingsFloat2(packedVaryings, value, pointer);
#else
    #define VARYINGS_FLOAT_COUNT_UV3 0
    #define SET_VARYINGS_UV3(packedVaryings, value)
    #define GET_VARYINGS_UV3(packedVaryings, value)
#endif

// color
#if defined(USE_COLOR_VARYING)
    #define VARYINGS_FLOAT_COUNT_COLOR 4
    #define SET_VARYINGS_COLOR(packedVaryings, value) SetVaryingsFloat4(packedVaryings, value, pointer);
    #define GET_VARYINGS_COLOR(packedVaryings, value) GetVaryingsFloat4(packedVaryings, value, pointer);
#else
    #define VARYINGS_FLOAT_COUNT_COLOR 0
    #define SET_VARYINGS_COLOR(packedVaryings, value)
    #define GET_VARYINGS_COLOR(packedVaryings, value)
#endif

// static lightmap uv
#if defined(USE_STATIC_LIGHTMAP_UV_VARYING)
    #define VARYINGS_FLOAT_COUNT_STATIC_LIGHTMAP_UV 2
    #define SET_VARYINGS_STATIC_LIGHTMAP_UV(packedVaryings, value) SetVaryingsFloat2(packedVaryings, value, pointer);
    #define GET_VARYINGS_STATIC_LIGHTMAP_UV(packedVaryings, value) GetVaryingsFloat2(packedVaryings, value, pointer);
#else
    #define VARYINGS_FLOAT_COUNT_STATIC_LIGHTMAP_UV 0
    #define SET_VARYINGS_STATIC_LIGHTMAP_UV(packedVaryings, value)
    #define GET_VARYINGS_STATIC_LIGHTMAP_UV(packedVaryings, value)
#endif

// vertex SH
#if defined(USE_VERTEX_SH_VARYING)
    #define VARYINGS_FLOAT_COUNT_VERTEX_SH 3
    #define SET_VARYINGS_VERTEX_SH(packedVaryings, value) SetVaryingsFloat3(packedVaryings, value, pointer);
    #define GET_VARYINGS_VERTEX_SH(packedVaryings, value) GetVaryingsFloat3(packedVaryings, value, pointer);
#else
    #define VARYINGS_FLOAT_COUNT_VERTEX_SH 0
    #define SET_VARYINGS_VERTEX_SH(packedVaryings, value)
    #define GET_VARYINGS_VERTEX_SH(packedVaryings, value)
#endif

#define VARYINGS_FLOAT_COUNT ( VARYINGS_FLOAT_COUNT_PACKAGES \
    + VARYINGS_FLOAT_COUNT_POSITION_WS \
    + VARYINGS_FLOAT_COUNT_NORMAL_WS \
    + VARYINGS_FLOAT_COUNT_TANGENT_WS \
    + VARYINGS_FLOAT_COUNT_UV0 \
    + VARYINGS_FLOAT_COUNT_UV1 \
    + VARYINGS_FLOAT_COUNT_UV2 \
    + VARYINGS_FLOAT_COUNT_UV3 \
    + VARYINGS_FLOAT_COUNT_COLOR \
    + VARYINGS_FLOAT_COUNT_STATIC_LIGHTMAP_UV \
    + VARYINGS_FLOAT_COUNT_VERTEX_SH \
    )

#endif