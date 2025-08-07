#ifndef HNRP_COMMON_FUNCTIONS_INCLUDED
#define HNRP_COMMON_FUNCTIONS_INCLUDED

float Remap(float t, float x0, float y0, float x1, float y1)
{
    return ((t - x0) * (y1 - x1) / (y0 - x0) + x1);
}

float RemapFrom01(float t, float x1, float y1)
{
    return (t * (y1 - x1) + x1);
}

float3 NormalizeNormalPerPixel(float3 normalWS)
{
#if defined(UNITY_NO_DXT5nm) && defined(_NORMALMAP)
    return SafeNormalize(normalWS);
#else
    return normalize(normalWS);
#endif
}


#endif