#ifndef HNRP_UNLIT_DEPTH_NORMAL_INCLUDED
#define HNRP_UNLIT_DEPTH_NORMAL_INCLUDED

#include "../Unlit/UnlitInput.hlsl"
#include "../Unlit/UnlitVaryingsDefine.hlsl"
#include "../Unlit/UnlitVaryings.hlsl"
#include "../Unlit/UnlitSurfaceData.hlsl"

// DepthNormal pass：深度由片元的 ZWrite 写入深度目标，法线编码后写入颜色目标。
// 法线经 [0,1] 编码（normalWS * 0.5 + 0.5），与常规法线纹理的编解码约定一致；
// Unlit 无法线贴图，法线即几何法线。alpha clip 与 Forward 共用同一路径。

PackedVaryings DepthNormalVert(Attributes attributes)
{
    HNRP_PKG_EMIT_UNLIT_DEPTHNORMAL_VERTEX_BEGIN();

    UNITY_SETUP_INSTANCE_ID(attributes);

    VertexInput vertexInput;
    ZERO_INITIALIZE(VertexInput, vertexInput);
    BuildVertexInput(attributes, vertexInput);

    UnlitVaryings unlitVaryings;
    ZERO_INITIALIZE(UnlitVaryings, unlitVaryings);
    BuildUnlitVaryings(vertexInput, unlitVaryings);
    HNRP_PKG_EMIT_UNLIT_DEPTHNORMAL_VERTEX_AFTERVARYINGS(unlitVaryings);

    PackedVaryings packedVaryings;
    ZERO_INITIALIZE(PackedVaryings, packedVaryings);
    BuildPackVaryings(unlitVaryings, packedVaryings);

    UNITY_TRANSFER_INSTANCE_ID(attributes, packedVaryings);

    HNRP_PKG_EMIT_UNLIT_DEPTHNORMAL_VERTEX_END();

    return packedVaryings;
}

float4 DepthNormalFrag(PackedVaryings packedVaryings) : SV_Target
{
    HNRP_PKG_EMIT_UNLIT_DEPTHNORMAL_FRAGMENT_BEGIN();

    UNITY_SETUP_INSTANCE_ID(packedVaryings);

    UnlitVaryings unlitVaryings;
    ZERO_INITIALIZE(UnlitVaryings, unlitVaryings);
    BuildUnpackVaryings(packedVaryings, unlitVaryings);
    HNRP_PKG_EMIT_UNLIT_DEPTHNORMAL_FRAGMENT_AFTERUNPACKVARYINGS(unlitVaryings);

    UnlitSurfaceData unlitSurfaceData;
    ZERO_INITIALIZE(UnlitSurfaceData, unlitSurfaceData);
    BuildUnlitSurfaceData(unlitVaryings, unlitSurfaceData);
    HNRP_PKG_EMIT_UNLIT_DEPTHNORMAL_FRAGMENT_AFTERSURFACEDATA(unlitSurfaceData, unlitVaryings);

    // 无法线贴图，normalTS 取切线空间正前方，GetNormalData 回退为几何法线。
    NormalData normalData = GetNormalData(
        unlitVaryings.normalWS,
        unlitVaryings.tangentWS,
        float3(0.0, 0.0, 1.0));

    HNRP_PKG_EMIT_UNLIT_DEPTHNORMAL_FRAGMENT_END();

    return float4(normalData.normalWS * 0.5 + 0.5, 0.0);
}

#endif
