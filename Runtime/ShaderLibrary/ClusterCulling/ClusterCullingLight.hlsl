#ifndef HNRP_CLUSTER_CULLING_LIGHT_INCLUDED
#define HNRP_CLUSTER_CULLING_LIGHT_INCLUDED

#if CLUSTER_CULLING_LIGHT

#include "../Core/Input.hlsl"

struct ClusterCullingLightIterator
{
    uint minIndex;
    uint maxIndex;
    uint headerIndex;
    uint currentIndex;
};

ClusterCullingLightIterator ClusterCullingLightInit(float2 normalizedScreenSpaceUV, float3 positionWS)
{
    ClusterCullingLightIterator it = (ClusterCullingLightIterator)0;

    uint2 clusterIndexXY = uint2(normalizedScreenSpaceUV * _CLUSTER_CULLING_LIGHT_XY_SCALE);
    float viewZ = dot(GetViewForwardDir(), positionWS - GetCameraPositionWS());
    uint clusterIndexZ = (uint)((IsPerspectiveProjection() ? log2(viewZ) : viewZ) * _CLUSTER_CULLING_LIGHT_Z_SCALE + _CLUSTER_CULLING_LIGHT_Z_OFFSET);
    it.headerIndex = (clusterIndexXY.x + 1) * (clusterIndexXY.y + 1) * clusterIndexZ - 1;
    uint header = _ClusterCullingLightMaskBuffer[it.headerIndex * _CLUSTER_CULLING_LIGHT_WORDS_PER_CLUSTER];
    it.minIndex = header & 0x0000FFFFu;
    it.maxIndex = (header & 0xFFFF0000u) >> 16;
    it.currentIndex = it.minIndex;

    return it;
}

bool ClusterCullingLightNext(inout ClusterCullingLightIterator it, out uint probeIndex)
{
    if(it.currentIndex >= it.minIndex && it.currentIndex <= it.maxIndex && (_CLUSTER_CULLING_LIGHT_LOCAL_LIGHT_COUNT + _CLUSTER_CULLING_LIGHT_DIRECTIONAL_LIGHT_COUNT) != 0)
    {
        bool valid = 0;
        do
        {
            uint wordIndex = it.currentIndex / 32 + 1;
            uint bitIndex = it.currentIndex % 32;
            uint mask = _ClusterCullingLightMaskBuffer[wordIndex];
            valid = ((mask >> bitIndex) & 1u) > 0;
            if(valid)
            {
                probeIndex = it.currentIndex;
            }
            it.currentIndex++;
        }
        while(!valid && it.currentIndex <= it.maxIndex);

        return probeIndex <= it.maxIndex;
    }
    return false;
}

#endif

#endif