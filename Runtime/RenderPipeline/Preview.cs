using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HN.HNRP
{
    [CreateAssetMenu(menuName = "Rendering/HN Rendering Pipeline/Preview")]
    public class Preview : HNRenderGraphBase
    {
        public override void Build()
        {
            // ColorBufferInput colorBufferInput = AddPass<ColorBufferInput>("Color Target");

            // DepthBufferInput depthBufferInput = AddPass<DepthBufferInput>("Depth Target");

            // ForwardOpaquePass forwardOpaquePass = AddPass<ForwardOpaquePass>("Opaque");
            // Connect(colorBufferInput.colorTargetSlot, forwardOpaquePass.colorTargetSlot);
            // Connect(depthBufferInput.depthTargetSlot, forwardOpaquePass.depthTargetSlot);

            // Connect(emptyTextureSlot, forwardOpaquePass.reflectionProbeAtlasSlot);

            // BuiltinSkyPass builtinSkyPass = AddPass<BuiltinSkyPass>("Sky");
            // Connect(forwardOpaquePass.colorTargetSlot, builtinSkyPass.colorTargetSlot);
            // Connect(forwardOpaquePass.depthTargetSlot, builtinSkyPass.depthTargetSlot);

            // TransparencyPass transparencyPass = AddPass<TransparencyPass>("Transparency");
            // Connect(builtinSkyPass.colorTargetSlot, transparencyPass.colorTargetSlot);
            // Connect(forwardOpaquePass.depthTargetSlot, transparencyPass.depthTargetSlot);

            // RenderOutput renderOutput = AddPass<RenderOutput>("Final Blit");
            // Connect(transparencyPass.colorTargetSlot, renderOutput.colorTargetSlot);
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
