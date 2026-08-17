// <copyright file="ClusterCullingReflectionProbePassEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEditor;
using UnityEngine;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// Inspector Editor for <see cref="ClusterCullingReflectionProbePass"/>.
    /// Displays PassName, IsEnabled, and slot information.
    /// </summary>
    public class ClusterCullingReflectionProbePassEditor : PassEditor
    {
        /// <summary>
        /// Draws the Inspector GUI for the given <see cref="ClusterCullingReflectionProbePass"/>.
        /// </summary>
        /// <param name="pass">The pass to inspect. Must not be null.</param>
        public void DrawPass(ClusterCullingReflectionProbePass pass)
        {
            DrawPassGUI(pass);
        }
    }
}
