using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    [BurstCompile(FloatMode = FloatMode.Fast, DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    struct ZBinningJob : IJobFor
    {
        public const int batchSize = 128;
        public const int headerLength = 2;

        [NativeDisableParallelForRestriction]
        public NativeArray<uint> bins;

        [ReadOnly]
        public NativeArray<float2> minMaxZs;

        public float zBinScale;
        public float zBinOffset;
        public int binCount;
        public int wordsPerTile;
        public int lightCount;
        public int reflectionProbeCount;
        public int batchCount;
        public bool isOrthographic;

        public void Execute(int jobIndex)
        {
            var batchIndex = jobIndex % batchCount;

            var binStart = batchSize * batchIndex;
            var binEnd = math.min(binStart + batchSize, binCount) - 1;

            var emptyHeader = ClusterCullingJobCommon.EncodeHeader(ushort.MaxValue, ushort.MinValue);
            for (var binIndex = binStart; binIndex <= binEnd; binIndex++)
            {
                bins[binIndex * (headerLength + wordsPerTile) + 0] = emptyHeader;
                bins[binIndex * (headerLength + wordsPerTile) + 1] = emptyHeader;
            }

            // Fill ZBins for lights.
            ClusterCullingJobCommon.FillZBins(ref bins, ref minMaxZs, isOrthographic, zBinScale, zBinOffset, headerLength, wordsPerTile, binStart, binEnd, 0, lightCount, 0);

            // Fill ZBins for reflection probes.
            ClusterCullingJobCommon.FillZBins(ref bins, ref minMaxZs, isOrthographic, zBinScale, zBinOffset, headerLength, wordsPerTile, binStart, binEnd, lightCount, lightCount + reflectionProbeCount, 1);
        }

    }
}
