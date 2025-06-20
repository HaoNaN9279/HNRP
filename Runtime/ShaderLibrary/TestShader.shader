Shader "Unlit/TestShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            Name "Forward"

            HLSLPROGRAM
            // #pragma multi_compile_instancing
            #pragma vertex vert
            #pragma fragment frag

            #include "Common.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _DefaultDrawColor;
            
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
                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
                // sample the texture
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                col = float4(_DefaultDrawColor.x, _DefaultDrawColor.y, _DefaultDrawColor.z, 1);
                return col;
            }
            ENDHLSL
        }
    }
}
