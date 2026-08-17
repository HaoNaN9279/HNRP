// <copyright file="BuildLightDataPassEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEditor;
using UnityEngine;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// Inspector Editor for <see cref="BuildLightDataPass"/>.
    /// Displays PassName, IsEnabled, and slot information.
    /// </summary>
    /// <remarks>
    /// This Editor is instantiated programmatically (e.g., by a
    /// <see cref="RenderGraphAsset"/> inspector) since <see cref="Pass"/>
    /// is not a <see cref="UnityEngine.Object"/>.
    /// </remarks>
    public class BuildLightDataPassEditor : PassEditor
    {
        /// <summary>
        /// Draws the Inspector GUI for the given <see cref="BuildLightDataPass"/>.
        /// </summary>
        /// <param name="pass">The pass to inspect. Must not be null.</param>
        public void DrawPass(BuildLightDataPass pass)
        {
            DrawPassGUI(pass);
        }
    }
}
