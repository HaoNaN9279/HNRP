#ifndef HNRP_DEBUG_CHANNEL_IDS_INCLUDED
#define HNRP_DEBUG_CHANNEL_IDS_INCLUDED

// HNRP 渲染调试 —— 每像素通道 ID（跨基底共享）。
//
// 通道 ID 必须与 C# 侧 ForwardPass.DebugChannels 的声明顺序一致。
// 各基底的 *Debug.hlsl 只实现自己支持的通道子集，但 ID 取自本文件，保证
// Lit / Unlit 对同一通道的编号永不漂移。

#define HN_DEBUG_ALBEDO                0
#define HN_DEBUG_NORMAL_WS             1
#define HN_DEBUG_SMOOTHNESS            2
#define HN_DEBUG_METALLIC              3
#define HN_DEBUG_OCCLUSION             4
#define HN_DEBUG_EMISSION              5
#define HN_DEBUG_POSITION_WS           6
#define HN_DEBUG_MAIN_LIGHT_DIFFUSE    7
#define HN_DEBUG_MAIN_LIGHT_SPECULAR   8
#define HN_DEBUG_ADD_LIGHT_DIFFUSE     9
#define HN_DEBUG_INDIRECT_DIFFUSE      10
#define HN_DEBUG_INDIRECT_SPECULAR     11
#define HN_DEBUG_ALPHA                 12
#define HN_DEBUG_NDOTV                 13
#define HN_DEBUG_ROUGHNESS             14
#define HN_DEBUG_FINAL_COLOR           15
#define HN_DEBUG_CLUSTER_LIGHT_COUNT   16
#define HN_DEBUG_CLUSTER_PROBE_COUNT   17
#define HN_DEBUG_SHADOW_FILTER_RADIUS  18

#endif // HNRP_DEBUG_CHANNEL_IDS_INCLUDED
