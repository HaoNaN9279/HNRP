using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    /// <summary>
    /// 每相机渲染上下文。
    /// 包装 <see cref="Camera"/> 与 <see cref="ScriptableRenderContext"/>，携带裁剪结果、
    /// 命令缓冲、光照数据等全部每帧渲染状态。实现 <see cref="System.IDisposable"/>——
    /// 帧结束时调用 <see cref="Dispose"/> 以释放池化资源与原生数组。
    /// </summary>
    public class CameraContext
    {
        /// <summary>
        /// 本帧正在渲染的相机。
        /// </summary>
        public Camera Camera { get; set; }

        /// <summary>
        /// 用于调度与执行渲染命令的 ScriptableRenderContext。
        /// </summary>
        public ScriptableRenderContext Context { get; set; }

        /// <summary>
        /// 当前帧 <c>context.Cull()</c> 产生的裁剪结果，
        /// 包含可见光、反射探针与待绘制渲染器。
        /// 仅当 <see cref="HasCullingResults"/> 为 <c>true</c> 时有效。
        /// </summary>
        public CullingResults CullingResults { get; set; }

        /// <summary>
        /// 获取或设置本帧相机裁剪是否成功。为 <c>false</c> 时
        /// <see cref="CullingResults"/> 为 <c>default(CullingResults)</c>，
        /// 不得用来构建渲染器列表（无效描述符会在渲染图编译期抛错）。
        /// </summary>
        public bool HasCullingResults { get; set; }

        /// <summary>
        /// 本次 <c>Cull</c> 实际生效的阴影距离（米），已并入相机与所有覆盖相机的方向光的
        /// 最远级联边界。
        /// </summary>
        /// <remarks>
        /// 引擎按该距离收集方向光的阴影投射者；阴影 pass 必须用同一个值作为级联分割比例的
        /// 归一化分母，否则引擎算出的绝对级联边界与本项目 splits 错位（远端级联取不到投射者）。
        /// </remarks>
        public float ShadowDistance { get; set; }

        /// <summary>
        /// 记录渲染命令的命令缓冲。构造时经 <see cref="CommandBufferPool.Get"/>
        /// 从池中分配，<see cref="Dispose"/> 中经 <see cref="CommandBufferPool.Release"/>
        /// 归还。
        /// </summary>
        public CommandBuffer Cmd { get; private set; }

        /// <summary>
        /// 获取或设置输出是否垂直翻转。
        /// </summary>
        public bool Flip { get; set; }

        /// <summary>
        /// 自定义渲染目标的面（cubemap 渲染用）。未知时为 <see cref="CubemapFace.Unknown"/>。
        /// </summary>
        public CubemapFace TargetFace { get; set; } = CubemapFace.Unknown;

        /// <summary>
        /// 自定义渲染目标的目标深度切片。
        /// </summary>
        public int TargetDepthSlice { get; set; } = -1;

        /// <summary>
        /// 包装自定义目标纹理的 RTHandle。
        /// </summary>
        public RTHandle CustomTargetRTHandle { get; set; }

        /// <summary>
        /// 从 <see cref="CullingResults"/> 得到的可见光。该原生数组
        /// 必须经 <see cref="Dispose"/> 释放。
        /// </summary>
        public NativeArray<VisibleLight> VisibleLights { get; set; }

        /// <summary>
        /// 本帧 main light 在 <see cref="VisibleLights"/> 中的索引；无方向光时为 -1。
        /// </summary>
        /// <remarks>
        /// 只依赖可见光列表与 <see cref="RenderSettings.sun"/>，帧内恒定。
        /// 由管线在裁剪后计算一次，供光照 / 阴影相关 pass 复用，避免重复遍历
        /// 可见光列表（上限 <see cref="HNRenderPipelineAsset.MAX_LOCAL_LIGHT_ON_SCREEN"/>
        /// + <see cref="HNRenderPipelineAsset.MAX_DIRECTIONAL_LIGHT_ON_SCREEN"/> 个）。
        /// </remarks>
        public int MainLightIndex { get; set; } = -1;

        /// <summary>
        /// 从 <see cref="CullingResults"/> 得到的可见反射探针。该原生数组
        /// 必须经 <see cref="Dispose"/> 释放。
        /// </summary>
        public NativeArray<VisibleReflectionProbe> VisibleReflectionProbes { get; set; }

        /// <summary>
        /// 本帧的帧级事实（F 通道）：由宿主管线或某个 pass 发布、供其后拓扑顺序的
        /// pass 消费的「本帧整体状态」。随本上下文每帧新建、<see cref="Dispose"/>
        /// 时复位，不得跨帧保留。详见架构文档 ADR-040。
        /// </summary>
        /// <remarks>
        /// 原先散落的帧级字段（如实时反射探针更新信号）逐步收敛到这里，
        /// 使「跨 pass 信息」有单一规范通道，而不是继续在上下文上零散加属性。
        /// <para>
        /// <b>所有权：</b>本对象不拥有事实板 —— 板由宿主 <see cref="CameraRenderer"/>
        /// （每相机、跨帧存活）持有并在构造本上下文时绑定，注册区数组一次分配长期复用，
        /// 保证严格零 GC。构造时调用 <see cref="PassFrameFacts.Reset"/> 清空，
        /// 避免残留上一帧事实。
        /// </para>
        /// </remarks>
        public PassFrameFacts Facts { get; }

        /// <summary>
        /// 管线共享的运行时资源（shader、纹理、compute buffer）。
        /// </summary>
        public HNRenderPipelineRuntimeResources RuntimeResources { get; set; }

        /// <summary>
        /// 本帧的渲染调试状态（由管线在记录 pass 前解析）。
        /// 生命周期与 <see cref="CameraContext"/> 一致 —— 每帧新建，不跨帧保留。
        /// </summary>
        public RenderDebugState DebugState { get; set; } = new RenderDebugState();

        /// <summary>
        /// 每帧填充时间、相机与光照参数的全局 shader 常量缓冲。
        /// </summary>
        public GlobalConstantBuffer ConstantBuffer { get; set; }

        /// <summary>
        /// 本相机挂载的 <see cref="HNAdditionalCameraData"/>；不存在时为 <c>null</c>
        /// （如烘焙反射等临时相机）。惰性解析并缓存，<b>不</b>创建组件。
        /// </summary>
        public HNAdditionalCameraData AdditionalCameraData
        {
            get
            {
                if (additionalCameraData == null && Camera != null)
                {
                    Camera.TryGetComponent(out additionalCameraData);
                }

                return additionalCameraData;
            }
        }

        /// <summary><see cref="AdditionalCameraData"/> 的惰性缓存。</summary>
        private HNAdditionalCameraData additionalCameraData;

        /// <summary>
        /// 初始化 <see cref="CameraContext"/> 的新实例。
        /// 从池中分配名为 <c>"CameraContext"</c> 的命令缓冲，并绑定宿主的帧级事实板
        /// （绑定即清空，保证每个渲染单元从干净的事实状态开始）。
        /// </summary>
        /// <param name="camera">本帧要渲染的相机。</param>
        /// <param name="context">当前帧的 ScriptableRenderContext。</param>
        /// <param name="facts">
        /// 宿主 <see cref="CameraRenderer"/> 持有的跨帧事实板（F 通道）。
        /// 不能为 <c>null</c>。
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// 当 <paramref name="facts"/> 为 <c>null</c> 时抛出。
        /// </exception>
        public CameraContext(
            Camera camera, ScriptableRenderContext context, PassFrameFacts facts)
        {
            if (facts == null)
            {
                throw new System.ArgumentNullException(nameof(facts));
            }

            Camera = camera;
            Context = context;
            Facts = facts;

            // 绑定即清空：本渲染单元开始时事实板为「未发布」状态。
            Facts.Reset();

            Cmd = CommandBufferPool.Get("CameraContext");
        }

        /// <summary>
        /// 释放池化命令缓冲与原生数组。可多次调用——后续调用为空操作。
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

            // 有意不在此释放 CustomTargetRTHandle：渲染图命令在 Dispose 之后
            // （HNRenderPipeline.Render 内）才提交 GPU，句柄须跨帧存活；
            // 由拥有者 RealtimeProbeRenderer 在 context.Submit() 后释放。
        }
    }
}
