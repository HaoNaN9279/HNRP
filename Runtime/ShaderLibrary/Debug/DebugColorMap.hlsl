#ifndef HNRP_DEBUG_COLOR_MAP_INCLUDED
#define HNRP_DEBUG_COLOR_MAP_INCLUDED

// HNRP 渲染调试 —— 每像素值 → 颜色渐变映射。
//
// 色带由 CPU 烘焙成 256×1 的 LUT（DebugColorMap.CreateLut），shader 只做一次
// 归一化与一次采样：改色带不产生任何 shader 变体。

TEXTURE2D(_HNRPDebugColorMap);

// 选中的通道 ID（由 IPassDebugProvider 定义，pass 自行解释）
uint _HNRPDebugChannelId;

// 抽取的标量通道（DebugColorChannel）
uint _HNRPDebugColorChannel;

// 映射区间：x = 下界，y = 上界
float2 _HNRPDebugColorRange;

// 越界处理：0 = Clamp，1 = Wrap
float _HNRPDebugColorWrap;

// 覆盖强度：0 = 不覆盖原渲染颜色，1 = 完全覆盖
float _HNRPDebugColorOpacity;

// 从值向量中抽取标量。
// 通道编号必须与 C# DebugColorChannel 一致。
float HN_DebugSelectChannel(float4 value, uint channel)
{
    switch (channel)
    {
        case 0u: return value.x;
        case 1u: return value.y;
        case 2u: return value.z;
        case 3u: return value.w;
        case 4u: return dot(value.rgb, float3(0.2126, 0.7152, 0.0722));
        case 5u: return length(value);
        default: return value.x;
    }
}

// 值 → 色带颜色（不含透明度混合）。
float3 HN_DebugMapValueToColor(float4 value)
{
    float v = HN_DebugSelectChannel(value, _HNRPDebugColorChannel);

    float span = max(1e-6, _HNRPDebugColorRange.y - _HNRPDebugColorRange.x);
    float t = (v - _HNRPDebugColorRange.x) / span;
    t = (_HNRPDebugColorWrap > 0.5) ? frac(t) : saturate(t);

    return SAMPLE_TEXTURE2D_LOD(_HNRPDebugColorMap, sampler_PointClamp, float2(t, 0.5), 0).rgb;
}

// 把原渲染颜色替换为映射色；覆盖强度决定混合比例。
float3 HN_DebugApplyPerPixelColor(float4 value, float3 originalColor)
{
    float3 mapped = HN_DebugMapValueToColor(value);
    return lerp(originalColor, mapped, saturate(_HNRPDebugColorOpacity));
}

#endif // HNRP_DEBUG_COLOR_MAP_INCLUDED
