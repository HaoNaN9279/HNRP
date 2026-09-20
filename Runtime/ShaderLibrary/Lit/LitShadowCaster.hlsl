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
    // 矩图软阴影用：光源世界位置 + 1/range（方向光 w=0）。
    float4   _ShadowLightPosRange;
    // x=1 表示透视（局部光），0 表示正交（方向光）。
    float4   _ShadowDepthFlags;
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
    float3 positionWS : TEXCOORD1;
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
    output.positionWS = positionWS;
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

    // 单通道深度图：只写归一化线性深度（0=近光，1=远光），
    // 与采样端 GetReceiverLinearDepth 严格同式；由 BlendOp Min 保留最近投射者。
    // 注意：变量不能命名为 linear —— 它是 HLSL 保留字（插值修饰符）。
    float linearDepth;
    if (_ShadowDepthFlags.x > 0.5)
    {
        // 透视局部光：到光源的距离 / range。
        linearDepth = length(input.positionWS - _ShadowLightPosRange.xyz) * _ShadowLightPosRange.w;
    }
    else
    {
        // 正交方向光：裁剪 z 已是线性；平台 reversed-Z 时翻转到 near=0 / far=1。
        linearDepth = input.positionCS.z;
#if UNITY_REVERSED_Z
        linearDepth = 1.0 - linearDepth;
#endif
    }

    linearDepth = saturate(linearDepth);
    return half4(linearDepth, 0.0, 0.0, 1.0);
}

#endif
