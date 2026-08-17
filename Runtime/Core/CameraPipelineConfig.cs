// <copyright file="CameraPipelineConfig.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using UnityEngine;

namespace HN.HNRP
{
    /// <summary>
    /// Camera-level pipeline configuration ScriptableObject.
    /// Replaces the old <see cref="RenderGraphViewBlock"/> / renderGraphViewIndex system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Selection priority chain</b> (ADR-006):
    /// <c>HNAdditionalCameraData.pipelineConfigOverride</c>
    /// ?? <c>HNRenderPipelineAsset.defaultXxxConfig</c>
    /// ?? null (skip camera).
    /// </para>
    /// <para>
    /// Cameras do <b>not</b> reference <see cref="RenderGraphAsset"/> directly.
    /// <see cref="CameraPipelineConfig"/> is the intermediary layer that maps a camera
    /// to a render graph template along with optional per-config settings overrides.
    /// All config instances are managed through
    /// <see cref="HNRenderPipelineGlobalSettings.cameraPipelineConfigs"/>.
    /// </para>
    /// <para>
    /// 📦 Todo 11 — full implementation when <see cref="RenderGraphAsset"/> (Todo 10)
    /// and <see cref="CameraRenderer"/> (Todo 12) are completed.
    /// </para>
    /// </remarks>
    /// <seealso cref="RenderGraphAsset"/>
    /// <seealso cref="CameraPipelineConfigSettings"/>
    /// <seealso cref="HNAdditionalCameraData"/>
    public class CameraPipelineConfig : ScriptableObject
    {
        [SerializeField, Tooltip("The render graph template this camera pipeline will use.")]
        private RenderGraphAsset m_RenderGraph;

        [SerializeField, Tooltip("Optional per-config settings override. Applied on top of global defaults.")]
        private CameraPipelineConfigSettings m_SettingsOverride;

        /// <summary>
        /// Gets or sets the <see cref="RenderGraphAsset"/> template
        /// that defines the render graph for cameras using this configuration.
        /// </summary>
        /// <value>
        /// The render graph asset. May be <c>null</c> if no graph has been assigned yet.
        /// </value>
        /// <remarks>
        /// Set via the Unity Inspector or through
        /// <see cref="HNRenderPipelineGlobalSettings.cameraPipelineConfigs"/> management.
        /// </remarks>
        public RenderGraphAsset RenderGraph
        {
            get => m_RenderGraph;
            set => m_RenderGraph = value;
        }

        /// <summary>
        /// Gets or sets the optional settings override for this configuration.
        /// When <see cref="CameraPipelineConfigSettings.IsOverridden"/> is <c>false</c>,
        /// the pipeline uses global defaults from
        /// <see cref="HNRenderPipelineGlobalSettings"/>.
        /// </summary>
        /// <value>
        /// A <see cref="CameraPipelineConfigSettings"/> struct with overridden values.
        /// Default struct (no overrides) means "use global defaults".
        /// </value>
        public CameraPipelineConfigSettings SettingsOverride
        {
            get => m_SettingsOverride;
            set => m_SettingsOverride = value;
        }
    }

    /// <summary>
    /// Optional per-config settings that can override global defaults
    /// for a specific <see cref="CameraPipelineConfig"/>.
    /// Applied on top of <see cref="HNRenderPipelineGlobalSettings"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a <c>[Serializable]</c> struct so it can be serialized inline
    /// in the <see cref="CameraPipelineConfig"/> ScriptableObject without
    /// requiring a separate asset file for each override.
    /// </para>
    /// <para>
    /// 🚧 Full implementation in Todo 11. Currently a placeholder.
    /// Future fields may include: resolution scale, AA mode, HDR toggle,
    /// post-processing enable, render-scale, and per-camera quality tier.
    /// </para>
    /// </remarks>
    [Serializable]
    public struct CameraPipelineConfigSettings
    {
        /// <summary>
        /// Whether this settings instance contains any non-default values.
        /// When <c>false</c>, the pipeline uses global defaults.
        /// </summary>
        /// <value>
        /// <c>true</c> if at least one setting has been explicitly overridden;
        /// <c>false</c> otherwise.
        /// </value>
        /// <remarks>
        /// This flag is checked before applying overrides. If it returns <c>false</c>,
        /// the entire struct is ignored and global defaults are used unmodified.
        /// </remarks>
        public bool IsOverridden
        {
            get => m_IsOverridden;
            set => m_IsOverridden = value;
        }

        [SerializeField]
        private bool m_IsOverridden;
    }
}
