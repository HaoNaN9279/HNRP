// <copyright file="ForwardPassEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEditor;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// <see cref="ForwardPass"/> 的参数面板：按颜色目标 / 深度目标 / 渲染器列表 /
    /// 光照分组，并在末尾提示本实例绘制的是不透明还是透明队列。
    /// </summary>
    /// <remarks>
    /// 标准图与反射图各有一个不透明实例，透明实例仅参数不同，因此提示按缓存里的
    /// <see cref="RendererListParams.ListKind"/> 动态给出。
    /// </remarks>
    [PassEditor(typeof(ForwardPass))]
    public sealed class ForwardPassEditor : PassEditor
    {
        /// <inheritdoc />
        public override void ConfigureFieldLayout(PassFieldLayout layout)
        {
            layout.Section("Color Target", "colorTargetParams");
            layout.Section("Depth Target", "depthTargetParams");
            layout.Section("Renderer List", "rendererListParams");
            layout.Section("Lighting", "receiveShadow");
        }

        /// <inheritdoc />
        public override void DrawPassParameters(RenderGraphPassGUIContext context)
        {
            base.DrawPassParameters(context);

            ForwardPass source = context.CachePass as ForwardPass ?? context.TemplatePass as ForwardPass;
            if (source == null)
            {
                return;
            }

            string listKind = source.RendererListParams.ListKind == RenderListKind.Transparent
                ? "透明"
                : "不透明";
            EditorGUILayout.HelpBox(
                $"本实例绘制{listKind}队列对象；Renderer List 输入未连接时按上述参数自建。",
                MessageType.None);
        }
    }
}
