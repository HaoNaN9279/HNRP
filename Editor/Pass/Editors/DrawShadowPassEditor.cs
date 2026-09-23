// <copyright file="DrawShadowPassEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEditor;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// <see cref="DrawShadowPass"/> 的参数面板：按 Atlas / 深度偏置 / 强度 / 软阴影 /
    /// Cascade 衔接分组；软阴影总开关关闭时整组灰显。
    /// </summary>
    [PassEditor(typeof(DrawShadowPass))]
    public sealed class DrawShadowPassEditor : PassEditor
    {
        /// <summary>软阴影总开关的序列化字段名（亦作为灰显条件的读取目标）。</summary>
        private const string SoftShadowEnabledField = "softShadowEnabled";

        /// <inheritdoc />
        public override void ConfigureFieldLayout(PassFieldLayout layout)
        {
            layout.Section("Atlas", "sliceResolution", "sliceCount");
            layout.Section(
                "Depth Bias",
                "shadowNearPlaneOffset",
                "depthBias",
                "slopBias",
                "shadowDepthBias");
            layout.Section("Shading", "shadowStrength");
            layout.Section(
                    "Soft Shadow (PCSS)",
                    SoftShadowEnabledField,
                    "blockerSampleCount",
                    "filterSampleCount")
                .DisableWhen(IsSoftShadowDisabled);
            layout.Section("Cascade Blend", "cascadeBlendPercentage");
        }

        /// <summary>
        /// 软阴影总开关关闭时，软阴影整组保持可见但禁用编辑
        /// （提示「有 light 选择 Soft 也不会生效」）。
        /// </summary>
        private static bool IsSoftShadowDisabled(RenderGraphPassGUIContext context)
        {
            if (context.PassProperty == null)
            {
                return false;
            }

            SerializedProperty property =
                context.PassProperty.FindPropertyRelative(SoftShadowEnabledField);
            return property != null && !property.boolValue;
        }
    }
}
