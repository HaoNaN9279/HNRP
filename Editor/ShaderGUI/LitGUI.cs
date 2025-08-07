using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;

namespace HN.HNRP.Editor
{
    public sealed class LitGUI : MaterialGUI
    {
        public MaterialGUIBlockList blocks = new MaterialGUIBlockList
        {
            new LitSurfaceOptionsBlock((uint)LitGUIBlocks.LitSurfaceOptionsBlock),
            new LitSurfaceInputBlock((uint)LitGUIBlocks.LitSurfaceInputBlock),
            new LitAdvancedOptionsBlock((uint)LitGUIBlocks.LitAdvancedOptionsBlock),
        };

        protected override void DrawGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            blocks.OnGUI(materialEditor, properties);
        }

        public override void ValidateMaterial(Material material)
        {
            blocks.OnValidateMaterial(material);
        }

        public MaterialProperty GetProperty(MaterialProperty[] properties, string propertyName)
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

        [Flags]
        public enum LitGUIBlocks : uint
        {
            LitSurfaceOptionsBlock = 1 << 0,
            LitSurfaceInputBlock = 1 << 1,
            LitAdvancedOptionsBlock = 1 << 2,
        }



    }
}
