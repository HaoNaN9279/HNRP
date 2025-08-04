#ifndef HNRP_SURFACE_DATA_INCLUDED
#define HNRP_SURFACE_DATA_INCLUDED

struct SurfaceData
{
    half3 albedo;
    half3 specular;
    half  metallic;
    half  smoothness;
    half3 normalTS;
    half3 emission;
    half  occlusion;
    half  alpha;
};

#endif