using System;
using System.Collections;
using System.Collections.Generic;
using Codice.CM.SEIDInfo;
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
        [SerializeField]
        public int colorTargetIndex = -1;


        public override void Record(RenderGraph renderGraph, RenderingData renderingData, List<TextureHandle> textureHandles)
        {
#if UNITY_EDITOR
            Camera camera = renderingData.Camera;
            if (camera.cameraType == CameraType.SceneView)
            {
                using (var builder = renderGraph.AddRenderPass<EditorWireOverlayPassData>($"{name}({PassName})", out var passData))
                {
                    builder.WriteTexture(textureHandles[colorTargetIndex]);
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


        public class EditorWireOverlayPassData : PassData
        {
            public Camera camera;
        }


        public const string PassName = "Editor Wire Overlay Pass";
    }
}
