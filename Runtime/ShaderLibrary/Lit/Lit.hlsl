#ifndef HNRP_FORWARD_PASS_INCLUDED
#define HNRP_FORWARD_PASS_INCLUDED

#include "../Lit/LitInput.hlsl"
#include "../Lit/LitVaryingsDefine.hlsl"
#include "../Lit/LitVaryings.hlsl"
#include "../Lit/LitSurfaceData.hlsl"
#include "../Lit/LitLighting.hlsl"

// 渲染调试的每像素颜色映射只需要值 → 颜色的映射函数，
// 通道抽取在本文件内完成（依赖 Lit 的数据结构）。
#ifdef HN_DEBUG_PER_PIXEL
#include "../Lit/LitDebug.hlsl"
#endif

PackedVaryings VertMain(Attributes attributes)
{
    UNITY_SETUP_INSTANCE_ID(attributes);

    VertexInput vertexInput;
    ZERO_INITIALIZE(VertexInput, vertexInput);
    BuildVertexInput(attributes, vertexInput);

    LitVaryings litVaryings;
    ZERO_INITIALIZE(LitVaryings, litVaryings);
    BuildLitVaryings(vertexInput, litVaryings);
    
    PackedVaryings packedVaryings;
    ZERO_INITIALIZE(PackedVaryings, packedVaryings);
    BuildPackVaryings(litVaryings, packedVaryings);

    UNITY_TRANSFER_INSTANCE_ID(attributes, packedVaryings);

    return packedVaryings;
}

float4 FragMain(PackedVaryings packedVaryings)
{
    UNITY_SETUP_INSTANCE_ID(packedVaryings);

    LitVaryings litVaryings;
    ZERO_INITIALIZE(LitVaryings, litVaryings);
    BuildUnpackVaryings(packedVaryings, litVaryings);

    LitSurfaceData litSurfaceData;
    ZERO_INITIALIZE(LitSurfaceData, litSurfaceData);
    BuildLitSurfaceData(litVaryings, litSurfaceData);

    PreBRDFData preBRDFData;
    ZERO_INITIALIZE(PreBRDFData, preBRDFData);
    BuildPreBRDFData(litVaryings, preBRDFData);

    BRDFData brdfData;
    ZERO_INITIALIZE(BRDFData, brdfData);
    BuildBRDFData(litSurfaceData, preBRDFData, brdfData);
    
    LightingInputData lightingInputData;
    ZERO_INITIALIZE(LightingInputData, lightingInputData);
    BuildLightingInputData(litVaryings, brdfData, lightingInputData);

    BRDFLightingData brdfLightingData;
    ZERO_INITIALIZE(BRDFLightingData, brdfLightingData);
    BuildBRDFLightingData(preBRDFData, brdfData, lightingInputData, brdfLightingData);

    LightingData lightingData;
    ZERO_INITIALIZE(LightingData, lightingData);
    BuildLightingData(litSurfaceData, brdfData, lightingInputData, brdfLightingData, lightingData);

    LightingOutputData lightingOutputData;
    ZERO_INITIALIZE(LightingOutputData, lightingOutputData);
    BuildLightingOutputData(litSurfaceData, lightingData, lightingOutputData);

// #if defined(USE_UV0_VARYING)
//     float test = 1;
// #else
//     float test = 0;
// #endif
    // float test = GetAdditionalLightsCount() - 2;
    // float3 test3 = float3(frac(lightingInputData.normalizedScreenSpaceUV.x * 8), frac(lightingInputData.normalizedScreenSpaceUV.y * 8), 0);
    // float4 outColor = float4(test3.x, test3.y, test3.z, 1);
    
    float4 outColor = float4(lightingOutputData.lightingColor.rgb, lightingOutputData.alpha);

#ifdef HN_DEBUG_PER_PIXEL
    // 渲染调试：把选中的中间值映射为颜色并按配置的透明度覆盖原渲染颜色。
    // 逐像素在几何遮蔽范围内生效，不需要额外的全屏 pass 或 UAV 写入。
    float4 debugRaw = HN_DebugLitChannelValue(
        _HNRPDebugChannelId,
        lightingInputData.normalizedScreenSpaceUV,
        litVaryings,
        litSurfaceData,
        preBRDFData,
        brdfData,
        lightingData,
        lightingOutputData);

    outColor.rgb = HN_DebugApplyPerPixelColor(debugRaw, outColor.rgb);
#endif

    return outColor;
}

void frag
(
    PackedVaryings packedVaryings,
    out float4 outColor : SV_Target
)
{
    outColor = FragMain(packedVaryings);
}

PackedVaryings vert(Attributes attributes)
{
    PackedVaryings packedVaryings = VertMain(attributes);
    return packedVaryings;
}

#endif