using System;
using System.Collections;
using System.Collections.Generic;
using Codice.Client.BaseCommands;
using HN.Graph;
using HN.Serialize;
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

        [SerializeField]
        public Material singleBlitMat;


        public override void Record(RenderGraph renderGraph, FrameData frameData, GraphObjectData graphObjectData, List<TextureHandle> textureHandles)
        {
            Debug.Log("Render Output");

            singleBlitMat = singleBlitMat ?? new Material(Shader.Find("Unlit/SingleBlitShader"));

            using (var builder = renderGraph.AddRenderPass<RenderOutputData>("Render Output", out var passData))
            {
                builder.AllowPassCulling(false);

                passData.singleBlitMat = singleBlitMat;
                passData.inputTexture = builder.ReadTexture(textureHandles[colorTargetIndex]);
                TextureHandle backBuffer = renderGraph.ImportBackbuffer(graphObjectData.TargetId);
                passData.renderTarget = builder.WriteTexture(backBuffer);
                builder.SetRenderFunc(
                    (RenderOutputData data, RenderGraphContext ctx) =>
                    {
                        CoreUtils.SetRenderTarget(ctx.cmd, data.renderTarget);

                        var materialPropertyBlock = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                        materialPropertyBlock.SetTexture("_MainTex", data.inputTexture);
                        CoreUtils.DrawFullScreen(ctx.cmd, data.singleBlitMat, materialPropertyBlock);
                    }
                );
            }
        }
    }


    public class RenderOutputData : PassData
    {
        public TextureHandle inputTexture;
        public TextureHandle renderTarget;
        public Material singleBlitMat;

    }


}

