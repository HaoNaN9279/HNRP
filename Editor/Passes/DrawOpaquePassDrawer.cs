using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace HN.HNRP.Editor
{
    [CustomPropertyDrawer(typeof(DrawOpaquePass))]
    public class DrawOpaquePassDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();
            var defaultDrawColorProperty = new PropertyField(property.FindPropertyRelative("defaultDrawColor"));
            container.Add(defaultDrawColorProperty);

            return container;
        }
    }
}
