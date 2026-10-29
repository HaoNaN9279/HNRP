#ifndef HNRP_UNLIT_SURFACE_DATA_INCLUDED
#define HNRP_UNLIT_SURFACE_DATA_INCLUDED

struct UnlitSurfaceData
{
    float3 color;
    float  alpha;
};

struct UnlitOutputData
{
    float3 color;
    float  alpha;
};

// 取样贴图并与颜色参数相乘；无贴图时直接用颜色参数。
// alpha 复用与 Lit 相同的 remap 约定（_AlphaRemapMin / _AlphaRemapMax）。
float4 GetUnlitColorAlpha(float2 uv)
{
#if defined(_BASEMAP)
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
    float3 color = baseMap.rgb * _BaseColor.rgb;
    float alpha = RemapFrom01(baseMap.a, _AlphaRemapMin, _AlphaRemapMax) * _BaseColor.a;
#if defined(_ALPHATEST_ON)
    clip(alpha - _Cutoff);
#endif
    return float4(color, alpha);
#else
    return _BaseColor;
#endif
}

void InitializeUnlitSurfaceData(float2 uv, out UnlitSurfaceData unlitSurfaceData)
{
    ZERO_INITIALIZE(UnlitSurfaceData, unlitSurfaceData);

    float4 colorAlpha = GetUnlitColorAlpha(uv);
    unlitSurfaceData.color = colorAlpha.rgb;
    unlitSurfaceData.alpha = colorAlpha.a;
}

void BuildUnlitSurfaceData(UnlitVaryings unlitVaryings, out UnlitSurfaceData unlitSurfaceData)
{
    InitializeUnlitSurfaceData(unlitVaryings.uv0, unlitSurfaceData);
}

void BuildUnlitOutputData(UnlitSurfaceData unlitSurfaceData, out UnlitOutputData unlitOutputData)
{
    ZERO_INITIALIZE(UnlitOutputData, unlitOutputData);

    float3 color = unlitSurfaceData.color;
#if defined(_ALPHAPREMULTIPLY_ON)
    color *= unlitSurfaceData.alpha;
#endif

    unlitOutputData.color = color;
    unlitOutputData.alpha = unlitSurfaceData.alpha;
}

#endif
