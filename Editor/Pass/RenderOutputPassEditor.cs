// <copyright file="RenderOutputPassEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// Inspector Editor for <see cref="RenderOutputPass"/>.
    /// Draws the vertical-flip parameter and defines the pass presets offered in
    /// the <see cref="RenderGraphAsset"/> inspector.
    /// </summary>
    [PassEditor(typeof(RenderOutputPass))]
    public class RenderOutputPassEditor : PassEditor
    {
        /// <inheritdoc />
        public override IReadOnlyList<IPassPreset> Presets => s_Presets;

        /// <inheritdoc />
        protected override void DrawParameters(SerializedProperty passProp, Pass pass)
        {
            SerializedProperty flip = passProp.FindPropertyRelative("m_Flip");
            if (flip != null)
            {
                EditorGUILayout.PropertyField(flip, new GUIContent(
                    "Flip",
                    "是否垂直翻转输出。注意：运行时该值会被 camera context 的 Flip 覆盖。"));
            }
        }

        /// <summary>
        /// 内置预设集合。新增预设：加一个静态字段并追加到此数组。
        /// </summary>
        private static readonly IPassPreset[] s_Presets =
        {
            new PassPreset<RenderOutputPass>("Default", new RenderOutputPass
            {
                Flip = false,
            }),
            new PassPreset<RenderOutputPass>("Flip Vertical", new RenderOutputPass
            {
                Flip = true,
            }),
        };
    }
}
