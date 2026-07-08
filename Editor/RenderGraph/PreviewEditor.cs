using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.Rendering;

namespace HN.HNRP.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Preview))]
    public class PreviewEditor : HNRenderGraphBaseEditor
    {
        protected override void DrawSettings()
        {

        }
    }
}
