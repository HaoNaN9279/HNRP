using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Rendering;

namespace HN.HNRP.Editor
{
    public class LitSurfaceOptionsBlock : MaterialGUIBlock
    {
        public LitSurfaceOptionsBlock(uint expandableBit) : base(expandableBit)
        {
            header = new GUIContent("Surface Options");
        }

        protected override void GetProperties(MaterialProperty[] properties)
        {
            surfaceTypeProperty = GetProperty(properties, MaterialPropertys.surfaceType);
            blendModeProperty = GetProperty(properties, MaterialPropertys.blendMode);
            alphaClipProperty = GetProperty(properties, MaterialPropertys.alphaClip);
            cutoffProperty = GetProperty(properties, MaterialPropertys.cutoff);
            cullModeProperty = GetProperty(properties, MaterialPropertys.cullMode);
            ztestModeProperty = GetProperty(properties, MaterialPropertys.ztestMode);
            zwriteProperty = GetProperty(properties, MaterialPropertys.zwrite);
            queueOffsetProperty = GetProperty(properties, MaterialPropertys.queueOffset);
        }

        protected override void DrawGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            DrawSurfaceType(materialEditor);
        }

        public override void OnValidateMaterial(Material material)
        {
            SetMaterialRenderQueue(material);
            SetMaterialBlendMode(material);
            SetMaterialKeywords(material);
        }

        protected void DrawSurfaceType(MaterialEditor materialEditor)
        {
            // Surface Type
            DrawPopup(materialEditor, surfaceTypeProperty, Styles.surfaceType, Styles.surfaceTypeNames);
            if ((MaterialGUI.SurfaceType)surfaceTypeProperty.floatValue == MaterialGUI.SurfaceType.Opaque)
            {
                EditorGUI.indentLevel++;
                // ZWrite Mode
                DrawFloatToggle(zwriteProperty, Styles.zwrite);
                EditorGUI.indentLevel--;
            }
            else if ((MaterialGUI.SurfaceType)surfaceTypeProperty.floatValue == MaterialGUI.SurfaceType.Transparent)
            {
                EditorGUI.indentLevel++;
                // Blend Mode
                DrawPopup(materialEditor, blendModeProperty, Styles.BlendMode, Styles.blendModeNames);
                EditorGUI.indentLevel--;
            }

            // Alpha Clip
            DrawFloatToggle(alphaClipProperty, Styles.alphaClip);
            if ((alphaClipProperty != null) && (cutoffProperty != null) && alphaClipProperty.floatValue == 1)
            {
                // Cutoff
                materialEditor.ShaderProperty(cutoffProperty, Styles.cutoff, 1);
            }

            // Cull Mode
            DrawPopup(materialEditor, cullModeProperty, Styles.cullMode, Styles.cullModeNames);

            // ZTest Mode
            DrawPopup(materialEditor, ztestModeProperty, Styles.ztestMode, Styles.ztestModeNames);

            // Queue Offset
            if ((MaterialGUI.SurfaceType)surfaceTypeProperty.floatValue == MaterialGUI.SurfaceType.Opaque)
            {
                if (alphaClipProperty != null && alphaClipProperty.floatValue > 0.5f)
                {
                    DrawQueueOffset(materialEditor, queueOffsetProperty, HNRenderQueue.OpaqueAlphaTest.lowerBound, HNRenderQueue.OpaqueAlphaTest.upperBound, Styles.queueOffset);
                }
                else
                {
                    DrawQueueOffset(materialEditor, queueOffsetProperty, HNRenderQueue.OpaqueNoAlphaTest.lowerBound, HNRenderQueue.OpaqueNoAlphaTest.upperBound, Styles.queueOffset);
                }
            }
            else if ((MaterialGUI.SurfaceType)surfaceTypeProperty.floatValue == MaterialGUI.SurfaceType.Transparent)
            {
                DrawQueueOffset(materialEditor, queueOffsetProperty, HNRenderQueue.Transparent.lowerBound, HNRenderQueue.Transparent.upperBound, Styles.queueOffset);
            }
        }

        protected void SetMaterialRenderQueue(Material material)
        {
            if (surfaceTypeProperty == null)
            {
                return;
            }

            int renderQueue = material.shader.renderQueue;
            if ((MaterialGUI.SurfaceType)surfaceTypeProperty.floatValue == MaterialGUI.SurfaceType.Opaque)
            {
                if (alphaClipProperty == null)
                {
                    return;
                }

                if (alphaClipProperty.floatValue > 0.5f)
                {
                    renderQueue = (int)HNRenderQueue.Priority.OpaqueAlphaTest;
                }
                else
                {
                    renderQueue = (int)HNRenderQueue.Priority.Opaque;
                }
            }
            else if ((MaterialGUI.SurfaceType)surfaceTypeProperty.floatValue == MaterialGUI.SurfaceType.Transparent)
            {
                renderQueue = (int)HNRenderQueue.Priority.Transparent;
            }

            if (queueOffsetProperty == null)
            {
                return;
            }

            renderQueue += (int)queueOffsetProperty.floatValue;
            material.renderQueue = renderQueue;
        }

        protected void SetMaterialBlendMode(Material material)
        {
            if (blendModeProperty == null)
            {
                return;
            }

            MaterialGUI.BlendMode blendMode = (MaterialGUI.BlendMode)blendModeProperty.floatValue;
            var srcBlendRGB = BlendMode.One;
            var dstBlendRGB = BlendMode.OneMinusSrcAlpha;
            var srcBlendA = BlendMode.One;
            var dstBlendA = BlendMode.OneMinusSrcAlpha;

            if ((MaterialGUI.SurfaceType)surfaceTypeProperty.floatValue == MaterialGUI.SurfaceType.Opaque)
            {
                srcBlendRGB = BlendMode.One;
                dstBlendRGB = BlendMode.Zero;
                srcBlendA = BlendMode.One;
                dstBlendA = BlendMode.Zero;
            }
            else if((MaterialGUI.SurfaceType)surfaceTypeProperty.floatValue == MaterialGUI.SurfaceType.Transparent)
            {
                switch (blendMode)
                {
                    // srcRGB * srcAlpha + dstRGB * (1 - srcAlpha)
                    // preserve spec:
                    // srcRGB * (<in shader> ? 1 : srcAlpha) + dstRGB * (1 - srcAlpha)
                    case MaterialGUI.BlendMode.Alpha:
                        srcBlendRGB = BlendMode.SrcAlpha;
                        dstBlendRGB = BlendMode.OneMinusSrcAlpha;
                        srcBlendA = BlendMode.One;
                        dstBlendA = dstBlendRGB;
                        break;

                    // srcRGB < srcAlpha, (alpha multiplied in asset)
                    // srcRGB * 1 + dstRGB * (1 - srcAlpha)
                    case MaterialGUI.BlendMode.Premultiply:
                        srcBlendRGB = BlendMode.One;
                        dstBlendRGB = BlendMode.OneMinusSrcAlpha;
                        srcBlendA = srcBlendRGB;
                        dstBlendA = dstBlendRGB;
                        break;

                    // srcRGB * srcAlpha + dstRGB * 1, (alpha controls amount of addition)
                    // preserve spec:
                    // srcRGB * (<in shader> ? 1 : srcAlpha) + dstRGB * (1 - srcAlpha)
                    case MaterialGUI.BlendMode.Additive:
                        srcBlendRGB = BlendMode.SrcAlpha;
                        dstBlendRGB = BlendMode.One;
                        srcBlendA = BlendMode.One;
                        dstBlendA = dstBlendRGB;
                        break;

                    // srcRGB * 0 + dstRGB * srcRGB
                    // in shader alpha controls amount of multiplication, lerp(1, srcRGB, srcAlpha)
                    // Multiply affects color only, keep existing alpha.
                    case MaterialGUI.BlendMode.Multiply:
                        srcBlendRGB = BlendMode.DstColor;
                        dstBlendRGB = BlendMode.Zero;
                        srcBlendA = BlendMode.Zero;
                        dstBlendA = BlendMode.One;
                        break;
                }
            }

            if (material.HasProperty(MaterialPropertys.srcBlend))
            {
                material.SetFloat(MaterialPropertys.srcBlend, (float)srcBlendRGB);
            }
            if (material.HasProperty(MaterialPropertys.dstBlend))
            {
                material.SetFloat(MaterialPropertys.dstBlend, (float)dstBlendRGB);
            }
            if (material.HasProperty(MaterialPropertys.srcBlendAlpha))
            {
                material.SetFloat(MaterialPropertys.srcBlendAlpha, (float)srcBlendA);
            }
            if (material.HasProperty(MaterialPropertys.dstBlendAlpha))
            {
                material.SetFloat(MaterialPropertys.dstBlendAlpha, (float)dstBlendA);
            }
        }

        protected void SetMaterialKeywords(Material material)
        {
            SetKeywordByInt(material, MaterialPropertys.blendMode, (int)MaterialGUI.BlendMode.Premultiply, MaterialLitKeywords.alphaPremultiply);
            SetKeywordByFloat(material, MaterialPropertys.alphaClip, MaterialLitKeywords.alphaTest);
        }


        protected MaterialProperty surfaceTypeProperty = null;
        protected MaterialProperty blendModeProperty = null;
        protected MaterialProperty alphaClipProperty = null;
        protected MaterialProperty cutoffProperty = null;
        protected MaterialProperty cullModeProperty = null;
        protected MaterialProperty ztestModeProperty = null;
        protected MaterialProperty zwriteProperty = null;
        protected MaterialProperty queueOffsetProperty = null;


        protected static class Styles
        {
            public static readonly GUIContent surfaceType = EditorGUIUtility.TrTextContent("Surface Type", "Select a surface type for your texture. Choose between Opaque or Transparent.");
            public static readonly GUIContent BlendMode = EditorGUIUtility.TrTextContent("Blend Mode", "");
            public static readonly GUIContent alphaClip = EditorGUIUtility.TrTextContent("Alpha Clip", "");
            public static readonly GUIContent cutoff = EditorGUIUtility.TrTextContent("Cutoff", "");
            public static readonly GUIContent cullMode = EditorGUIUtility.TrTextContent("Cull Mode", "");
            public static readonly GUIContent ztestMode = EditorGUIUtility.TrTextContent("ZTest Mode", "");
            public static readonly GUIContent zwrite = EditorGUIUtility.TrTextContent("ZWrite", "");
            public static readonly GUIContent queueOffset = EditorGUIUtility.TrTextContent("Queue Offset", "");

            public static string[] surfaceTypeNames = Enum.GetNames(typeof(MaterialGUI.SurfaceType));
            public static string[] blendModeNames = Enum.GetNames(typeof(MaterialGUI.BlendMode));
            public static string[] cullModeNames = Enum.GetNames(typeof(MaterialGUI.CullMode));
            public static string[] ztestModeNames = Enum.GetNames(typeof(MaterialGUI.ZTestMode));
        }

    }
}
