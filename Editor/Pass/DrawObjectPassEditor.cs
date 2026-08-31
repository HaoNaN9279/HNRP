// <copyright file="DrawObjectPassEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// Inspector Editor for <see cref="DrawObjectPass"/>.
    /// Draws the rendering layer mask and light-globals parameters, and defines
    /// the pass presets offered in the <see cref="RenderGraphAsset"/> inspector.
    /// </summary>
    [PassEditor(typeof(DrawObjectPass))]
    public class DrawObjectPassEditor : PassEditor
    {
        /// <inheritdoc />
        public override IReadOnlyList<IPassPreset> Presets => s_Presets;

        /// <inheritdoc />
        protected override void DrawParameters(SerializedProperty passProp, Pass pass)
        {
            SerializedProperty layerMask = passProp.FindPropertyRelative("m_RenderingLayerMask");
            if (layerMask != null)
            {
                EditorGUILayout.PropertyField(layerMask, new GUIContent(
                    "Rendering Layer Mask",
                    "渲染层掩码。仅匹配层上的渲染器被绘制。"));
            }

            SerializedProperty setLightGlobals = passProp.FindPropertyRelative("m_SetLightGlobals");
            if (setLightGlobals != null)
            {
                EditorGUILayout.PropertyField(setLightGlobals, new GUIContent(
                    "Set Light Globals",
                    "绘制前设置 probe / light / light-data shader 全局。无 cluster culling 数据的图（如预览）应关闭。"));
            }
        }

        /// <summary>
        /// 内置预设集合。新增预设：加一个静态字段并追加到此数组。
        /// </summary>
        private static readonly IPassPreset[] s_Presets =
        {
            new PassPreset<DrawObjectPass>("Default", new DrawObjectPass
            {
                RenderingLayerMask = 0x00000001,
                SetLightGlobals = true,
            }),
            new PassPreset<DrawObjectPass>("No Light Globals", new DrawObjectPass
            {
                RenderingLayerMask = 0x00000001,
                SetLightGlobals = false,
            }),
        };
    }
}
