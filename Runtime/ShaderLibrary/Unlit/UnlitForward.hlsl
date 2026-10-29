#ifndef HNRP_UNLIT_FORWARD_INCLUDED
#define HNRP_UNLIT_FORWARD_INCLUDED

#include "../Unlit/UnlitInput.hlsl"
#include "../Unlit/UnlitVaryingsDefine.hlsl"
#include "../Unlit/UnlitVaryings.hlsl"
#include "../Unlit/UnlitSurfaceData.hlsl"

// 渲染调试的每像素颜色映射只需要值 → 颜色的映射函数，
// 通道抽取在本文件内完成（依赖 Unlit 的数据结构）。
#ifdef HN_DEBUG_PER_PIXEL
#include "../Unlit/UnlitDebug.hlsl"
#endif

PackedVaryings VertMain(Attributes attributes)
{
    HNRP_PKG_EMIT_UNLIT_FORWARD_VERTEX_BEGIN();

    UNITY_SETUP_INSTANCE_ID(attributes);

    VertexInput vertexInput;
    ZERO_INITIALIZE(VertexInput, vertexInput);
    BuildVertexInput(attributes, vertexInput);
    HNRP_PKG_EMIT_UNLIT_FORWARD_VERTEX_AFTERVERTEXINPUT(vertexInput);

    UnlitVaryings unlitVaryings;
    ZERO_INITIALIZE(UnlitVaryings, unlitVaryings);
    BuildUnlitVaryings(vertexInput, unlitVaryings);
    HNRP_PKG_EMIT_UNLIT_FORWARD_VERTEX_AFTERVARYINGS(unlitVaryings);

    PackedVaryings packedVaryings;
    ZERO_INITIALIZE(PackedVaryings, packedVaryings);
    BuildPackVaryings(unlitVaryings, packedVaryings);

    UNITY_TRANSFER_INSTANCE_ID(attributes, packedVaryings);

    HNRP_PKG_EMIT_UNLIT_FORWARD_VERTEX_END();

    return packedVaryings;
}

float4 FragMain(PackedVaryings packedVaryings)
{
    HNRP_PKG_EMIT_UNLIT_FORWARD_FRAGMENT_BEGIN();

    UNITY_SETUP_INSTANCE_ID(packedVaryings);

    UnlitVaryings unlitVaryings;
    ZERO_INITIALIZE(UnlitVaryings, unlitVaryings);
    BuildUnpackVaryings(packedVaryings, unlitVaryings);
    HNRP_PKG_EMIT_UNLIT_FORWARD_FRAGMENT_AFTERUNPACKVARYINGS(unlitVaryings);

    UnlitSurfaceData unlitSurfaceData;
    ZERO_INITIALIZE(UnlitSurfaceData, unlitSurfaceData);
    BuildUnlitSurfaceData(unlitVaryings, unlitSurfaceData);
    HNRP_PKG_EMIT_UNLIT_FORWARD_FRAGMENT_AFTERSURFACEDATA(unlitSurfaceData, unlitVaryings);

    UnlitOutputData unlitOutputData;
    ZERO_INITIALIZE(UnlitOutputData, unlitOutputData);
    BuildUnlitOutputData(unlitSurfaceData, unlitOutputData);
    HNRP_PKG_EMIT_UNLIT_FORWARD_FRAGMENT_AFTEROUTPUT(unlitOutputData, unlitSurfaceData);

    float4 outColor = float4(unlitOutputData.color, unlitOutputData.alpha);

#ifdef HN_DEBUG_PER_PIXEL
    // 渲染调试：把选中的中间值映射为颜色并按配置的透明度覆盖原渲染颜色。
    // 仅覆盖 Unlit 支持的通道；其余通道保持原色（不做任何映射）。
    if (HN_DebugUnlitSupports(_HNRPDebugChannelId))
    {
        float4 debugRaw = HN_DebugUnlitChannelValue(
            _HNRPDebugChannelId,
            unlitVaryings,
            unlitSurfaceData,
            unlitOutputData);

        outColor.rgb = HN_DebugApplyPerPixelColor(debugRaw, outColor.rgb);
    }
#endif

    HNRP_PKG_EMIT_UNLIT_FORWARD_FRAGMENT_END();

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
