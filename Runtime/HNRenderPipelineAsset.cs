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
        [SerializeField]
        public bool useDynamicBatching;

        [SerializeField]
        public bool useGPUInstancing;

        [SerializeField]
        public bool useSRPBatcher;

        [SerializeField]
        public bool useLightsPerObjectData;

#if UNITY_EDITOR
        [SerializeField]
        public RenderGraphViewBlock editorRenderGraphViews;
#endif

        [SerializeField]
        public RenderGraphViewBlock runtimeRenderGraphViews;

        [SerializeField]
        public ShadowSettings shadowSettings;

        [SerializeField]
        public PostProcessingSettings postProcessingSettings;


        public HNRenderPipelineAsset()
        {
            useDynamicBatching = false;
            useGPUInstancing = true;
            useSRPBatcher = true;
            useLightsPerObjectData = true;

#if UNITY_EDITOR
            editorRenderGraphViews = new RenderGraphViewBlock(EditorDefaultViews);
#endif
            runtimeRenderGraphViews = new RenderGraphViewBlock(RuntimeDefaultViews);
            shadowSettings = default;
            postProcessingSettings = default;
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
    }
}
