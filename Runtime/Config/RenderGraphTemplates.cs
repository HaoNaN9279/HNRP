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
        /// <summary>标准渲染图模板（8 pass / 18 slot / 4 resource / 4 resource connection，PerPixel+HDR）。</summary>
        public static readonly RenderGraphTemplate Standard = new RenderGraphTemplate(
            "StandardGraph",
            HNRenderPipelineGlobalSettings.HNRenderPipelinePath + "Runtime/Resources/RenderGraphs/StandardGraph.asset",
            "RenderGraphs/StandardGraph",
            PopulateStandardGraph);

        /// <summary> 反射渲染图模板（7 pass / 11 slot / 7 resource / 8 resource connection，PerPixel+HDR）。</summary>
        public static readonly RenderGraphTemplate Reflection = new RenderGraphTemplate(
            "ReflectionGraph",
            HNRenderPipelineGlobalSettings.HNRenderPipelinePath + "Runtime/Resources/RenderGraphs/ReflectionGraph.asset",
            "RenderGraphs/ReflectionGraph",
            PopulateReflectionGraph);

        /// <summary>预览渲染图模板（2 pass / 1 slot / 3 resource / 3 resource connection，PerVertex+无HDR）。</summary>
        public static readonly RenderGraphTemplate Preview = new RenderGraphTemplate(
            "PreviewGraph",
            HNRenderPipelineGlobalSettings.HNRenderPipelinePath + "Runtime/Resources/RenderGraphs/PreviewGraph.asset",
            "RenderGraphs/PreviewGraph",
            PopulatePreviewGraph);

        // 未来扩展示例：
        // public static readonly RenderGraphTemplate Xxx = new RenderGraphTemplate(...);

        /// <summary>确保所有模板资源存在（Editor 下创建，非 Editor 下 Resources.Load）。</summary>
        public static void EnsureAll()
        {
            Standard.Ensure();
            Reflection.Ensure();
            Preview.Ensure(); 
        }

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
                    SlotConnection.Create("clusterProbe", "reflectionProbeAtlasOutput", "transparency", "ReflectionProbeAtlas"),
                    SlotConnection.Create("clusterProbe", "clusterCullingReflectionProbeMaskBuffer", "transparency", "ProbeMask"),
                    SlotConnection.Create("clusterProbe", "clusterCullingReflectionProbeDatasBuffer", "transparency", "ProbeDatas"),
                    SlotConnection.Create("clusterLight", "clusterCullingLightMaskBuffer", "transparency", "LightMask"),
                    SlotConnection.Create("buildLight", "lightDatasBuffer", "clusterLight", "lightDatasBuffer"),
                    SlotConnection.Create("buildLight", "lightDatasBuffer", "forwardOpaque", "LightDatas"),
                    SlotConnection.Create("clusterLight", "clusterCullingLightMaskBuffer", "forwardOpaque", "LightMask"),
                    SlotConnection.Create("clusterProbe", "reflectionProbeAtlasOutput", "forwardOpaque", "ReflectionProbeAtlas"),
                    SlotConnection.Create("clusterProbe", "clusterCullingReflectionProbeMaskBuffer", "forwardOpaque", "ProbeMask"),
                    SlotConnection.Create("clusterProbe", "clusterCullingReflectionProbeDatasBuffer", "forwardOpaque", "ProbeDatas"),
                },
                new List<ResourceDefinition>
                {
                    new TextureResourceDefinition { ResourceName = "ColorBuffer" },
                    new TextureResourceDefinition { ResourceName = "DepthBuffer", DepthBits = DepthBits.Depth32 },
                    new RendererListResourceDefinition { ResourceName = "OpaqueRendererList", ListKind = RenderListKind.Opaque, RenderingLayerMask = 1 },
                    new RendererListResourceDefinition { ResourceName = "TransparentRendererList", ListKind = RenderListKind.Transparent, RenderingLayerMask = 1 },
                    new TextureResourceDefinition
                    {
                        ResourceName = "ReflectionProbeAtlas",
                        Width = 4096,
                        Height = 4096,
                        ColorFormat = GraphicsFormat.B10G11R11_UFloatPack32,
                        ClearBuffer = true,
                        ClearColor = Color.black,
                        UseMipMap = true,
                        AutoGenerateMips = false,
                        FilterMode = FilterMode.Trilinear,
                        WrapMode = TextureWrapMode.Clamp,
                        TextureDimension = TextureDimension.Tex2D,
                    },
                },
                new List<ResourceConnection>
                {
                    new ResourceConnection { ResourceName = "ColorBuffer", PassName = "forwardOpaque", SlotName = "ColorTarget" },
                    new ResourceConnection { ResourceName = "DepthBuffer", PassName = "forwardOpaque", SlotName = "DepthTarget" },
                    new ResourceConnection { ResourceName = "OpaqueRendererList", PassName = "forwardOpaque", SlotName = "RendererList" },
                    new ResourceConnection { ResourceName = "TransparentRendererList", PassName = "transparency", SlotName = "RendererList" },
                    new ResourceConnection { ResourceName = "ReflectionProbeAtlas", PassName = "clusterProbe", SlotName = "reflectionProbeAtlas" },
                },
                new RenderGraphSettings { SHEvalMode = SHEvalMode.PerPixel, AllowHDR = true });
        }

        private static void PopulateReflectionGraph(RenderGraphAsset g)
        {
            g.SetDefinition(
                new List<PassDefinition>
                {
                    PassDefinition.Create("Build Light Data", "buildLight"),
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
                    SlotConnection.Create("clusterLight", "clusterCullingLightMaskBuffer", "transparency", "LightMask"),
                    SlotConnection.Create("buildLight", "lightDatasBuffer", "clusterLight", "lightDatasBuffer"),
                    SlotConnection.Create("clusterLight", "clusterCullingLightMaskBuffer", "forwardOpaque", "LightMask"),
                },
                new List<ResourceDefinition>
                {
                    new TextureResourceDefinition { ResourceName = "ColorBuffer" },
                    new TextureResourceDefinition { ResourceName = "DepthBuffer", DepthBits = DepthBits.Depth32 },
                    new RendererListResourceDefinition { ResourceName = "OpaqueRendererList", ListKind = RenderListKind.Opaque, RenderingLayerMask = 1 },
                    new RendererListResourceDefinition { ResourceName = "TransparentRendererList", ListKind = RenderListKind.Transparent, RenderingLayerMask = 1 },
                    new TextureResourceDefinition { ResourceName = "ReflectionProbeAtlas", ExternalTextureName = "emptyTexture"},
                },
                new List<ResourceConnection>
                {
                    new ResourceConnection { ResourceName = "ColorBuffer", PassName = "forwardOpaque", SlotName = "ColorTarget" },
                    new ResourceConnection { ResourceName = "DepthBuffer", PassName = "forwardOpaque", SlotName = "DepthTarget" },
                    new ResourceConnection { ResourceName = "OpaqueRendererList", PassName = "forwardOpaque", SlotName = "RendererList" },
                    new ResourceConnection { ResourceName = "TransparentRendererList", PassName = "transparency", SlotName = "RendererList" },
                    new ResourceConnection { ResourceName = "ReflectionProbeAtlas", PassName = "forwardOpaque", SlotName = "ReflectionProbeAtlas" },
                    new ResourceConnection { ResourceName = "ReflectionProbeAtlas", PassName = "transparency", SlotName = "ReflectionProbeAtlas" },
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
                    new TextureResourceDefinition { ResourceName = "ColorBuffer" },
                    new TextureResourceDefinition { ResourceName = "DepthBuffer", DepthBits = DepthBits.Depth32 },
                    new RendererListResourceDefinition { ResourceName = "OpaqueRendererList", ListKind = RenderListKind.Opaque },
                },
                new List<ResourceConnection>
                {
                    new ResourceConnection { ResourceName = "ColorBuffer", PassName = "opaque", SlotName = "ColorTarget" },
                    new ResourceConnection { ResourceName = "DepthBuffer", PassName = "opaque", SlotName = "DepthTarget" },
                    new ResourceConnection { ResourceName = "OpaqueRendererList", PassName = "opaque", SlotName = "RendererList" },
                },
                new RenderGraphSettings { SHEvalMode = SHEvalMode.PerVertex, AllowHDR = false });
        }
    }
}
