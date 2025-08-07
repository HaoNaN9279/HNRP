using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HN.HNRP.Editor
{
    public class LitAdvancedOptionsBlock : MaterialGUIBlock
    {
        public LitAdvancedOptionsBlock(uint expandableBit) : base(expandableBit)
        {
            header = new GUIContent("Advanced Options");
        }

        protected override void GetProperties(MaterialProperty[] properties)
        {

        }

        protected override void DrawGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            materialEditor.EnableInstancingField();
        }

        public override void OnValidateMaterial(Material material)
        {
            
        }
    }
}
