using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public partial class RenderRequest
    {
        public RenderRequest(
            ScriptableRenderContext context,
            RenderGraph renderGraph,
            ref RenderingData renderingData
            )
        {
            this.context = context;
            this.renderGraph = renderGraph;
            this.renderingData = renderingData;
        }

        public void RecordAndExecute()
        {
            RTHandles.SetReferenceSize(renderingData.Camera.pixelWidth, renderingData.Camera.pixelHeight);

            renderingData.Cmd.ClearRenderTarget(true, true, Color.gray);

            if (renderingData.Camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(renderingData.Camera);
            }

            if (!TryCull())
            {
                Debug.LogError("Culling failed for camera: " + renderingData.Camera.name);
                return;
            }

            InitializeRenderingData(renderingData.CullingResults);

            UpdateGlobalTexture(renderingData.Cmd);
            UpdateGlobalConstantBuffer(renderingData.CameraData, renderingData.Cmd);
            UpdateGlobalKeywords(renderingData);

            if (GL.wireframe)
            {
                RenderWireFrame(renderingData.CullingResults, renderingData.Camera, renderingData.TargetId, context, renderingData.Cmd);
            }

            RecordPasses();
        }

        public void Cleanup()
        {
            renderingData.GraphObject.Dispose();
        }


        private void RecordPasses()
        {
            if (renderingData.GraphObject == null)
            {
                Debug.LogError("RenderGraph is null.");
                return;
            }

            renderCount++;

            using (renderGraph.RecordAndExecute(new RenderGraphParameters
            {
                executionName = "execution_" + renderingData.Camera.name,
                currentFrameIndex = renderingData.FrameCount,
                rendererListCulling = true,
                scriptableRenderContext = context,
                commandBuffer = renderingData.Cmd
            }))
            {
                UpdateGraphData();

                renderingData.GraphObject.UpdateData(renderGraph, ref renderingData);
                renderingData.GraphObject.RecordRenderGraph();
            }
        }

        private void InitializeRenderingData(CullingResults cullingResults)
        {
            renderingData.visibleLights = cullingResults.visibleLights;

            renderingData.visibleReflectionProbes = cullingResults.visibleReflectionProbes;
            int reflectionProbeCount = renderingData.visibleReflectionProbes.Length;
            HNRenderPipelineUtils.FilterReflectionProbe(ref renderingData.visibleReflectionProbes, reflectionProbeCount);
        }

        private void UpdateGlobalTexture(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(GlobalPropertyIDs.glossyEnvironmentCubeMap, ReflectionProbe.defaultTexture);
        }

        private void UpdateGlobalConstantBuffer(HNAdditionalCameraData cameraData, CommandBuffer cmd)
        {
            SetupCameraProperties(context, cameraData.BuiltinCamera, cmd);

            UpdateTimeGlobalConstantBuffer();
            cameraData.UpdateCameraGlobalConstantBuffer(ref globalConstantBuffer);
            UpdateLightGlobalConstantBuffer(ref globalConstantBuffer);

            ConstantBuffer.PushGlobal(cmd, globalConstantBuffer, GlobalPropertyIDs.ShaderVariablesGlobal);
            
        }

        private void UpdateGlobalKeywords(RenderingData renderingData)
        {
            ResetGlobalKeywords(renderingData.Cmd);

            UpdateLightGlobalKeywords(renderingData);
        }

        private void ResetGlobalKeywords(CommandBuffer cmd)
        {
            cmd.DisableShaderKeyword(GlobalKeywords.evaluateSHVertex);
            cmd.DisableShaderKeyword(GlobalKeywords.evaluateSHMixed);
            cmd.DisableShaderKeyword(GlobalKeywords.forwardPlus);
        }

        private void SetupCameraProperties(ScriptableRenderContext context, Camera camera, CommandBuffer cmd)
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            context.SetupCameraProperties(camera);
        }

        private bool TryCull()
        {
            if (renderingData.Camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters))
            {
                // TODO: SetupCullingParameters
                renderingData.CullingResults = context.Cull(ref cullingParameters);
                return true;
            }
            return false;
        }

        private void UpdateTimeGlobalConstantBuffer()
        {
            float ct = Time.time;
            float dt = Time.deltaTime;
            float sdt = Time.smoothDeltaTime;

            globalConstantBuffer._Time = new Vector4(ct * 0.05f, ct * 2.0f, ct * 3.0f);
            globalConstantBuffer._SinTime = new Vector4(Mathf.Sin(ct * 0.125f), Mathf.Sin(ct * 0.25f), Mathf.Sin(ct * 0.5f), Mathf.Sin(ct));
            globalConstantBuffer._CosTime = new Vector4(Mathf.Cos(ct * 0.125f), Mathf.Cos(ct * 0.25f), Mathf.Cos(ct * 0.5f), Mathf.Cos(ct));
            globalConstantBuffer.unity_DeltaTime = new Vector4(dt, 1.0f / dt, sdt, 1.0f / sdt);
            globalConstantBuffer._TimeParameters = new Vector4(ct, Mathf.Sin(ct), Mathf.Cos(ct), 0.0f);
        }

        private void RenderWireFrame(CullingResults cullingResults, Camera camera, RenderTargetIdentifier backBuffer, ScriptableRenderContext context, CommandBuffer cmd)
        {
            CoreUtils.SetRenderTarget(cmd, backBuffer, ClearFlag.Color, CoreRenderPipelinePreferences.previewBackgroundColor);

            var opaqueRendererList = context.CreateRendererList(HNRenderPipelineUtils.GetOpaqueRendererListDesc(ShaderPassNames.AllForwardNames, cullingResults, camera, 1));
            CoreUtils.DrawRendererList(context, cmd, opaqueRendererList);

            var transparentRendererList = context.CreateRendererList(HNRenderPipelineUtils.GetTransparentRendererListDesc(ShaderPassNames.AllForwardNames, cullingResults, camera, 1));
            CoreUtils.DrawRendererList(context, cmd, transparentRendererList);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        private void UpdateGraphData()
        {
            if (renderingData.GraphData.textureHandles == null)
            {
                renderingData.GraphData.textureHandles = new List<TextureHandle>();
            }
            else
            {
                renderingData.GraphData.textureHandles.Clear();
            }

            if (renderingData.GraphData.computeBufferHandles == null)
            {
                renderingData.GraphData.computeBufferHandles = new List<ComputeBufferHandle>();
            }
            else
            {
                renderingData.GraphData.computeBufferHandles.Clear();
            }
        }


        public ScriptableRenderContext Context => context;
        public RenderingData RenderingData => renderingData;

        private ScriptableRenderContext context;
        private RenderGraph renderGraph;
        private RenderingData renderingData;
        private int renderCount = 0;

        private GlobalConstantBuffer globalConstantBuffer = default;

    }
}
