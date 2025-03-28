using System.Collections;
using System.Collections.Generic;
using HN.Serialize;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP.Generated
{
    public class HNRenderGraphTarget_New_HN_Render_Graph : HNRenderGraph.HNRenderGraphTarget
    {
        public override void Execute()
        {
            Debug.Log("Generated Render.");

            TextureHandle backBuffer = renderGraph.ImportBackbuffer(targetId);

#region ForwardOpaquePass_0
            TextureHandle _ForwardOpaquePassParams_0_ColorTarget = ForwardOpaquePass.Record(renderGraph, passParamsData[0]);
#endregion

#region RenderOutput_1
            RenderOutput.Record(renderGraph, _ForwardOpaquePassParams_0_ColorTarget, backBuffer);
#endregion

        }
    }
}