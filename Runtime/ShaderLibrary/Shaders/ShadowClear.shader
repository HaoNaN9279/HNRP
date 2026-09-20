Shader "Hidden/HNRP/ShadowClear"
{
    SubShader
    {
        Pass
        {
            // 阴影 atlas 为单通道 R16 深度图（无深度附件）：本 pass 用覆盖写把区域复位为远深度 1。
            // 覆盖写（无 Blend）是必须的——投射者 pass 用 BlendOp Min，若这里也用 Min 则无法复位。
            ZWrite Off
            ZTest Always
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            // 全屏三角形，配合 viewport 局部复位阴影图区域。
            float4 vert(uint vertexID : SV_VertexID) : SV_POSITION
            {
                return GetFullScreenTriangleVertexPosition(vertexID);
            }

            half4 frag() : SV_Target
            {
                // 远深度：线性深度 1（0=近光，1=远光）。
                // 背景区域按「无遮挡物」参与比较，必须复位为远平面。
                return half4(1.0, 0.0, 0.0, 1.0);
            }
            ENDHLSL
        }
    }
}
