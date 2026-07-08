using System;
using System.Collections;
using System.Collections.Generic;
using Codice.Client.BaseCommands;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    [Serializable]
    public class RenderOutput : PassBase
    {
        public override void OnCreate(HNRenderGraphBase hnRenderGraph, string passName)
        {
            base.OnCreate(hnRenderGraph, passName);

            colorTargetSlot = new TexturePassSlot(hnRenderGraph, PassSlotType.ReadOnly);
        }

        public override void Record(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            Material singleBlitMat = CoreUtils.CreateEngineMaterial(renderingData.runtimeResources.shaderResources.Blit);

            using (var builder = renderGraph.AddRenderPass<RenderOutputData>($"{name}({PassName})", out var passData))
            {
                builder.AllowPassCulling(false);

                var graphObject = renderingData.GraphObject;
                passData.blitMaterial = singleBlitMat;
                passData.flip = renderingData.Camera.cameraType == CameraType.Game && renderingData.Camera.targetTexture == null;
                passData.viewport = renderingData.CameraData.FinalViewport;
                passData.inputTexture = builder.ReadTexture(graphObject.GetTextureHandle(colorTargetSlot));
                TextureHandle backBuffer = renderGraph.ImportBackbuffer(renderingData.TargetId);
                passData.renderTarget = builder.WriteTexture(backBuffer);
                builder.SetRenderFunc(
                    (RenderOutputData data, RenderGraphContext ctx) =>
                    {
                        var propertyBlock = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                        RTHandle inputTexture = data.inputTexture;
                        RTHandle renderTarget = data.renderTarget;
                        CoreUtils.SetRenderTarget(ctx.cmd, renderTarget);
                        var scaleBias = new Vector4((float)data.viewport.width / inputTexture.rt.width, (float)data.viewport.height / inputTexture.rt.height, 0.0f, 0.0f);
                        if(passData.flip)
                        {
                            scaleBias.w = scaleBias.y;
                            scaleBias.y *= -1.0f;
                        }
                        Blitter.BlitTexture(ctx.cmd, propertyBlock, inputTexture, scaleBias, 0, true);
                    }
                );
            }
        }

        public override void Cleanup()
        {
            
        }


        [SerializeField]
        public TexturePassSlot colorTargetSlot;

        private RTHandle backBufferHandle;

        public const string PassName = "Render Output";


        public class RenderOutputData : PassData
        {
            public TextureHandle inputTexture;
            public TextureHandle renderTarget;
            public Material blitMaterial;
            public bool flip;
            public Rect viewport;

        }

    }

}

