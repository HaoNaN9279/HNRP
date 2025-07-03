Shader "HNRP/Lit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)

        [ToggleUI] _AlphaClip("Clip", Float) = 0.0
        _Cutoff("Aplha Cutoff", Range(0.0, 1.0)) = 0.5

        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
    }

    HLSLINCLUDE

    #pragma shader_feature_local _ALPHATEST_ON

    #include "Common.hlsl"

    ENDHLSL

    SubShader
    {
        Pass
        {
            Tags
            {
                "LightMode" = "Forward"
            }

            Blend[_SrcBlend][_DstBlend]
            // Cull Off
            // ZTest Always
            // ZClip False

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _Color;
            float _Cutoff;
            
            struct Attributes
            {
                float3 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.uv = v.uv;
                o.vertex = TransformObjectToHClip(v.vertex);
                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Color;

#if _ALPHATEST_ON
                clip(col.a - _Cutoff);
#endif

                return col;
            }
            ENDHLSL
        }
    }

    CustomEditor "HN.HNRP.Editor.LitGUI"
}
