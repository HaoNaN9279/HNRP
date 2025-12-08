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
    [BurstCompile]
    struct ReflectionProbeMinMaxZJob : IJobFor
    {
        public float4x4 worldToView;

        [ReadOnly]
        public NativeArray<VisibleReflectionProbe> reflectionProbes;
        public NativeArray<float2> minMaxZs;

        public void Execute(int index)
        {
            var minMax = math.float2(float.MaxValue, float.MinValue);
            var reflectionProbeIndex = index % reflectionProbes.Length;
            var reflectionProbe = reflectionProbes[reflectionProbeIndex];
            var centerWS = (float3)reflectionProbe.bounds.center;
            var extentsWS = (float3)reflectionProbe.bounds.extents;
            for (var i = 0; i < 8; i++)
            {
                // Convert index to x, y, and z in [-1, 1]
                var x = ((i << 1) & 2) - 1;
                var y = (i & 2) - 1;
                var z = ((i >> 1) & 2) - 1;
                var cornerVS = math.mul(worldToView, math.float4(centerWS + extentsWS * math.float3(x, y, z), 1));
                cornerVS.z *= -1;
                minMax.x = math.min(minMax.x, cornerVS.z);
                minMax.y = math.max(minMax.y, cornerVS.z);
            }

            minMaxZs[index] = minMax;
        }
    }
}
