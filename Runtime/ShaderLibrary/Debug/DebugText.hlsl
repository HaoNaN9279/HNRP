#ifndef HNRP_DEBUG_TEXT_INCLUDED
#define HNRP_DEBUG_TEXT_INCLUDED

// HNRP 渲染调试 —— GPU 端文本绘制。
//
// 设计参考 HDRP Runtime/Debug/DebugDisplay.hlsl（DrawCharacter / DrawInteger /
// DrawFloatExplicitPrecision），并按本项目的字模布局重写了坐标映射：
//   · 字模图集 256×128，16×16 字块，16 列 × 8 行，覆盖 ASCII 32..126。
//   · 屏幕坐标（pixelCoord / cursor）以「像素、原点在左上、y 向下」为单位。
//   · 字块内屏幕第 y 行对应图集内第 (15 - y) 行（图集按 UV 空间 y 向上书写）。
// 纯片元着色器实现：无几何、无回读、无 CPU 参与。

TEXTURE2D(_HNRPDebugFont);
float4 _HNRPDebugTextOrigin;
float  _HNRPDebugFontScale;
float4 _HNRPDebugFontColor;

#define DEBUG_FONT_TEXT_WIDTH       16
#define DEBUG_FONT_TEXT_HEIGHT      16
#define DEBUG_FONT_TEXT_COUNT_X     16
#define DEBUG_FONT_TEXT_COUNT_Y     8
#define DEBUG_FONT_TEXT_ASCII_START 32
#define DEBUG_FONT_TEXT_SCALE_WIDTH 12
#define DEBUG_FONT_TEXT_ATLAS_W     256
#define DEBUG_FONT_TEXT_ATLAS_H     128

// 常用 ASCII（避免 HLSL 中直接书写字符字面量）
#define DEBUG_CHAR_0     48u
#define DEBUG_CHAR_MINUS 45u
#define DEBUG_CHAR_DOT   46u
#define DEBUG_CHAR_LP    40u
#define DEBUG_CHAR_RP    41u
#define DEBUG_CHAR_COMMA 44u
#define DEBUG_CHAR_E     69u
#define DEBUG_CHAR_e     101u
#define DEBUG_CHAR_N     78u
#define DEBUG_CHAR_a     97u

// 当前缩放下的字符格边长（像素）
int HN_DebugCellSize()
{
    return max(DEBUG_FONT_TEXT_WIDTH, (int)(DEBUG_FONT_TEXT_WIDTH * _HNRPDebugFontScale));
}

// 当前缩放下的字符步进（像素）
int HN_DebugStepWidth(int stepWidth)
{
    return max(2, (int)(stepWidth * _HNRPDebugFontScale));
}

// 绘制单个字符；命中字块时覆盖 color / alpha，并把光标前进一格。
// direction 为 +1（自左向右书写）或 -1（自右向左书写，用于从低位到高位输出数字）。
void HN_DebugDrawCharacter(uint asciiValue, float3 fontColor, int2 pixelCoord,
                           inout int2 cursor, inout float3 color, inout float alpha,
                           int direction, int stepWidth)
{
    int cell = HN_DebugCellSize();
    int2 local = pixelCoord - cursor;

    if (local.x >= 0 && local.x < cell && local.y >= 0 && local.y < cell)
    {
        // 最近邻放大到 16×16 字块坐标
        uint2 glyphLocal;
        glyphLocal.x = (uint)local.x * (uint)DEBUG_FONT_TEXT_WIDTH / (uint)cell;
        glyphLocal.y = (uint)local.y * (uint)DEBUG_FONT_TEXT_HEIGHT / (uint)cell;

        uint glyph = asciiValue - (uint)DEBUG_FONT_TEXT_ASCII_START;
        uint2 blockOrigin = uint2(
            glyph % (uint)DEBUG_FONT_TEXT_COUNT_X,
            glyph / (uint)DEBUG_FONT_TEXT_COUNT_X) * (uint)DEBUG_FONT_TEXT_WIDTH;

        uint2 texel = blockOrigin + uint2(
            glyphLocal.x,
            ((uint)DEBUG_FONT_TEXT_HEIGHT - 1u) - glyphLocal.y);

        float2 uv = (float2(texel) + 0.5)
            / float2((float)DEBUG_FONT_TEXT_ATLAS_W, (float)DEBUG_FONT_TEXT_ATLAS_H);

        // 字模遮罩存放在 alpha 通道（alpha 不受 sRGB 转换影响）。
        float mask = SAMPLE_TEXTURE2D_LOD(_HNRPDebugFont, sampler_PointClamp, uv, 0).a;

        color = lerp(color, fontColor, mask);
        alpha = max(alpha, mask);
    }

    cursor.x += direction * HN_DebugStepWidth(stepWidth);
}

// 绘制一个字符（默认自左向右，使用标准步进）
void HN_DebugDrawCharacter(uint asciiValue, float3 fontColor, int2 pixelCoord,
                           inout int2 cursor, inout float3 color, inout float alpha)
{
    HN_DebugDrawCharacter(
        asciiValue, fontColor, pixelCoord, cursor, color, alpha, 1, DEBUG_FONT_TEXT_SCALE_WIDTH);
}

// 绘制有符号整数。
// leading0 用于补齐小数部分的前导零；forceNegativeSign 用于显示负零。
void HN_DebugDrawInteger(int intValue, float3 fontColor, int2 pixelCoord,
                         inout int2 cursor, inout float3 color, inout float alpha,
                         int leading0, bool forceNegativeSign)
{
    const uint maxStringSize = 12u;
    const int step = DEBUG_FONT_TEXT_SCALE_WIDTH;

    uint absIntValue = (uint)abs(intValue);
    int numEntries = min(
        (int)((intValue == 0 ? 0.0 : log10((float)absIntValue))
            + ((intValue < 0 || forceNegativeSign) ? 1 : 0)
            + leading0),
        (int)maxStringSize);

    cursor.x += numEntries * HN_DebugStepWidth(step);

    bool drawCharacter = true;
    for (uint j = 0u; j < maxStringSize; ++j)
    {
        if (drawCharacter)
        {
            HN_DebugDrawCharacter(
                (absIntValue % 10u) + DEBUG_CHAR_0,
                fontColor, pixelCoord, cursor, color, alpha, -1, step);
        }

        if (absIntValue < 10u)
        {
            drawCharacter = false;
        }

        absIntValue /= 10u;
    }

    for (int i = 0; i < leading0; ++i)
    {
        HN_DebugDrawCharacter(DEBUG_CHAR_0, fontColor, pixelCoord, cursor, color, alpha, -1, step);
    }

    if (intValue < 0 || forceNegativeSign)
    {
        HN_DebugDrawCharacter(DEBUG_CHAR_MINUS, fontColor, pixelCoord, cursor, color, alpha, -1, step);
    }

    cursor.x += (numEntries + 2) * HN_DebugStepWidth(step);
}

// 绘制无符号整数。
void HN_DebugDrawUInteger(uint uintValue, float3 fontColor, int2 pixelCoord,
                          inout int2 cursor, inout float3 color, inout float alpha)
{
    const uint maxStringSize = 12u;
    const int step = DEBUG_FONT_TEXT_SCALE_WIDTH;

    uint absValue = uintValue;
    int digits = (uintValue == 0u) ? 1 : ((int)floor(log10((float)uintValue)) + 1);
    int numEntries = min(digits - 1, (int)maxStringSize);

    cursor.x += numEntries * HN_DebugStepWidth(step);

    bool drawCharacter = true;
    for (uint j = 0u; j < maxStringSize; ++j)
    {
        if (drawCharacter)
        {
            HN_DebugDrawCharacter(
                (absValue % 10u) + DEBUG_CHAR_0,
                fontColor, pixelCoord, cursor, color, alpha, -1, step);
        }

        if (absValue < 10u)
        {
            drawCharacter = false;
        }

        absValue /= 10u;
    }

    cursor.x += (numEntries + 2) * HN_DebugStepWidth(step);
}

// 绘制十六进制无符号整数（8 位，位掩码 / 索引查看友好）。
void HN_DebugDrawHex(uint value, float3 fontColor, int2 pixelCoord,
                     inout int2 cursor, inout float3 color, inout float alpha)
{
    const int step = DEBUG_FONT_TEXT_SCALE_WIDTH;

    // 8 个十六进制位从高位到低位，自右向左推进光标。
    cursor.x += 8 * HN_DebugStepWidth(step);

    for (int i = 0; i < 8; ++i)
    {
        uint nibble = (value >> (uint)(i * 4)) & 0xFu;
        uint ascii = nibble < 10u ? (nibble + DEBUG_CHAR_0) : (nibble - 10u + 65u);
        HN_DebugDrawCharacter(ascii, fontColor, pixelCoord, cursor, color, alpha, -1, step);
    }

    cursor.x += 10 * HN_DebugStepWidth(step);
}

// 绘制浮点（显式小数位数）。
void HN_DebugDrawFloat(float floatValue, int digitCount, float3 fontColor, int2 pixelCoord,
                       inout int2 cursor, inout float3 color, inout float alpha)
{
    const int step = DEBUG_FONT_TEXT_SCALE_WIDTH;
    digitCount = clamp(digitCount, 0, 7);

    // 非有限值检测：NaN 与自身不等；Inf 大于 float 最大值。
    // 用比较而非 isnan / isinf 内建，避免个别平台 HLSL 编译器的差异。
    bool nonFinite = !(floatValue == floatValue) || (abs(floatValue) > 3.402823466e38);

    if (nonFinite)
    {
        HN_DebugDrawCharacter(DEBUG_CHAR_N, fontColor, pixelCoord, cursor, color, alpha, 1, step);
        HN_DebugDrawCharacter(DEBUG_CHAR_a, fontColor, pixelCoord, cursor, color, alpha, 1, step);
        HN_DebugDrawCharacter(DEBUG_CHAR_N, fontColor, pixelCoord, cursor, color, alpha, 1, step);
        return;
    }

    // 超出定点精度范围时退化为 m e±exp 形式，避免 int 转换溢出。
    int exponent = 0;
    float value = floatValue;
    float magnitude = abs(floatValue);
    if (magnitude >= 100000000.0)
    {
        exponent = (int)floor(log10(magnitude));
        value = floatValue / pow(10.0, (float)exponent);
    }

    int intValue = (int)value;
    bool forceNegativeSign = value >= 0.0 ? false : true;

    HN_DebugDrawInteger(intValue, fontColor, pixelCoord, cursor, color, alpha, 0, forceNegativeSign);
    HN_DebugDrawCharacter(DEBUG_CHAR_DOT, fontColor, pixelCoord, cursor, color, alpha, 1, step);

    int fracValue = (int)(frac(abs(value)) * pow(10.0, (float)digitCount));
    int leading0 = digitCount - ((fracValue <= 0 ? 0 : (int)log10((float)fracValue)) + 1);
    HN_DebugDrawInteger(
        fracValue, fontColor, pixelCoord, cursor, color, alpha, max(0, leading0), false);

    if (exponent != 0)
    {
        HN_DebugDrawCharacter(DEBUG_CHAR_e, fontColor, pixelCoord, cursor, color, alpha, 1, step);
        HN_DebugDrawInteger(exponent, fontColor, pixelCoord, cursor, color, alpha, 0, false);
    }
}

// 绘制布尔（true / false）。
void HN_DebugDrawBool(uint value, float3 fontColor, int2 pixelCoord,
                      inout int2 cursor, inout float3 color, inout float alpha)
{
    const int step = DEBUG_FONT_TEXT_SCALE_WIDTH;

    if (value != 0u)
    {
        HN_DebugDrawCharacter(116u, fontColor, pixelCoord, cursor, color, alpha, 1, step); // t
        HN_DebugDrawCharacter(114u, fontColor, pixelCoord, cursor, color, alpha, 1, step); // r
        HN_DebugDrawCharacter(117u, fontColor, pixelCoord, cursor, color, alpha, 1, step); // u
        HN_DebugDrawCharacter(101u, fontColor, pixelCoord, cursor, color, alpha, 1, step); // e
    }
    else
    {
        HN_DebugDrawCharacter(102u, fontColor, pixelCoord, cursor, color, alpha, 1, step); // f
        HN_DebugDrawCharacter(97u, fontColor, pixelCoord, cursor, color, alpha, 1, step);  // a
        HN_DebugDrawCharacter(108u, fontColor, pixelCoord, cursor, color, alpha, 1, step); // l
        HN_DebugDrawCharacter(115u, fontColor, pixelCoord, cursor, color, alpha, 1, step); // s
        HN_DebugDrawCharacter(101u, fontColor, pixelCoord, cursor, color, alpha, 1, step); // e
    }
}

#endif // HNRP_DEBUG_TEXT_INCLUDED
