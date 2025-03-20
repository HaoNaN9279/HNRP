using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace HN.HNRP.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HNRenderGraph))]
    public class HNRenderGraphEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            return base.CreateInspectorGUI();
        }

    }
}
