using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    [CreateAssetMenu(menuName = "Rendering/HN Rendering Pipeline/Standard")]
    public class Standard : HNRenderGraphBase
    {
        public override void Initialize()
        {
            ColorBufferInput colorBufferInput = AddPass<ColorBufferInput>("Color Target");
            DepthBufferInput depthBufferInput = AddPass<DepthBufferInput>("Depth Target");

            ForwardOpaquePass forwardOpaquePass = AddPass<ForwardOpaquePass>("Opaque");
            Connect(colorBufferInput.colorTargetIndex, ref forwardOpaquePass.colorTargetIndex);
            Connect(depthBufferInput.depthTargetIndex, ref forwardOpaquePass.depthTargetIndex);

            BuiltinSkyPass builtinSkyPass = AddPass<BuiltinSkyPass>("Sky");
            Connect(forwardOpaquePass.colorTargetIndex, ref builtinSkyPass.colorTargetIndex);
            Connect(forwardOpaquePass.depthTargetIndex, ref builtinSkyPass.depthTargetIndex);

            TransparencyPass transparencyPass = AddPass<TransparencyPass>("Transparency");
            Connect(builtinSkyPass.colorTargetIndex, ref transparencyPass.colorTargetIndex);
            Connect(forwardOpaquePass.depthTargetIndex, ref transparencyPass.depthTargetIndex);

            EditorWireOverlayPass editorWireOverlayPass = AddPass<EditorWireOverlayPass>("Wire Overlay");
            Connect(transparencyPass.colorTargetIndex, ref editorWireOverlayPass.colorTargetIndex);

            RenderOutput renderOutput = AddPass<RenderOutput>("Final Blit");
            Connect(editorWireOverlayPass.colorTargetIndex, ref renderOutput.colorTargetIndex);
        }

        public override void RecordRenderGraph(List<TextureHandle> textureHandles)
        {
            Debug.Log("Standard RenderGraph Record Called");

            if (passes == null || passes.Count == 0)
            {
                Debug.LogWarning("No passes found in the RenderGraph. Please ensure you have added passes before recording.");
                return;
            }
            
            foreach(var pass in passes)
            {
                if (pass == null)
                {
                    Debug.LogWarning("Found a null pass in the RenderGraph. Skipping this pass.");
                    continue;
                }

                pass.Record(renderGraph, frameData, graphObjectData, textureHandles);
            }
        }
    }
}
