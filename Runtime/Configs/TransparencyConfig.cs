// <copyright file="TransparencyConfig.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    /// <summary>
    /// ScriptableObject configuration for <see cref="TransparencyPass"/>.
    /// Holds all configurable parameters — render-queue range, layer mask, sorting mode —
    /// and applies them to the pass via <see cref="ApplyToPass"/>.
    /// </summary>
    /// <remarks>
    /// Create an asset via <c>Create → HN/HNRP/Configs/TransparencyConfig</c>.
    /// Place assets in <c>Runtime/Resources/PassConfigs/</c> for runtime loading.
    /// </remarks>
    [CreateAssetMenu(
        menuName = "HN/HNRP/Configs/TransparencyConfig",
        fileName = "TransparencyConfig",
        order = 102)]
    public sealed class TransparencyConfig : PassConfigBase
    {
        [SerializeField]
        private RenderQueueRange m_RenderQueueRange = new RenderQueueRange(
            HNRenderQueue.Transparent.lowerBound, HNRenderQueue.Transparent.upperBound);

        [SerializeField]
        private uint m_RenderingLayerMask = 0x00000001;

        [SerializeField]
        private LayerMask m_LayerMask = -1;

        [SerializeField]
        private bool m_SortBackToFront = true;

        /// <summary>
        /// Gets or sets the render-queue range for transparent geometry.
        /// Default is <see cref="HNRenderQueue.Transparent"/>.
        /// </summary>
        public RenderQueueRange RenderQueueRange
        {
            get => m_RenderQueueRange;
            set => m_RenderQueueRange = value;
        }

        /// <summary>
        /// Gets or sets the rendering-layer mask used for culling.
        /// Default is <c>0x00000001</c> (layer 0).
        /// </summary>
        public uint RenderingLayerMask
        {
            get => m_RenderingLayerMask;
            set => m_RenderingLayerMask = value;
        }

        /// <summary>
        /// Gets or sets the layer mask for camera culling.
        /// Default is <c>-1</c> (Everything).
        /// </summary>
        public LayerMask LayerMask
        {
            get => m_LayerMask;
            set => m_LayerMask = value;
        }

        /// <summary>
        /// Gets or sets whether transparent objects should be sorted
        /// back-to-front before rendering.
        /// Default is <c>true</c>.
        /// </summary>
        public bool SortBackToFront
        {
            get => m_SortBackToFront;
            set => m_SortBackToFront = value;
        }

        /// <inheritdoc />
        public override void ApplyToPass(Pass pass)
        {
            if (pass == null)
                return;

            pass.IsEnabled = true;

            if (pass is TransparencyPass transparency)
            {
                transparency.RenderingLayerMask = m_RenderingLayerMask;
            }
        }
    }
}
