using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public class HNRenderPipeline : RenderPipeline
    {
        public static HNRenderPipelineAsset Asset
        {
            get => GraphicsSettings.currentRenderPipeline as HNRenderPipelineAsset;
        }


        public override RenderPipelineGlobalSettings defaultSettings => globalSettings;
        private HNRenderPipelineGlobalSettings globalSettings;

        private CameraRenderer cameraRenderer;


        public HNRenderPipeline(HNRenderPipelineAsset asset)
        {
            GraphicsSettings.lightsUseLinearIntensity = QualitySettings.activeColorSpace == ColorSpace.Linear;
            GraphicsSettings.lightsUseColorTemperature = true;
            GraphicsSettings.defaultRenderingLayerMask = defaultRenderingLayerMask;
            GraphicsSettings.useScriptableRenderPipelineBatching = asset.useSRPBatcher;
            cameraRenderer = new CameraRenderer(asset, globalSettings);

        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            Render(context, new List<Camera>(cameras));
        }

        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            BeginContextRendering(context, cameras);
            
#if UNITY_EDITOR
            if(globalSettings == null || HNRenderPipelineGlobalSettings.Instance == null)
            {
                globalSettings = HNRenderPipelineGlobalSettings.Ensure();
                if(globalSettings == null)
                    return;
            }
#endif
            
            foreach (Camera camera in cameras)
            {
                cameraRenderer.RenderCamera(context, camera);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            cameraRenderer.Dispose();
        }

        internal const int defaultRenderingLayerMask = 0x00000001;
    }
}
