using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace HN.HNRP
{
    [Serializable]
    public class EditorWireOverlayPass : PassBase
    {
        public override void OnCreate(HNRenderGraphBase hnRenderGraph, string passName)
        {
            base.OnCreate(hnRenderGraph, passName);

            colorTargetSlot = new TexturePassSlot(hnRenderGraph, PassSlotType.ReadWrite);
        }

        public override void Record(RenderGraph renderGraph, ref RenderingData renderingData)
        {
#if UNITY_EDITOR
            Camera camera = renderingData.Camera;
            if (camera.cameraType == CameraType.SceneView)
            {
                using (var builder = renderGraph.AddRenderPass<EditorWireOverlayPassData>($"{name}({PassName})", out var passData))
                {
                    var graphObject = renderingData.GraphObject;
                    builder.WriteTexture(graphObject.GetTextureHandle(colorTargetSlot));
                    passData.camera = camera;

                    builder.SetRenderFunc(
                        (EditorWireOverlayPassData data, RenderGraphContext ctx) =>
                        {
                            ctx.renderContext.ExecuteCommandBuffer(ctx.cmd);
                            ctx.cmd.Clear();
                            ctx.renderContext.DrawWireOverlay(data.camera);
                        }
                    );
                }
            }
#endif
        }

        public override void Cleanup()
        {
            
        }


        [SerializeField]
        public TexturePassSlot colorTargetSlot;

        public const string PassName = "Editor Wire Overlay Pass";


        public class EditorWireOverlayPassData : PassData
        {
            public Camera camera;
        }

    }
}
