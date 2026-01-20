#ifndef HNRP_UNITY_INPUT_INCLUDED
#define HNRP_UNITY_INPUT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_ON_SCREEN (16)
#define MAX_LOCAL_LIGHT_ON_SCREEN (512)
#define MAX_REFLECTION_PROBES_ON_SCREEN (64)
#define REFLECTION_PROBE_ATLAS_MIP_COUNT (8)

GLOBAL_CBUFFER_START(ShaderVariablesGlobal, b0) // Per Frame
    // Time (t = time since current level load) values from Unity
    float4 _Time; // (t/20, t, t*2, t*3)
    float4 _SinTime; // sin(t/8), sin(t/4), sin(t/2), sin(t)
    float4 _CosTime; // cos(t/8), cos(t/4), cos(t/2), cos(t)
    float4 unity_DeltaTime; // dt, 1/dt, smoothdt, 1/smoothdt
    float4 _TimeParameters; // t, sin(t), cos(t)

    float4 _ScreenSize;       // {w, h, 1/w, 1/h}

    float4 _WorldSpaceCameraPos;

    // x = 1 or -1 (-1 if projection is flipped)
    // y = near plane
    // z = far plane
    // w = 1/far plane
    float4 _ProjectionParams;

    // x = width
    // y = height
    // z = 1 + 1.0/width
    // w = 1 + 1.0/height
    float4 _ScreenParams;

    // Values used to linearize the Z buffer (http://www.humus.name/temp/Linearize%20depth.txt)
    // x = 1-far/near
    // y = far/near
    // z = x/far
    // w = y/far
    // or in case of a reversed depth buffer (UNITY_REVERSED_Z is 1)
    // x = -1+far/near
    // y = 1
    // z = x/far
    // w = 1/far
    float4 _ZBufferParams;

    // x = orthographic camera's width
    // y = orthographic camera's height
    // z = unused
    // w = 1.0 if camera is ortho, 0.0 if perspective
    float4 unity_OrthoParams;

    float4x4 unity_MatrixV;
    float4x4 unity_MatrixInvV;
    float4x4 glstate_matrix_projection;
    float4x4 unity_MatrixInvP;
    float4x4 unity_MatrixVP;
    float4x4 unity_MatrixInvVP;

    float4 _FrustumPlanes[6]; // {(a, b, c) = N, d = -dot(N, P)} [L, R, T, B, N, F]

    // x = main light index
    // y = light count
    // z = unused
    // w = unused
	float4 _LightConstantData;

    float4 _GlossyEnvironmentColor;
    float4 _GlossyEnvironmentCubeMap_HDR;
    float4 _SubtractiveShadowColor;
    float4 unity_AmbientSky;
    float4 unity_AmbientEquator;
    float4 unity_AmbientGround;

    // float4 glstate_lightmodel_ambient;
    // float4 unity_IndirectSpecColor;
    // float4 unity_FogParams;
    // float4 unity_FogColor;

    // float4 unity_ShadowColor;
CBUFFER_END

CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    // float4 unity_LODFade; // x is the fade value ranging within [0,1]. y is x quantized into 16 levels
    float4 unity_WorldTransformParams; // w is usually 1.0, or -1.0 for odd-negative scale transforms

    // Render Layer block feature
    // Only the first channel (x) contains valid data and the float must be reinterpreted using asuint() to extract the original 32 bits values.
    float4 unity_RenderingLayer;

    // Light Indices block feature
    // These are set internally by the engine upon request by RendererConfiguration.
    float4 unity_LightData;
    float4 unity_LightIndices[2];

    float4 unity_ProbesOcclusion;

    // Reflection Probe 0 block feature
    // HDR environment map decode instructions
    float4 unity_SpecCube0_HDR;
    float4 unity_SpecCube1_HDR;

    float4 unity_SpecCube0_BoxMax;          // w contains the blend distance
    float4 unity_SpecCube0_BoxMin;          // w contains the lerp value
    float4 unity_SpecCube0_ProbePosition;   // w is set to 1 for box projection
    float4 unity_SpecCube1_BoxMax;          // w contains the blend distance
    float4 unity_SpecCube1_BoxMin;          // w contains the sign of (SpecCube0.importance - SpecCube1.importance)
    float4 unity_SpecCube1_ProbePosition;   // w is set to 1 for box projection

    // Lightmap block feature
    float4 unity_LightmapST;
    float4 unity_DynamicLightmapST;

    // SH block feature
    float4 unity_SHAr;
    float4 unity_SHAg;
    float4 unity_SHAb;
    float4 unity_SHBr;
    float4 unity_SHBg;
    float4 unity_SHBb;
    float4 unity_SHC;

    // Renderer bounding box.
    float4 unity_RendererBounds_Min;
    float4 unity_RendererBounds_Max;

    // Velocity
    float4x4 unity_MatrixPreviousM;
    float4x4 unity_MatrixPreviousMI;
    //X : Use last frame positions (right now skinned meshes are the only objects that use this
    //Y : Force No Motion
    //Z : Z bias value
    //W : Camera only
    float4 unity_MotionVectorsParams;
CBUFFER_END

// Unity specific
TEXTURECUBE(unity_SpecCube0);
SAMPLER(samplerunity_SpecCube0);
TEXTURECUBE(unity_SpecCube1);
SAMPLER(samplerunity_SpecCube1);
TEXTURECUBE(_GlossyEnvironmentCubeMap);
SAMPLER(sampler_GlossyEnvironmentCubeMap);

// Reflection Probes Atlas
TEXTURE2D(_ReflectionProbeAtlas);
SAMPLER(sampler_ReflectionProbeAtlas);

// Main lightmap
TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);
TEXTURE2D_ARRAY(unity_Lightmaps);
SAMPLER(samplerunity_Lightmaps);

// Dynamic lightmap
// TEXTURE2D(unity_DynamicLightmap);
// SAMPLER(samplerunity_DynamicLightmap);

// Dual or directional lightmap (always used with unity_Lightmap, so can share sampler)
TEXTURE2D(unity_LightmapInd);
TEXTURE2D_ARRAY(unity_LightmapsInd);
TEXTURE2D(unity_DynamicDirectionality);
// TEXTURE2D_ARRAY(unity_DynamicDirectionality);

TEXTURE2D(unity_ShadowMask);
SAMPLER(samplerunity_ShadowMask);
TEXTURE2D_ARRAY(unity_ShadowMasks);
SAMPLER(samplerunity_ShadowMasks);

#define UNITY_MATRIX_M        unity_ObjectToWorld
#define UNITY_MATRIX_I_M      unity_WorldToObject
#define UNITY_MATRIX_V        unity_MatrixV
#define UNITY_MATRIX_I_V      unity_MatrixInvV
#define UNITY_MATRIX_P        OptimizeProjectionMatrix(glstate_matrix_projection)
#define UNITY_MATRIX_I_P      unity_MatrixInvP
#define UNITY_MATRIX_VP       unity_MatrixVP
#define UNITY_MATRIX_I_VP     unity_MatrixInvVP
#define UNITY_MATRIX_MV       mul(UNITY_MATRIX_V, UNITY_MATRIX_M)
#define UNITY_MATRIX_T_MV     transpose(UNITY_MATRIX_MV)
#define UNITY_MATRIX_IT_MV    transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V))
#define UNITY_MATRIX_MVP      mul(UNITY_MATRIX_VP, UNITY_MATRIX_M)
#define UNITY_PREV_MATRIX_M   unity_MatrixPreviousM
#define UNITY_PREV_MATRIX_I_M unity_MatrixPreviousMI

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonShadow.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/AreaLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/BSDF.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SphericalHarmonics.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/MetaPass.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"

#endif