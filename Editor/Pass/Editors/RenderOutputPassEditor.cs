// <copyright file="RenderOutputPassEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEditor;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// <see cref="RenderOutputPass"/> 的参数面板：无用户可调参数，仅说明输出方式。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>flip</c> 由相机上下文每帧写入（决定目标是否为翻转的后备缓冲面），不应被用户
    /// 在资源上覆盖，故在此显式隐藏 —— 该规则属于本 pass 的私有知识，不再放进通用的
    /// <see cref="PassParametersGUI"/>。
    /// </para>
    /// <para>
    /// 本 pass 目前没有其它可序列化参数，故面板只输出说明。若后续新增用户可调参数，
    /// 需改为调用 <c>base.DrawPassParameters(context)</c>（或在
    /// <see cref="ConfigureFieldLayout"/> 中为其声明分组）。
    /// </para>
    /// </remarks>
    [PassEditor(typeof(RenderOutputPass))]
    public sealed class RenderOutputPassEditor : PassEditor
    {
        /// <inheritdoc />
        public override void ConfigureFieldLayout(PassFieldLayout layout)
        {
            layout.Hide("flip");
        }

        /// <inheritdoc />
        public override void DrawPassParameters(RenderGraphPassGUIContext context)
        {
            EditorGUILayout.HelpBox(
                "本 pass 无用户可调参数：颜色目标与是否翻转均由相机上下文逐帧提供。",
                MessageType.None);
        }
    }
}
