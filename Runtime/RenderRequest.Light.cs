using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public partial class RenderRequest
    {
        private void InitializeLightData(NativeArray<VisibleLight> visibleLights, int mainLightIndex, out LightData lightData)
        {
            lightData.mainLightIndex = mainLightIndex;
            lightData.visibleLights = visibleLights;
        }

        private int GetMainLightIndex(NativeArray<VisibleLight> visibleLights)
        {
            int totalVisibleLights = visibleLights.Length;

            if (totalVisibleLights == 0)
                return -1;

            Light sunLight = RenderSettings.sun;
            int brightestDirectionalLightIndex = -1;
            float brightestLightIntensity = 0.0f;
            for (int i = 0; i < totalVisibleLights; i++)
            {
                VisibleLight currVisibleLight = default;
                HNRenderPipelineUtils.GetVisibleLight(visibleLights, i, ref currVisibleLight);
                Light currLight = currVisibleLight.light;

                if (currVisibleLight == null)
                    break;

                if (currVisibleLight.lightType == LightType.Directional)
                {
                    if (currLight = sunLight)
                        return i;

                    if (currLight.intensity > brightestLightIntensity)
                    {
                        brightestLightIntensity = currLight.intensity;
                        brightestDirectionalLightIndex = i;
                    }
                }
            }

            return brightestDirectionalLightIndex;
        }

        private void UpdateLightGlobalConstantBuffer(ref GlobalConstantBuffer globalConstantBuffer)
        {
            UpdateMainLightGlobalConstantBuffer(ref globalConstantBuffer);
            UpdateEnvironmentLightGlobalConstantBuffer(ref globalConstantBuffer);
        }

        private void UpdateMainLightGlobalConstantBuffer(ref GlobalConstantBuffer globalConstantBuffer)
        {
            Vector4 mainLightDirection = defaultLightPosition;
            Color mainLightColor = defaultLightColor;

            LightData lightData = renderingData.LightData;
            VisibleLight mainVisibleLight = default;
            HNRenderPipelineUtils.GetVisibleLight(lightData.visibleLights, lightData.mainLightIndex, ref mainVisibleLight);
            var mainLightLocalToWorld = mainVisibleLight.localToWorldMatrix;
            Vector4 mainLightMatDir = mainLightLocalToWorld.GetColumn(2);
            mainLightDirection = new Vector4(mainLightMatDir.x, mainLightMatDir.y, mainLightMatDir.z, 0.0f);
            mainLightColor = mainVisibleLight.finalColor;

            globalConstantBuffer._MainLightPosition = -mainLightDirection;
            globalConstantBuffer._MainLightColor = mainLightColor;
        }

        private void UpdateEnvironmentLightGlobalConstantBuffer(ref GlobalConstantBuffer globalConstantBuffer)
        {
            // When glossy reflections are OFF in the shader we set a constant color to use as indirect specular
            SphericalHarmonicsL2 ambientSH = RenderSettings.ambientProbe;
            Color linearGlossyEnvColor = new Color(ambientSH[0, 0], ambientSH[1, 0], ambientSH[2, 0]) * RenderSettings.reflectionIntensity;
            Color glossyEnvColor = CoreUtils.ConvertLinearToActiveColorSpace(linearGlossyEnvColor);
            globalConstantBuffer._GlossyEnvironmentColor = glossyEnvColor;

            globalConstantBuffer._GlossyEnvironmentCubeMap_HDR = ReflectionProbe.defaultTextureHDRDecodeValues;

            globalConstantBuffer.unity_AmbientSky = CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.ambientSkyColor);
            globalConstantBuffer.unity_AmbientEquator = CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.ambientEquatorColor);
            globalConstantBuffer.unity_AmbientGround = CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.ambientGroundColor);

            globalConstantBuffer._SubtractiveShadowColor = CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.subtractiveShadowColor);
        }

        private void UpdateLightGlobalKeywords(RenderingData renderingData)
        {
            var graphObject = renderingData.GraphObject;
            var cmd = renderingData.Cmd;

            var shEvalMode = graphObject.SHEvalMode;
            CoreUtils.SetKeyword(cmd, GlobalKeywords.evaluateSHVertex, shEvalMode == SHEvalMode.PerVertex);
            CoreUtils.SetKeyword(cmd, GlobalKeywords.evaluateSHMixed, shEvalMode == SHEvalMode.Mixed);
        }


        private static readonly Vector3 defaultLightPosition = Vector3.zero;
        private static readonly Color defaultLightColor = Color.white;
    }
}
