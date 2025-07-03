using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace HN.HNRP.Editor
{
    public class LitGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            base.OnGUI(materialEditor, properties);

            Material material = materialEditor.target as Material;

            BlendMode srcBlend = (BlendMode)material.GetFloat("_SrcBlend");
            BlendMode dstBlend = (BlendMode)material.GetFloat("_DstBlend");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Blend Mode");
            EditorGUILayout.BeginHorizontal();
            srcBlend = (BlendMode)EditorGUILayout.EnumPopup(srcBlend);
            dstBlend = (BlendMode)EditorGUILayout.EnumPopup(dstBlend);
            EditorGUILayout.EndHorizontal();

            material.SetFloat("_SrcBlend", (float)srcBlend);
            material.SetFloat("_DstBlend", (float)dstBlend);

            float alphaClip = material.GetFloat("_AlphaClip");
            if (alphaClip > 0.5f)
                material.EnableKeyword("_ALPHATEST_ON");
            else
                material.DisableKeyword("_ALPHATEST_ON");
        }
    }
}
