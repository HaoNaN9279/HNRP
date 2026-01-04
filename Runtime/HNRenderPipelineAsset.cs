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
            editorRenderGraphViews = new RenderGraphViewBlock(EditorDefaultViews);
#endif
            runtimeRenderGraphViews = new RenderGraphViewBlock(RuntimeDefaultViews);
        }

        protected override RenderPipeline CreatePipeline()
        {
            return new HNRenderPipeline(this);
        }


        public static string[] EditorDefaultViews = new string[]
        {
            "SceneView",
            "Preview",
            "Reflection",
        };

        public static string[] RuntimeDefaultViews = new string[]
        {
            "MainGameView",
        };


#if UNITY_EDITOR
        [SerializeField]
        public RenderGraphViewBlock editorRenderGraphViews;
#endif

        [SerializeField]
        public RenderGraphViewBlock runtimeRenderGraphViews;


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
