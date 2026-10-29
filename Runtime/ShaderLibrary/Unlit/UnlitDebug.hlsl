#ifndef HNRP_UNLIT_DEBUG_INCLUDED
#define HNRP_UNLIT_DEBUG_INCLUDED

#include "../Debug/DebugColorMap.hlsl"
#include "../Debug/DebugChannelIds.hlsl"

// HNRP 渲染调试 —— Unlit 前向 pass 暴露的每像素通道。
//
// Unlit 无光照 / GI / 阴影，只暴露材质自身的量：NormalWS / Emission / PositionWS /
// Alpha / FinalColor。通道 ID 取自 ../Debug/DebugChannelIds.hlsl（与 Lit 共享编号）。
// 未列入的通道不做覆盖（保持原渲染颜色），避免把无意义的量误映射成色带。

// 该通道是否由 Unlit 支持。
bool HN_DebugUnlitSupports(uint channelId)
{
    switch (channelId)
    {
        case HN_DEBUG_NORMAL_WS:
        case HN_DEBUG_EMISSION:
        case HN_DEBUG_POSITION_WS:
        case HN_DEBUG_ALPHA:
        case HN_DEBUG_FINAL_COLOR:
            return true;

        default:
            return false;
    }
}

// 按选中的通道号取出一个四分量值，供颜色映射使用。
float4 HN_DebugUnlitChannelValue(
    uint channelId,
    UnlitVaryings varyings,
    UnlitSurfaceData surface,
    UnlitOutputData output)
{
    switch (channelId)
    {
        case HN_DEBUG_NORMAL_WS:
            return float4(varyings.normalWS, 1.0);

        case HN_DEBUG_EMISSION:
            // Unlit 的「颜色参数 × 贴图采样结果」即自发光量。
            return float4(surface.color, 1.0);

        case HN_DEBUG_POSITION_WS:
            return float4(varyings.positionWS, 1.0);

        case HN_DEBUG_ALPHA:
            return float4(surface.alpha, 0.0, 0.0, 1.0);

        case HN_DEBUG_FINAL_COLOR:
            return float4(output.color, 1.0);

        default:
            return float4(output.color, 1.0);
    }
}

#endif // HNRP_UNLIT_DEBUG_INCLUDED
