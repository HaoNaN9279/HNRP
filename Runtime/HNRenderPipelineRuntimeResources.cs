using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public class HNRenderPipelineRuntimeResources : RenderPipelineResources
    {
        protected override string packagePath => HNRenderPipelineGlobalSettings.HNRenderPipelinePath;


        [Serializable]
        public class ShaderResources
        {
            public Shader singleBlit;
        }

        public ShaderResources shaderResources;
    }
}
