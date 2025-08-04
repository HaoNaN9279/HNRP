using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace HN.HNRP.Editor
{
    public abstract class MaterialGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (!(RenderPipelineManager.currentPipeline is HNRenderPipeline))
            {
                base.OnGUI(materialEditor, properties);
            }
            else
            {
                DrawGUI(materialEditor, properties);
            }
        }

        protected abstract void DrawGUI(MaterialEditor materialEditor, MaterialProperty[] properties);


        public class MaterialGUIBlockList : List<MaterialGUIBlock>
        {
            public void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
            {
                foreach (var block in this)
                {
                    block.OnGUI(materialEditor, properties);
                }
            }

            public void OnValidateMaterial(Material material)
            {
                foreach (var block in this)
                {
                    block.OnValidateMaterial(material);
                }
            }
        }


        public enum SurfaceType
        {
            Opaque,
            Transparent,
        }

        public enum BlendMode
        {
            Alpha,
            Premultiply,
            Additive,
            Multiply,
        }

        public enum CullMode
        {
            Back,
            Front,
            Off,
        }

        public enum ZTestMode
        {
            Disabled,
            Never,
            Less,
            Equal,
            LEqual,
            Greater,
            NotEqual,
            GEqual,
            Always,
        }
    }
}
