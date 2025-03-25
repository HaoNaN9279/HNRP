using System.Collections;
using System.Collections.Generic;
using HN.Serialize;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP.Generated
{
    public static class _New_HN_Render_Graph
    {
        public static void Render(RenderGraph renderGraph, List<JsonData> passParamsData)
        {
            Debug.Log("Generated Render.");
#region TextureInput_0
            TextureHandle _TextureInputParams_0_ColorTarget = TextureInput.Record(renderGraph);
#endregion

#region ForwardOpaquePass_1
            ForwardOpaquePass.Record(renderGraph, passParamsData[1], _TextureInputParams_0_ColorTarget);
#endregion

#region RenderOutput_2
            RenderOutput.Record(renderGraph, _TextureInputParams_0_ColorTarget);
#endregion

        }
    }
}