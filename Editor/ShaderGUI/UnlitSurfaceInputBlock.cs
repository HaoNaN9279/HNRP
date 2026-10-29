using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace HN.HNRP.Editor
{
    public class UnlitSurfaceInputBlock : MaterialGUIBlock
    {
        public UnlitSurfaceInputBlock(uint expandableBit) : base(expandableBit)
        {
            header = new GUIContent("Surface Inputs");
        }

        protected override void GetProperties(MaterialProperty[] properties)
        {
            baseMapProperty = GetProperty(properties, MaterialPropertys.baseMap);
            baseColorProperty = GetProperty(properties, MaterialPropertys.baseColor);
            alphaRemapMinProperty = GetProperty(properties, MaterialPropertys.alphaRemapMin);
            alphaRemapMaxProperty = GetProperty(properties, MaterialPropertys.alphaRemapMax);
        }

        protected override void DrawGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            // 基础贴图 / 颜色参数
            DrawTextureAndColor(materialEditor, baseMapProperty, baseColorProperty, Styles.baseMap);

            // Alpha（仅透明 + 有基础贴图时提供 remap）
            MaterialGUI.SurfaceType surfaceType = (MaterialGUI.SurfaceType)(materialEditor.target as Material).GetFloat(MaterialPropertys.surfaceType);
            if (baseMapProperty != null && baseMapProperty.textureValue != null && surfaceType == MaterialGUI.SurfaceType.Transparent)
            {
                DrawMinMaxSlider(materialEditor, alphaRemapMinProperty, alphaRemapMaxProperty, 0.0f, 1.0f, Styles.alphaRemapping);
            }

            // 基础贴图缩放偏移
            DrawTextureScaleOffset(materialEditor, baseMapProperty);
        }

        public override void OnValidateMaterial(Material material)
        {
            SetMaterialKeywords(material);
        }

        protected void SetMaterialKeywords(Material material)
        {
            SetKeywordByTexture(material, MaterialPropertys.baseMap, MaterialLitKeywords.basemap);
        }


        protected MaterialProperty baseMapProperty = null;
        protected MaterialProperty baseColorProperty = null;
        protected MaterialProperty alphaRemapMinProperty = null;
        protected MaterialProperty alphaRemapMaxProperty = null;


        protected static class Styles
        {
            public static readonly GUIContent baseMap = EditorGUIUtility.TrTextContent("Base Map", "Color(RGB) Alpha(A)");
            public static readonly GUIContent alphaRemapping = EditorGUIUtility.TrTextContent("Alpha Remapping", "(0, 1)");
        }
    }
}
