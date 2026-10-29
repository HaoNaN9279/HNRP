#ifndef HNRP_UNLIT_INPUT_INCLUDED
#define HNRP_UNLIT_INPUT_INCLUDED

// 包系统：锚点默认宏 + 关键字统一 undef 入口。
// 组合头（Generated）若已定义包级属性，会先于本文件生效；此处仅兜底。
#include "../Core/PackageAnchors.hlsl"
#include "../Core/KeywordsList.hlsl"

#define ATTRIBUTES_NEED_NORMAL
#define USE_POSITION_WS_VARYING
#define USE_NORMAL_WS_VARYING

#if defined(_BASEMAP)
    #define ATTRIBUTES_NEED_UV0
    #define USE_UV0_VARYING
#endif

#include "../Common/Common.hlsl"
#include "../Core/UnityInput.hlsl"

#if defined(_BASEMAP)
TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
#endif

// 包纹理 / 采样器声明段（组合头注入；CBUFFER 之外）。
HNRP_PKG_TEXTURE_DECLARATIONS

CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
float4 _BaseMap_TexelSize;
float4 _BaseMap_MipInfo;
float4 _BaseColor;
float _Cutoff;
#if defined(_BASEMAP)
float _AlphaRemapMin;
float _AlphaRemapMax;
#endif

// 包参数声明段（组合头注入）。
// 约束：无条件编译，不随 keyword 变化，保证同组合 SRP Batcher 布局一致（方案 D2）。
HNRP_PKG_MATERIAL_DECLARATIONS
CBUFFER_END

#include "../Common/CommonFunctions.hlsl"

#include "../Core/Attributes.hlsl"
#include "../Core/VertexInput.hlsl"
#include "../Core/SurfaceData.hlsl"

#endif
