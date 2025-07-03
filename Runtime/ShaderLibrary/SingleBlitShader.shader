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
            // #pragma multi_compile_local _ BLIT_DECODE_HDR
            #pragma vertex vert
            #pragma fragment frag

            #include "Common.hlsl"

            TEXTURE2D(_tex);
            SAMPLER(sampler_tex);
            float4 _testColor;
            float _flip;
            
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
                o.positionCS = GetFullScreenTriangleVertexPosition(v.vertexID);
                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
                if(_flip > 0.5)
                    i.uv.y = 1 - i.uv.y;

                float4 col = SAMPLE_TEXTURE2D(_tex, sampler_tex, i.uv);

                return float4(col.x, col.y, col.z, 1.0);
            }
            ENDHLSL
        }
    }
}
