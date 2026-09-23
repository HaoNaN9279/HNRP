// <copyright file="ScreenSpaceShadowMaskPassEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEditor;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// <see cref="ScreenSpaceShadowMaskPass"/> 的参数面板：按 mask 目标分组。
    /// </summary>
    [PassEditor(typeof(ScreenSpaceShadowMaskPass))]
    public sealed class ScreenSpaceShadowMaskPassEditor : PassEditor
    {
        /// <inheritdoc />
        public override void ConfigureFieldLayout(PassFieldLayout layout)
        {
            layout.Section("Shadow Mask Target", "shadowMaskParams");
        }

        /// <inheritdoc />
        public override void DrawPassParameters(RenderGraphPassGUIContext context)
        {
            base.DrawPassParameters(context);

            EditorGUILayout.HelpBox(
                "用 ComputeShader 把当前视锥内所有投影光源（方向光 / 点光 / 聚光）的阴影" +
                "合并绘制到一张单通道 mask；深度由上游预渲染 pass 经 slot 提供。",
                MessageType.None);
        }
    }
}
