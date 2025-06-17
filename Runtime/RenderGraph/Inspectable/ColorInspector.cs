using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HN.Serialize;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace HN.HNRP
{
    public class ColorInspector : InspectableInfo
    {
        string label;
        bool isHdr;
        bool hasAlpha;
        Color value;


        public ColorInspector(string label, bool isHdr, bool hasAlpha)
        {
            this.label = label;
            this.isHdr = isHdr;
            this.hasAlpha = hasAlpha;
        }

        public override VisualElement Inspect(JsonData jsonData, FieldInfo fieldInfo)
        {            
            value = (Color)fieldInfo.GetValue(jsonData.Obj);
            
            var colorField = new ColorField(label)
            {
                value = this.value,
                hdr = isHdr,
                showAlpha = hasAlpha
            };
            colorField.RegisterValueChangedCallback((e) =>
            {
                value = e.newValue;
                fieldInfo.SetValue(jsonData.Obj, value);
                jsonData.Serialize();
            });

            return colorField;
        }
    }
}
