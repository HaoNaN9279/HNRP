using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using System.Linq;
using Unity.Mathematics;
using UnityEditor.VersionControl;

namespace HN.HNRP
{
    [Serializable]
    public class ReflectionProbeAtlasPass : PassBase
    {
        public override void Initialize(HNRenderGraphBase hnRenderGraph, string passName)
        {
            base.Initialize(hnRenderGraph, passName);
        }

        public override void Record(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<ReflectionProbeAtlasPassData>($"{name}({PassName})", out var passData))
            {
                if(renderingData.Camera.cameraType == CameraType.Reflection)
                {
                    return;
                }
                
                builder.AllowPassCulling(false);

                ClearProbesRef();

                var reflectionProbes = renderingData.visibleReflectionProbes;
                int probeCount = 0;
                for(int i = 0; i < reflectionProbes.Length; i++)
                {
                    var probe = reflectionProbes[i];
                    if(probe.texture == null)
                        continue;
                    
                    var probeData = probe.reflectionProbe.GetHNAdditionalReflectionProbeData();
                    UpdateProbeRef(probe, probeData);
                    probeCount++;
                }

                passData.catchedReflectionProbeData = renderingData.catchedReflectionProbeData;
                CatcheProbes(ref passData.catchedReflectionProbeData);
                renderingData.catchedReflectionProbeData = passData.catchedReflectionProbeData;

                passData.reflectionProbeAtlas = renderGraph.CreateTexture(new TextureDesc(4096, 4096)
                {
                    colorFormat = REFLECTION_PROBE_ATLAS_FORMAT,
                    dimension = REFLECTION_PROBE_ATLAS_DIMENSION,
                    slices = 18,  // Must be multiple of 6 for cube map arrays (3 * 6 for 16 probes)
                    filterMode = REFLECTION_PROBE_ATLAS_FILTER_MODE,
                    wrapMode = REFLECTION_PROBE_ATLAS_WRAP_MODE,
                    useMipMap = true,
                    autoGenerateMips = false,
                    name = REFLECTION_PROBE_ATLAS_NAME
                });
                builder.WriteTexture(passData.reflectionProbeAtlas);

                builder.SetRenderFunc(
                    (ReflectionProbeAtlasPassData data, RenderGraphContext ctx) =>
                    {
                        for(int i = 0; i < MAX_REFLECTION_PROBES_ON_SCREEN; i++)
                        {
                            if(passData.catchedReflectionProbeData.needUpdate[i])
                            {
                                var texture = passData.catchedReflectionProbeData.probe[i].texture;
                                Vector4 scaleOffset = GetTextureScaleOffsetInAtlas(passData.catchedReflectionProbeData.scaleOffset[i]);
                                Vector2 textureSizeWithoutPadding = GetTextureSizeWithoutpadding(scaleOffset, REFLECTION_PROBE_ATLAS_TEXEL_PADDING);
                                bool isBilinear = texture.filterMode == FilterMode.Bilinear || texture.filterMode == FilterMode.Trilinear;
                                for(int mipLevel = 0; mipLevel < REFLECTION_PROBE_ATLAS_MIP_COUNT; mipLevel++)
                                {
                                    ctx.cmd.SetRenderTarget(passData.reflectionProbeAtlas, mipLevel, CubemapFace.Unknown, 0);
                                    Blitter.BlitCubeToOctahedral2DQuadWithPadding(ctx.cmd, texture, textureSizeWithoutPadding, scaleOffset, mipLevel, isBilinear, REFLECTION_PROBE_ATLAS_TEXEL_PADDING);
                                }
                            }
                        }
                        ctx.cmd.SetGlobalTexture(PropertyIDs.reflectionProbeAtlas, passData.reflectionProbeAtlas);
                    }
                );
            }
        }

        public override void EndRecord()
        {

        }

        public override void Dispose()
        {
            
        }


        private void ClearProbesRef()
        {
            foreach(var probesDict in refProbes)
            {
                probesDict.Clear();
            }
        }

        private void UpdateProbeRef(VisibleReflectionProbe probe, HNAdditionalReflectionProbeData probeData)
        {
            if(probe.texture == null)
                return;
            
            int resolution = probe.texture.width;
            uint probeHash = GetProbeHash(probe, probeData, resolution);
            int index = (int)Math.Log(4096 / resolution, 2);
            refProbes[index].Add(probeHash, probe);
        }

        private uint GetProbeHash(VisibleReflectionProbe probe, HNAdditionalReflectionProbeData probeData, int resolution)
        {
            uint probeCount = (uint)probeData.UpdateCount;
            uint textureID = (uint)probe.texture.GetInstanceID();

            const uint kPrime = 31;
            return (kPrime + (uint)resolution) * textureID + probeCount;
        }

        private void CatcheProbes(ref CatchedReflectionProbeData catchedreflectionProbeData)
        {
            int catchedProbeCount = 0;
            uint offsetMask = 0;
            uint maxOffsetMask = 0x00FFC000; // 0000 0000 1111 1111 1100 0000 0000 0000
            for(int i = 0; i < refProbes.Length; i++)
            {
                int index = 0;
                int maxCount = MAX_REFLECTION_PROBES_ON_SCREEN;
                var hashes = refProbes[i].Keys.ToList();
                while(refProbes[i].Count > 0 && index < hashes.Count && index < maxCount && offsetMask < maxOffsetMask)
                {
                    int width = 4096 / (i + 1);
                    GetOffset(offsetMask, out int offsetX, out int offsetY);
                    int4 scaleOffset = new int4(width, width, offsetX, offsetY);
                    if(catchedreflectionProbeData.probeHash[catchedProbeCount] == hashes[index])
                    {
                        catchedreflectionProbeData.probeHash[catchedProbeCount] = hashes[index];
                        catchedreflectionProbeData.probe[catchedProbeCount] = refProbes[i][hashes[index]];
                        catchedreflectionProbeData.scaleOffset[catchedProbeCount] = scaleOffset;
                        catchedreflectionProbeData.needUpdate[catchedProbeCount] = true;
                    }
                    else
                    {
                        if(Int4Equal(catchedreflectionProbeData.scaleOffset[catchedProbeCount], scaleOffset))
                        {
                            catchedreflectionProbeData.scaleOffset[catchedProbeCount] = scaleOffset;
                            catchedreflectionProbeData.needUpdate[catchedProbeCount] = true;
                        }
                        else
                        {
                            catchedreflectionProbeData.needUpdate[catchedProbeCount] = false;
                        }
                    }
                    index++;
                    catchedProbeCount++;
                    offsetMask += (uint)1 << (3 + i);
                }
            }
        }

        private void GetOffset(uint offsetMask, out int offsetX, out int offsetY)
        {
            // 在offsetMask中按位存储当前texture在atlas中的位置
            // Mask中的有效位只有中间的2 * 5 = 10位，2表示x和y，即相邻两位左边表示x，右边表示y；5表示最多支持5种分辨率的reflection probe
            // 将atlas分成四块，四块分别用00, 01, 10, 11表示，分好的每一块又可以再次四分，用低两位00, 01, 10, 11表示，如此递归
            // 最终可以用2 * 5 = 10位表示从最大4096分辨率到256分辨率的reflection probe在atlas中的位置
            // 有效位放在offsetMask的第15到24位，是为了方便计算分辨率
            // 下面的计算是为了将相邻的x和y位拆开，分别计算出offsetX和offsetY

            offsetX = offsetY = 0;
            // 0000 0000 1010 1010 1000 0000 0000 0000
            uint offsetXBits = offsetMask & 0x00AA8000;
            offsetXBits = (offsetXBits | (offsetXBits >> 1)) & 0xCCCCCCCC;
            offsetXBits = (offsetXBits | (offsetXBits >> 2)) & 0xF0F0F0F0;
            offsetXBits = (offsetXBits | (offsetXBits >> 4)) & 0xFF00FF00;
            offsetXBits = (offsetXBits | (offsetXBits >> 8)) & 0xFFFF0000;
            
            // 0000 0000 0101 0101 0100 0000 0000 0000
            uint offsetYBits = offsetMask & 0x00554000;
            offsetYBits = (offsetYBits | (offsetYBits >> 1)) & 0x33333333;
            offsetYBits = (offsetYBits | (offsetYBits >> 2)) & 0x0F0F0F0F;
            offsetYBits = (offsetYBits | (offsetYBits >> 4)) & 0x00FF00FF;
            offsetYBits = (offsetYBits | (offsetYBits >> 8)) & 0x0000FFFF;

            offsetX = (int)offsetXBits;
            offsetY = (int)offsetYBits;
        }

        private bool Int4Equal(int4 int4A, int4 int4B)
        {
            return int4A.x == int4B.x && int4A.y == int4B.y && int4A.z == int4B.z && int4A.w == int4B.w;
        }

        private Vector4 GetTextureScaleOffsetInAtlas(int4 scaleOffset)
        {
            float atlasSize = REFLECTION_PROBE_ATLAS_SIZE;
            float scaleX = scaleOffset.x / atlasSize;
            float scaleY = scaleOffset.y / atlasSize;
            float offsetX = scaleOffset.z / atlasSize;
            float offsetY = scaleOffset.w / atlasSize;
            return new Vector4(scaleX, scaleY, offsetX, offsetY);
        }

        private Vector2 GetTextureSizeWithoutpadding(Vector4 scaleOffset, int texelPadding)
        {
            float width = scaleOffset.x * REFLECTION_PROBE_ATLAS_SIZE - texelPadding * 2;
            return new Vector2(width, width);
        }


        private Dictionary<uint, VisibleReflectionProbe>[] refProbes = new Dictionary<uint, VisibleReflectionProbe>[5]
        {
            new Dictionary<uint, VisibleReflectionProbe>(),
            new Dictionary<uint, VisibleReflectionProbe>(),
            new Dictionary<uint, VisibleReflectionProbe>(),
            new Dictionary<uint, VisibleReflectionProbe>(),
            new Dictionary<uint, VisibleReflectionProbe>()
        };
        

        public const string PassName = "Reflection Probe Atlas Pass";

        private const int MAX_REFLECTION_PROBES_ON_SCREEN = HNRenderPipelineAsset.MAX_REFLECTION_PROBES_ON_SCREEN;
        private const int REFLECTION_PROBE_ATLAS_SIZE = 4096;
        private const GraphicsFormat REFLECTION_PROBE_ATLAS_FORMAT = GraphicsFormat.B10G11R11_UFloatPack32;
        private const TextureDimension REFLECTION_PROBE_ATLAS_DIMENSION = TextureDimension.CubeArray;
        private const FilterMode REFLECTION_PROBE_ATLAS_FILTER_MODE = FilterMode.Bilinear;
        private const TextureWrapMode REFLECTION_PROBE_ATLAS_WRAP_MODE = TextureWrapMode.Clamp;
        private const int REFLECTION_PROBE_ATLAS_MIP_COUNT = 8;
        private const int REFLECTION_PROBE_ATLAS_TEXEL_PADDING = 2;
        private const string REFLECTION_PROBE_ATLAS_NAME = "HN_ReflectionProbeAtlas";


        public class ReflectionProbeAtlasPassData
        {
            public TextureHandle reflectionProbeAtlas;
            public CatchedReflectionProbeData catchedReflectionProbeData;
        }


        public static class PropertyIDs
        {
            public static readonly int reflectionProbeAtlas = Shader.PropertyToID("_HN_ReflectionProbeAtlas");
        }
    }
}

