#ifndef HNRP_CLUSTER_CULLING_REFLECTION_PROBE_INCLUDED
#define HNRP_CLUSTER_CULLING_REFLECTION_PROBE_INCLUDED

#if CLUSTER_CULLING_REFLECTION_PROBE

struct ClusterCullingReflectionProbeIterator
{
    uint minIndex;
    uint maxIndex;
    uint headerIndex;
    uint currentIndex;
};

ClusterCullingReflectionProbeIterator ClusterCullingReflectionProbeInit(float2 normalizedScreenSpaceUV, float3 positionWS)
{
    ClusterCullingReflectionProbeIterator it = (ClusterCullingReflectionProbeIterator)0;

    uint2 clusterIndexXY = uint2(normalizedScreenSpaceUV * _CLUSTER_CULLING_REFLECTION_PROBE_XY_SCALE);
    float viewZ = dot(GetViewForwardDir(), positionWS - GetCameraPositionWS());
    uint clusterIndexZ = (uint)((IsPerspectiveProjection() ? log2(viewZ) : viewZ) * _CLUSTER_CULLING_REFLECTION_PROBE_Z_SCALE + _CLUSTER_CULLING_REFLECTION_PROBE_Z_OFFSET);
    it.headerIndex = (clusterIndexXY.x + 1) * (clusterIndexXY.y + 1) * clusterIndexZ - 1;
    uint header = _ClusterCullingReflectionProbeMaskBuffer[it.headerIndex * _CLUSTER_CULLING_REFLECTION_PROBE_WORDS_PER_CLUSTER];
    it.minIndex = header & 0x0000FFFFu;
    it.maxIndex = (header & 0xFFFF0000u) >> 16;
    it.currentIndex = it.minIndex;

    return it;
}

bool ClusterCullingReflectionProbeNext(inout ClusterCullingReflectionProbeIterator it, out uint probeIndex)
{
    if(it.currentIndex >= it.minIndex && it.currentIndex <= it.maxIndex)
    {
        bool valid = 0;
        do
        {
            uint wordIndex = it.currentIndex / 32 + 1;
            uint bitIndex = it.currentIndex % 32;
            uint mask = _ClusterCullingReflectionProbeMaskBuffer[wordIndex];
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