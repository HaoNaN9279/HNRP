using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace HN.HNRP.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TransparencyPass))]
    public class TransparencyPassEditor : PassBaseEditor
    {
        public override VisualElement CreateInspectorGUI()
        {
            return new VisualElement();
        }
    }
}
