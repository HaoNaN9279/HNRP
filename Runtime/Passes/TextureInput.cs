using System;
using System.Collections;
using System.Collections.Generic;
using HN.Graph;
using HN.Serialize;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public class TextureInput
    {
        public static TextureHandle Record(RenderGraph renderGraph)
        {
            Debug.Log("Texture Input");
            TextureHandle output = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            {
                colorFormat = GraphicsFormat.R8G8B8A8_UNorm, clearBuffer = true, clearColor = Color.red, name = "ColorTarget"
            });
            
            return output;
        }

    }


    [Serializable]
    [NodeInfo("Texture Input", NodeInfo.NodeType.RenderTarget, "Render Target/Texture Input")]
    public class TextureInputParams : NodeParams
    {
        [PortInfo("Color Target", PortInfo.Direction.Output, PortInfo.Capacity.Single)]
        public TexturePort OutputColorTarget
        {
            get => outputColorTarget;
            set => outputColorTarget = value;
        }

        [SerializeField]
        private TexturePort outputColorTarget;


        public override void SetupOutput(int nodeIndex)
        {
            OutputColorTarget = new TexturePort($"_TextureInputParams_{nodeIndex}_ColorTarget");
        }

        public override void AppendScript(ref string main, int nodeIndex)
        {
            string script = 
$@"
#region TextureInput_{nodeIndex}
            TextureHandle _TextureInputParams_{nodeIndex}_ColorTarget = TextureInput.Record(renderGraph);
#endregion
";
            
            main += script;
        }
    }
}
