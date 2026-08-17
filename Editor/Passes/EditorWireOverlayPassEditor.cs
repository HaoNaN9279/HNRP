// <copyright file="EditorWireOverlayPassEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEditor;
using UnityEngine;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// Inspector Editor for <see cref="EditorWireOverlayPass"/>.
    /// Displays PassName, IsEnabled, and slot information.
    /// </summary>
    public class EditorWireOverlayPassEditor : PassEditor
    {
        /// <summary>
        /// Draws the Inspector GUI for the given <see cref="EditorWireOverlayPass"/>.
        /// </summary>
        /// <param name="pass">The pass to inspect. Must not be null.</param>
        public void DrawPass(EditorWireOverlayPass pass)
        {
            DrawPassGUI(pass);
        }
    }
}
