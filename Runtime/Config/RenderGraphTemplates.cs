// <copyright file="RenderGraphTemplates.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace HN.HNRP
{
    /// <summary>
    /// 渲染图模板注册表。未来扩展新模板：在此文件新增一个静态模板实例 + 对应填充方法即可，
    /// 无需改动 <see cref="RenderGraphAsset"/>。
    /// </summary>
    public static class RenderGraphTemplates
    {
        /// <summary>标准渲染图模板（8 pass / 12 slot / 9 resource / 14 resource connection，PerPixel+HDR）。</summary>
        public static readonly RenderGraphTemplate Standard = new RenderGraphTemplate(
            "StandardGraph",
            HNRenderPipelineGlobalSettings.HNRenderPipelinePath + "Runtime/Resources/RenderGraphs/StandardGraph.asset",
            "RenderGraphs/StandardGraph",
            PopulateStandardGraph);

        /// <summary>预览渲染图模板（2 pass / 1 slot / 3 resource / 3 resource connection，PerVertex+无HDR）。</summary>
        public static readonly RenderGraphTemplate Preview = new RenderGraphTemplate(
            "PreviewGraph",
            HNRenderPipelineGlobalSettings.HNRenderPipelinePath + "Runtime/Resources/RenderGraphs/PreviewGraph.asset",
            "RenderGraphs/PreviewGraph",
            PopulatePreviewGraph);

        // 未来扩展示例：
        // public static readonly RenderGraphTemplate Xxx = new RenderGraphTemplate(...);

        private static void PopulateStandardGraph(RenderGraphAsset g)
        {
            g.SetDefinition(
                new List<PassDefinition>
                {
                    PassDefinition.Create("Build Light Data", "buildLight"),
                    PassDefinition.Create("Cluster Culling Probe", "clusterProbe"),
                    PassDefinition.Create("Cluster Culling Light", "clusterLight"),
                    PassDefinition.Create("Draw Object", "forwardOpaque"),
                    PassDefinition.Create("Builtin Sky", "sky"),
                    PassDefinition.Create("Draw Object", "transparency"),
                    PassDefinition.Create("Editor Wire Overlay", "wireOverlay"),
                    PassDefinition.Create("Render Output", "finalBlit"),
                },
                new List<SlotConnection>
                {
                    SlotConnection.Create("forwardOpaque", "ColorTargetOutput", "sky", "ColorTarget"),
                    SlotConnection.Create("forwardOpaque", "DepthTargetOutput", "sky", "DepthTarget"),
                    SlotConnection.Create("sky", "ColorTargetOutput", "transparency", "ColorTarget"),
                    SlotConnection.Create("sky", "DepthTargetOutput", "transparency", "DepthTarget"),
                    SlotConnection.Create("transparency", "ColorTargetOutput", "wireOverlay", "ColorTarget"),
                    SlotConnection.Create("transparency", "ColorTargetOutput", "finalBlit", "ColorTarget"),
                    SlotConnection.Create("buildLight", "lightDatasBuffer", "forwardOpaque", "LightDatas"),
                    SlotConnection.Create("buildLight", "lightDatasBuffer", "transparency", "LightDatas"),
                    SlotConnection.Create("clusterProbe", "reflectionProbeAtlas", "transparency", "ReflectionProbeAtlas"),
                    SlotConnection.Create("clusterProbe", "clusterCullingReflectionProbeMaskBuffer", "transparency", "ProbeMask"),
                    SlotConnection.Create("clusterProbe", "clusterCullingReflectionProbeDatasBuffer", "transparency", "ProbeDatas"),
                    SlotConnection.Create("clusterLight", "clusterCullingLightMaskBuffer", "transparency", "LightMask"),
                },
                new List<ResourceDefinition>
                {
                    new ResourceDefinition { ResourceName = "ColorBuffer", ResourceKind = ResourceKind.Texture },
                    new ResourceDefinition { ResourceName = "DepthBuffer", ResourceKind = ResourceKind.Texture, DepthBits = UnityEngine.Rendering.DepthBits.Depth32 },
                    new ResourceDefinition { ResourceName = "LightDatas", ResourceKind = ResourceKind.ComputeBuffer, BufferCount = 528, BufferStride = 64 },
                    new ResourceDefinition { ResourceName = "LightMask", ResourceKind = ResourceKind.ComputeBuffer, BufferCount = 16384, BufferStride = 4 },
                    new ResourceDefinition { ResourceName = "ReflectionProbeAtlas", ResourceKind = ResourceKind.Texture, ColorFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.B10G11R11_UFloatPack32 },
                    new ResourceDefinition { ResourceName = "ProbeMask", ResourceKind = ResourceKind.ComputeBuffer, BufferCount = 16384, BufferStride = 4 },
                    new ResourceDefinition { ResourceName = "ProbeDatas", ResourceKind = ResourceKind.ComputeBuffer, BufferCount = 64, BufferStride = 64 },
                    new ResourceDefinition { ResourceName = "OpaqueRendererList", ResourceKind = ResourceKind.RendererList, ListKind = RenderListKind.Opaque, RenderingLayerMask = 1 },
                    new ResourceDefinition { ResourceName = "TransparentRendererList", ResourceKind = ResourceKind.RendererList, ListKind = RenderListKind.Transparent, RenderingLayerMask = 1 },
                },
                new List<ResourceConnection>
                {
                    new ResourceConnection { ResourceName = "LightDatas", PassName = "buildLight", SlotName = "lightDatasBuffer", Direction = ResourceConnectionDirection.PassToResource },
                    new ResourceConnection { ResourceName = "LightMask", PassName = "clusterLight", SlotName = "clusterCullingLightMaskBuffer", Direction = ResourceConnectionDirection.PassToResource },
                    new ResourceConnection { ResourceName = "ReflectionProbeAtlas", PassName = "clusterProbe", SlotName = "reflectionProbeAtlas", Direction = ResourceConnectionDirection.PassToResource },
                    new ResourceConnection { ResourceName = "ProbeMask", PassName = "clusterProbe", SlotName = "clusterCullingReflectionProbeMaskBuffer", Direction = ResourceConnectionDirection.PassToResource },
                    new ResourceConnection { ResourceName = "ProbeDatas", PassName = "clusterProbe", SlotName = "clusterCullingReflectionProbeDatasBuffer", Direction = ResourceConnectionDirection.PassToResource },
                    new ResourceConnection { ResourceName = "ColorBuffer", PassName = "forwardOpaque", SlotName = "ColorTarget", Direction = ResourceConnectionDirection.ResourceToPass },
                    new ResourceConnection { ResourceName = "DepthBuffer", PassName = "forwardOpaque", SlotName = "DepthTarget", Direction = ResourceConnectionDirection.ResourceToPass },
                    new ResourceConnection { ResourceName = "LightDatas", PassName = "clusterLight", SlotName = "lightDatasBuffer", Direction = ResourceConnectionDirection.ResourceToPass },
                    new ResourceConnection { ResourceName = "LightMask", PassName = "forwardOpaque", SlotName = "LightMask", Direction = ResourceConnectionDirection.ResourceToPass },
                    new ResourceConnection { ResourceName = "ReflectionProbeAtlas", PassName = "forwardOpaque", SlotName = "ReflectionProbeAtlas", Direction = ResourceConnectionDirection.ResourceToPass },
                    new ResourceConnection { ResourceName = "ProbeMask", PassName = "forwardOpaque", SlotName = "ProbeMask", Direction = ResourceConnectionDirection.ResourceToPass },
                    new ResourceConnection { ResourceName = "ProbeDatas", PassName = "forwardOpaque", SlotName = "ProbeDatas", Direction = ResourceConnectionDirection.ResourceToPass },
                    new ResourceConnection { ResourceName = "OpaqueRendererList", PassName = "forwardOpaque", SlotName = "RendererList", Direction = ResourceConnectionDirection.ResourceToPass },
                    new ResourceConnection { ResourceName = "TransparentRendererList", PassName = "transparency", SlotName = "RendererList", Direction = ResourceConnectionDirection.ResourceToPass },
                },
                new RenderGraphSettings { SHEvalMode = SHEvalMode.PerPixel, AllowHDR = true });
        }

        private static void PopulatePreviewGraph(RenderGraphAsset g)
        {
            g.SetDefinition(
                new List<PassDefinition>
                {
                    PassDefinition.Create("Draw Object", "opaque"),
                    PassDefinition.Create("Render Output", "finalBlit"),
                },
                new List<SlotConnection>
                {
                    SlotConnection.Create("opaque", "ColorTargetOutput", "finalBlit", "ColorTarget"),
                },
                new List<ResourceDefinition>
                {
                    new ResourceDefinition { ResourceName = "ColorBuffer", ResourceKind = ResourceKind.Texture },
                    new ResourceDefinition { ResourceName = "DepthBuffer", ResourceKind = ResourceKind.Texture, DepthBits = UnityEngine.Rendering.DepthBits.Depth32 },
                    new ResourceDefinition { ResourceName = "OpaqueRendererList", ResourceKind = ResourceKind.RendererList, ListKind = RenderListKind.Opaque },
                },
                new List<ResourceConnection>
                {
                    new ResourceConnection { ResourceName = "ColorBuffer", PassName = "opaque", SlotName = "ColorTarget", Direction = ResourceConnectionDirection.ResourceToPass },
                    new ResourceConnection { ResourceName = "DepthBuffer", PassName = "opaque", SlotName = "DepthTarget", Direction = ResourceConnectionDirection.ResourceToPass },
                    new ResourceConnection { ResourceName = "OpaqueRendererList", PassName = "opaque", SlotName = "RendererList", Direction = ResourceConnectionDirection.ResourceToPass },
                },
                new RenderGraphSettings { SHEvalMode = SHEvalMode.PerVertex, AllowHDR = false });
        }
    }
}
