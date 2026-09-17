Shader "Hidden/HNRP/DebugDisplay"
{
    HLSLINCLUDE

        #pragma target 4.5
        #pragma editor_sync_compilation
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

        #include "../Common/Common.hlsl"
        #include "../Debug/DebugDisplay.hlsl"

    ENDHLSL

    SubShader
    {
        // 0: 单值文本叠层
        // 读 _HNRPDebugValues（值）与 _HNRPDebugStrings（打包标签），
        // 用 _HNRPDebugFont 逐字符覆盖颜色；不读取目标颜色，叠加交给固定管线混合。
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
                #pragma vertex DebugDisplayVert
                #pragma fragment HN_DebugFragmentText
            ENDHLSL
        }

        // 1: 纹理预览（2D / Tex2DArray 的单层视图由 C# 侧选择 pass）
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
                #pragma vertex DebugDisplayVert
                #pragma fragment HN_DebugFragmentPreview2D

                TEXTURE2D(_HNRPDebugPreview);

                float4 HN_DebugFragmentPreview2D(DebugDisplayVaryings input) : SV_Target
                {
                    float2 uv;
                    if (!HN_DebugPreviewUV(
                        HN_DebugToDisplayPixel(
                            int2(input.positionCS.xy), _HNRPDebugFlipY, _HNRPDebugPreviewRect.w),
                        uv))
                    {
                        return float4(0.0, 0.0, 0.0, 0.0);
                    }

                    float3 previewColor = SAMPLE_TEXTURE2D_LOD(
                        _HNRPDebugPreview, sampler_PointClamp, uv, _HNRPDebugPreviewMip).rgb;

                    return float4(saturate(previewColor), 1.0);
                }
            ENDHLSL
        }

        // 2: 纹理预览（Texture2DArray 指定 slice）
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
                #pragma vertex DebugDisplayVert
                #pragma fragment HN_DebugFragmentPreviewArray

                TEXTURE2D_ARRAY(_HNRPDebugPreview);

                float4 HN_DebugFragmentPreviewArray(DebugDisplayVaryings input) : SV_Target
                {
                    float2 uv;
                    if (!HN_DebugPreviewUV(
                        HN_DebugToDisplayPixel(
                            int2(input.positionCS.xy), _HNRPDebugFlipY, _HNRPDebugPreviewRect.w),
                        uv))
                    {
                        return float4(0.0, 0.0, 0.0, 0.0);
                    }

                    float3 previewColor = SAMPLE_TEXTURE2D_ARRAY_LOD(
                        _HNRPDebugPreview,
                        sampler_PointClamp,
                        uv,
                        _HNRPDebugPreviewSlice,
                        _HNRPDebugPreviewMip).rgb;

                    return float4(saturate(previewColor), 1.0);
                }
            ENDHLSL
        }
    }

    Fallback Off
}
