using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public class HNRenderPipelineEditorResources : RenderPipelineResources
    {
        protected override string packagePath => HNRenderPipelineGlobalSettings.HNRenderPipelinePath;

        public ShaderResources shaderResources;
        public MaterialResources materialResources;


        [Serializable, ReloadGroup]
        public class ShaderResources
        {
            [Reload("Runtime/ShaderLibrary/Shaders/Lit.shader")]
            public Shader defaultShader;
        }


        [Serializable, ReloadGroup]
        public class MaterialResources
        {
            [Reload("Runtime/Materials/Lit.mat")]
            public Material defaultMaterial;
        }
    }
}
