// <copyright file="DrawShadowPass.Render.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public sealed partial class DrawShadowPass
    {
        private void RenderShadows(RenderGraphContext ctx, RTHandle atlas)
        {
            CommandBuffer cmd = ctx.cmd;

            // 上传两张表。全局绑定（SetGlobalBuffer / SetGlobalTexture / PushGlobal）
            // 由 BindGlobalShaderResources 在绘制前统一完成。
            if (shadowLightDatasBuffer != null && shadowLightDatasArray != null)
            {
                cmd.SetBufferData(shadowLightDatasBuffer, shadowLightDatasArray);
            }

            if (shadowMapDatasBuffer != null && shadowMapDatasArray != null)
            {
                cmd.SetBufferData(shadowMapDatasBuffer, shadowMapDatasArray);
            }

            if (shadowCameraDatasBuffer != null && shadowCameraDatasArray != null)
            {
                cmd.SetBufferData(shadowCameraDatasBuffer, shadowCameraDatasArray);
            }

            // 0) atlas 重分配后整图清远深度（一次性，经 RenderGraph 命令缓冲提交）。
            if (needsAtlasClear)
            {
                for (int slice = 0; slice < allocatedSliceCount; slice++)
                {
                    cmd.SetRenderTarget((RenderTargetIdentifier)atlas, 0, CubemapFace.Unknown, slice);
                    cmd.ClearRenderTarget(true, false, Color.black);
                }

                needsAtlasClear = false;
            }

            EnsureClearMaterial();

            // 1) 重分配搬移：把被移动 map 的旧区域内容复制到新区域。
            for (int i = 0; i < copyCommands.Count; i++)
            {
                ShadowCopyCommand copy = copyCommands[i];
                int size = Mathf.RoundToInt(copy.toScaleOffset.x * allocatedResolution);
                int srcX = Mathf.RoundToInt(copy.fromScaleOffset.z * allocatedResolution);
                int srcY = Mathf.RoundToInt(copy.fromScaleOffset.w * allocatedResolution);
                int dstX = Mathf.RoundToInt(copy.toScaleOffset.z * allocatedResolution);
                int dstY = Mathf.RoundToInt(copy.toScaleOffset.w * allocatedResolution);

                cmd.CopyTexture(
                    (RenderTargetIdentifier)atlas, copy.fromSlice, 0, srcX, srcY, size, size,
                    (RenderTargetIdentifier)atlas, copy.toSlice, 0, dstX, dstY);
            }

            // 2) 重绘：局部清远深度后把投射者几何直接画进本 map 区域；
            //    未重绘的 map 内容跨帧保留。
            //    逐 map 切换光视图 / 投影矩阵与 viewport，并用 context.DrawShadows 绘制。
            //    ScriptableRenderContext.DrawShadows 为 context 级 API，读取已提交到
            //    context 的状态，故每次绘制前先 ExecuteCommandBuffer 刷新命令缓冲
            //    （与 URP 的 ShadowUtils.RenderShadowSlice 一致）。
            if (drawCommands.Count > 0 && cameraContext.Camera != null)
            {
                for (int i = 0; i < drawCommands.Count; i++)
                {
                    ShadowDrawCommand command = drawCommands[i];
                    int slice = command.allocation.SliceIndex;
                    float scale = command.allocation.ScaleOffset.x;
                    int resolution = Mathf.RoundToInt(scale * allocatedResolution);
                    int offsetX = Mathf.RoundToInt(command.allocation.ScaleOffset.z * allocatedResolution);
                    int offsetY = Mathf.RoundToInt(command.allocation.ScaleOffset.w * allocatedResolution);

                    // 绑定 atlas 的目标 slice + viewport，使清屏与投射者绘制只落在本 map 区域。
                    cmd.SetRenderTarget((RenderTargetIdentifier)atlas, 0, CubemapFace.Unknown, slice);
                    cmd.SetViewport(new Rect(offsetX, offsetY, resolution, resolution));

                    // 全屏三角形（ShadowClear）写远深度，受 viewport 限制只清本 map 区域。
                    if (shadowClearMaterial != null)
                    {
                        CoreUtils.DrawFullScreen(cmd, shadowClearMaterial, null, 0);
                    }

                    // 深度偏置抑制自阴影；光矩阵决定投射者落到区域内的位置。
                    cmd.SetGlobalDepthBias(depthBias, slopBias);

                    // 注意：SetViewProjectionMatrices 只作用于引擎内置矩阵（剔除 / LOD），
                    // 本项目 shader 的 UNITY_MATRIX_V / VP 来自自定义 ShaderVariablesGlobal（b0，
                    // 按相机上传），因此投射者必须额外用 _ShadowViewProj 取光源矩阵。
                    // 投影统一走 GetShadowDrawProjection（含平台 z 映射与渲染到纹理的 Y 翻转），
                    // 保证写入深度与采样侧 GetShadowTransform 严格同源。
                    cmd.SetViewProjectionMatrices(command.view, GetShadowDrawProjection(command.proj));
                    shadowViewProjParams._ShadowViewProj =
                        GetShadowDrawProjection(command.proj) * command.view;

                    // 逐 map 推送：_ShadowViewProj 随 map 变化，必须与随后的 ExecuteCommandBuffer
                    // 一起提交，确保 DrawShadows 读到本 map 的矩阵。
                    ConstantBuffer.PushGlobal(
                        cmd,
                        shadowViewProjParams,
                        PropertyIDs.shadowViewProjBuffer);

                    ctx.renderContext.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    // 用引擎 DrawShadows 取代 DrawRenderers：DrawRenderers 只能取相机可见集，
                    // 会漏掉相机视锥外、但投影落在视锥内的投射者（阴影跳变根因）；
                    // DrawShadows 取引擎裁剪期按光源收集的投射者集合，并用 splitData 做二次裁剪。
                    // 方向光的 splitData 为「组级」引擎结果（见 GetDirectionalGroupSplitData），
                    // 组内各级共享，且 8 级也不再受引擎 1/2/4 级 cascade 的限制。
                    if (command.hasCasters)
                    {
                        var shadowDrawingSettings = new ShadowDrawingSettings(
                            cameraContext.CullingResults,
                            command.lightIndex,
                            command.lightType == LightType.Directional
                                ? BatchCullingProjectionType.Orthographic
                                : BatchCullingProjectionType.Perspective)
                        {
                            splitData = command.splitData,
                            objectsFilter = ShadowObjectsFilter.AllObjects,
                        };
                        ctx.renderContext.DrawShadows(ref shadowDrawingSettings);
                    }

                    cmd.DisableScissorRect();
                    cmd.SetGlobalDepthBias(0f, 0f);
                    ctx.renderContext.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                }
            }

            copyCommands.Clear();
        }

        private void EnsureClearMaterial()
        {
            if (shadowClearMaterial != null)
            {
                return;
            }

            Shader clearShader = cameraContext.RuntimeResources.shaderResources.ShadowClear;
            if (clearShader != null)
            {
                shadowClearMaterial = new Material(clearShader)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                };
            }
        }
    }
}
