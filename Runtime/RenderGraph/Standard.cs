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
            SetLightDataPass setLightDataPass = AddPass<SetLightDataPass>("Set Light Data");

            ColorBufferInput colorBufferInput = AddPass<ColorBufferInput>("Color Target");
            DepthBufferInput depthBufferInput = AddPass<DepthBufferInput>("Depth Target");

            ForwardPlusLightCullingPass forwardPlusLightCullingPass = AddPass<ForwardPlusLightCullingPass>("Forward Plus Light Culling");

            ForwardOpaquePass forwardOpaquePass = AddPass<ForwardOpaquePass>("Opaque");
            Connect(colorBufferInput.colorTargetIndex, ref forwardOpaquePass.colorTargetIndex);
            Connect(depthBufferInput.depthTargetIndex, ref forwardOpaquePass.depthTargetIndex);
            Connect(forwardPlusLightCullingPass.forwardPlusZBinsBufferIndex, ref forwardOpaquePass.forwardPlusZBinsBufferIndex);
            Connect(forwardPlusLightCullingPass.forwardPlusTileMasksBufferIndex, ref forwardOpaquePass.forwardPlusTileMasksBufferIndex);
            Connect(setLightDataPass.lightDatasBufferIndex, ref forwardOpaquePass.lightDatasBufferIndex);

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

        public override void RecordRenderGraph()
        {
            Debug.Log("Standard RenderGraph Record Called.");

            if (passes == null || passes.Count == 0)
            {
                Debug.LogWarning("No passes found in the RenderGraph. Please ensure you have added passes before recording.");
                return;
            }

            foreach (var pass in passes)
            {
                if (pass == null)
                {
                    Debug.LogWarning("Found a null pass in the RenderGraph. Skipping this pass.");
                    continue;
                }

                if (!pass.IsEnable)
                {
                    continue;
                }

                pass.Record(renderGraph, ref renderingData);
            }
        }

        public override void EndRecordRenderGraph()
        {
            Debug.Log("Standard RenderGraph End Record Called.");

            if (passes == null || passes.Count == 0)
            {
                Debug.LogWarning("No passes found in the RenderGraph. Please ensure you have added passes before recording.");
                return;
            }

            foreach (var pass in passes)
            {
                if (pass == null)
                {
                    Debug.LogWarning("Found a null pass in the RenderGraph. Skipping this pass.");
                    continue;
                }

                if (!pass.IsEnable)
                {
                    continue;
                }

                pass.EndRecord();
            }
        }

        public override void Dispose()
        {
            Debug.Log("Standard RenderGraph Dispose Called.");

            if (passes == null || passes.Count == 0)
            {
                Debug.LogWarning("No passes found in the RenderGraph. Please ensure you have added passes before disposing.");
                return;
            }

            foreach (var pass in passes)
            {
                if (pass == null)
                {
                    Debug.LogWarning("Found a null pass in the RenderGraph. Skipping this pass.");
                    continue;
                }

                pass.Dispose();
            }
        }
    }
}
