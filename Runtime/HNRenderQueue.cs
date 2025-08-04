using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public static class HNRenderQueue
    {
        public enum Priority
        {
            Background = RenderQueue.Background,
            Opaque = RenderQueue.Geometry,
            OpaqueAlphaTest = RenderQueue.AlphaTest,
            OpaqueLast = RenderQueue.GeometryLast,
            Transparent = RenderQueue.Transparent,
            TransparentLast = RenderQueue.Transparent + 500,
            Overlay = 4000,
            UI = 5000
        }


        public static readonly RenderQueueRange Background = new RenderQueueRange((int)Priority.Background, (int)Priority.Opaque - 1);
        public static readonly RenderQueueRange OpaqueNoAlphaTest = new RenderQueueRange((int)Priority.Opaque, (int)Priority.OpaqueAlphaTest - 1);
        public static readonly RenderQueueRange OpaqueAlphaTest = new RenderQueueRange((int)Priority.OpaqueAlphaTest, (int)Priority.OpaqueLast);
        public static readonly RenderQueueRange AllOpaqueNoBackground = new RenderQueueRange((int)Priority.Opaque, (int)Priority.OpaqueLast);
        public static readonly RenderQueueRange AllOpaque = new RenderQueueRange((int)Priority.Background, (int)Priority.OpaqueLast);

        public static readonly RenderQueueRange Transparent = new RenderQueueRange((int)Priority.Transparent, (int)Priority.TransparentLast);

        public static readonly RenderQueueRange Overlay = new RenderQueueRange((int)Priority.Overlay, (int)Priority.UI - 1);

        public static readonly RenderQueueRange UI = new RenderQueueRange((int)Priority.UI, (int)Priority.UI);

        public static readonly RenderQueueRange All = new RenderQueueRange((int)Priority.Background, (int)Priority.UI);
    }
}
