#ifndef HNRP_UNLIT_SHADOW_CASTER_INCLUDED
#define HNRP_UNLIT_SHADOW_CASTER_INCLUDED

#include "../Common/Common.hlsl"
#include "../Shadow/ShadowInput.hlsl"
#include "UnlitInput.hlsl"

// ShadowCaster pass 的最小顶点 / 片元实现：仅把几何体变换到光源裁剪空间。
// 深度由绘制侧直接绑定 atlas 的指定区域写入（不再依赖 DrawShadows 的阴影图绑定）。
// atlas 为无深度附件的 R16 颜色图，SetGlobalDepthBias 无效，自阴影抑制由写入端的
// normal bias + slope-scale depth bias 与采样端固定偏置共同承担（透视局部光为主）。
// alpha clip 与 Forward pass 一致，保证裁切体（树叶 / 草等）的阴影形状正确。
// 逻辑与 LitShadowCaster 相同，仅去掉光照相关的表面数据。

// 光源视图投影矩阵常量缓冲：由 DrawShadowPass 逐 map 上传。
// 不能使用 UNITY_MATRIX_VP：本工程 shader 的它来自自定义 ShaderVariablesGlobal（b0）
// 常量缓冲，内容始终是当前相机矩阵，不随引擎的 SetViewProjectionMatrices 变化。
GLOBAL_CBUFFER_START(_ShadowViewProjBuffer, b4)
    float4x4 _ShadowViewProj;
    // 矩图软阴影用：光源世界位置 + 1/range（方向光 w=0）。
    float4   _ShadowLightPosRange;
    // x=1 表示透视（局部光），0 表示正交（方向光）；y=normal bias（世界单位，仅局部光使用）。
    float4   _ShadowDepthFlags;
CBUFFER_END

struct UnlitShadowCasterAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
#if defined(_BASEMAP)
    float2 uv0 : TEXCOORD0;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct UnlitShadowCasterVaryings
{
    float4 positionCS : SV_POSITION;
#if defined(_BASEMAP)
    float2 uv0 : TEXCOORD0;
#endif
    float3 positionWS : TEXCOORD1;
    float3 normalWS : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

UnlitShadowCasterVaryings ShadowCasterVert(UnlitShadowCasterAttributes input)
{
    HNRP_PKG_EMIT_UNLIT_SHADOWCASTER_VERTEX_BEGIN();

    UnlitShadowCasterVaryings output;
    ZERO_INITIALIZE(UnlitShadowCasterVaryings, output);

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

    // normal bias（仅透视局部光）：只对背向光源的投射者沿法线外扩（覆盖薄面 / 背面，
    // 减少漏光）。朝光表面必须不外扩——无条件外扩会把投射者（如地面）沿法线推向光源，
    // 使其写入深度变小，反而让朝光面整体自遮挡（点光下表现为整个照射范围变暗）。
    // 自阴影本身由片元的 slope-scale depth bias 抑制。世界单位由 _ShadowDepthFlags.y 下发。
    if (_ShadowDepthFlags.x > 0.5)
    {
        float3 toSurface = positionWS - _ShadowLightPosRange.xyz;
        float3 lightDirWS = toSurface / max(length(toSurface), 1e-5);
        float nDotL = dot(normalWS, -lightDirWS);
        positionWS += normalWS * (_ShadowDepthFlags.y * saturate(-nDotL));
    }

    float4 positionCS = mul(_ShadowViewProj, float4(positionWS, 1.0));

    // Shadow pancaking（与 URP ShadowCasterPass 一致）：把落在光源近平面之前的投射者
    // 压到近平面，而不是交给硬件裁掉。视锥外、但投影落在视锥内的较大物体（如大建筑）
    // 会沿光方向越过近平面，若不钳制会被近平面部分裁剪，导致 atlas 中该物体残缺、阴影缺失。
    // 正交投影下近平面之前的投射者本就位于「光源 → 接收者」射线之间，压到最近深度语义正确。
    #if UNITY_REVERSED_Z
        positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
    #else
        positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
    #endif

    output.positionCS = positionCS;
#if defined(_BASEMAP)
    output.uv0 = TRANSFORM_TEX(input.uv0, _BaseMap);
#endif
    output.positionWS = positionWS;
    output.normalWS = normalWS;
    HNRP_PKG_EMIT_UNLIT_SHADOWCASTER_VERTEX_AFTERVERTEX(output);
    HNRP_PKG_EMIT_UNLIT_SHADOWCASTER_VERTEX_END();
    return output;
}

half4 ShadowCasterFrag(UnlitShadowCasterVaryings input) : SV_Target
{
    HNRP_PKG_EMIT_UNLIT_SHADOWCASTER_FRAGMENT_BEGIN();

#if defined(_ALPHATEST_ON)
#if defined(_BASEMAP)
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv0);
    float alpha = RemapFrom01(baseMap.a, _AlphaRemapMin, _AlphaRemapMax) * _BaseColor.a;
#else
    float alpha = _BaseColor.a;
#endif
    clip(alpha - _Cutoff);
#endif

    // 单通道深度图：只写归一化线性深度（0=近光，1=远光），
    // 与采样端 GetReceiverLinearDepth 严格同式；由 BlendOp Min 保留最近投射者。
    // 注意：变量不能命名为 linear —— 它是 HLSL 保留字（插值修饰符）。
    float linearDepth;
    if (_ShadowDepthFlags.x > 0.5)
    {
        // 透视局部光：到光源的距离 / range。
        float3 toLight = input.positionWS - _ShadowLightPosRange.xyz;
        float lightDistance = length(toLight);
        linearDepth = lightDistance * _ShadowLightPosRange.w;

        // slope-scale depth bias（写入端）：按投射者法线与光源方向的夹角放大深度偏置。
        // 掠射（NdotL → 0）时单 texel 的深度跨度剧增，固定偏置不足；此处把写入深度往远推
        // （超出采样端固定偏置的部分），抵消由此产生的自阴影。slope 上限避免过度偏移。
        float3 lightDir = toLight / max(lightDistance, 1e-5);
        float ndotl = abs(dot(normalize(input.normalWS), -lightDir));
        float slope = min(1.0 / max(ndotl, 1e-3), 8.0);
        linearDepth += _ShadowDepthBias * (slope - 1.0);
    }
    else
    {
        // 正交方向光：裁剪 z 已是线性；平台 reversed-Z 时翻转到 near=0 / far=1。
        linearDepth = input.positionCS.z;
#if UNITY_REVERSED_Z
        linearDepth = 1.0 - linearDepth;
#endif
    }

    linearDepth = saturate(linearDepth);
    HNRP_PKG_EMIT_UNLIT_SHADOWCASTER_FRAGMENT_END();
    return half4(linearDepth, 0.0, 0.0, 1.0);
}

#endif
