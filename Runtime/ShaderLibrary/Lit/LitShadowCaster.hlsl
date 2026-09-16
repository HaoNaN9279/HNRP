#ifndef HNRP_LIT_SHADOW_CASTER_INCLUDED
#define HNRP_LIT_SHADOW_CASTER_INCLUDED

#include "../Common/Common.hlsl"
#include "../Core/Input.hlsl"
#include "LitInput.hlsl"

// ShadowCaster pass 的最小顶点 / 片元实现：仅把几何体变换到光源裁剪空间。
// 深度由绘制侧直接绑定 atlas 的指定区域写入（不再依赖 DrawShadows 的阴影图绑定），
// 深度偏置由绘制侧 SetGlobalDepthBias 承担。alpha clip 与 Forward pass 一致，
// 保证裁切体（树叶 / 草等）的阴影形状正确。

// 光源视图投影矩阵常量缓冲：由 DrawShadowPass 逐 map 上传。
// 不能使用 UNITY_MATRIX_VP：本工程 shader 的它来自自定义 ShaderVariablesGlobal（b0）
// 常量缓冲，内容始终是当前相机矩阵，不随引擎的 SetViewProjectionMatrices 变化。
GLOBAL_CBUFFER_START(_ShadowViewProjBuffer, b4)
    float4x4 _ShadowViewProj;
CBUFFER_END

struct ShadowCasterAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
#if defined(_BASEMAP)
    float2 uv0 : TEXCOORD0;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct ShadowCasterVaryings
{
    float4 positionCS : SV_POSITION;
#if defined(_BASEMAP)
    float2 uv0 : TEXCOORD0;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

ShadowCasterVaryings ShadowCasterVert(ShadowCasterAttributes input)
{
    ShadowCasterVaryings output;
    ZERO_INITIALIZE(ShadowCasterVaryings, output);

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float4 positionCS = mul(_ShadowViewProj, float4(positionWS, 1.0));

    // 不做近平面临界钳制：光源视图体之外的几何应被硬件裁剪掉。
    // 若把它钳到近平面（深度 0），这些片元会以"最近深度"写入阴影图，导致该区域整片被判为遮挡。

    output.positionCS = positionCS;
#if defined(_BASEMAP)
    output.uv0 = TRANSFORM_TEX(input.uv0, _BaseMap);
#endif
    return output;
}

half4 ShadowCasterFrag(ShadowCasterVaryings input) : SV_Target
{
#if defined(_ALPHATEST_ON)
#if defined(_BASEMAP)
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv0);
    float alpha = RemapFrom01(baseMap.a, _AlphaRemapMin, _AlphaRemapMax) * _BaseColor.a;
#else
    float alpha = _BaseColor.a;
#endif
    clip(alpha - _Cutoff);
#endif
    return 0;
}

#endif
