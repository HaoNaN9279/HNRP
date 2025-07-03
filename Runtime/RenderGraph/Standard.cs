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
            TextureInput textureInput = AddPass<TextureInput>("Color Target");
            ForwardOpaquePass forwardOpaquePass = AddPass<ForwardOpaquePass>("Opaque");
            Connect(textureInput.colorTargetIndex, ref forwardOpaquePass.colorTargetIndex);
            // TransparencyPass transparencyPass = AddPass<TransparencyPass>("Transparency");
            // Connect(forwardOpaquePass.colorTargetIndex, ref transparencyPass.colorTargetIndex);
            RenderOutput renderOutput = AddPass<RenderOutput>("Final Blit");
            Connect(forwardOpaquePass.colorTargetIndex, ref renderOutput.colorTargetIndex);
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
