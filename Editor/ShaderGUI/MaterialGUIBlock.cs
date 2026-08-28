using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Graphs;

namespace HN.HNRP.Editor
{
    public abstract class MaterialGUIBlock
    {
        public MaterialGUIBlock(uint expandableBit)
        {
            this.expandableBit = expandableBit;
        }

        public void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            GetProperties(properties);
            
            using var scope = new MaterialHeaderScope(header, expandableBit, materialEditor);
            if (scope.expanded)
            {
                DrawGUI(materialEditor, properties);
            }
        }

        protected MaterialProperty GetProperty(MaterialProperty[] properties, string propertyName)
        {
            if (properties == null | properties.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < properties.Length; i++)
            {
                if (propertyName == properties[i].name)
                {
                    return properties[i];
                }
            }
            return null;
        }

        protected void SetKeywordByTexture(Material material, string texturePropertyName, string keyword)
        {
            if (material.HasProperty(texturePropertyName) && material.GetTexture(texturePropertyName) != null)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
            
        }

        protected void SetKeywordByFloat(Material material, string floatPropertyName, string keyword)
        {
            if (material.HasProperty(floatPropertyName) && material.GetFloat(floatPropertyName) > 0.5f)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }

        protected void SetKeywordByInt(Material material, string floatPropertyName, int value, string keyword)
        {
            if (material.HasProperty(floatPropertyName) && (int)material.GetFloat(floatPropertyName) == value)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }

        protected void SetKeywordByEnum(Material material, string enumPropertyName, string[] keywords)
        {
            if(material.HasProperty(enumPropertyName))
            {
                int enumValue = (int)material.GetFloat(enumPropertyName);
                for (int i = 0; i < keywords.Length; i++)
                {
                    if (i == enumValue)
                    {
                        material.EnableKeyword(keywords[i]);
                    }
                    else
                    {
                        material.DisableKeyword(keywords[i]);
                    }
                }
            }
        }

        protected void DrawPopup(MaterialEditor materialEditor, MaterialProperty property, GUIContent label, string[] options)
        {
            if (property == null)
            {
                return;
            }

            materialEditor.PopupShaderProperty(property, label, options);
        }

        protected void DrawFloatToggle(MaterialProperty property, GUIContent label)
        {
            if (property == null)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            MaterialEditor.BeginProperty(property);
            bool newValue = EditorGUILayout.Toggle(label, property.floatValue == 1);
            if (EditorGUI.EndChangeCheck())
            {
                property.floatValue = newValue ? 1.0f : 0.0f;
            }
            MaterialEditor.EndProperty();
        }

        protected void DrawQueueOffset(MaterialEditor materialEditor, MaterialProperty property, int lowerBound, int upperBound, GUIContent label)
        {
            if (property == null)
            {
                return;
            }

            materialEditor.IntSliderShaderProperty(property, 0, upperBound - lowerBound, label);
        }

        protected void DrawTextureAndColor(MaterialEditor materialEditor, MaterialProperty textureProperty, MaterialProperty colorProperty, GUIContent label)
        {
            if (textureProperty == null || colorProperty == null)
            {
                return;
            }

            materialEditor.TexturePropertySingleLine(label, textureProperty, colorProperty);
        }

        protected void DrawMinMaxSlider(MaterialEditor materialEditor, MaterialProperty minProperty, MaterialProperty maxProperty, float minLimit, float maxLimit, GUIContent label)
        {
            if (minProperty == null || maxProperty == null)
            {
                return;
            }

            materialEditor.MinMaxShaderProperty(minProperty, maxProperty, minLimit, maxLimit, label);
        }

        protected void DrawTexture(MaterialEditor materialEditor, MaterialProperty property, GUIContent label)
        {
            if (property == null)
            {
                return;
            }

            materialEditor.TexturePropertySingleLine(label, property);
        }

        protected void DrawSlider(MaterialEditor materialEditor, MaterialProperty property, GUIContent label)
        {
            if (property == null)
            {
                return;
            }

            materialEditor.ShaderProperty(property, label);
        }

        protected void DrawTextureAndSlider(MaterialEditor materialEditor, MaterialProperty textureProperty, MaterialProperty floatProperty, GUIContent label)
        {
            if (textureProperty == null || floatProperty == null)
            {
                return;
            }

            materialEditor.TexturePropertySingleLine(label, textureProperty, floatProperty);
        }

        protected void DrawTextureScaleOffset(MaterialEditor materialEditor, MaterialProperty textureProperty)
        {
            if (textureProperty == null)
            {
                return;
            }

            materialEditor.TextureScaleOffsetProperty(textureProperty);
        }

        public abstract void OnValidateMaterial(Material material);
        protected abstract void DrawGUI(MaterialEditor materialEditor, MaterialProperty[] properties);
        protected abstract void GetProperties(MaterialProperty[] properties);


        protected GUIContent header;
        protected uint expandableBit;
    }
}
