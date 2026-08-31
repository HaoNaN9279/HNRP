// <copyright file="ClusterCullingReflectionProbePassEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System.Collections.Generic;
using UnityEngine;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// Inspector Editor for <see cref="ClusterCullingReflectionProbePass"/>.
    /// This pass exposes no serialized parameters; the default field traversal
    /// draws only the common header.
    /// </summary>
    [PassEditor(typeof(ClusterCullingReflectionProbePass))]
    public class ClusterCullingReflectionProbePassEditor : PassEditor
    {
        /// <inheritdoc />
        public override IReadOnlyList<IPassPreset> Presets => s_Presets;

        /// <summary>
        /// 内置预设集合。新增预设：加一个静态字段并追加到此数组。
        /// </summary>
        private static readonly IPassPreset[] s_Presets =
        {
            new PassPreset<ClusterCullingReflectionProbePass>("Default", new ClusterCullingReflectionProbePass()),
        };
    }
}
