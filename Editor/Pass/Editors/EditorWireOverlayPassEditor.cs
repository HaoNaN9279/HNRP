// <copyright file="EditorWireOverlayPassEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEditor;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// <see cref="EditorWireOverlayPass"/> 的参数面板：无用户可调参数，仅说明生效范围。
    /// </summary>
    /// <remarks>
    /// 本 pass 目前没有可序列化参数，故面板只输出说明。若后续新增用户可调参数，
    /// 需改为调用 <c>base.DrawPassParameters(context)</c>。
    /// </remarks>
    [PassEditor(typeof(EditorWireOverlayPass))]
    public sealed class EditorWireOverlayPassEditor : PassEditor
    {
        /// <inheritdoc />
        public override void DrawPassParameters(RenderGraphPassGUIContext context)
        {
            EditorGUILayout.HelpBox(
                "本 pass 仅在 Editor 中对 SceneView 相机绘制线框叠加层（gizmos、选中轮廓），" +
                "Player 构建中整段实现被剔除，无 pass 级参数。",
                MessageType.None);
        }
    }
}
