Shader "HNRP/Unlit"
{
    Properties
    {
        _BaseMap ("BaseMap", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _AlphaRemapMin ("AlphaRemapMin", Float) = 0.0
        _AlphaRemapMax ("AlphaRemapMax", Float) = 1.0

        [ToggleUI] _AlphaClip("Clip", Float) = 0.0
        _Cutoff("Aplha Cutoff", Range(0.0, 1.0)) = 0.5

        _SurfaceType("__surfacetype", Float) = 0.0
        _BlendMode("__blendmode", Float) = 0.0
        _SrcBlend("__src", Float) = 1.0
        _DstBlend("__dst", Float) = 0.0
        _SrcBlendAlpha("__srcA", Float) = 1.0
        _DstBlendAlpha("__dstA", Float) = 0.0
        _CullMode("__cullmode", Float) = 2
        _ZTestMode("__ztestmode", Float) = 4
        _ZWrite("__zwrite", Float) = 0
        _QueueOffset("__queueoffset", Float) = 0.0

        // 组合生成器在此注入各包参数属性（HNRP_PACKAGE_PROPERTY_ANCHOR）。
    }

    HLSLINCLUDE

        // 组合生成器在此注入组合头 include（HNRP_PACKAGE_INCLUDE_ANCHOR）。
        // 组合生成器在此注入各包局部关键字 pragma（HNRP_PACKAGE_KEYWORD_ANCHOR）。

    ENDHLSL

    SubShader
    {
        Pass
        {
            Tags
            {
                "LightMode" = "Forward"
            }

            Blend[_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
            Cull[_CullMode]
            ZTest[_ZTestMode]
            ZWrite[_ZWrite]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local _BASEMAP

            // 渲染调试：全局唯一 keyword，具体通道由 _HNRPDebugChannelId 选择
            #pragma multi_compile _ HN_DEBUG_PER_PIXEL

            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma enable_d3d11_debug_symbols

            #include "Assets/HNRP/Runtime/ShaderLibrary/Unlit/UnlitForward.hlsl"
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            // 阴影 atlas 为单通道 R16 深度图（无深度附件）：用 Min blend 直接保留最近投射者的深度
            // （深度单调，min 等价于取最近深度）。区域复位由 ShadowClear 覆盖写入 1。
            ZWrite Off
            ZTest Always
            Blend One One
            BlendOp Min
            ColorMask R
            Cull[_CullMode]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex ShadowCasterVert
            #pragma fragment ShadowCasterFrag

            // Material Keywords
            #pragma shader_feature_local _BASEMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma enable_d3d11_debug_symbols

            #include "Assets/HNRP/Runtime/ShaderLibrary/Unlit/UnlitShadowCaster.hlsl"
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            // 只写深度：无颜色输出（ColorMask 0），深度由 ZWrite 写入深度目标。
            ZWrite On
            ColorMask 0
            Cull[_CullMode]
            ZTest[_ZTestMode]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthOnlyVert
            #pragma fragment DepthOnlyFrag

            // Material Keywords
            #pragma shader_feature_local _BASEMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma enable_d3d11_debug_symbols

            #include "Assets/HNRP/Runtime/ShaderLibrary/Unlit/UnlitDepthOnly.hlsl"
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "DepthNormal"
            }

            // 深度写深度目标，法线写颜色目标（RGB），两者分离。
            ZWrite On
            ColorMask RGB
            Cull[_CullMode]
            ZTest[_ZTestMode]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthNormalVert
            #pragma fragment DepthNormalFrag

            // Material Keywords
            // keyword 集合与 Forward 的 UnityPerMaterial CBUFFER 对齐，保证 SRP Batcher 兼容。
            #pragma shader_feature_local _BASEMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma enable_d3d11_debug_symbols

            #include "Assets/HNRP/Runtime/ShaderLibrary/Unlit/UnlitDepthNormal.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "HN.HNRP.Editor.UnlitGUI"
}
