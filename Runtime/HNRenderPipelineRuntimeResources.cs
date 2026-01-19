using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public class HNRenderPipelineRuntimeResources : RenderPipelineResources
    {
        protected override string packagePath => HNRenderPipelineGlobalSettings.HNRenderPipelinePath;

        public ShaderResources shaderResources;


        [Serializable, ReloadGroup]
        public class ShaderResources
        {
            [Reload("Runtime/ShaderLibrary/Shaders/Lit.shader")]
            public Shader Lit;

            [Reload("Runtime/ShaderLibrary/Shaders/Blit.shader")]
            public Shader Blit;

            [Reload("Runtime/ShaderLibrary/Shaders/BlitColorAndDepth.shader")]
            public Shader BlitColorAndDepth;
        }
    }
}
