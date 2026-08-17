// <copyright file="ClusterCullingLightPassEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEditor;
using UnityEngine;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// Inspector Editor for <see cref="ClusterCullingLightPass"/>.
    /// Displays PassName, IsEnabled, and slot information.
    /// </summary>
    public class ClusterCullingLightPassEditor : PassEditor
    {
        /// <summary>
        /// Draws the Inspector GUI for the given <see cref="ClusterCullingLightPass"/>.
        /// </summary>
        /// <param name="pass">The pass to inspect. Must not be null.</param>
        public void DrawPass(ClusterCullingLightPass pass)
        {
            DrawPassGUI(pass);
        }
    }
}
