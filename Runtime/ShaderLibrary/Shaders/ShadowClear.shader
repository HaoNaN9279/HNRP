Shader "Hidden/HNRP/ShadowClear"
{
    SubShader
    {
        Pass
        {
            ZWrite On
            ZTest Always
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            // 全屏三角形；把深度写到远平面，配合 viewport 局部清空阴影图区域。
            // 阴影图采用非 reversed-Z 深度约定（near=0 / far=1），与比较采样器的
            // 语义一致，故远平面恒写 1，不随平台 UNITY_REVERSED_Z 变化。
            float4 vert(uint vertexID : SV_VertexID) : SV_POSITION
            {
                float4 positionCS = GetFullScreenTriangleVertexPosition(vertexID);
                positionCS.z = 1.0;
                return positionCS;
            }

            half4 frag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
