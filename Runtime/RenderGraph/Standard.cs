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
            ReflectionProbeAtlasPass reflectionProbeAtlasPass = AddPass<ReflectionProbeAtlasPass>("Reflection Probe Atlas");

            BuildLightDataPass buildLightDataPass = AddPass<BuildLightDataPass>("Build Light Data");

            ColorBufferInput colorBufferInput = AddPass<ColorBufferInput>("Color Target");
            DepthBufferInput depthBufferInput = AddPass<DepthBufferInput>("Depth Target");

            ForwardPlusLightCullingPass forwardPlusLightCullingPass = AddPass<ForwardPlusLightCullingPass>("Forward Plus Light Culling");

            ForwardOpaquePass forwardOpaquePass = AddPass<ForwardOpaquePass>("Opaque");
            Connect(colorBufferInput.colorTargetIndex, ref forwardOpaquePass.colorTargetIndex);
            Connect(depthBufferInput.depthTargetIndex, ref forwardOpaquePass.depthTargetIndex);
            Connect(forwardPlusLightCullingPass.forwardPlusZBinsBufferIndex, ref forwardOpaquePass.forwardPlusZBinsBufferIndex);
            Connect(forwardPlusLightCullingPass.forwardPlusTileMasksBufferIndex, ref forwardOpaquePass.forwardPlusTileMasksBufferIndex);

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

        public override void EndRecordRenderGraph()
        {
            // Debug.Log("Standard RenderGraph End Record Called.");
            base.EndRecordRenderGraph();
        }

        public override void Dispose()
        {
            // Debug.Log("Standard RenderGraph Dispose Called.");
            base.Dispose();
        }
    }
}
