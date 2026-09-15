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
            // 远平面取值随平台阴影图深度约定（DrawShadowPass.RemapZToShadowMap 同步）：
            // reversed-Z 平台（UNITY_REVERSED_Z=1）near=1 / far=0，否则 near=0 / far=1。
            float4 vert(uint vertexID : SV_VertexID) : SV_POSITION
            {
                float4 positionCS = GetFullScreenTriangleVertexPosition(vertexID);
#if UNITY_REVERSED_Z
                positionCS.z = 0.0;
#else
                positionCS.z = 1.0;
#endif
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
