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
            Connect(buildLightDataPass.lightDatasBufferSlot, clusterCullingLightPass.lightDatasBufferSlot);

            ColorBufferInput colorBufferInput = AddPass<ColorBufferInput>("Color Target");

            DepthBufferInput depthBufferInput = AddPass<DepthBufferInput>("Depth Target");

            ForwardOpaquePass forwardOpaquePass = AddPass<ForwardOpaquePass>("Opaque");
            Connect(colorBufferInput.colorTargetSlot, forwardOpaquePass.colorTargetSlot);
            Connect(depthBufferInput.depthTargetSlot, forwardOpaquePass.depthTargetSlot);

            Connect(buildLightDataPass.lightDatasBufferSlot, forwardOpaquePass.lightDatasBufferSlot);

            Connect(clusterCullingReflectionProbePass.reflectionProbeAtlasSlot, forwardOpaquePass.reflectionProbeAtlasSlot);
            Connect(clusterCullingReflectionProbePass.clusterCullingReflectionProbeMaskBufferSlot, forwardOpaquePass.clusterCullingReflectionProbeMaskBufferSlot);
            Connect(clusterCullingReflectionProbePass.clusterCullingReflectionProbeDatasBufferSlot, forwardOpaquePass.clusterCullingReflectionProbeDatasBufferSlot);
            // Connect(emptyTextureSlot, forwardOpaquePass.reflectionProbeAtlasSlot);
            // Connect(emptyComputeBufferSlot, forwardOpaquePass.clusterCullingReflectionProbeMaskBufferSlot);
            // Connect(emptyComputeBufferSlot, forwardOpaquePass.clusterCullingReflectionProbeDatasBufferSlot);

            Connect(clusterCullingLightPass.clusterCullingLightMaskBufferSlot, forwardOpaquePass.clusterCullingLightMaskBufferSlot);
            // Connect(emptyComputeBufferSlot, forwardOpaquePass.clusterCullingLightMaskBufferSlot);

            BuiltinSkyPass builtinSkyPass = AddPass<BuiltinSkyPass>("Sky");
            Connect(forwardOpaquePass.colorTargetSlot, builtinSkyPass.colorTargetSlot);
            Connect(forwardOpaquePass.depthTargetSlot, builtinSkyPass.depthTargetSlot);

            TransparencyPass transparencyPass = AddPass<TransparencyPass>("Transparency");
            Connect(builtinSkyPass.colorTargetSlot, transparencyPass.colorTargetSlot);
            Connect(forwardOpaquePass.depthTargetSlot, transparencyPass.depthTargetSlot);

            EditorWireOverlayPass editorWireOverlayPass = AddPass<EditorWireOverlayPass>("Wire Overlay");
            Connect(transparencyPass.colorTargetSlot, editorWireOverlayPass.colorTargetSlot);

            RenderOutput renderOutput = AddPass<RenderOutput>("Final Blit");
            Connect(editorWireOverlayPass.colorTargetSlot, renderOutput.colorTargetSlot);
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
