Shader "HNRP/Lit"
{
    Properties
    {
        _BaseMap ("BaseMap", 2D) = "white" {}
        _BaseColor ("Color", Color) = (0.5, 0.5, 0.5, 1.0)
        _AlphaRemapMin ("AlphaRemapMin", Float) = 0.0
        _AlphaRemapMax ("AlphaRemapMax", Float) = 1.0
        _MaskMap ("MaskMap", 2D) = "white" {}
        _Metallic ("Metallic", Range(0.0, 1.0)) = 0
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.5
        _MetallicRemapMin ("MetallicRemapMin", Float) = 0.0
        _MetallicRemapMax ("MetallicRemapMax", Float) = 1.0
        _SmoothnessRemapMin ("SmoothnessRemapMin", Float) = 0.0
        _SmoothnessRemapMax ("RoughmessRemapMax", Float) = 1.0
        _AORemapMin ("AORemapMin", Float) = 0.0
        _AORemapMax ("AORemapMax", Float) = 1.0
        _NormalMap ("NormalMap", 2D) = "bump" {}
        _NormalScale ("NormalScale", Range(0.0, 8.0)) = 1
        _EmissionMap ("EmissionMap", 2D) = "black" {}
        [HDR] _EmissionColor ("EmissionColor", Color) = (1.0, 1.0, 1.0, 1.0)

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
    }

    HLSLINCLUDE

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
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _BASEMAP
            #pragma shader_feature_local_fragment _MASKMAP
            #pragma shader_feature_local_fragment _EMISSIONMAP

            // #pragma enable_d3d11_debug_symbols

            #include "../Forward/ForwardPass.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "HN.HNRP.Editor.LitGUI"
}
