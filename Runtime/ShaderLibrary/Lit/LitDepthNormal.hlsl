#ifndef HNRP_LIT_DEPTH_NORMAL_INCLUDED
#define HNRP_LIT_DEPTH_NORMAL_INCLUDED

#include "../Lit/LitInput.hlsl"
#include "../Lit/LitVaryingsDefine.hlsl"
#include "../Lit/LitVaryings.hlsl"
#include "../Lit/LitSurfaceData.hlsl"

// DepthNormal pass：深度由片元的 ZWrite 写入深度目标，法线编码后写入颜色目标。
// 法线经 [0,1] 编码（normalWS * 0.5 + 0.5），与常规法线纹理的编解码约定一致；
// 顶点 / 片元沿用 Forward 的 varyings 与 SurfaceData 体系，保证法线与 Forward 完全一致。
// alpha clip 与 Forward 共用同一路径，裁切体的法线 / 深度形状正确。

PackedVaryings DepthNormalVert(Attributes attributes)
{
    UNITY_SETUP_INSTANCE_ID(attributes);

    VertexInput vertexInput;
    ZERO_INITIALIZE(VertexInput, vertexInput);
    BuildVertexInput(attributes, vertexInput);

    LitVaryings litVaryings;
    ZERO_INITIALIZE(LitVaryings, litVaryings);
    BuildLitVaryings(vertexInput, litVaryings);

    PackedVaryings packedVaryings;
    ZERO_INITIALIZE(PackedVaryings, packedVaryings);
    BuildPackVaryings(litVaryings, packedVaryings);

    UNITY_TRANSFER_INSTANCE_ID(attributes, packedVaryings);

    return packedVaryings;
}

float4 DepthNormalFrag(PackedVaryings packedVaryings) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(packedVaryings);

    LitVaryings litVaryings;
    ZERO_INITIALIZE(LitVaryings, litVaryings);
    BuildUnpackVaryings(packedVaryings, litVaryings);

    LitSurfaceData litSurfaceData;
    ZERO_INITIALIZE(LitSurfaceData, litSurfaceData);
    BuildLitSurfaceData(litVaryings, litSurfaceData);

    NormalData normalData = GetNormalData(
        litVaryings.normalWS,
        litVaryings.tangentWS,
        litSurfaceData.normalTS);

    return float4(normalData.normalWS * 0.5 + 0.5, 0.0);
}

#endif
