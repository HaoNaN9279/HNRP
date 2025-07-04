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
        [SerializeField]
        public int colorTargetIndex = -1;


        public override void Record(RenderGraph renderGraph, FrameData frameData, GraphObjectData graphObjectData, List<TextureHandle> textureHandles)
        {
            Debug.Log("Render Output");

            Material singleBlitMat = new Material(Shader.Find("Unlit/SingleBlitShader"));

            using (var builder = renderGraph.AddRenderPass<RenderOutputData>("Render Output", out var passData))
            {
                builder.AllowPassCulling(false);

                passData.blitMaterial = singleBlitMat;
                passData.flip = graphObjectData.Camera.cameraType == CameraType.Game && graphObjectData.Camera.targetTexture == null;
                passData.inputTexture = builder.ReadTexture(textureHandles[colorTargetIndex]);
                TextureHandle backBuffer = renderGraph.ImportBackbuffer(graphObjectData.TargetId);
                passData.renderTarget = builder.WriteTexture(backBuffer);
                builder.SetRenderFunc(
                    (RenderOutputData data, RenderGraphContext ctx) =>
                    {
                        CoreUtils.SetRenderTarget(ctx.cmd, data.renderTarget);

                        var propertyBlock = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                        propertyBlock.SetTexture("_tex", data.inputTexture);
                        propertyBlock.SetFloat("_flip", data.flip ? 1.0f : 0.0f);
                        CoreUtils.DrawFullScreen(ctx.cmd, data.blitMaterial, propertyBlock);
                    }
                );
            }
        }


        public class RenderOutputData : PassData
        {
            public TextureHandle inputTexture;
            public TextureHandle renderTarget;
            public Material blitMaterial;
            public bool flip;

        }
    }

}

