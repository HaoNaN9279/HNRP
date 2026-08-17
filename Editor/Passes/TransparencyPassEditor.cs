// <copyright file="TransparencyPassEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEditor;
using UnityEngine;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// Inspector Editor for <see cref="TransparencyPass"/>.
    /// Displays PassName, IsEnabled, slot information, and RenderingLayerMask.
    /// </summary>
    public class TransparencyPassEditor : PassEditor
    {
        /// <summary>
        /// Draws the Inspector GUI for the given <see cref="TransparencyPass"/>.
        /// </summary>
        /// <param name="pass">The pass to inspect. Must not be null.</param>
        public void DrawPass(TransparencyPass pass)
        {
            DrawPassGUI(pass);

            if (pass != null)
            {
                EditorGUILayout.Space();
                pass.RenderingLayerMask = (uint)EditorGUILayout.LongField(
                    "Rendering Layer Mask", (long)pass.RenderingLayerMask);
            }
        }
    }
}
