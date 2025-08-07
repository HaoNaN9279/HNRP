Shader "Hidden/HNRP/SingleBlitShader"
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
            // #pragma multi_compile_local _ BLIT_DECODE_HDR
            #pragma vertex vert
            #pragma fragment frag

            #pragma enable_d3d11_debug_symbols

            #include "../Common.hlsl"

            TEXTURE2D(_tex);
            SAMPLER(sampler_tex);
            float4 _testColor;
            float _flip;

            uniform float4 _BlitScaleBias;
            
            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.uv = GetFullScreenTriangleTexCoord(v.vertexID);
                if(_flip > 0.5)
                    o.uv.y = 1 - o.uv.y;
                o.uv = o.uv * _BlitScaleBias.xy + _BlitScaleBias.zw;
                o.positionCS = GetFullScreenTriangleVertexPosition(v.vertexID);
                return o;
            }

// #define TEST(flag) \
//     #if (flag) \
//         const int test = 112233; \
//     #endif

// TEST(1)
            float4 frag (Varyings i) : SV_Target
            {

                float4 col = SAMPLE_TEXTURE2D(_tex, sampler_tex, i.uv);

                return float4(col.x, col.y, col.z, 1.0);
            }
            ENDHLSL
        }
    }
}
