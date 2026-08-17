using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace HN.HNRP
{
    /// <summary>
    /// Abstract base class for render passes in the HNRP custom render pipeline.
    /// Passes are pure C# objects — not ScriptableObjects — that define a unit of
    /// rendering work within the render graph.
    /// </summary>
    /// <remarks>
    /// <para>Lifecycle order:</para>
    /// <list type="number">
    ///   <item><see cref="SetupSlots"/> — declare input/output slots (e.g. TextureSlot, ComputeBufferSlot)</item>
    ///   <item><see cref="Initialize"/> — load resources using camera-specific context</item>
    ///   <item><see cref="Record"/> — record render commands into the render graph</item>
    ///   <item><see cref="Cleanup"/> — release resources held by this pass</item>
    /// </list>
    /// <para>
    /// Subclasses should be decorated with the <c>[Pass("Name")]</c> attribute
    /// for automatic discovery via reflection (Editor) or code generation (Player).
    /// </para>
    /// </remarks>
    public abstract class Pass
    {
        /// <summary>
        /// Gets the name of this pass, set at construction time.
        /// </summary>
        public string PassName { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this pass is enabled.
        /// When <c>false</c>, the caller (e.g. <c>CameraRenderer</c>) skips
        /// <see cref="Record"/> for this pass.
        /// Default is <c>true</c>.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pass"/> class.
        /// </summary>
        /// <param name="passName">
        /// The name of this pass. Must be non-null and unique within a render graph.
        /// </param>
        protected Pass(string passName)
        {
            PassName = passName;
        }

        /// <summary>
        /// Declares input and output slots for this pass.
        /// Called once during setup, before <see cref="Initialize"/>.
        /// </summary>
        /// <remarks>
        /// Slots define data dependencies — the render graph uses them to derive
        /// execution order automatically. Typical slot types: <c>TextureSlot</c>,
        /// <c>ComputeBufferSlot</c>, <c>RendererListSlot</c>.
        /// </remarks>
        public abstract void SetupSlots();

        /// <summary>
        /// Initializes this pass with camera-specific rendering context.
        /// Use this method to load shaders, materials, and other resources.
        /// </summary>
        /// <param name="context">
        /// The camera rendering context providing camera-specific data.
        /// </param>
        public abstract void Initialize(CameraContext context);

        /// <summary>
        /// Records render commands into the render graph.
        /// Called only when <see cref="IsEnabled"/> is <c>true</c>.
        /// </summary>
        /// <param name="renderGraph">
        /// The render graph to record commands into. Output slots create resources
        /// via <c>renderGraph.CreateTexture</c> etc.; input slots read from connected outputs.
        /// </param>
        public abstract void Record(RenderGraph renderGraph);

        /// <summary>
        /// Releases resources held by this pass.
        /// Default implementation is a no-op. Override to release
        /// materials, compute buffers, or other disposable resources.
        /// </summary>
        public virtual void Cleanup()
        {
        }
    }
}
