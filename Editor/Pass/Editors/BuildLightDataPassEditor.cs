// <copyright file="BuildLightDataPassEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEditor;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// <see cref="BuildLightDataPass"/> 的参数面板：无用户可调参数，仅说明数据来源。
    /// </summary>
    /// <remarks>
    /// 本 pass 目前没有可序列化参数，故面板只输出说明。若后续新增用户可调参数，
    /// 需改为调用 <c>base.DrawPassParameters(context)</c>。
    /// </remarks>
    [PassEditor(typeof(BuildLightDataPass))]
    public sealed class BuildLightDataPassEditor : PassEditor
    {
        /// <inheritdoc />
        public override void DrawPassParameters(RenderGraphPassGUIContext context)
        {
            EditorGUILayout.HelpBox(
                "本 pass 从当前相机的可见光源生成光照数据缓冲，内容完全由场景光源与" +
                "相机设置推导，无 pass 级参数。",
                MessageType.None);
        }
    }
}
