using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    [CreateAssetMenu(menuName = "Rendering/HN Rendering Pipeline Asset")]
    public class HNRenderPipelineAsset : RenderPipelineAsset
    {
        public HNRenderPipelineAsset()
        {
        }

        protected override RenderPipeline CreatePipeline()
        {
            return new HNRenderPipeline(this);
        }

        [SerializeField]
        public CameraPipelineConfig DefaultGameCameraConfig;

#if UNITY_EDITOR
        [SerializeField]
        public CameraPipelineConfig DefaultSceneViewCameraConfig;

        [SerializeField]
        public CameraPipelineConfig DefaultPreviewCameraConfig;
#endif

        [SerializeField]
        public CameraPipelineConfig DefaultReflectionCameraConfig;

        public override string[] renderingLayerMaskNames => globalSettings.RenderingLayerNames;
        public override string[] prefixedRenderingLayerMaskNames => globalSettings.PrefixedRenderingLayerNames;
        public override Material defaultMaterial => editorResources.materialResources.defaultMaterial;
        public override Shader defaultShader => editorResources.shaderResources.defaultShader;

        public HNRenderPipelineGlobalSettings globalSettings => HNRenderPipelineGlobalSettings.Instance;
        internal HNRenderPipelineRuntimeResources runtimeResources => globalSettings.HNRenderPipelineRuntimeResources;
#if UNITY_EDITOR
        internal HNRenderPipelineEditorResources editorResources => globalSettings.HNRenderPipelineEditorResources;
#endif

        public const int MAX_DIRECTIONAL_LIGHT_ON_SCREEN = 16;
        public const int MAX_LOCAL_LIGHT_ON_SCREEN = 512;
    }
}
