// <copyright file="ForwardOpaqueConfig.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    /// <summary>
    /// ScriptableObject configuration for an opaque <see cref="DrawObjectPass"/>.
    /// Holds all configurable parameters — render-queue range, layer mask, etc. —
    /// and applies them to the pass via <see cref="ApplyToPass"/>.
    /// </summary>
    /// <remarks>
    /// Create an asset via <c>Create → HN/HNRP/Configs/ForwardOpaqueConfig</c>.
    /// Place assets in <c>Runtime/Resources/PassConfigs/</c> for runtime loading.
    /// </remarks>
    [CreateAssetMenu(
        menuName = "HN/HNRP/Configs/ForwardOpaqueConfig",
        fileName = "ForwardOpaqueConfig",
        order = 100)]
    public sealed class ForwardOpaqueConfig : PassConfigBase
    {
        [SerializeField]
        private int m_RenderQueueLower = (int)HNRenderQueue.Priority.Opaque;

        [SerializeField]
        private int m_RenderQueueUpper = (int)HNRenderQueue.Priority.OpaqueLast;

        [SerializeField]
        private uint m_RenderingLayerMask = 0x00000001;

        [SerializeField]
        private LayerMask m_LayerMask = -1;

        /// <summary>
        /// Gets or sets the render-queue range for opaque geometry.
        /// Only renderers whose <c>Material.renderQueue</c> falls within this
        /// range are included in the opaque draw call list.
        /// </summary>
        public RenderQueueRange RenderQueueRange
        {
            get => new RenderQueueRange(m_RenderQueueLower, m_RenderQueueUpper);
            set
            {
                m_RenderQueueLower = value.lowerBound;
                m_RenderQueueUpper = value.upperBound;
            }
        }

        /// <summary>
        /// Gets or sets the rendering-layer mask used for culling.
        /// Only renderers on matching rendering layers are drawn.
        /// Default is <c>0x00000001</c> (layer 0).
        /// </summary>
        public uint RenderingLayerMask
        {
            get => m_RenderingLayerMask;
            set => m_RenderingLayerMask = value;
        }

        /// <summary>
        /// Gets or sets the layer mask for camera culling.
        /// Only objects on matching Unity layers are processed.
        /// Default is <c>-1</c> (Everything).
        /// </summary>
        public LayerMask LayerMask
        {
            get => m_LayerMask;
            set => m_LayerMask = value;
        }

        /// <inheritdoc />
        public override void ApplyToPass(Pass pass)
        {
            if (pass == null)
                return;

            pass.IsEnabled = true;

            if (pass is DrawObjectPass drawObject)
            {
                drawObject.RenderingLayerMask = m_RenderingLayerMask;
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            m_RenderQueueLower = (int)HNRenderQueue.Priority.Opaque;
            m_RenderQueueUpper = (int)HNRenderQueue.Priority.OpaqueLast;
            m_RenderingLayerMask = 0x00000001;
            m_LayerMask = -1;
        }
#endif
    }
}
