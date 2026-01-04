using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    [CreateAssetMenu(menuName = "Rendering/HN Rendering Pipeline Asset")]
    public class HNRenderPipelineAsset : RenderPipelineAsset
    {
        public HNRenderPipelineAsset()
        {
#if UNITY_EDITOR
            sceneViewRenderGraphViews = new SceneViewRenderGraphViewBlock();
            previewRenderGraphViews = new PreviewRenderGraphViewBlock();
#endif
            gameViewRenderGraphViews = new GameViewRenderGraphViewBlock();
            reflectionRenderGraphViews = new ReflectionRenderGraphViewBlock();
        }

        protected override RenderPipeline CreatePipeline()
        {
            return new HNRenderPipeline(this);
        }


#if UNITY_EDITOR
        [SerializeField]
        public SceneViewRenderGraphViewBlock sceneViewRenderGraphViews;

        [SerializeField]
        public PreviewRenderGraphViewBlock previewRenderGraphViews;
#endif

        [SerializeField]
        public GameViewRenderGraphViewBlock gameViewRenderGraphViews;

        [SerializeField]
        public ReflectionRenderGraphViewBlock reflectionRenderGraphViews;


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
        public const int MAX_REFLECTION_PROBES_ON_SCREEN = 16;
    }
}
