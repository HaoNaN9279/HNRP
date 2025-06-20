Shader "Unlit/SingleBlitShader"
{
    Properties
    {
        _tex ("Texture", 2D) = "white" {}
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

            TEXTURE2D(_tex);
            // SAMPLER(sampler_MainTex);
            SamplerState sampler_PointClamp;
            float4 _testColor;
            
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
                o.uv = GetFullScreenTriangleTexCoord(v.vertex);
                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_tex, sampler_PointClamp, i.uv);
                // float4 col = float4(0.8, 0.4, 0.5, 1);
                return float4(col.x, col.y, col.z, 1.0);
                // return float4(_testColor.x, _testColor.y, _testColor.z, 1.0);
            }
            ENDHLSL
        }
    }
}
