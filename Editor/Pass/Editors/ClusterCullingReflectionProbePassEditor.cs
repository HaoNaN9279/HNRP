// <copyright file="ClusterCullingReflectionProbePassEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEditor;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// <see cref="ClusterCullingReflectionProbePass"/> 的参数面板：图集分组 +
    /// 分辨率 / slice 取值范围说明。
    /// </summary>
    [PassEditor(typeof(ClusterCullingReflectionProbePass))]
    public sealed class ClusterCullingReflectionProbePassEditor : PassEditor
    {
        /// <inheritdoc />
        public override void ConfigureFieldLayout(PassFieldLayout layout)
        {
            layout.Section("Reflection Probe Atlas", "atlasSliceResolution", "atlasSliceCount");
        }

        /// <inheritdoc />
        public override void DrawPassParameters(RenderGraphPassGUIContext context)
        {
            base.DrawPassParameters(context);

            EditorGUILayout.HelpBox(
                "单 slice 分辨率收敛到 [256, 2048]，slice 数量收敛到 [1, 2]；" +
                "上限来自 TextureAllocator 的「分辨率 / 最小块 ≤ 8」约束" +
                "（最小块必须为 256）。容量不足时按 importance 优先保留。",
                MessageType.None);
        }
    }
}
