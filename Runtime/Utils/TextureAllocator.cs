using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HN.HNRP
{
    /// <summary>
    /// 在一张方形texture（一般为4096）上动态紧凑分配若干2次幂的方形区域，平衡texture的利用率与各个方形区域的更新频率。
    /// Block：任意2次幂尺寸的方形区域
    /// Brick：最小2次幂尺寸的方形区域
    /// Level：Block的分辨率层级，最低分辨率（即Brick的分辨率）为0级
    /// Entity：Block中真正绘制的实体
    /// 最大支持4096分辨率贴图，brick最小支持512，即brick数量最大为64
    /// </summary>
    public class TextureAllocator
    {
        private int textureResolution;

        private int minBlockSize;

        private int maxBlockSize;

        private int maxBrickCount;

        private int sliceCount;

        private int sliceBrickCount;

        /// <summary>
        /// 64位mask，将brick按Z型排列（Morton 码），每位存储每个brick是否被占用。
        /// </summary>
        private ulong[] brickMask;

        /// <summary>
        /// key: entity的id
        /// value: 当前block的信息，按位存储。
        /// 0 - 1位（2位）表示当前block的写入状态：00：未写入 01：已写入；
        /// 2 - 7位（6位）表示当前block的编号（即brickMask的位号）；
        /// 8 - 11位（4位）表示当前block的level
        /// </summary>
        private Dictionary<uint, uint> blocks = new Dictionary<uint, uint>();

        /// <summary>
        /// key: entity 的 id
        /// value: 该 entity 当前占据的 block 分配结果。分配时写入，<see cref="Release"/> 时移除。
        /// 供跨帧复用（避免重复 <see cref="Allocate(ref Dictionary{uint, TextureAllocatorResult}, uint, int)"/>）
        /// 以及释放时反查所在 slice / block 使用。
        /// </summary>
        private Dictionary<uint, TextureAllocatorResult> allocatedResults = new Dictionary<uint, TextureAllocatorResult>();


        public TextureAllocator(int textureResolution, int minBlockSize, int maxBlockSize, int sliceCount)
        {
            if(textureResolution <= 0 || minBlockSize <= 0 || maxBlockSize <= 0)
            {
                Debug.LogError("TextureAllocator: textureResolution, minBlockSize and maxBlockSize must be greater than 0");
                return;
            }

            if(minBlockSize > maxBlockSize)
            {
                Debug.LogError("TextureAllocator: minBlockSize must be less than or equal to maxBlockSize");
                return;
            }

            if(textureResolution < maxBlockSize)
            {
                Debug.LogError("TextureAllocator: textureResolution must be greater than or equal to maxBlockSize");
                return;
            }

            if(!Mathf.IsPowerOfTwo(textureResolution) || !Mathf.IsPowerOfTwo(minBlockSize) || !Mathf.IsPowerOfTwo(maxBlockSize))
            {
                Debug.LogError("TextureAllocator: textureResolution, minBlockSize and maxBlockSize must be power of two");
                return;
            }

            if(textureResolution / minBlockSize > 8)
            {
                Debug.LogError("TextureAllocator: textureResolution / minBlockSize must be less than or equal to 8");
                return;
            }

            if(sliceCount <= 0)
            {
                Debug.LogError("TextureAllocator: sliceCount must be grater than zero.");
                return;
            }

            this.textureResolution = textureResolution;
            this.minBlockSize = minBlockSize;
            this.maxBlockSize = maxBlockSize;
            this.sliceCount = sliceCount;
            sliceBrickCount = (textureResolution / minBlockSize) * (textureResolution / minBlockSize);
            maxBrickCount = sliceBrickCount * sliceCount;
            brickMask = new ulong[sliceCount];
        }


        public void Allocate(ref Dictionary<uint, TextureAllocatorResult> results, uint entityId, int size)
        {
            Allocate(ref results, entityId, size, 0, false, Vector4.zero, 0);
        }


        /// <summary>
        /// 判断指定 entity 当前是否占据一个 block。
        /// </summary>
        /// <param name="entityId">要查询的 entity id。</param>
        /// <returns>占据时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
        public bool Contains(uint entityId)
        {
            return blocks.ContainsKey(entityId);
        }


        /// <summary>
        /// 取回指定 entity 当前占据 block 的分配结果（供跨帧复用，避免重复分配）。
        /// </summary>
        /// <param name="entityId">要查询的 entity id。</param>
        /// <param name="result">该 entity 的分配结果；未占据时为默认值。</param>
        /// <returns>存在时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
        public bool TryGetResult(uint entityId, out TextureAllocatorResult result)
        {
            return allocatedResults.TryGetValue(entityId, out result);
        }


        /// <summary>
        /// 释放指定 entity 占据的 block：清除对应 brick 占用位并移除记录。
        /// 仅更新分配状态，不触碰 texture 内容（旧内容由后续绘制覆盖）。
        /// </summary>
        /// <param name="entityId">要释放的 entity id。</param>
        /// <returns>该 entity 存在并被释放时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
        public bool Release(uint entityId)
        {
            if(!blocks.ContainsKey(entityId))
            {
                return false;
            }

            ClearBlockMask(entityId);
            blocks.Remove(entityId);
            allocatedResults.Remove(entityId);
            return true;
        }


        /// <summary>
        /// 归还指定 entity 当前 block 在 <see cref="brickMask"/> 中的占用位。
        /// 只清占用位，不改动 <see cref="blocks"/> / <see cref="allocatedResults"/> 记录，
        /// 供 <see cref="Release"/> 与分配重排（reorg）共用。
        /// </summary>
        /// <param name="entityId">要归还占用位的 entity id。</param>
        private void ClearBlockMask(uint entityId)
        {
            if(!blocks.TryGetValue(entityId, out uint value))
            {
                return;
            }

            int level = (int)((value >> 8) & 0xFu);
            int blockBitCount = GetBlockBitCountByLevel(level);
            if(allocatedResults.TryGetValue(entityId, out TextureAllocatorResult result))
            {
                int brickIndex = result.SliceIndex * sliceBrickCount + (int)result.BlockId;
                SetBrickMaskByBlock(brickIndex / blockBitCount, blockBitCount, false);
            }
        }


        /// <summary>
        /// 取指定 entity 当前 block 起始 brick 在整图（跨 slice）中的全局索引。
        /// </summary>
        /// <param name="entityId">要查询的 entity id。</param>
        /// <returns>全局 brick 索引；该 entity 未分配时返回 <c>-1</c>。</returns>
        private int GetGlobalBrickIndex(uint entityId)
        {
            if(allocatedResults.TryGetValue(entityId, out TextureAllocatorResult result))
            {
                return result.SliceIndex * sliceBrickCount + (int)result.BlockId;
            }

            return -1;
        }


        private void Allocate(ref Dictionary<uint, TextureAllocatorResult> results, uint entityId, int size, int startBrickIndex, bool isReorg, Vector4 oldScaleOffset, int oldSliceIndex)
        {
            if(size < minBlockSize || size > maxBlockSize)
            {
                Debug.LogError($"Allocate failed, size {size} is out of range [{minBlockSize}, {maxBlockSize}]");
                return;
            }

            // 同一 entity 已在位时先整块归还（清占用位并移除记录），
            // 避免重分配旧占用位残留在 mask 中。
            Release(entityId);

            int level = GetLevelByBlockSize(size);
            int blockBitCount = GetBlockBitCountByLevel(level);
            int steps = maxBrickCount / blockBitCount;
            for(int i = startBrickIndex / blockBitCount; i < steps; i++)
            {
                ulong selectedBlockBit = GetBrickMasksByBlock(i, blockBitCount);
                if(IsBlockEmpty(selectedBlockBit))
                {
                    blocks[entityId] = BuildBlockValue(true, i * blockBitCount, level);
                    TextureAllocatorResult allocResult = new TextureAllocatorResult(){
                        ScaleOffset = GetScaleOffset(entityId), 
                        SliceIndex = GetSliceIndexByBrickIndex(i * blockBitCount),
                        BlockId = (uint)((i * blockBitCount) % sliceBrickCount),
                        IsReorg = isReorg,
                        OldScaleOffset = oldScaleOffset,
                        OldSliceIndex = oldSliceIndex
                    };
                    results[entityId] = allocResult;
                    allocatedResults[entityId] = allocResult;
                    SetBrickMaskByBlock(i, blockBitCount, true);
                    break;
                }

                if(IsBlockNeedReorg(blockBitCount, selectedBlockBit))
                {
                    int candidateStart = i * blockBitCount;
                    int candidateEnd = candidateStart + blockBitCount;
                    List<(uint, int)> reorgEntities = GetReorgEntitiesByBlock(candidateStart);

                    // 记录各待搬移 entity 搬移前的位置（写入结果的 OldScaleOffset，
                    // 供 DrawShadowPass 判定是否需要重绘），并先腾空落在候选 block 内的 entity：
                    // 它们与新 block 区域重叠，若不先清位，登记新 entity 后归还旧位会误清新 block 的位。
                    Vector4[] oldScaleOffsets = new Vector4[reorgEntities.Count];
                    for(int j = 0; j < reorgEntities.Count; j++)
                    {
                        uint movedId = reorgEntities[j].Item1;
                        oldScaleOffsets[j] = GetScaleOffset(movedId);

                        int movedBrick = GetGlobalBrickIndex(movedId);
                        if(movedBrick >= candidateStart && movedBrick < candidateEnd)
                        {
                            Release(movedId);
                        }
                    }

                    blocks[entityId] = BuildBlockValue(true, candidateStart, level);
                    int sliceIndex = GetSliceIndexByBrickIndex(candidateStart);
                    TextureAllocatorResult allocResult = new TextureAllocatorResult(){
                        ScaleOffset = GetScaleOffset(entityId),
                        SliceIndex = sliceIndex,
                        BlockId = (uint)(candidateStart % sliceBrickCount),
                        IsReorg = isReorg,
                        OldScaleOffset = oldScaleOffset,
                        OldSliceIndex = oldSliceIndex
                    };
                    results[entityId] = allocResult;
                    allocatedResults[entityId] = allocResult;
                    SetBrickMaskByBlock(i, blockBitCount, true);

                    // 逐个重分配：仍在位的先 Release 归还其当前占用位（内层 reorg 可能已移动过
                    // 同一 entity，故用 Release 而非 blocks.Remove，避免残留占用位）。
                    for(int j = 0; j < reorgEntities.Count; j++)
                    {
                        uint movedId = reorgEntities[j].Item1;
                        if(blocks.ContainsKey(movedId))
                        {
                            Release(movedId);
                        }

                        Allocate(ref results, movedId, reorgEntities[j].Item2, candidateEnd, true, oldScaleOffsets[j], sliceIndex);
                    }
                    break;
                }
            }
        }


        private bool GetBrickMaskByBitIndex(int index, out bool result)
        {
            if(index >= 0 && index < maxBrickCount)
            {
                int sliceIndex = index / sliceBrickCount;
                int sliceBrickIndex = index % sliceBrickCount;
                result = (brickMask[sliceIndex] & (1U << sliceBrickIndex)) != 0;
                return true;
            }
            result = false;
            return false;
        }

        private int GetSliceIndexByBrickIndex(int brickIndex)
        {
            return brickIndex / sliceBrickCount;
        }

        private ulong GetBrickMasksByBlock(int blockIndex, int blockBitCount)
        {
            int brickIndex = blockIndex * blockBitCount;
            int sliceIndex = GetSliceIndexByBrickIndex(brickIndex);
            int sliceBrickIndex = brickIndex % sliceBrickCount;
            ulong selector = ((1UL << blockBitCount) - 1) << sliceBrickIndex;
            return (brickMask[sliceIndex] & selector) >> blockIndex * blockBitCount;
        }

        private bool SetBrickMaskByBitIndex(int brickIndex, bool value)
        {
            if(brickIndex >= 0 && brickIndex < maxBrickCount)
            {
                int sliceIndex = GetSliceIndexByBrickIndex(brickIndex);
                int sliceBrickIndex = brickIndex % sliceBrickCount;
                ulong temp = value ? brickMask[sliceIndex] : ~brickMask[sliceIndex];
                temp &= 1UL << sliceBrickIndex;
                brickMask[sliceIndex] = value ? temp : ~temp;
                return true;
            }
            return false;
        }

        private void SetBrickMaskByBlock(int blockIndex, int blockBitCount, bool value)
        {
            int brickIndex = blockIndex * blockBitCount;
            int sliceIndex = GetSliceIndexByBrickIndex(brickIndex);
            int sliceBrickIndex = brickIndex % sliceBrickCount;
            ulong temp = value ? brickMask[sliceIndex] : ~brickMask[sliceIndex];
            temp |= ((1UL << blockBitCount) - 1) << sliceBrickIndex;
            brickMask[sliceIndex] = value ? temp : ~temp;
        }

        private bool GetBlockState(uint entityId, out bool state)
        {
            if(blocks.ContainsKey(entityId))
            {
                uint value = blocks[entityId];
                state = (value & 1U) != 0;
                return true;
            }
            state = false;
            return false;
        }

        private void GetBlockId(uint blockValue, out uint blockId)
        {
            blockId = (blockValue >> 2) & 63U;
        }

        private int GetBlockLevel(uint blockValue)
        {
            return (int)(blockValue >> 8);
        }

        private bool SetBlockState(uint entityId, bool state)
        {
            if(blocks.ContainsKey(entityId))
            {
                uint value = state ? blocks[entityId] : ~blocks[entityId];
                value &= 1U;
                blocks[entityId] = state ? value : ~value;
                return true;
            }
            state = false;
            return false;
        }


        private int GetLevelByBlockSize(int blockSize)
        {
            return (int)Mathf.Log(blockSize / minBlockSize, 2);
        }

        private int GetBlockSizeByLevel(int level)
        {
            return (int)Mathf.Pow(2, level) * minBlockSize;
        }

        private int GetBlockBitCountByLevel(int level)
        {
            return (int)Mathf.Pow(4, level);
        }

        private uint BuildBlockValue(bool state, int brickId, int level)
        {
            return ((uint)level << 8) | ((uint)(brickId % sliceBrickCount) << 2) | (state ? 1U : 0U);
        }

        private List<(uint, int)> GetReorgEntitiesByBlock(int blockId)
        {
            var entities = new List<(uint, int)>();
            foreach(KeyValuePair<uint, uint> pair in blocks)
            {
                GetBlockId(pair.Value, out uint tempBlockId);
                if(tempBlockId >= (uint)blockId)
                {
                    int tempBlockLevel = GetBlockLevel(pair.Value);
                    entities.Add((pair.Key, GetBlockSizeByLevel(tempBlockLevel)));
                }
            }
            return entities;
        }

        private Vector4 GetScaleOffset(uint entityId)
        {
            Vector4 scaleOffset = new Vector4(1, 1, 0, 0);
            if(!blocks.TryGetValue(entityId, out uint value))
            {
                return scaleOffset;
            }

            int level = (int)((value >> 8) & 0xFu);
            float scale = (float)Mathf.Pow(2, level) * minBlockSize / textureResolution;
            scaleOffset.x = scale;
            scaleOffset.y = scale;

            uint blockId = (value >> 2) & 63U;
            uint xId = 0;
            uint yId = 0;
            for(int i = 0; i < 3; i++)
            {
                xId |= (((blockId >> i * 2) >> 0) & 1U) << i;
                yId |= (((blockId >> i * 2) >> 1) & 1U) << i;
            }
            scaleOffset.z = (float)xId * minBlockSize / textureResolution;
            scaleOffset.w = (float)yId * minBlockSize / textureResolution;
            return scaleOffset;
        }

        private bool IsBlockEmpty(ulong selectedBlockBit)
        {
            return selectedBlockBit == 0;
        }

        private bool IsBlockNeedReorg(int blockBitSize, ulong selectedBlockBit)
        {
            return IsBlockOccupiedLessThanHalf(blockBitSize, selectedBlockBit);
        }

        private bool IsBlockOccupiedLessThanHalf(int blockBitSize, ulong selectedBlockBit)
        {
            int count = 0;
            for(int i = 0; i < blockBitSize; i++)
            {
                if((selectedBlockBit & (1UL << i)) != 0)
                {
                    count++;
                }
            }
            return count <= blockBitSize / 2;
        }
    }


    public struct TextureAllocatorResult
    {
        public Vector4 ScaleOffset;

        public int SliceIndex;

        /// <summary>
        /// 该 block 在所在 slice 内的起始 brick 索引（Morton 码排列），取值 0..63。
        /// 与 <see cref="ScaleOffset"/> 的 x/y 偏移一一对应。
        /// </summary>
        public uint BlockId;

        public bool IsReorg;

        public Vector4 OldScaleOffset;

        public int OldSliceIndex;
    }
}
