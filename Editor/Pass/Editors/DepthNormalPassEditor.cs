// <copyright file="DepthNormalPassEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEditor;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// <see cref="DepthNormalPass"/> 的参数面板：按深度目标 / 法线目标 / 渲染器列表分组。
    /// </summary>
    [PassEditor(typeof(DepthNormalPass))]
    public sealed class DepthNormalPassEditor : PassEditor
    {
        /// <inheritdoc />
        public override void ConfigureFieldLayout(PassFieldLayout layout)
        {
            layout.Section("Depth Target", "depthTargetParams");
            layout.Section("Normal Target", "normalTargetParams");
            layout.Section("Renderer List", "rendererListParams");
        }

        /// <inheritdoc />
        public override void DrawPassParameters(RenderGraphPassGUIContext context)
        {
            base.DrawPassParameters(context);

            EditorGUILayout.HelpBox(
                "深度与法线分别绘制到两张目标纹理；输入未连接时按上述参数自建，两者的输出句柄供下游使用。",
                MessageType.None);
        }
    }
}
