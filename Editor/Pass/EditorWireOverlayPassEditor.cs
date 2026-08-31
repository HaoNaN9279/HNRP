// <copyright file="EditorWireOverlayPassEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System.Collections.Generic;
using UnityEngine;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// Inspector Editor for <see cref="EditorWireOverlayPass"/>.
    /// This pass exposes no serialized parameters; the default field traversal
    /// draws only the common header.
    /// </summary>
    [PassEditor(typeof(EditorWireOverlayPass))]
    public class EditorWireOverlayPassEditor : PassEditor
    {
        /// <inheritdoc />
        public override IReadOnlyList<IPassPreset> Presets => s_Presets;

        /// <summary>
        /// 内置预设集合。新增预设：加一个静态字段并追加到此数组。
        /// </summary>
        private static readonly IPassPreset[] s_Presets =
        {
            new PassPreset<EditorWireOverlayPass>("Default", new EditorWireOverlayPass()),
        };
    }
}
