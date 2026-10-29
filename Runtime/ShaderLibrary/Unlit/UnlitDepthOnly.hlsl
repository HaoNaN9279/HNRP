#ifndef HNRP_UNLIT_DEPTH_ONLY_INCLUDED
#define HNRP_UNLIT_DEPTH_ONLY_INCLUDED

#include "../Common/Common.hlsl"
#include "UnlitInput.hlsl"

// DepthOnly pass 的最小实现：只把几何体变换到裁剪空间，深度由片元的 ZWrite 写入
// 深度目标；片元不输出颜色（着色器 Pass 使用 ColorMask 0），仅保留与 Forward 一致的
// alpha clip，保证裁切体（树叶 / 草等）的深度形状正确。

struct UnlitDepthOnlyAttributes
{
    float4 positionOS : POSITION;
#if defined(ATTRIBUTES_NEED_UV0)
    float2 uv0 : TEXCOORD0;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct UnlitDepthOnlyVaryings
{
    float4 positionCS : SV_POSITION;
#if defined(ATTRIBUTES_NEED_UV0)
    float2 uv0 : TEXCOORD0;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

UnlitDepthOnlyVaryings DepthOnlyVert(UnlitDepthOnlyAttributes input)
{
    HNRP_PKG_EMIT_UNLIT_DEPTHONLY_VERTEX_BEGIN();

    UnlitDepthOnlyVaryings output;
    ZERO_INITIALIZE(UnlitDepthOnlyVaryings, output);

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
#if defined(ATTRIBUTES_NEED_UV0)
    output.uv0 = input.uv0;
#endif
    HNRP_PKG_EMIT_UNLIT_DEPTHONLY_VERTEX_AFTERVERTEX(output);
    HNRP_PKG_EMIT_UNLIT_DEPTHONLY_VERTEX_END();
    return output;
}

half4 DepthOnlyFrag(UnlitDepthOnlyVaryings input) : SV_Target
{
    HNRP_PKG_EMIT_UNLIT_DEPTHONLY_FRAGMENT_BEGIN();

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
    HNRP_PKG_EMIT_UNLIT_DEPTHONLY_FRAGMENT_END();
    return half4(0.0, 0.0, 0.0, 0.0);
}

#endif
