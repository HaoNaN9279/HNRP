// <copyright file="DepthOnlyPassEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEditor;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// <see cref="DepthOnlyPass"/> 的参数面板：按深度目标 / 渲染器列表分组。
    /// </summary>
    [PassEditor(typeof(DepthOnlyPass))]
    public sealed class DepthOnlyPassEditor : PassEditor
    {
        /// <inheritdoc />
        public override void ConfigureFieldLayout(PassFieldLayout layout)
        {
            layout.Section("Depth Target", "depthTargetParams");
            layout.Section("Renderer List", "rendererListParams");
        }

        /// <inheritdoc />
        public override void DrawPassParameters(RenderGraphPassGUIContext context)
        {
            base.DrawPassParameters(context);

            EditorGUILayout.HelpBox(
                "深度目标输入未连接时按上述参数自建；解析后的深度句柄经输出 slot 供下游使用。",
                MessageType.None);
        }
    }
}
