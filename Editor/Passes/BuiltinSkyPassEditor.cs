// <copyright file="BuiltinSkyPassEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEditor;
using UnityEngine;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// Inspector Editor for <see cref="BuiltinSkyPass"/>.
    /// Displays PassName, IsEnabled, and slot information.
    /// </summary>
    public class BuiltinSkyPassEditor : PassEditor
    {
        /// <summary>
        /// Draws the Inspector GUI for the given <see cref="BuiltinSkyPass"/>.
        /// </summary>
        /// <param name="pass">The pass to inspect. Must not be null.</param>
        public void DrawPass(BuiltinSkyPass pass)
        {
            DrawPassGUI(pass);
        }
    }
}
