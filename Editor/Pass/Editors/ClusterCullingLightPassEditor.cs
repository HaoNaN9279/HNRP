// <copyright file="ClusterCullingLightPassEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEditor;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// <see cref="ClusterCullingLightPass"/> 的参数面板：无用户可调参数，仅说明来源。
    /// </summary>
    /// <remarks>
    /// 本 pass 目前没有可序列化参数，故面板只输出说明。若后续新增用户可调参数，
    /// 需改为调用 <c>base.DrawPassParameters(context)</c>。
    /// </remarks>
    [PassEditor(typeof(ClusterCullingLightPass))]
    public sealed class ClusterCullingLightPassEditor : PassEditor
    {
        /// <inheritdoc />
        public override void DrawPassParameters(RenderGraphPassGUIContext context)
        {
            EditorGUILayout.HelpBox(
                "本 pass 的簇划分与每簇光源上限由相机尺寸与管线全局设置推导，" +
                "无 pass 级参数。",
                MessageType.None);
        }
    }
}
