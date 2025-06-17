Shader "Unlit/SingleBlitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        ZWrite Off ZTest Always Blend Off Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma multi_compile_local _ BLIT_DECODE_HDR
            #pragma vertex vert
            #pragma fragment frag

            #include "Common.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            struct Attributes
            {
                uint vertex : SV_VertexID;
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
                o.vertex = GetFullScreenTriangleVertexPosition(v.vertex);
                o.uv = GetQuadTexCoord(v.vertex);
                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                // float4 col = float4(0.8, 0.4, 0.5, 1);
                return float4(col.r, col.g, col.b, 1.0);
            }
            ENDHLSL
        }
    }
}
