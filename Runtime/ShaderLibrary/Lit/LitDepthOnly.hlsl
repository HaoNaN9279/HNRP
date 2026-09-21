#ifndef HNRP_LIT_DEPTH_ONLY_INCLUDED
#define HNRP_LIT_DEPTH_ONLY_INCLUDED

#include "../Common/Common.hlsl"
#include "LitInput.hlsl"

// DepthOnly pass 的最小实现：只把几何体变换到裁剪空间，深度由片元的 ZWrite 写入
// 深度目标；片元不输出颜色（着色器 Pass 使用 ColorMask 0），仅保留与 Forward 一致的
// alpha clip，保证裁切体（树叶 / 草等）的深度形状正确。

struct DepthOnlyAttributes
{
    float4 positionOS : POSITION;
#if defined(ATTRIBUTES_NEED_UV0)
    float2 uv0 : TEXCOORD0;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct DepthOnlyVaryings
{
    float4 positionCS : SV_POSITION;
#if defined(ATTRIBUTES_NEED_UV0)
    float2 uv0 : TEXCOORD0;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

DepthOnlyVaryings DepthOnlyVert(DepthOnlyAttributes input)
{
    DepthOnlyVaryings output;
    ZERO_INITIALIZE(DepthOnlyVaryings, output);

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
#if defined(ATTRIBUTES_NEED_UV0)
    output.uv0 = input.uv0;
#endif
    return output;
}

half4 DepthOnlyFrag(DepthOnlyVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);

#if defined(_ALPHATEST_ON)
#if defined(_BASEMAP)
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv0);
    float alpha = RemapFrom01(baseMap.a, _AlphaRemapMin, _AlphaRemapMax) * _BaseColor.a;
#else
    float alpha = _BaseColor.a;
#endif
    clip(alpha - _Cutoff);
#endif

    // ColorMask 0：返回值被丢弃，深度由片元写入深度目标。
    return half4(0.0, 0.0, 0.0, 0.0);
}

#endif
