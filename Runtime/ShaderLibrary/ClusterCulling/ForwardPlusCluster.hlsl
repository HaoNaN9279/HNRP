#ifndef HNRP_FORWARD_PLUS_CLUSTER_INCLUDED
#define HNRP_FORWARD_PLUS_CLUSTER_INCLUDED

#if FORWARD_PLUS

struct ClusterIterator
{
    uint tileOffset;
    uint zBinOffset;
    uint tileMask;
    uint entityIndexNextMax;
};

ClusterIterator ClusterInit(float2 normalizedScreenSpaceUV, float3 positionWS, int headerIndex)
{
    ClusterIterator state = (ClusterIterator)0;

    uint2 tileId = uint2(normalizedScreenSpaceUV * FP_TILE_SCALE);
    state.tileOffset = tileId.y * FP_TILE_COUNT_X + tileId.x;
    state.tileOffset *= FP_WORDS_PER_TILE;

    float viewZ = dot(GetViewForwardDir(), positionWS - GetCameraPositionWS());
    uint zBinBaseIndex = (uint)((IsPerspectiveProjection() ? log2(viewZ) : viewZ) * FP_ZBIN_SCALE + FP_ZBIN_OFFSET);

    zBinBaseIndex = zBinBaseIndex * (2 + FP_WORDS_PER_TILE);
    zBinBaseIndex = min(zBinBaseIndex, 4 * MAX_ZBIN_VEC4S - (2 + FP_WORDS_PER_TILE));

    uint zBinHeaderIndex = zBinBaseIndex + headerIndex;
    state.zBinOffset = zBinBaseIndex + 2;

    uint header = Select4(asuint(_ForwardPlusZBinsBuffer[zBinHeaderIndex / 4]), zBinHeaderIndex % 4);
    state.entityIndexNextMax = header;

    return state;
}

bool ClusterNext(inout ClusterIterator it, out uint entityIndex)
{
    uint maxIndex = it.entityIndexNextMax >> 16;
    while(it.tileMask == 0 && (it.entityIndexNextMax & 0xFFFF) <= maxIndex)
    {
        uint wordIndex = ((it.entityIndexNextMax & 0xFFFF) >> 5);
        uint tileIndex = it.tileOffset + wordIndex;
        uint zBinIndex = it.zBinOffset + wordIndex;
        it.tileMask =
            Select4(asuint(_ForwardPlusTileMasksBuffer[tileIndex / 4]), tileIndex % 4) &
            Select4(asuint(_ForwardPlusZBinsBuffer[zBinIndex / 4]), zBinIndex % 4) &
            (0xFFFFFFFFu << (it.entityIndexNextMax & 0x1F)) & (0xFFFFFFFFu >> (31 - min(31, maxIndex - wordIndex * 32)));
        it.entityIndexNextMax = (it.entityIndexNextMax + 32) & ~31;
    }
    bool hasNext = it.tileMask != 0;
    uint bitIndex = firstbitlow(it.tileMask);
    it.tileMask ^= (1 << bitIndex);
    entityIndex = (((it.entityIndexNextMax - 32) & (0xFFFF & ~31))) + bitIndex;
    
    return hasNext;
}

#endif

#endif