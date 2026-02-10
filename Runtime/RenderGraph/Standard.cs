using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HN.HNRP
{
    [CreateAssetMenu(menuName = "Rendering/HN Rendering Pipeline/Standard")]
    public class Standard : HNRenderGraphBase
    {
        public override void Build()
        {
            BuildLightDataPass buildLightDataPass = AddPass<BuildLightDataPass>("Build Light Data");
            
            ClusterCullingReflectionProbePass clusterCullingReflectionProbePass = AddPass<ClusterCullingReflectionProbePass>("Cluster Culling Reflection Probe");
            
            ClusterCullingLightPass clusterCullingLightPass = AddPass<ClusterCullingLightPass>("Cluster Culling Light Pass");
            Connect(buildLightDataPass.lightDatasBufferIndex, ref clusterCullingLightPass.lightDatasBufferIndex);

            ColorBufferInput colorBufferInput = AddPass<ColorBufferInput>("Color Target");

            DepthBufferInput depthBufferInput = AddPass<DepthBufferInput>("Depth Target");

            ForwardOpaquePass forwardOpaquePass = AddPass<ForwardOpaquePass>("Opaque");
            Connect(colorBufferInput.colorTargetIndex, ref forwardOpaquePass.colorTargetIndex);
            Connect(depthBufferInput.depthTargetIndex, ref forwardOpaquePass.depthTargetIndex);

            Connect(buildLightDataPass.lightDatasBufferIndex, ref forwardOpaquePass.lightDatasBufferIndex);

            Connect(clusterCullingReflectionProbePass.reflectionProbeAtlasIndex, ref forwardOpaquePass.reflectionProbeAtlasIndex);
            Connect(clusterCullingReflectionProbePass.clusterCullingReflectionProbeMaskBufferIndex, ref forwardOpaquePass.clusterCullingReflectionProbeMaskBufferIndex);
            Connect(clusterCullingReflectionProbePass.clusterCullingReflectionProbeDatasBufferIndex, ref forwardOpaquePass.clusterCullingReflectionProbeDatasBufferIndex);
            // Connect(emptyTextureIndex, ref forwardOpaquePass.reflectionProbeAtlasIndex);
            // Connect(emptyComputeBufferIndex, ref forwardOpaquePass.clusterCullingReflectionProbeMaskBufferIndex);
            // Connect(emptyComputeBufferIndex, ref forwardOpaquePass.clusterCullingReflectionProbeDatasBufferIndex);

            Connect(clusterCullingLightPass.clusterCullingLightMaskBufferIndex, ref forwardOpaquePass.clusterCullingLightMaskBufferIndex);
            // Connect(emptyComputeBufferIndex, ref forwardOpaquePass.clusterCullingLightMaskBufferIndex);

            BuiltinSkyPass builtinSkyPass = AddPass<BuiltinSkyPass>("Sky");
            Connect(forwardOpaquePass.colorTargetIndex, ref builtinSkyPass.colorTargetIndex);
            Connect(forwardOpaquePass.depthTargetIndex, ref builtinSkyPass.depthTargetIndex);

            TransparencyPass transparencyPass = AddPass<TransparencyPass>("Transparency");
            Connect(builtinSkyPass.colorTargetIndex, ref transparencyPass.colorTargetIndex);
            Connect(forwardOpaquePass.depthTargetIndex, ref transparencyPass.depthTargetIndex);

            EditorWireOverlayPass editorWireOverlayPass = AddPass<EditorWireOverlayPass>("Wire Overlay");
            Connect(transparencyPass.colorTargetIndex, ref editorWireOverlayPass.colorTargetIndex);

            RenderOutput renderOutput = AddPass<RenderOutput>("Final Blit");
            Connect(transparencyPass.colorTargetIndex, ref renderOutput.colorTargetIndex);
        }

        public override void RecordRenderGraph()
        {
            // Debug.Log("Standard RenderGraph Record Called.");
            base.RecordRenderGraph();
        }

        public override void Dispose()
        {
            // Debug.Log("Standard RenderGraph Dispose Called.");
            base.Dispose();
        }
    }
}
