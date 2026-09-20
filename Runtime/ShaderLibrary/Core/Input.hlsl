#ifndef HNRP_INPUT_INCLUDED
#define HNRP_INPUT_INCLUDED

#include "./UnityInput.hlsl"

#define MAX_DIRECTIONAL_LIGHT_ON_SCREEN (16)
#define MAX_LOCAL_LIGHT_ON_SCREEN (512)

#define MAX_REFLECTION_PROBE_MASK_WORDS (16384)
#define MAX_REFLECTION_PROBES_ON_SCREEN (64)
#define REFLECTION_PROBE_ATLAS_TEXEL_PADDING (2)

// Light Data Structure
struct LightData
{
    float3 positionWS;
    uint lightType;
    float3 color;
    float range;
    float4 attenuation;
    float3 directionWS;
    uint renderingLayerMask;
};

// Light Data Buffer
StructuredBuffer<LightData> _LightDatasBuffer;

#if CLUSTER_CULLING_REFLECTION_PROBE

struct ClusterCullingReflectionProbeDatas
{
    float3 boxMax;
    float blendDistance;
    float3 boxMin;
    float importance;
    float3 positionWS;
    float intensity;
    float4 scaleOffset;
    float mipCount;
    float sliceIndex;
    float2 unused;
};

StructuredBuffer<ClusterCullingReflectionProbeDatas> _ClusterCullingReflectionProbeDatasBuffer;

GLOBAL_CBUFFER_START(_ClusterCullingReflectionProbeParamsBuffer, b2)
    float2 _ClusterCullingReflectionProbeClusterSizeXY;
    float2 _ClusterCullingReflectionProbeClusterZScaleOffset;
    int _ClusterCullingReflectionProbeWordsPerCluster;
    int _ClusterCullingReflectionProbeReflectionProbeCount;
    float _ClusterCullingReflectionProbeAtlasSize;
    float _ClusterCullingReflectionProbeAtlasSliceCount;
CBUFFER_END

#define _CLUSTER_CULLING_REFLECTION_PROBE_XY_SCALE (_ClusterCullingReflectionProbeClusterSizeXY.xy)
#define _CLUSTER_CULLING_REFLECTION_PROBE_Z_SCALE (_ClusterCullingReflectionProbeClusterZScaleOffset.x)
#define _CLUSTER_CULLING_REFLECTION_PROBE_Z_OFFSET (_ClusterCullingReflectionProbeClusterZScaleOffset.y)
#define _CLUSTER_CULLING_REFLECTION_PROBE_WORDS_PER_CLUSTER (_ClusterCullingReflectionProbeWordsPerCluster)
#define _CLUSTER_CULLING_REFLECTION_PROBE_COUNT (_ClusterCullingReflectionProbeReflectionProbeCount)
#define _CLUSTER_CULLING_REFLECTION_PROBE_ATLAS_SIZE (_ClusterCullingReflectionProbeAtlasSize)

StructuredBuffer<uint> _ClusterCullingReflectionProbeMaskBuffer;

TEXTURE2D_ARRAY(_ReflectionProbeAtlas);

#endif

#if CLUSTER_CULLING_LIGHT

GLOBAL_CBUFFER_START(_ClusterCullingLightParamsBuffer, b3)
    float2 _ClusterCullingLightClusterSize;
    float2 _ClusterCullingLightClusterZScaleOffset;
    int _ClusterCullingLightWordsPerCluster;
    int _ClusterCullingLightDirectionalLightCount;
    int _ClusterCullingLightLocalLightCount;
    int _ClusterCullingLightMainLightIndex;
CBUFFER_END

#define _CLUSTER_CULLING_LIGHT_XY_SCALE (_ClusterCullingLightClusterSize)
#define _CLUSTER_CULLING_LIGHT_Z_SCALE (_ClusterCullingLightClusterZScaleOffset.x)
#define _CLUSTER_CULLING_LIGHT_Z_OFFSET (_ClusterCullingLightClusterZScaleOffset.y)
#define _CLUSTER_CULLING_LIGHT_WORDS_PER_CLUSTER (_ClusterCullingLightWordsPerCluster)
#define _CLUSTER_CULLING_LIGHT_DIRECTIONAL_LIGHT_COUNT (_ClusterCullingLightDirectionalLightCount)
#define _CLUSTER_CULLING_LIGHT_LOCAL_LIGHT_COUNT (_ClusterCullingLightLocalLightCount)
#define _CLUSTER_CULLING_LIGHT_MAIN_LIGHT_INDEX (_ClusterCullingLightMainLightIndex)

StructuredBuffer<uint> _ClusterCullingLightMaskBuffer;

#endif

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
    float    _ShadowSoftness;             // 半影尺寸全局乘子
    float    _ShadowMinFilterRadius;      // 最小滤波半径（texel）
    float    _ShadowMaxFilterRadius;      // 全局最大滤波半径（texel）
    float    _ShadowBlockerSampleCount;   // blocker 搜索采样数
    float    _ShadowFilterSampleCount;    // PCF 滤波采样数
    float    _ShadowCascadeBlend;         // 方向光 cascade 级间衔接过渡区宽度（占分割距离百分比）
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