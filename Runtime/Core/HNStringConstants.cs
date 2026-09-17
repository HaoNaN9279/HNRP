using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public static class ShaderPassNames
    {
        public static readonly string ForwardStr = "Forward";
        public static readonly string ShadowCasterStr = "ShadowCaster";


        public static readonly ShaderTagId ForwardName = new ShaderTagId(ForwardStr);
        public static readonly ShaderTagId ShadowCasterName = new ShaderTagId(ShadowCasterStr);


        public static readonly ShaderTagId[] AllForwardNames = new[] { ForwardName };
        public static readonly ShaderTagId[] AllShadowCasterNames = new[] { ShadowCasterName };
    }


    public static class GlobalPropertyIDs
    {
        public static readonly int ShaderVariablesGlobal = Shader.PropertyToID("ShaderVariablesGlobal");
        public static readonly int glossyEnvironmentCubeMap = Shader.PropertyToID("_GlossyEnvironmentCubeMap");
    }


    /// <summary>
    /// 渲染调试（<see cref="HNRPDebug"/>）使用的全局 shader 属性 ID。
    /// 名称必须与 <c>ShaderLibrary/Debug/*.hlsl</c> 中的声明逐字一致。
    /// </summary>
    public static class DebugPropertyIDs
    {
        /// <summary>单值缓冲（<c>StructuredBuffer&lt;DebugValueEntry&gt;</c>）。</summary>
        public static readonly int Values = Shader.PropertyToID("_HNRPDebugValues");

        /// <summary>标签下标缓冲（<c>StructuredBuffer&lt;uint&gt;</c>）。</summary>
        public static readonly int Strings = Shader.PropertyToID("_HNRPDebugStrings");

        /// <summary>字模图集。</summary>
        public static readonly int Font = Shader.PropertyToID("_HNRPDebugFont");

        /// <summary>本帧单值条目数。</summary>
        public static readonly int EntryCount = Shader.PropertyToID("_HNRPDebugEntryCount");

        /// <summary>
        /// 文本区左上角（像素，原点在左上）。
        /// <c>xy</c> = 原点像素坐标，<c>z</c> = Y 轴翻转开关（0/1），<c>w</c> = 目标高度（像素）。
        /// </summary>
        public static readonly int TextOrigin = Shader.PropertyToID("_HNRPDebugTextOrigin");

        /// <summary>
        /// 叠层 Y 轴翻转开关（0/1）。
        /// </summary>
        /// <remarks>
        /// 当相机最终 blit 未做 Y 翻转时（SceneView），相机目标是「上下倒置」存储的，
        /// 由消费方（SceneView GUI）再翻回来，因此叠层必须自行翻转才能与画面一致。
        /// 取值来自 <see cref="CameraContext.Flip"/> 的取反。
        /// </remarks>
        public static readonly int FlipY = Shader.PropertyToID("_HNRPDebugFlipY");

        /// <summary>文本缩放系数。</summary>
        public static readonly int FontScale = Shader.PropertyToID("_HNRPDebugFontScale");

        /// <summary>文本颜色。</summary>
        public static readonly int FontColor = Shader.PropertyToID("_HNRPDebugFontColor");

        /// <summary>值 → 颜色渐变 LUT。</summary>
        public static readonly int ColorMap = Shader.PropertyToID("_HNRPDebugColorMap");

        /// <summary>每像素调试选中的通道 ID。</summary>
        public static readonly int ChannelId = Shader.PropertyToID("_HNRPDebugChannelId");

        /// <summary>每像素调试抽取的标量通道。</summary>
        public static readonly int ColorChannel = Shader.PropertyToID("_HNRPDebugColorChannel");

        /// <summary>每像素调试的映射区间（x = 下界，y = 上界）。</summary>
        public static readonly int ColorRange = Shader.PropertyToID("_HNRPDebugColorRange");

        /// <summary>每像素调试的越界处理（0 = Clamp，1 = Wrap）。</summary>
        public static readonly int ColorWrap = Shader.PropertyToID("_HNRPDebugColorWrap");

        /// <summary>每像素调试的覆盖强度。</summary>
        public static readonly int ColorOpacity = Shader.PropertyToID("_HNRPDebugColorOpacity");

        /// <summary>纹理预览源纹理。</summary>
        public static readonly int Preview = Shader.PropertyToID("_HNRPDebugPreview");

        /// <summary>纹理预览的 slice 索引。</summary>
        public static readonly int PreviewSlice = Shader.PropertyToID("_HNRPDebugPreviewSlice");

        /// <summary>纹理预览的 mip 级别。</summary>
        public static readonly int PreviewMip = Shader.PropertyToID("_HNRPDebugPreviewMip");

        /// <summary>纹理预览矩形（x = 边长，y = 目标高度）。</summary>
        public static readonly int PreviewRect = Shader.PropertyToID("_HNRPDebugPreviewRect");
    }


    public static class MaterialPropertys
    {
        public static readonly string surfaceType = "_SurfaceType";
        public static readonly string blendMode = "_BlendMode";
        public static readonly string srcBlend = "_SrcBlend";
        public static readonly string dstBlend = "_DstBlend";
        public static readonly string srcBlendAlpha = "_SrcBlendAlpha";
        public static readonly string dstBlendAlpha = "_DstBlendAlpha";
        public static readonly string alphaClip = "_AlphaClip";
        public static readonly string cutoff = "_Cutoff";
        public static readonly string cullMode = "_CullMode";
        public static readonly string ztestMode = "_ZTestMode";
        public static readonly string zwrite = "_ZWrite";
        public static readonly string queueOffset = "_QueueOffset";

        public static readonly string baseMap = "_BaseMap";
        public static readonly string baseColor = "_BaseColor";
        public static readonly string alphaRemapMin = "_AlphaRemapMin";
        public static readonly string alphaRemapMax = "_AlphaRemapMax";
        public static readonly string maskMap = "_MaskMap";
        public static readonly string metallicRemapMin = "_MetallicRemapMin";
        public static readonly string metallicRemapMax = "_MetallicRemapMax";
        public static readonly string smoothnessRemapMin = "_SmoothnessRemapMin";
        public static readonly string smoothnessRemapMax = "_SmoothnessRemapMax";
        public static readonly string aoRemapMin = "_AORemapMin";
        public static readonly string aoRemapMax = "_AORemapMax";
        public static readonly string metallic = "_Metallic";
        public static readonly string smoothness = "_Smoothness";
        public static readonly string normalMap = "_NormalMap";
        public static readonly string normalScale = "_NormalScale";
        public static readonly string emissionMap = "_EmissionMap";
        public static readonly string emissionColor = "_EmissionColor";
    }


    public static class MaterialLitKeywords
    {
        public static readonly string alphaPremultiply = "_ALPHAPREMULTIPLY_ON";
        public static readonly string alphaTest = "_ALPHATEST_ON";
        public static readonly string basemap = "_BASEMAP";
        public static readonly string normalMap = "_NORMALMAP";
        public static readonly string maskMap = "_MASKMAP";
        public static readonly string emissionMap = "_EMISSIONMAP";
    }


    public static class GlobalKeywords
    {
        public static readonly string evaluateSHMixed = "EVALUATE_SH_MIXED";
        public static readonly string evaluateSHVertex = "EVALUATE_SH_VERTEX";
        public static readonly string shadowMap = "SHADOW_MAP";
        public static readonly string screenSpaceShadowMap = "SCREEN_SPACE_SHADOW_MAP";
        public static readonly string clusterCullingReflectionProbe = "CLUSTER_CULLING_REFLECTION_PROBE";
        public static readonly string clusterCullingLight = "CLUSTER_CULLING_LIGHT";

        /// <summary>
        /// 每像素渲染调试开关。全局唯一 keyword：具体通道由
        /// <c>_HNRPDebugChannelId</c> 这个 uniform 选择，避免逐通道生成变体。
        /// </summary>
        public static readonly string debugPerPixel = "HN_DEBUG_PER_PIXEL";
    }
}
