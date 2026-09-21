// <copyright file="BuiltinSkyPassEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEditor;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// <see cref="BuiltinSkyPass"/> 的参数面板：无用户可调参数，仅说明天空盒来源。
    /// </summary>
    /// <remarks>
    /// 本 pass 目前没有可序列化参数，故面板只输出说明。若后续新增用户可调参数，
    /// 需改为调用 <c>base.DrawPassParameters(context)</c>。
    /// </remarks>
    [PassEditor(typeof(BuiltinSkyPass))]
    public sealed class BuiltinSkyPassEditor : PassEditor
    {
        /// <inheritdoc />
        public override void DrawPassParameters(RenderGraphPassGUIContext context)
        {
            EditorGUILayout.HelpBox(
                "本 pass 使用 Unity 内置天空盒，外观由场景 Lighting 的 Environment 设置" +
                "（或相机上的 Skybox 覆盖）决定，无 pass 级参数。",
                MessageType.None);
        }
    }
}
