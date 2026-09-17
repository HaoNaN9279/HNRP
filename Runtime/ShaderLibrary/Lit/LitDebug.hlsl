#ifndef HNRP_LIT_DEBUG_INCLUDED
#define HNRP_LIT_DEBUG_INCLUDED

#include "../Debug/DebugColorMap.hlsl"

// 群集掩码缓冲与簇索引推导（与光照 / GI 循环使用同一套函数，保证簇归属一致）。
#include "../ClusterCulling/ClusterCullingLight.hlsl"
#include "../ClusterCulling/ClusterCullingReflectionProbe.hlsl"

// HNRP 渲染调试 —— Lit 前向 pass 暴露的每像素通道。
//
// 通道 ID 必须与 C# 侧 DrawObjectPass.DebugChannels 的声明顺序一致。
// 新增通道时同步修改两处即可，无需改动管线或显示 pass。
//
// 说明：每像素值的求值必然发生在「有像素的地方」，即本着色器；因此所有每像素通道
// （含数据来自其它 pass 的群集计数通道）都由这里实现。群集通道读取的是
// ClusterCullingLightPass / ClusterCullingReflectionProbePass 绑定到全局的掩码缓冲。

#define HN_LIT_DEBUG_ALBEDO                0
#define HN_LIT_DEBUG_NORMAL_WS             1
#define HN_LIT_DEBUG_SMOOTHNESS            2
#define HN_LIT_DEBUG_METALLIC              3
#define HN_LIT_DEBUG_OCCLUSION             4
#define HN_LIT_DEBUG_EMISSION              5
#define HN_LIT_DEBUG_POSITION_WS           6
#define HN_LIT_DEBUG_MAIN_LIGHT_DIFFUSE    7
#define HN_LIT_DEBUG_MAIN_LIGHT_SPECULAR   8
#define HN_LIT_DEBUG_ADD_LIGHT_DIFFUSE     9
#define HN_LIT_DEBUG_INDIRECT_DIFFUSE      10
#define HN_LIT_DEBUG_INDIRECT_SPECULAR     11
#define HN_LIT_DEBUG_ALPHA                 12
#define HN_LIT_DEBUG_NDOTV                 13
#define HN_LIT_DEBUG_ROUGHNESS             14
#define HN_LIT_DEBUG_FINAL_COLOR           15
#define HN_LIT_DEBUG_CLUSTER_LIGHT_COUNT   16
#define HN_LIT_DEBUG_CLUSTER_PROBE_COUNT   17

// 统计群集掩码中实际置位的条目数。
// word 0 是压缩范围头（min|max），掩码从 word 1 起。
uint HN_DebugCountMaskBits(
    uint headerIndex,
    uint wordsPerCluster,
    int minIndex,
    int maxIndex,
    StructuredBuffer<uint> maskBuffer)
{
    if (maxIndex < minIndex || wordsPerCluster <= 1u)
    {
        return 0u;
    }

    uint bits = 0u;
    for (uint word = 1u; word < wordsPerCluster; ++word)
    {
        bits += countbits(maskBuffer[headerIndex * wordsPerCluster + word]);
    }

    return bits;
}

// 按选中的通道号取出一个四分量值，供颜色映射使用。
float4 HN_DebugLitChannelValue(
    uint channelId,
    float2 normalizedScreenSpaceUV,
    LitVaryings varyings,
    LitSurfaceData surface,
    PreBRDFData preBRDF,
    BRDFData brdf,
    LightingData lighting,
    LightingOutputData output)
{
    switch (channelId)
    {
        case HN_LIT_DEBUG_ALBEDO:
            return float4(surface.albedo, 1.0);

        case HN_LIT_DEBUG_NORMAL_WS:
            return float4(brdf.normalWS, 1.0);

        case HN_LIT_DEBUG_SMOOTHNESS:
            return float4(surface.smoothness, 0.0, 0.0, 1.0);

        case HN_LIT_DEBUG_METALLIC:
            return float4(surface.metallic, 0.0, 0.0, 1.0);

        case HN_LIT_DEBUG_OCCLUSION:
            return float4(surface.occlusion, 0.0, 0.0, 1.0);

        case HN_LIT_DEBUG_EMISSION:
            return float4(surface.emission, 1.0);

        case HN_LIT_DEBUG_POSITION_WS:
            return float4(varyings.positionWS, 1.0);

        case HN_LIT_DEBUG_MAIN_LIGHT_DIFFUSE:
            return float4(lighting.mainDirectLight.diffuse, 1.0);

        case HN_LIT_DEBUG_MAIN_LIGHT_SPECULAR:
            return float4(lighting.mainDirectLight.specular, 1.0);

        case HN_LIT_DEBUG_ADD_LIGHT_DIFFUSE:
            return float4(lighting.additionalDirectLight.diffuse, 1.0);

        case HN_LIT_DEBUG_INDIRECT_DIFFUSE:
            return float4(lighting.indirectLight.diffuse, 1.0);

        case HN_LIT_DEBUG_INDIRECT_SPECULAR:
            return float4(lighting.indirectLight.specular, 1.0);

        case HN_LIT_DEBUG_ALPHA:
            return float4(surface.alpha, 0.0, 0.0, 1.0);

        case HN_LIT_DEBUG_NDOTV:
            return float4(brdf.NdotV, 0.0, 0.0, 1.0);

        case HN_LIT_DEBUG_ROUGHNESS:
            return float4(brdf.perceptualRoughness, 0.0, 0.0, 1.0);

        case HN_LIT_DEBUG_FINAL_COLOR:
            return float4(output.lightingColor, 1.0);

        case HN_LIT_DEBUG_CLUSTER_LIGHT_COUNT:
        {
            // 本像素所属簇内被剔除保留的光源数量（热力图：暗→亮 = 少→多）。
#if defined(CLUSTER_CULLING_LIGHT)
            ClusterCullingLightIterator lightIterator =
                ClusterCullingLightInit(normalizedScreenSpaceUV, varyings.positionWS);
            uint lightCount = HN_DebugCountMaskBits(
                lightIterator.headerIndex,
                (uint)max(1, _CLUSTER_CULLING_LIGHT_WORDS_PER_CLUSTER),
                (int)lightIterator.minIndex,
                (int)lightIterator.maxIndex,
                _ClusterCullingLightMaskBuffer);
            return float4((float)lightCount, 0.0, 0.0, 1.0);
#else
            // 群集剔除未启用：返回 0 而不是抛错，热力图全黑即「无数据」。
            return float4(0.0, 0.0, 0.0, 1.0);
#endif
        }

        case HN_LIT_DEBUG_CLUSTER_PROBE_COUNT:
        {
            // 本像素所属簇内被剔除保留的反射探针数量。
#if defined(CLUSTER_CULLING_REFLECTION_PROBE)
            ClusterCullingReflectionProbeIterator probeIterator =
                ClusterCullingReflectionProbeInit(normalizedScreenSpaceUV, varyings.positionWS);
            uint probeCount = HN_DebugCountMaskBits(
                probeIterator.headerIndex,
                (uint)max(1, _CLUSTER_CULLING_REFLECTION_PROBE_WORDS_PER_CLUSTER),
                (int)probeIterator.minIndex,
                (int)probeIterator.maxIndex,
                _ClusterCullingReflectionProbeMaskBuffer);
            return float4((float)probeCount, 0.0, 0.0, 1.0);
#else
            return float4(0.0, 0.0, 0.0, 1.0);
#endif
        }

        default:
            return float4(output.lightingColor, 1.0);
    }
}

#endif // HNRP_LIT_DEBUG_INCLUDED
