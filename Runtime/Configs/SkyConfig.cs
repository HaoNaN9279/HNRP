// <copyright file="SkyConfig.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    /// <summary>
    /// ScriptableObject configuration for <see cref="BuiltinSkyPass"/>.
    /// Holds sky-rendering parameters — intensity, tint color, depth options —
    /// and applies them to the pass via <see cref="ApplyToPass"/>.
    /// </summary>
    /// <remarks>
    /// Create an asset via <c>Create → HN/HNRP/Configs/SkyConfig</c>.
    /// Place assets in <c>Runtime/Resources/PassConfigs/</c> for runtime loading.
    /// </remarks>
    [CreateAssetMenu(
        menuName = "HN/HNRP/Configs/SkyConfig",
        fileName = "SkyConfig",
        order = 101)]
    public sealed class SkyConfig : PassConfigBase
    {
        [SerializeField]
        private float m_SkyIntensity = 1.0f;

        [SerializeField]
        private Color m_SkyTint = Color.white;

        [SerializeField]
        private bool m_WriteDepth = true;

        [SerializeField]
        private CompareFunction m_DepthTest = CompareFunction.LessEqual;

        /// <summary>
        /// Gets or sets the sky rendering intensity multiplier.
        /// Clamped to [0, 2] in <see cref="ApplyToPass"/>.
        /// Default is <c>1.0</c>.
        /// </summary>
        public float SkyIntensity
        {
            get => m_SkyIntensity;
            set => m_SkyIntensity = value;
        }

        /// <summary>
        /// Gets or sets the sky tint color.
        /// Multiplicatively modulates the skybox color during rendering.
        /// Default is <see cref="Color.white"/> (no tint).
        /// </summary>
        public Color SkyTint
        {
            get => m_SkyTint;
            set => m_SkyTint = value;
        }

        /// <summary>
        /// Gets or sets whether the sky pass writes to the depth buffer.
        /// Default is <c>true</c>.
        /// </summary>
        public bool WriteDepth
        {
            get => m_WriteDepth;
            set => m_WriteDepth = value;
        }

        /// <summary>
        /// Gets or sets the depth comparison function for sky rendering.
        /// Default is <see cref="CompareFunction.LessEqual"/>.
        /// </summary>
        public CompareFunction DepthTest
        {
            get => m_DepthTest;
            set => m_DepthTest = value;
        }

        /// <inheritdoc />
        public override void ApplyToPass(Pass pass)
        {
            if (pass == null)
                return;

            var clampedIntensity = Mathf.Clamp(m_SkyIntensity, 0f, 2f);
            pass.IsEnabled = clampedIntensity > 0f;

            Shader.SetGlobalFloat("_HNSkyIntensity", clampedIntensity);
            Shader.SetGlobalColor("_HNSkyTint", m_SkyTint);
        }
    }
}
