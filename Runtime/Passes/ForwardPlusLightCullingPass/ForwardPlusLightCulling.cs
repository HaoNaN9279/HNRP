using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public class ForwardPlusLightCulling : ClusterCulling
    {
        public override void Cleanup()
        {
            base.Cleanup();
            if(reflectionProbes.IsCreated)
            {
                reflectionProbes.Dispose();
            }
        }

        public void PrepareLightData(ref RenderingData renderingData)
        {
            camera = renderingData.Camera;

            lightCount = renderingData.visibleLights.Length;
            int lightOffset = 0;
            while (lightOffset < lightCount && renderingData.visibleLights[lightOffset].lightType == LightType.Directional)
            {
                lightOffset++;
            }
            lightCount -= lightOffset;
            directionalLightCount = lightCount;
            if (renderingData.mainLightIndex != -1 && directionalLightCount != 0)
            {
                directionalLightCount -= 1;
            }

            visibleLights = renderingData.visibleLights.GetSubArray(lightOffset, lightCount);
            reflectionProbes = new NativeArray<VisibleReflectionProbe>(renderingData.catchedReflectionProbes, Allocator.TempJob);
            reflectionProbeCount = reflectionProbes.Length;
            itemsPerTile = visibleLights.Length + reflectionProbeCount;

            itemsGroupCount = ITEMS_GROUP_COUNT;

            PrepareData(camera, itemsPerTile, itemsGroupCount);

            lightMinMaxZJob = new LightMinMaxZJob
            {
                worldToView = worldToView,
                lights = visibleLights,
                minMaxZs = minMaxZs.GetSubArray(0, lightCount)
            };
            var lightMinMaxZHandle = lightMinMaxZJob.ScheduleParallel(lightCount, 32, cullingHandle);

            reflectionProbeMinMaxZJob = new ReflectionProbeMinMaxZJob
            {
                worldToView = worldToView,
                reflectionProbes = reflectionProbes,
                minMaxZs = minMaxZs.GetSubArray(lightCount, reflectionProbeCount)
            };
            var reflectionProbeMinMaxZHandle = reflectionProbeMinMaxZJob.ScheduleParallel(reflectionProbeCount, 32, lightMinMaxZHandle);

            zBinningJob = new ZBinningJob
            {
                bins = zBins,
                minMaxZs = minMaxZs,
                zBinScale = zBinScale,
                zBinOffset = zBinOffset,
                binCount = binCount,
                wordsPerTile = wordsPerTile,
                lightCount = lightCount,
                reflectionProbeCount = reflectionProbeCount,
                batchCount = zBinningBatchCount,
                isOrthographic = camera.orthographic
            };
            var zBinningHandle = zBinningJob.ScheduleParallel(zBinningBatchCount, 1, reflectionProbeMinMaxZHandle);

            reflectionProbeMinMaxZHandle.Complete();

            tilingJob = new TilingJob
            {
                lights = visibleLights,
                reflectionProbes = reflectionProbes,
                tileRanges = tileRanges,
                itemsPerTile = itemsPerTile,
                rangesPerItem = rangesPerItem,
                worldToView = worldToView,
                tileScale = (float2)screenResolution / actualTileWidth,
                tileScaleInv = actualTileWidth / (float2)screenResolution,
                viewPlaneBottom = viewPlaneBot,
                viewPlaneTop = viewPlaneTop,
                viewToViewportScaleBias = viewToViewportScaleBias,
                tileCount = tileResolution,
                near = camera.nearClipPlane,
                isOrthographic = camera.orthographic
            };
            var tilingHandle = tilingJob.ScheduleParallel(itemsPerTile, 1, reflectionProbeMinMaxZHandle);

            tileRangeExpansionJob = new TileRangeExpansionJob
            {
                tileRanges = tileRanges,
                tileMasks = tileMasks,
                rangesPerItem = rangesPerItem,
                itemsPerTile = itemsPerTile,
                wordsPerTile = wordsPerTile,
                tileResolution = tileResolution
            };
            var tileRangeExpansionHandle = tileRangeExpansionJob.ScheduleParallel(tileResolution.y, 1, tilingHandle);

            cullingHandle = JobHandle.CombineDependencies(minMaxZs.Dispose(zBinningHandle), tileRanges.Dispose(tileRangeExpansionHandle), reflectionProbes.Dispose(tilingHandle));
            JobHandle.ScheduleBatchedJobs();

            params0 = math.float4(zBinScale, zBinOffset, lightCount, directionalLightCount);
            params1 = math.float4(camera.pixelRect.size / actualTileWidth, tileResolution.x, wordsPerTile);
            params2 = math.float4(binCount, tileResolution.x * tileResolution.y, 0, 0);
        }




        public JobHandle cullingHandle = default;
        public float4 params0, params1, params2;

        private Camera camera;
        private int itemsPerTile;
        private int itemsGroupCount;

        private int lightCount;
        private int directionalLightCount;
        private NativeArray<VisibleLight> visibleLights;
        private int reflectionProbeCount;
        private NativeArray<VisibleReflectionProbe> reflectionProbes;

        private LightMinMaxZJob lightMinMaxZJob;
        private ReflectionProbeMinMaxZJob reflectionProbeMinMaxZJob;
        private ZBinningJob zBinningJob;
        private TilingJob tilingJob;
        private TileRangeExpansionJob tileRangeExpansionJob;
        
        private const int ITEMS_GROUP_COUNT = 2;
    }
}
