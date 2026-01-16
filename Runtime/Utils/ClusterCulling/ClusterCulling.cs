using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace HN.HNRP
{
    public abstract class ClusterCulling
    {
        public virtual void Initialize()
        {
            zBins = new NativeArray<uint>(maxZBinWords, Allocator.Persistent);
            tileMasks = new NativeArray<uint>(maxTileWords, Allocator.Persistent); 
        }

        public virtual void Cleanup()
        {
            zBins.Dispose();
            tileMasks.Dispose();
        }

        protected void PrepareData(Camera camera, int itemsPerTile, int itemsGroupCount)
        {
            wordsPerTile = (itemsPerTile + 31) / 32;
            
            screenResolution = math.int2(camera.pixelWidth, camera.pixelHeight);

            cameraData = camera.GetHNRPAdditionalCameraData();
            if(cameraData == null)
            {
                Debug.LogError($"Can not get HNRPAdditionalCameraData from camera {camera}.");
                return;
            }
            worldToView = cameraData.viewConstants.viewMatrix;
            viewToClip = cameraData.viewConstants.projMatrix;
            GetViewParams(camera, viewToClip);

            actualTileWidth = 8 >> 1;
            do
            {
                actualTileWidth <<= 1;
                tileResolution = (screenResolution + actualTileWidth - 1) / actualTileWidth;
            }
            while(tileResolution.x * tileResolution.y * wordsPerTile > maxTileWords);
            rangesPerItem = AlignByteCount((1 + tileResolution.y) * UnsafeUtility.SizeOf<InclusiveRange>(), 128) / UnsafeUtility.SizeOf<InclusiveRange>();
            minMaxZs = new NativeArray<float2>(itemsPerTile, Allocator.TempJob);
            tileRanges = new NativeArray<InclusiveRange>(rangesPerItem * itemsPerTile, Allocator.TempJob);

            if(!camera.orthographic)
            {
                zBinScale = maxZBinWords / ((math.log2(camera.farClipPlane) - math.log2(camera.nearClipPlane)) * (itemsGroupCount + wordsPerTile));
                zBinOffset = -math.log2(camera.nearClipPlane) * zBinScale;
                binCount = (int)(math.log2(camera.farClipPlane) * zBinScale + zBinOffset);                
            }
            else
            {
                zBinScale = maxZBinWords / ((camera.farClipPlane - camera.nearClipPlane) * (itemsGroupCount + wordsPerTile));
                zBinOffset = -camera.nearClipPlane * zBinScale;
                binCount = (int)(camera.farClipPlane * zBinScale + zBinOffset);
            }
            zBinningBatchCount = (binCount + zBinningBatchSize - 1) / zBinningBatchSize;
        }

        private int AlignByteCount(int count, int align) => align * ((count + align - 1) / align);

        private void GetViewParams(Camera camera, float4x4 viewToClip)
        {
            var viewPlaneHalfSizeInv = math.float2(viewToClip[0][0], viewToClip[1][1]);
            var viewPlaneHalfSize = math.rcp(viewPlaneHalfSizeInv);
            var centerClipSpace = camera.orthographic ? -math.float2(viewToClip[3][0], viewToClip[3][1]) : math.float2(viewToClip[2][0], viewToClip[2][1]);
            viewPlaneBot = centerClipSpace.y * viewPlaneHalfSize.y - viewPlaneHalfSize.y;
            viewPlaneTop = centerClipSpace.y * viewPlaneHalfSize.y + viewPlaneHalfSize.y;
            viewToViewportScaleBias = math.float4(viewPlaneHalfSizeInv * 0.5f, -centerClipSpace * 0.5f + 0.5f);
        }


        public NativeArray<uint> zBins;
        public NativeArray<uint> tileMasks;

        protected int2 screenResolution = default;
        protected HNAdditionalCameraData cameraData;
        protected Matrix4x4 worldToView;
        protected Matrix4x4 viewToClip;
        protected float viewPlaneBot;
        protected float viewPlaneTop;
        protected float4 viewToViewportScaleBias;
        protected int wordsPerTile;
        protected int actualTileWidth;
        protected int2 tileResolution;
        protected float zBinScale;
        protected float zBinOffset;
        protected int binCount;
        protected int zBinningBatchCount;
        protected int rangesPerItem;
        protected NativeArray<float2> minMaxZs;
        protected NativeArray<InclusiveRange> tileRanges;

        public static int maxZBinWords = 1024 * 4;
        public static int maxTileWords = 4096 * 4;
        public static int zBinningBatchSize = 128;
    }
}
