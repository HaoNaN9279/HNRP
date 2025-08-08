using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace HN.HNRP.Editor
{
    public class LitSurfaceInputBlock : MaterialGUIBlock
    {
        public LitSurfaceInputBlock(uint expandableBit) : base(expandableBit)
        {
            header = new GUIContent("Surface Inputs");
        }

        protected override void GetProperties(MaterialProperty[] properties)
        {
            baseMapProperty = GetProperty(properties, Propertys.baseMap);
            baseColorProperty = GetProperty(properties, Propertys.baseColor);
            alphaRemapMinProperty = GetProperty(properties, Propertys.alphaRemapMin);
            alphaRemapMaxProperty = GetProperty(properties, Propertys.alphaRemapMax);
            maskMapProperty = GetProperty(properties, Propertys.maskMap);
            metallicRemapMinProperty = GetProperty(properties, Propertys.metallicRemapMin);
            metallicRemapMaxProperty = GetProperty(properties, Propertys.metallicRemapMax);
            smoothnessRemapMinProperty = GetProperty(properties, Propertys.smoothnessRemapMin);
            smoothnessRemapMaxProperty = GetProperty(properties, Propertys.smoothnessRemapMax);
            aoRemapMinProperty = GetProperty(properties, Propertys.aoRemapMin);
            aoRemapMaxProperty = GetProperty(properties, Propertys.aoRemapMax);
            metallicProperty = GetProperty(properties, Propertys.metallic);
            smoothnessProperty = GetProperty(properties, Propertys.smoothness);
            normalMapProperty = GetProperty(properties, Propertys.normalMap);
            normalScaleProperty = GetProperty(properties, Propertys.normalScale);
            emissionMapProperty = GetProperty(properties, Propertys.emissionMap);
            emissionColorProperty = GetProperty(properties, Propertys.emissionColor);
        }

        protected override void DrawGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            // Base Map
            DrawTextureAndColor(materialEditor, baseMapProperty, baseColorProperty, Styles.baseMap);

            // Alpha
            MaterialGUI.SurfaceType blendMode = (MaterialGUI.SurfaceType)(materialEditor.target as Material).GetFloat(Propertys.surfaceType);
            if (baseMapProperty != null && baseMapProperty.textureValue != null && blendMode == MaterialGUI.SurfaceType.Transparent)
            {
                DrawMinMaxSlider(materialEditor, alphaRemapMinProperty, alphaRemapMaxProperty, 0.0f, 1.0f, Styles.alphaRemapping);
            }

            // Smoothness Metallic
            if (maskMapProperty == null || maskMapProperty.textureValue == null)
            {
                DrawSlider(materialEditor, smoothnessProperty, Styles.smoothness);
                DrawSlider(materialEditor, metallicProperty, Styles.metallic);
            }

            EditorGUILayout.Space();

            // Mask Map
            DrawTexture(materialEditor, maskMapProperty, Styles.maskMap);

            // SmoothnessRemap MetallicRemap AORemap
            if (maskMapProperty != null && maskMapProperty.textureValue != null)
            {
                DrawMinMaxSlider(materialEditor, smoothnessRemapMinProperty, smoothnessRemapMaxProperty, 0.0f, 1.0f, Styles.smoothnessRemapping);
                DrawMinMaxSlider(materialEditor, metallicRemapMinProperty, metallicRemapMaxProperty, 0.0f, 1.0f, Styles.metallicRemapping);
                DrawMinMaxSlider(materialEditor, aoRemapMinProperty, aoRemapMaxProperty, 0.0f, 1.0f, Styles.aoRemapping);
            }

            // Normal Map
            DrawTextureAndSlider(materialEditor, normalMapProperty, normalScaleProperty, Styles.normalMap);

            // Emission Map
            DrawTextureAndColor(materialEditor, emissionMapProperty, emissionColorProperty, Styles.emissionMap);

            // Base Scale Offset
            DrawTextureScaleOffset(materialEditor, baseMapProperty);
        }

        public override void OnValidateMaterial(Material material)
        {
            SetMaterialKeywords(material);
        }

        protected void SetMaterialKeywords(Material material)
        {
            SetKeywordByTexture(material, Propertys.baseMap, MaterialLitKeywords.basemap);    
            SetKeywordByTexture(material, Propertys.normalMap, MaterialLitKeywords.normalMap);
            SetKeywordByTexture(material, Propertys.maskMap, MaterialLitKeywords.maskMap);
            SetKeywordByTexture(material, Propertys.emissionMap, MaterialLitKeywords.emissionMap);
        }


        protected MaterialProperty baseMapProperty = null;
        protected MaterialProperty baseColorProperty = null;
        protected MaterialProperty alphaRemapMinProperty = null;
        protected MaterialProperty alphaRemapMaxProperty = null;
        protected MaterialProperty maskMapProperty = null;
        protected MaterialProperty metallicRemapMinProperty = null;
        protected MaterialProperty metallicRemapMaxProperty = null;
        protected MaterialProperty smoothnessRemapMinProperty = null;
        protected MaterialProperty smoothnessRemapMaxProperty = null;
        protected MaterialProperty aoRemapMinProperty = null;
        protected MaterialProperty aoRemapMaxProperty = null;
        protected MaterialProperty metallicProperty = null;
        protected MaterialProperty smoothnessProperty = null;
        protected MaterialProperty normalMapProperty = null;
        protected MaterialProperty normalScaleProperty = null;
        protected MaterialProperty emissionMapProperty = null;
        protected MaterialProperty emissionColorProperty = null;


        protected static class Styles
        {
            public static readonly GUIContent baseMap = EditorGUIUtility.TrTextContent("Base Map", "Color(RGB) Alpha(A)");
            public static readonly GUIContent alphaRemapping = EditorGUIUtility.TrTextContent("Alpha Remapping", "(0, 1)");
            public static readonly GUIContent maskMap = EditorGUIUtility.TrTextContent("Mask Map", "Smoothness(R) Metallic(G) Occlusion(B) DetailMask(A)");
            public static readonly GUIContent metallicRemapping = EditorGUIUtility.TrTextContent("Metallic Remap", "(0, 1)");
            public static readonly GUIContent smoothnessRemapping = EditorGUIUtility.TrTextContent("Smoothness Remap", "(0, 1)");
            public static readonly GUIContent aoRemapping = EditorGUIUtility.TrTextContent("AO Remap", "(0, 1)");
            public static readonly GUIContent metallic = EditorGUIUtility.TrTextContent("Metallic", "(0, 1)");
            public static readonly GUIContent smoothness = EditorGUIUtility.TrTextContent("Smoothness", "(0, 1)");
            public static readonly GUIContent normalMap = EditorGUIUtility.TrTextContent("Normal Map", "(0, 8)");
            public static readonly GUIContent emissionMap = EditorGUIUtility.TrTextContent("Emission Map", "");
        }


    }
}
