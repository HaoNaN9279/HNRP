using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            RenderOutput renderOutput = AddPass<RenderOutput>("Render Output");
            Connect(forwardOpaquePass.colorTargetIndex, ref renderOutput.colorTargetIndex);
        }

        public override void Record()
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
