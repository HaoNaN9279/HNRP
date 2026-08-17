using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    /// <summary>
    /// Per-camera rendering context.
    /// Wraps a <see cref="Camera"/> and <see cref="ScriptableRenderContext"/> with all
    /// per-frame rendering state including culling results, command buffer, and lighting data.
    /// Implements <see cref="System.IDisposable"/> — call <see cref="Dispose"/> when the
    /// frame is complete to release pooled resources and native arrays.
    /// </summary>
    public class CameraContext
    {
        /// <summary>
        /// The camera being rendered this frame.
        /// </summary>
        public Camera Camera { get; set; }

        /// <summary>
        /// The scriptable render context used to schedule and execute rendering commands.
        /// </summary>
        public ScriptableRenderContext Context { get; set; }

        /// <summary>
        /// Culling results produced by <c>context.Cull()</c> for the current frame.
        /// Contains visible lights, reflection probes, and draw renderers.
        /// </summary>
        public CullingResults CullingResults { get; set; }

        /// <summary>
        /// Command buffer for recording render commands.
        /// Allocated from the pool via <see cref="CommandBufferPool.Get"/> during construction
        /// and released back via <see cref="CommandBufferPool.Release"/> in <see cref="Dispose"/>.
        /// </summary>
        public CommandBuffer Cmd { get; private set; }

        /// <summary>
        /// The render target identifier for the camera's current color target.
        /// Set by the pipeline before recording passes.
        /// </summary>
        public RenderTargetIdentifier TargetId { get; set; }

        /// <summary>
        /// Visible lights obtained from <see cref="CullingResults"/>.
        /// This is a native array that must be disposed via <see cref="Dispose"/>.
        /// </summary>
        public NativeArray<VisibleLight> VisibleLights { get; set; }

        /// <summary>
        /// Visible reflection probes obtained from <see cref="CullingResults"/>.
        /// This is a native array that must be disposed via <see cref="Dispose"/>.
        /// </summary>
        public NativeArray<VisibleReflectionProbe> VisibleReflectionProbes { get; set; }

        /// <summary>
        /// Cached reflection probes that need rendering this frame.
        /// Managed array allocated and released outside the context lifecycle.
        /// </summary>
        public VisibleReflectionProbe[] CatchedReflectionProbes { get; set; }

        /// <summary>
        /// Shared runtime resources (shaders, textures, compute buffers) used across the pipeline.
        /// </summary>
        public HNRenderPipelineRuntimeResources RuntimeResources { get; set; }

        /// <summary>
        /// Global shader constant buffer populated each frame with time, camera,
        /// and lighting parameters.
        /// </summary>
        public GlobalConstantBuffer ConstantBuffer { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraContext"/> class.
        /// Allocates a command buffer from the pool named <c>"CameraContext"</c>.
        /// </summary>
        /// <param name="camera">The camera to render this frame.</param>
        /// <param name="context">The scriptable render context for the current frame.</param>
        public CameraContext(Camera camera, ScriptableRenderContext context)
        {
            Camera = camera;
            Context = context;
            Cmd = CommandBufferPool.Get("CameraContext");
        }

        /// <summary>
        /// Releases the pooled command buffer and disposes native arrays.
        /// Safe to call multiple times — subsequent calls are no-ops.
        /// </summary>
        public void Dispose()
        {
            if (Cmd != null)
            {
                CommandBufferPool.Release(Cmd);
                Cmd = null;
            }

            if (VisibleLights.IsCreated)
            {
                VisibleLights.Dispose();
            }

            if (VisibleReflectionProbes.IsCreated)
            {
                VisibleReflectionProbes.Dispose();
            }
        }
    }
}
