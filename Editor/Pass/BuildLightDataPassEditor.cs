// <copyright file="BuildLightDataPassEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System.Collections.Generic;
using UnityEngine;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// Inspector Editor for <see cref="BuildLightDataPass"/>.
    /// This pass exposes no serialized parameters; the default field traversal
    /// draws only the common header.
    /// </summary>
    [PassEditor(typeof(BuildLightDataPass))]
    public class BuildLightDataPassEditor : PassEditor
    {
        /// <inheritdoc />
        public override IReadOnlyList<IPassPreset> Presets => s_Presets;

        /// <summary>
        /// 内置预设集合。新增预设：加一个静态字段并追加到此数组。
        /// </summary>
        private static readonly IPassPreset[] s_Presets =
        {
            new PassPreset<BuildLightDataPass>("Default", new BuildLightDataPass()),
        };
    }
}
