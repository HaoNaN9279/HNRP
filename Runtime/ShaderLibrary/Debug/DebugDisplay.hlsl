#ifndef HNRP_DEBUG_DISPLAY_INCLUDED
#define HNRP_DEBUG_DISPLAY_INCLUDED

#include "DebugValue.hlsl"
#include "DebugText.hlsl"

// HNRP 渲染调试 —— 显示叠层的片元逻辑。
// 由 DebugDisplay.shader 的各 pass 调用；纹理预览的资源声明放在 shader 里
//（2D 与 2DArray 需要不同的纹理类型声明）。

uint _HNRPDebugEntryCount;
float4 _HNRPDebugPreviewRect;   // xy = 预览边长（像素），zw = 目标尺寸（像素）
float _HNRPDebugPreviewSlice;
float _HNRPDebugPreviewMip;
float _HNRPDebugFlipY;          // 0 = 目标即显示方向，1 = 目标上下倒置（需翻转叠层）

struct DebugDisplayVaryings
{
    float4 positionCS : SV_POSITION;
};

// 把目标像素坐标换算到「显示空间」。
//
// 某些相机（SceneView）会把画面上下倒置地存进相机目标，交由消费方翻回来显示，
// 此时叠层若按目标原始坐标绘制就会上下颠倒。用同一个变换同时作用于「定位」与
// 「内容取样」，位置与朝向一次性都对。
int2 HN_DebugToDisplayPixel(int2 pixelCoord, float flipY, float targetHeight)
{
    if (flipY > 0.5)
    {
        pixelCoord.y = (int)targetHeight - 1 - pixelCoord.y;
    }

    return pixelCoord;
}

// 全屏三角形顶点着色器。
DebugDisplayVaryings DebugDisplayVert(uint vertexID : SV_VertexID)
{
    DebugDisplayVaryings output;
    output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
    return output;
}

// 绘制数值的一个分量。
void HN_DebugDrawComponent(uint type, uint raw, int digits, float3 fontColor, int2 pixelCoord,
                           inout int2 cursor, inout float3 color, inout float alpha)
{
    if (HN_DebugIsFloat(type))
    {
        HN_DebugDrawFloat(asfloat(raw), digits, fontColor, pixelCoord, cursor, color, alpha);
    }
    else if (HN_DebugIsUnsigned(type))
    {
        HN_DebugDrawUInteger(raw, fontColor, pixelCoord, cursor, color, alpha);
    }
    else if (type == DV_BOOL)
    {
        HN_DebugDrawBool(raw, fontColor, pixelCoord, cursor, color, alpha);
    }
    else
    {
        HN_DebugDrawInteger(asint(raw), fontColor, pixelCoord, cursor, color, alpha, 0, false);
    }
}

// 绘制完整数值：标量直出，向量以 (a, b, c) 形式输出。
void HN_DebugDrawEntryValue(uint type, uint4 raw, int digits, float3 fontColor, int2 pixelCoord,
                            inout int2 cursor, inout float3 color, inout float alpha)
{
    uint count = HN_DebugComponentCount(type);
    const int step = DEBUG_FONT_TEXT_SCALE_WIDTH;

    if (count > 1u)
    {
        HN_DebugDrawCharacter(DEBUG_CHAR_LP, fontColor, pixelCoord, cursor, color, alpha, 1, step);
    }

    for (uint i = 0u; i < 4u; ++i)
    {
        if (i >= count)
        {
            break;
        }

        if (i > 0u)
        {
            HN_DebugDrawCharacter(DEBUG_CHAR_COMMA, fontColor, pixelCoord, cursor, color, alpha, 1, step);
        }

        uint component = raw.x;
        if (i == 1u) component = raw.y;
        else if (i == 2u) component = raw.z;
        else if (i == 3u) component = raw.w;

        HN_DebugDrawComponent(type, component, digits, fontColor, pixelCoord, cursor, color, alpha);
    }

    if (count > 1u)
    {
        HN_DebugDrawCharacter(DEBUG_CHAR_RP, fontColor, pixelCoord, cursor, color, alpha, 1, step);
    }
}

// 单值文本叠层。
// 输入 alpha 为 0 的像素不改变目标颜色（配合 Blend SrcAlpha OneMinusSrcAlpha）。
float4 HN_DebugFragmentText(DebugDisplayVaryings input) : SV_Target
{
    float3 color = 0.0;
    float alpha = 0.0;

    // _HNRPDebugTextOrigin.zw = (Y 翻转开关, 目标高度)
    int2 pixelCoord = HN_DebugToDisplayPixel(
        int2(input.positionCS.xy), _HNRPDebugTextOrigin.z, _HNRPDebugTextOrigin.w);
    int2 textOrigin = int2(_HNRPDebugTextOrigin.xy);

    int cell = HN_DebugCellSize();
    int lineHeight = max(1, cell + max(2, (int)(4.0 * _HNRPDebugFontScale)));
    int step = HN_DebugStepWidth(DEBUG_FONT_TEXT_SCALE_WIDTH);

    // 纵向粗筛：只处理落在某一行内的像素，避免逐像素遍历全部条目。
    int row = (pixelCoord.y - textOrigin.y) / lineHeight;
    if (row < 0 || row >= (int)_HNRPDebugEntryCount)
    {
        return float4(0.0, 0.0, 0.0, 0.0);
    }

    // 横向粗筛：一行最多 16 个标签字符 + 分隔 + 约 40 个数值字符。
    if (pixelCoord.x < textOrigin.x || pixelCoord.x >= (textOrigin.x + (64 * step)))
    {
        return float4(0.0, 0.0, 0.0, 0.0);
    }

    DebugValueEntry entry = _HNRPDebugValues[row];
    uint header = entry.header;
    uint type = HN_DebugEntryType(header);
    if (type == DV_NONE)
    {
        return float4(0.0, 0.0, 0.0, 0.0);
    }

    float3 fontColor = _HNRPDebugFontColor.rgb;

    // 按分组层级缩进（每级 2 个字符宽）
    int indent = (int)HN_DebugEntryIndent(header);
    int2 cursor = int2(textOrigin.x + (indent * 2 * step), textOrigin.y + (row * lineHeight));

    // 标签（CPU 打包的 ASCII 下标，shader 解包后索引字模）
    uint labelChars = HN_DebugEntryLabelCharCount(header);
    uint stringBase = (uint)row * 4u;

    for (uint u = 0u; u < 4u; ++u)
    {
        uint packed = _HNRPDebugStrings[stringBase + u];
        for (uint b = 0u; b < 4u; ++b)
        {
            uint index = (u * 4u) + b;
            if (index >= labelChars)
            {
                break;
            }

            HN_DebugDrawCharacter(
                (packed >> (b * 8u)) & 0xFFu,
                fontColor,
                pixelCoord,
                cursor,
                color,
                alpha,
                1,
                DEBUG_FONT_TEXT_SCALE_WIDTH);
        }
    }

    // 分组标题行：只画 "分组名:"，本行结束。
    if (HN_DebugEntryIsGroupHeader(header))
    {
        HN_DebugDrawCharacter(
            58u, fontColor, pixelCoord, cursor, color, alpha, 1, DEBUG_FONT_TEXT_SCALE_WIDTH);
        return float4(color, alpha);
    }

    // 值行：分隔符 " : " 后跟数值
    HN_DebugDrawCharacter(58u, fontColor, pixelCoord, cursor, color, alpha, 1, DEBUG_FONT_TEXT_SCALE_WIDTH);
    HN_DebugDrawCharacter(32u, fontColor, pixelCoord, cursor, color, alpha, 1, DEBUG_FONT_TEXT_SCALE_WIDTH);

    uint4 raw = uint4(entry.v0, entry.v1, entry.v2, entry.v3);
    HN_DebugDrawEntryValue(
        type, raw, (int)HN_DebugEntryDecimalDigits(header), fontColor, pixelCoord, cursor, color, alpha);

    return float4(color, alpha);
}

// 计算当前像素在预览区内的归一化 UV（含上下翻转，使纹理正向显示）。
// 返回 false 表示该像素不在预览区内。
bool HN_DebugPreviewUV(int2 pixelCoord, out float2 uv)
{
    uv = float2(0.0, 0.0);

    float size = max(16.0, _HNRPDebugPreviewRect.x);
    float2 target = _HNRPDebugPreviewRect.zw;
    float margin = max(8.0, size * 0.05);

    // 右下角
    float2 topLeft = target - float2(size + margin, size + margin);
    float2 minPx = topLeft;
    float2 maxPx = topLeft + size;
    float2 pixel = float2(pixelCoord);

    if (pixel.x < minPx.x || pixel.x >= maxPx.x || pixel.y < minPx.y || pixel.y >= maxPx.y)
    {
        return false;
    }

    uv = (pixel - minPx) / size;
    uv.y = 1.0 - uv.y;
    return true;
}

#endif // HNRP_DEBUG_DISPLAY_INCLUDED
