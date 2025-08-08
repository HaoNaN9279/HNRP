using System;
using System.Collections;
using System.Collections.Generic;
using HN.Serialize;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public partial class RenderRequest
    {
        public ScriptableRenderContext Context => context;
        public RenderingData RenderingData => renderingData;

        private ScriptableRenderContext context;
        private RenderGraph renderGraph;
        private RenderingData renderingData;

        private GlobalConstantBuffer globalConstantBuffer = default;


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

            UpdateGlobalConstantBuffer(renderingData.CameraData, renderingData.Cmd);
            UpdateGlobalKeywords(renderingData);

            if (GL.wireframe)
            {
                RenderWireFrame(renderingData.CullingResults, renderingData.Camera, renderingData.TargetId, context, renderingData.Cmd);
            }

            RecordPasses();

        }


        private void RecordPasses()
        {
            if (renderingData.GraphObject == null)
            {
                Debug.LogError("RenderGraph is null.");
                return;
            }

            renderingData.GraphObject.UpdateData(renderGraph, renderingData);

            using (renderGraph.RecordAndExecute(new RenderGraphParameters
            {
                executionName = "execution_" + renderingData.Camera.name,
                currentFrameIndex = renderingData.FrameCount,
                rendererListCulling = true,
                scriptableRenderContext = context,
                commandBuffer = renderingData.Cmd
            }))
            {
                List<TextureHandle> textureHandles = new List<TextureHandle>();

                renderingData.GraphObject.RecordRenderGraph(textureHandles);
            }
        }

        private void InitializeRenderingData(CullingResults cullingResults)
        {
            var visibleLights = cullingResults.visibleLights;

            int mainLightIndex = GetMainLightIndex(visibleLights);
            InitializeLightData(visibleLights, mainLightIndex, out renderingData.LightData);
        }

        private void UpdateGlobalConstantBuffer(HNAdditionalCameraData cameraData, CommandBuffer cmd)
        {
            SetupCameraProperties(context, cameraData.BuiltinCamera, cmd);

            UpdateTimeGlobalConstantBuffer();
            cameraData.UpdateCameraGlobalConstantBuffer(ref globalConstantBuffer);
            UpdateLightGlobalConstantBuffer(ref globalConstantBuffer);

            ConstantBuffer.PushGlobal(cmd, globalConstantBuffer, PropertyIDs.ShaderVariablesGlobal);
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
        
    }
}
