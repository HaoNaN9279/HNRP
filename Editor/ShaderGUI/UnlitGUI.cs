using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;

namespace HN.HNRP.Editor
{
    public sealed class UnlitGUI : MaterialGUI
    {
        public MaterialGUIBlockList blocks = new MaterialGUIBlockList
        {
            new LitSurfaceOptionsBlock((uint)UnlitGUIBlocks.UnlitSurfaceOptionsBlock),
            new UnlitSurfaceInputBlock((uint)UnlitGUIBlocks.UnlitSurfaceInputBlock),
            new LitAdvancedOptionsBlock((uint)UnlitGUIBlocks.UnlitAdvancedOptionsBlock),
            new PackageListBlock((uint)UnlitGUIBlocks.PackageListBlock),
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
        public enum UnlitGUIBlocks : uint
        {
            UnlitSurfaceOptionsBlock = 1 << 0,
            UnlitSurfaceInputBlock = 1 << 1,
            UnlitAdvancedOptionsBlock = 1 << 2,
            PackageListBlock = 1 << 3,
        }
    }
}
