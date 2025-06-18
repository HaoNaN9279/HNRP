using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.Rendering;

namespace HN.HNRP.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Standard))]
    public class StandardEditor : HNRenderGraphBaseEditor
    {
        protected override void DrawSettings()
        {

        }
    }
}
