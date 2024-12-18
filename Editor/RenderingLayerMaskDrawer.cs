using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.Rendering;

namespace HN.HNRP.Editor
{
    [CustomPropertyDrawer(typeof(RenderingLayerMaskFieldAttribute))]
    public class RenderingLayerMaskDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            int mask = property.intValue;
            List<string> layerNames = new List<string>();
            Debug.Log(layerNames.Count);
            if (GraphicsSettings.currentRenderPipeline != null && GraphicsSettings.currentRenderPipeline.renderingLayerMaskNames != null)
            {
                foreach (string name in GraphicsSettings.currentRenderPipeline.renderingLayerMaskNames)
                {
                    layerNames.Add(name);
                }
                var maskField = new MaskField("Rendering Layer Mask", layerNames, -1, OnSelected, OnListItem);
                return maskField;
            }
            return null;
        }


        private string OnSelected(string arg)
        {
            Debug.Log("OnSelected");
            return arg;
        }

        private string OnListItem(string arg)
        {
            Debug.Log("OnListItem");
            return arg;
        }
    }
}
