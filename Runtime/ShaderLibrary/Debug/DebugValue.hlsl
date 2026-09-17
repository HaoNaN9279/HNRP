#ifndef HNRP_DEBUG_VALUE_INCLUDED
#define HNRP_DEBUG_VALUE_INCLUDED

// HNRP 渲染调试 —— 单值条目定义与只读访问。
//
// 类型编码必须与 C# 侧 DebugValueType 一一对应；
// 条目布局必须与 C# 侧 DebugValueCodec（stride 24 字节）一致。
// 刻意全部使用 uint 字段：HLSL 中 uint4 具有 16 字节对齐，若把值写成 uint4
// 则结构体会被补齐到 32 字节，与 C# 侧的 24 字节 stride 不匹配。

#define DV_NONE    0u
#define DV_UINT    1u
#define DV_INT     2u
#define DV_FLOAT   3u
#define DV_UINT2   4u
#define DV_INT2    5u
#define DV_FLOAT2  6u
#define DV_UINT3   7u
#define DV_INT3    8u
#define DV_FLOAT3  9u
#define DV_UINT4   10u
#define DV_INT4    11u
#define DV_FLOAT4  12u
#define DV_BOOL    13u

// header 位布局（与 DebugValueCodec.PackHeader 对应）
#define DV_TYPE_MASK            0x7Fu
#define DV_LABEL_COUNT_SHIFT    8u
#define DV_LABEL_COUNT_MASK     0xFFu
#define DV_DECIMAL_SHIFT        16u
#define DV_DECIMAL_MASK         0xFu
#define DV_INDENT_SHIFT         20u
#define DV_INDENT_MASK          0xFu
#define DV_GROUP_HEADER         (1u << 24)

struct DebugValueEntry
{
    uint header;    // type | labelCharCount << 8 | decimalDigits << 16
    uint tag;       // 保留（标签前 4 字符的 ASCII 打包）
    uint v0;
    uint v1;
    uint v2;
    uint v3;
};

// 只读视图（显示 pass）
StructuredBuffer<DebugValueEntry> _HNRPDebugValues;

// 标签缓冲：每条目占 4 个 uint（16 个字符，低字节在前）
StructuredBuffer<uint> _HNRPDebugStrings;

// header 解包
uint HN_DebugEntryType(uint header)
{
    return header & DV_TYPE_MASK;
}

uint HN_DebugEntryLabelCharCount(uint header)
{
    return (header >> DV_LABEL_COUNT_SHIFT) & DV_LABEL_COUNT_MASK;
}

uint HN_DebugEntryDecimalDigits(uint header)
{
    return (header >> DV_DECIMAL_SHIFT) & DV_DECIMAL_MASK;
}

// 分组缩进层级（0 = 顶层值行 / 一级分组标题）
uint HN_DebugEntryIndent(uint header)
{
    return (header >> DV_INDENT_SHIFT) & DV_INDENT_MASK;
}

// 本行是否为分组标题行（只画 "分组名:"，不画数值）
bool HN_DebugEntryIsGroupHeader(uint header)
{
    return (header & DV_GROUP_HEADER) != 0u;
}

// 类型 → 分量数量
uint HN_DebugComponentCount(uint type)
{
    if (type == DV_BOOL) return 1u;
    if (type <= DV_FLOAT) return 1u;
    if (type <= DV_FLOAT2) return 2u;
    if (type <= DV_FLOAT3) return 3u;
    if (type <= DV_FLOAT4) return 4u;
    return 1u;
}

// 类型是否属于无符号整数族（uint / uint2 / uint3 / uint4）
bool HN_DebugIsUnsigned(uint type)
{
    return type == DV_UINT || type == DV_UINT2 || type == DV_UINT3 || type == DV_UINT4;
}

// 类型是否属于浮点族（float / float2 / float3 / float4）
bool HN_DebugIsFloat(uint type)
{
    return type == DV_FLOAT || type == DV_FLOAT2 || type == DV_FLOAT3 || type == DV_FLOAT4;
}

// 类型是否为有符号整数族（int / int2 / int3 / int4）
bool HN_DebugIsSigned(uint type)
{
    return type == DV_INT || type == DV_INT2 || type == DV_INT3 || type == DV_INT4;
}

#endif // HNRP_DEBUG_VALUE_INCLUDED
