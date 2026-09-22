#ifndef HNRP_SHADOW_INPUT_INCLUDED
#define HNRP_SHADOW_INPUT_INCLUDED

// ── GPU 数据布局（须与 C# DrawShadowPass.cs 一致）──

/// 按 light 的阴影元数据（两级查表第一级）。
struct ShadowLightData
{
    float4 lightPosRange;        // xyz=光源世界位置，w=1/range（方向光为 0）
    int    lightIndex;           // = buffer 下标
    int    resolution;           // 单张 map 分辨率；0 = 该 light 无阴影
    uint   blockDatas0;          // map 0..3 的 atlas 位置，每张 8 位 = (slice << 6) | blockId
    uint   blockDatas1;          // map 4..7 的 atlas 位置
    uint   typeAndCascadeCount;  // bit0..7=lightType, bit8..15=cascadeCount
    int    cameraDataIndex;      // 方向光：_ShadowCameraDatas 下标；其他类型无意义
};

/// 每方向光阴影参数（按方向光槽位索引）：方向光 cascade 的各级远边界（沿相机视轴深度）。
struct ShadowCameraData
{
    float4 cascadeSplits0;       // cascade 0..3
    float4 cascadeSplits1;       // cascade 4..7
};

/// 按 atlas 槽的阴影数据（两级查表第二级）。buffer 下标 = atlas 位置字段。
struct ShadowMapData
{
    float4   shadowParams;   // x=strength, y=soft(0/1), z=cascadeIndex/faceIndex, w=penumbraRadius
    float4x4 worldToShadow;  // 世界 → 阴影裁剪（含 [0,1] remap，不含 atlas offset）
};

// 阴影全局标量常量缓冲（b1）。
// 布局按 16 字节对齐：共 8 个 float。
GLOBAL_CBUFFER_START(_ShadowMapParamsBuffer, b1)
    float    _ShadowSliceResolution;      // atlas 单 slice 分辨率
    float    _ShadowDepthBias;            // 接收者深度偏置（归一化线性深度单位）
    float    _ShadowBlockerSampleCount;   // blocker 搜索采样数
    float    _ShadowFilterSampleCount;    // PCF 滤波采样数
    float    _ShadowCascadeBlend;         // 方向光 cascade 级间衔接过渡区宽度（占分割距离百分比）
    float    _ShadowParamUnused0;
    float    _ShadowParamUnused1;
    float    _ShadowParamUnused2;
CBUFFER_END

#define _SHADOW_SLICE_RESOLUTION (_ShadowSliceResolution)

#if defined(SHADOW_MAP)
StructuredBuffer<ShadowLightData> _ShadowLightDatas;
StructuredBuffer<ShadowMapData> _ShadowMapDatas;
StructuredBuffer<ShadowCameraData> _ShadowCameraDatas;
// 阴影图集为单张 R16 深度图（仅归一化线性深度，无深度平方、无 mip）。
// Hard 模式使用 sampler_LinearClamp 3x3
// PCSS 采样用 CoreRP 全局 sampler_PointClamp（逐 texel 精确比较）。
TEXTURE2D_ARRAY(_ShadowMapArray);
#endif

#endif