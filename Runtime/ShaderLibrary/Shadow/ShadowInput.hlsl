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
    float    _ShadowSoftness;             // PCSS 软度系数（乘在滤波半径上）
    float    _ShadowMinFilterRadius;      // PCSS 最小滤波半径（texel）
    float    _ShadowMaxFilterRadius;      // PCSS 最大滤波半径（texel，同时约束 blocker 搜索核上限）
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

#if defined(SCREEN_SPACE_SHADOW_MAP)
// 屏幕空间阴影 mask：由 ScreenSpaceShadowMaskPass 用 ComputeShader 产出，
// ForwardPass 在绘制前经全局纹理绑定。单通道，1 = 完全受光，0 = 被任一投影光源完全遮挡。
TEXTURE2D(_ScreenSpaceShadowMask);

/// 采样本像素的合并屏幕空间阴影可见性。
/// <remarks>
/// mask 由 <c>ScreenSpaceShadowMaskCS</c> 以「纹理原始行」写入 —— 输出纹素第 r 行存的是
/// 第 r 行片元的阴影，与深度附件同向。而 <c>GetNormalizedScreenSpaceUV</c> 在
/// <c>UNITY_UV_STARTS_AT_TOP</c>（D3D）下会翻转 Y，直接用它采样会取到镜像行、
/// 导致阴影上下颠倒，故此处翻回原始行列后再采样。
/// </remarks>
float SampleScreenSpaceShadowMask(float2 screenUV)
{
    float2 maskUV = screenUV;
#if UNITY_UV_STARTS_AT_TOP
    maskUV.y = 1.0 - maskUV.y;
#endif
    return SAMPLE_TEXTURE2D(_ScreenSpaceShadowMask, sampler_LinearClamp, maskUV).r;
}
#endif

#endif