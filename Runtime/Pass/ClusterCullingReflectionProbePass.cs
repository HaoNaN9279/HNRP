// <copyright file="ClusterCullingReflectionProbePass.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    [Pass("Cluster Culling Probe")]
    public sealed class ClusterCullingReflectionProbePass : Pass, IGlobalShaderResource
    {
        /// <summary>
        /// 获取或设置反射探针图集单 slice 的分辨率（正方形边长）。
        /// 取值收敛到 [<see cref="MinProbeBlockSize"/>, <see cref="MaxAtlasResolution"/>]。
        /// </summary>
        /// <remarks>
        /// 上限 2048 由 <see cref="TextureAllocator"/> 的约束决定：
        /// 其要求 <c>textureResolution / minBlockSize &lt;= 8</c>，
        /// 而最小块尺寸必须为 256（满足 <c>ReflectionProbe256</c> 分辨率）。
        /// </remarks>
        public int AtlasSliceResolution
        {
            get => atlasSliceResolution;
            set => atlasSliceResolution = value;
        }

        /// <summary>
        /// 获取或设置反射探针图集的 slice 数量（最多 <see cref="MaxAtlasSliceCount"/>）。
        /// </summary>
        public int AtlasSliceCount
        {
            get => atlasSliceCount;
            set => atlasSliceCount = value;
        }

        // ── Slot ──

        /// <summary>
        /// 获取反射探针图集的输出纹理 slot
        /// （<see cref="TextureSlot"/>，<see cref="SlotDirection.Output"/>）。
        /// 本 pass 持有并写入图集（Tex2DArray），把其渲染图句柄发布到此 slot，
        /// 下游 pass 无需单独的资源节点即可连接。
        /// </summary>
        public TextureSlot ReflectionProbeAtlasOutputSlot { get; private set; }

        /// <summary>
        /// 获取簇剔除反射探针掩码缓冲的输出计算缓冲 slot
        /// （<see cref="ComputeBufferSlot"/>，<see cref="SlotDirection.Output"/>）。
        /// </summary>
        public ComputeBufferSlot ClusterCullingReflectionProbeMaskBufferSlot { get; private set; }

        /// <summary>
        /// 获取簇剔除反射探针数据缓冲的输出计算缓冲 slot
        /// （<see cref="ComputeBufferSlot"/>，<see cref="SlotDirection.Output"/>）。
        /// </summary>
        public ComputeBufferSlot ClusterCullingReflectionProbeDatasBufferSlot { get; private set; }

        // ── 可配置参数 ──

        /// <summary>
        /// 图集单 slice 分辨率（正方形边长）。默认 2048。
        /// </summary>
        [SerializeField]
        private int atlasSliceResolution = DefaultAtlasResolution;

        /// <summary>
        /// 图集 slice 数量（最多 <see cref="MaxAtlasSliceCount"/>）。默认 2。
        /// </summary>
        [SerializeField]
        private int atlasSliceCount = DefaultAtlasSliceCount;

        // ── 相机上下文 ──

        private CameraContext cameraContext;
        private ComputeShader computeShader;

        // ── 持久资源（随本 pass 实例跨帧保留，见 ADR-020）──

        /// <summary>
        /// 反射探针图集（Tex2DArray，逐 slice 一个 mip 链）。
        /// 位置与 slice 由 <see cref="textureAllocator"/> 分配。
        /// </summary>
        private RTHandle reflectionProbeAtlas;

        /// <summary>当前图集实际分配的单 slice 分辨率。</summary>
        private int allocatedResolution;

        /// <summary>当前图集实际分配的 slice 数量。</summary>
        private int allocatedSliceCount;

        /// <summary>图集槽位分配器（每 slice 一个 64 位 brick 掩码）。</summary>
        private TextureAllocator textureAllocator;

        /// <summary>图集重分配后需要在下次 render func 内整图清空。</summary>
        private bool needsAtlasClear;

        // ── 可复用暂存缓冲（每帧零 GC）──
        // 渲染循环每帧填充这些预分配缓冲，而不新建数组/列表。
        // 懒初始化一次，永久复用。

        private List<ProbeEntry> probeEntries;
        private List<uint> activeProbeIds;
        private HashSet<uint> visibleProbeIds;
        private Dictionary<uint, TextureAllocatorResult> allocateResults;
        private TextureAllocatorResult[] probeAllocations;
        private ReflectionProbeData4CS[] cullingDatas;
        private ClusterCullingReflectionProbeDatas[] sampleDatas;
        private Texture[] probeTextures;
        private bool[] probeDirty;
        private int[] probeBlockSizes;
        private int[] probeMipLevels;

        // ── 图集内容复用状态 ──
        // baked / custom 探针写入一次后不再重写；实时探针仅在 Phase B
        // 重渲染其 cubemap 后重写。状态随图集重建一同失效。

        /// <summary>驻留探针内容状态表，键为探针实例 id。</summary>
        private Dictionary<uint, ProbeSlot> residentProbes;

        /// <summary><see cref="ProbeSlot"/> 对象池，避免每帧堆分配。</summary>
        private Stack<ProbeSlot> freeProbeSlots;

        /// <summary>
        /// 本帧实时探针更新信号（Phase B 产出）。为 <c>null</c> 时无更新信息，
        /// 保守地对实时探针每帧重写（等价未接入信号前的行为）。
        /// </summary>
        private IReflectionProbeUpdateSource realtimeProbeUpdates;

        /// <summary>图集内容是否已失效（图集重建，或记录期被外部禁用导致未写入）。</summary>
        private bool atlasContentInvalid;

        // ── 常量 ──

        private const int MaxReflectionProbesOnScreen = 64;

        /// <summary>分配器最小块尺寸（brick），满足 <c>ReflectionProbe256</c>。</summary>
        private const int MinProbeBlockSize = 256;

        /// <summary>图集单 slice 分辨率上限（受 <see cref="TextureAllocator"/> 约束）。</summary>
        private const int MaxAtlasResolution = 2048;

        /// <summary>图集 slice 数量上限。</summary>
        private const int MaxAtlasSliceCount = 2;

        private const int DefaultAtlasResolution = 2048;
        private const int DefaultAtlasSliceCount = 2;
        private const int ReflectionProbeAtlasTexelPadding = 2;
        private const int MaxClusterMaskWords = 4096 * 4;
        private const string ClusterCullingKernelName = "ClusterCullingReflectionProbeCS";

        // ── 构造函数 ──

        /// <summary>
        /// 初始化 <see cref="ClusterCullingReflectionProbePass"/> 的新实例。
        /// pass 以默认图集参数（2048 分辨率、2 张 slice）启动；
        /// 图集参数可经模板代码 / 编辑器缓存 / 运行时动态设置覆盖。
        /// </summary>
        /// <remarks>
        /// 无参构造仅供 <see cref="RenderGraphAsset"/> 上参数缓存 Pass 的
        /// <c>[SerializeReference]</c> 反序列化使用；实例名随后由序列化数据填充。
        /// 参数默认值与带名构造保持一致。
        /// </remarks>
        public ClusterCullingReflectionProbePass()
            : base(string.Empty)
        {
        }

        /// <summary>
        /// 初始化 <see cref="ClusterCullingReflectionProbePass"/> 的新实例。
        /// </summary>
        /// <param name="passName">
        /// 本 pass 的实例名。必须非 null 且在渲染图内唯一。
        /// </param>
        public ClusterCullingReflectionProbePass(string passName)
            : base(passName)
        {
        }

        // ── 生命周期 ──

        /// <inheritdoc />
        public override void SetupSlots()
        {
            ReflectionProbeAtlasOutputSlot = new TextureSlot("reflectionProbeAtlasOutput", SlotDirection.Output);
            RegisterSlot(ReflectionProbeAtlasOutputSlot);
            ClusterCullingReflectionProbeMaskBufferSlot = new ComputeBufferSlot(
                "clusterCullingReflectionProbeMaskBuffer", SlotDirection.Output);
            RegisterSlot(ClusterCullingReflectionProbeMaskBufferSlot);
            ClusterCullingReflectionProbeDatasBufferSlot = new ComputeBufferSlot(
                "clusterCullingReflectionProbeDatasBuffer", SlotDirection.Output);
            RegisterSlot(ClusterCullingReflectionProbeDatasBufferSlot);
        }

        /// <inheritdoc />
        /// <remarks>
        /// 保存相机上下文，并从 <see cref="CameraContext.RuntimeResources"/> 解析簇剔除
        /// compute shader。不再重置图集参数 —— 默认值在构造函数初始化，
        /// 逐帧重置会覆盖模板代码 / 编辑器缓存 / 运行时动态设置的参数。
        /// </remarks>
        public override void PreRecord(RenderGraphAsset template, CameraContext context)
        {
            cameraContext = context;

            if (context.RuntimeResources != null)
            {
                computeShader = context.RuntimeResources.clusterCullingReflectionProbeCS;
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// 图集为 pass 持有的持久 <c>Tex2DArray</c>（尺寸 / slice 变化时重建），
        /// 掩码缓冲与两个探针数据缓冲仍为渲染图资源。记录的渲染函数执行：
        /// <list type="bullet">
        ///   <item>按 importance 排序可见探针，经 <see cref="TextureAllocator"/> 增量分配图集槽位</item>
        ///   <item>上传可见实时探针数据（剔除包围盒 + 采样数据）</item>
        ///   <item>派发簇剔除 compute shader 填充掩码缓冲</item>
        ///   <item>将每个实时探针 cubemap blit 进其八面体图集区域（含目标 slice）</item>
        ///   <item>生成图集 mip 链</item>
        /// </list>
        /// </remarks>
        public override void Record(RenderGraph renderGraph)
        {
            if (computeShader == null)
            {
                Debug.LogError(
                    "Cluster Culling Reflection Probe Compute Shader 为 null。 " +
                    "请确保已在管线资源的 HNRenderPipelineRuntimeResources 中赋值。");
                return;
            }

            if (cameraContext == null)
            {
                Debug.LogError("CameraContext 为 null。必须在 Record 前调用 Initialize。");
                return;
            }

            EnsureResources();

            // 上一帧记录后图集内容被判定失效（记录期被禁用而未写入）：全量标脏。
            if (atlasContentInvalid)
            {
                InvalidateAllProbeContent();
            }

            realtimeProbeUpdates = cameraContext.ReflectionProbeUpdates;

            // ── 收集可见探针（烘焙 + 实时）并按 importance 降序排序 ──
            // 可见的烘焙探针贡献其烘焙 cubemap；实时探针（在主相机之前的
            // Phase B 渲染）贡献其实时 cubemap。排序保证图集容量不足时
            // 优先保留重要性更高的探针，且同重要性下顺序稳定（不随可见顺序抖动）。
            EnsureScratchBuffers();
            List<ProbeEntry> entries = probeEntries;
            entries.Clear();
            if (cameraContext.VisibleReflectionProbes.IsCreated)
            {
                var visibleProbes = cameraContext.VisibleReflectionProbes;
                for (int i = 0; i < visibleProbes.Length; i++)
                {
                    UnityEngine.Rendering.VisibleReflectionProbe visibleProbe = visibleProbes[i];
                    ReflectionProbe probe = ReflectionProbeRenderUtils.GetReflectionProbe(visibleProbe);
                    if (probe == null)
                    {
                        continue;
                    }

                    // HNRP 自己管理实时探针的 cubemap（见 ReflectionProbeRenderer），
                    // Unity 的 VisibleReflectionProbe.texture 对这类探针为空，
                    // 因此仍按 mode 从 component 取纹理：
                    //   Realtime -> realtimeTexture；
                    //   Baked / Custom -> customBakedTexture。
                    // 时间切片只控制 cubemap 何时重渲染，不影响图集是否包含该探针。
                    Texture texture;
                    if (ReflectionProbeRenderUtils.IsRealtimeProbe(probe))
                    {
                        texture = probe.realtimeTexture;
                    }
                    else if (ReflectionProbeRenderUtils.IsBakedProbe(probe) || ReflectionProbeRenderUtils.IsCustomBakedProbe(probe))
                    {
                        texture = probe.customBakedTexture;
                    }
                    else
                    {
                        continue;
                    }

                    if (texture == null)
                    {
                        continue;
                    }

                    if (ClampProbeResolution(probe.resolution) <= 0)
                    {
                        continue;
                    }

                    entries.Add(new ProbeEntry
                    {
                        Probe = probe,
                        Texture = texture,
                        ProbeId = (uint)probe.GetInstanceID(),
                        Resolution = probe.resolution,
                        Importance = probe.importance,
                    });
                }
            }

            entries.Sort(CompareProbeEntries);

            UpdateProbeAllocations(entries);

            // ── 逐个探针填入采样数据（图集槽位以分配器最终结果为准）──
            // 分配器在容量不足时会触发重组（reorg）搬移既有块，故偏移必须在
            // 全部分配完成后统一回读（见 UpdateProbeAllocations），
            // 不能在分配循环内边分配边写。
            int probeCount = 0;
            for (int i = 0; i < entries.Count && probeCount < MaxReflectionProbesOnScreen; i++)
            {
                ProbeEntry entry = entries[i];
                uint probeId = entry.ProbeId;
                if (!residentProbes.TryGetValue(probeId, out ProbeSlot slot)
                    || !textureAllocator.Contains(probeId))
                {
                    continue;
                }

                ReflectionProbe probe = entry.Probe;
                Bounds bounds = probe.bounds;

                // Unity 对反射探针的可见性剔除按 bounds 外扩 blendDistance 计算
                //（VisibleReflectionProbe 的收录范围就是 box + blendDistance）。
                // 逐簇剔除必须用同一个外扩后的包围盒：否则当原始 box 离开视锥、
                // 而 box + blendDistance 仍与视锥相交时，所有簇都会被清空，
                // 整个探针被错误剔除，表现为相机移动时的跳变。
                // 注意：shader 的权重盒仍用原始 bounds（boxMin/boxMax），
                // 外扩只用于剔除包围盒。
                Vector3 cullExtents = bounds.extents + Vector3.one * probe.blendDistance;
                cullingDatas[probeCount] = new ReflectionProbeData4CS
                {
                    boundCenter = bounds.center,
                    boundExtents = cullExtents,
                };

                sampleDatas[probeCount] = new ClusterCullingReflectionProbeDatas
                {
                    boxMax = bounds.max,
                    boxMin = bounds.min,
                    positionWS = probe.transform.position,
                    blendDistance = probe.blendDistance,
                    importance = probe.importance,
                    intensity = probe.intensity,
                    scaleOffset = slot.allocation.ScaleOffset,
                    mipCount = GetBlockMipLevels(slot.blockSize),
                    sliceIndex = slot.allocation.SliceIndex,
                };

                probeAllocations[probeCount] = slot.allocation;
                probeBlockSizes[probeCount] = slot.blockSize;
                probeMipLevels[probeCount] = GetBlockMipLevels(slot.blockSize);
                probeDirty[probeCount] = slot.dirty;
                probeTextures[probeCount] = entry.Texture;
                probeCount++;
            }

            // ── 输入/输出：反射探针图集 ──
            // 图集归本 pass 所有（持久 Tex2DArray），导入渲染图后写入，
            // 并透传到输出 slot 供下游 pass 使用。

            TextureHandle atlasHandle = renderGraph.ImportTexture(reflectionProbeAtlas);
            if (!atlasHandle.IsValid())
            {
                // 本帧不会写入图集：已标记「有内容」的驻留状态必须作废。
                InvalidateAllProbeContent();
                return;
            }

            if (ReflectionProbeAtlasOutputSlot != null)
            {
                ReflectionProbeAtlasOutputSlot.SetHandle(atlasHandle);
            }

            using (var builder = renderGraph.AddRenderPass<ClusterCullingReflectionProbePassData>(
                PassName, out var passData))
            {
                builder.AllowPassCulling(false);

                passData.reflectionProbeAtlas = builder.WriteTexture(atlasHandle);

                // ── 输出：掩码缓冲 ──

                ComputeBufferHandle maskHandle = renderGraph.CreateComputeBuffer(
                    new ComputeBufferDesc(
                        MaxClusterMaskWords,
                        sizeof(uint))
                    { name = "Cluster Culling Reflection Probe Mask Buffer" });

                passData.clusterCullingReflectionProbeMaskBuffer = builder.WriteComputeBuffer(maskHandle);

                // ── 输出：剔除数据缓冲（ReflectionProbeData4CS 布局）──

                ComputeBufferHandle cullingDatasHandle = renderGraph.CreateComputeBuffer(
                    new ComputeBufferDesc(
                        MaxReflectionProbesOnScreen,
                        UnsafeUtility.SizeOf<ReflectionProbeData4CS>())
                    { name = "Cluster Culling Reflection Probe Culling Datas Buffer" });

                passData.cullingDatasBuffer = builder.WriteComputeBuffer(cullingDatasHandle);

                // ── 输出：采样数据缓冲（ClusterCullingReflectionProbeDatas 布局）──

                ComputeBufferHandle sampleDatasHandle = renderGraph.CreateComputeBuffer(
                    new ComputeBufferDesc(
                        MaxReflectionProbesOnScreen,
                        UnsafeUtility.SizeOf<ClusterCullingReflectionProbeDatas>())
                    { name = "Cluster Culling Reflection Probe Datas Buffer" });

                passData.sampleDatasBuffer = builder.WriteComputeBuffer(sampleDatasHandle);

                // ── 把真实渲染图句柄发布到输出 slot ──

                ClusterCullingReflectionProbeMaskBufferSlot!.SetHandle(maskHandle);
                ClusterCullingReflectionProbeDatasBufferSlot!.SetHandle(sampleDatasHandle);

                // ── compute shader 配置 ──

                passData.clusterCullingReflectionProbeCS = computeShader;
                passData.clusterCullingKernel = computeShader.FindKernel(ClusterCullingKernelName);

                Camera camera = cameraContext.Camera;
                int2 screenResolution = math.int2(camera.pixelWidth, camera.pixelHeight);
                int3 clusterSize = GetClusterSize(screenResolution);
                float2 clusterZScaleOffset = GetClusterZScaleOffset(
                    clusterSize, camera.orthographic,
                    camera.nearClipPlane, camera.farClipPlane);

                int itemsPerCluster = MaxReflectionProbesOnScreen;
                int wordsPerCluster = (itemsPerCluster + 31) / 32 + 1;

                // compute shader 通过 GPU 投影（D3D 风格，z 在 [0,1]）把簇切片深度
                // 变换到裁剪空间，再到世界空间与探针包围盒做 AABB 重叠测试。
                // 若直接使用原始 OpenGL projectionMatrix（NDC z 在 [-1,1]），
                // 一半裁剪 z 范围会低于 shader 的 [0,1] clamp，导致重叠测试失败。
                // HNRP 所有 pass 均经 RenderGraph 渲染，内部总是渲染到渲染纹理，
                // 因此 renderIntoTexture 恒为 true。
                Matrix4x4 gpuProj = GL.GetGPUProjectionMatrix(
                    camera.projectionMatrix, true);
                Matrix4x4 clipToView = gpuProj.inverse;
                Matrix4x4 viewToClip = gpuProj;
                Matrix4x4 clipToWorld = (gpuProj * camera.worldToCameraMatrix).inverse;

                // ── 每帧参数（经 PushGlobal 上传到 shader）──

                passData.clusterCullingReflectionProbeParams.clusterSizeXY =
                    new Vector2(clusterSize.x, clusterSize.y);
                passData.clusterCullingReflectionProbeParams.clusterZScaleOffset =
                    new Vector2(clusterZScaleOffset.x, clusterZScaleOffset.y);
                passData.clusterCullingReflectionProbeParams.wordsPerCluster =
                    wordsPerCluster;
                passData.clusterCullingReflectionProbeParams.reflectionProbeCount =
                    probeCount;
                passData.clusterCullingReflectionProbeParams.atlasSize =
                    allocatedResolution;
                passData.clusterCullingReflectionProbeParams.atlasSliceCount =
                    allocatedSliceCount;

                // ── 把每帧值存到池化 pass data 上，
                // 使渲染函数闭包只捕获 `this`（零分配）──

                passData.clusterSize = clusterSize;
                passData.probeCount = probeCount;
                passData.cameraOrthographic = camera.orthographic;
                passData.clipToView = clipToView;
                passData.viewToClip = viewToClip;
                passData.clipToWorld = clipToWorld;

                // ── 渲染函数 ──

                builder.SetRenderFunc(
                    (ClusterCullingReflectionProbePassData data, RenderGraphContext ctx) =>
                    {
                        if(!IsEnabled)
                        {
                            // 记录期被外部禁用：本帧不会写入图集，已标记为
                            // 「有内容」的驻留状态必须作废，避免下次错误跳过写入。
                            atlasContentInvalid = true;
                            return;
                        }

                        // 图集重分配后整图清空（一次性）：未被本帧探针覆盖的区域
                        // 保留上一帧内容（或未初始化内容），清一次避免残留。
                        if (needsAtlasClear)
                        {
                            for (int slice = 0; slice < this.allocatedSliceCount; slice++)
                            {
                                ctx.cmd.SetRenderTarget(
                                    (RenderTargetIdentifier)data.reflectionProbeAtlas,
                                    0,
                                    CubemapFace.Unknown,
                                    slice);
                                ctx.cmd.ClearRenderTarget(false, true, Color.black);
                            }

                            needsAtlasClear = false;
                        }

                        // 上传剔除派发与 shader 使用的探针数据。
                        ctx.cmd.SetBufferData(data.cullingDatasBuffer, this.cullingDatas);
                        ctx.cmd.SetBufferData(data.sampleDatasBuffer, this.sampleDatas);

                        ctx.cmd.SetComputeBufferParam(
                            data.clusterCullingReflectionProbeCS,
                            data.clusterCullingKernel,
                            PropertyIDs.clusterCullingReflectionProbeMaskBuffer,
                            data.clusterCullingReflectionProbeMaskBuffer);
                        ctx.cmd.SetComputeBufferParam(
                            data.clusterCullingReflectionProbeCS,
                            data.clusterCullingKernel,
                            PropertyIDs.reflectionProbeDatas4CSBuffer,
                            data.cullingDatasBuffer);

                        Vector2 clusterZScaleOffsetInPass = data.clusterCullingReflectionProbeParams.clusterZScaleOffset;
                        int wordsPerClusterInPass = data.clusterCullingReflectionProbeParams.wordsPerCluster;
                        int3 clusterSizeInPass = data.clusterSize;
                        int probeCountInPass = data.probeCount;

                        ctx.cmd.SetComputeVectorParam(
                            data.clusterCullingReflectionProbeCS,
                            PropertyIDs.cullingParams0,
                            new Vector4(
                                clusterZScaleOffsetInPass.x,
                                clusterZScaleOffsetInPass.y,
                                wordsPerClusterInPass,
                                data.cameraOrthographic ? 1.0f : 0.0f));
                        ctx.cmd.SetComputeVectorParam(
                            data.clusterCullingReflectionProbeCS,
                            PropertyIDs.cullingParams1,
                            new Vector4(clusterSizeInPass.x, clusterSizeInPass.y, clusterSizeInPass.z, probeCountInPass));

                        ctx.cmd.SetComputeMatrixParam(
                            data.clusterCullingReflectionProbeCS,
                            PropertyIDs.cullingClipToViewMatrix,
                            data.clipToView);
                        ctx.cmd.SetComputeMatrixParam(
                            data.clusterCullingReflectionProbeCS,
                            PropertyIDs.cullingViewToClipMatrix,
                            data.viewToClip);
                        ctx.cmd.SetComputeMatrixParam(
                            data.clusterCullingReflectionProbeCS,
                            PropertyIDs.cullingClipToWorldMatrix,
                            data.clipToWorld);

                        // 在整个 3D 网格上按每簇一个线程派发。
                        // numthreads(8,8,1)：线程组覆盖 x/y，z 向线程组覆盖每个深度
                        // 切片（id.z 即簇索引的 z）。
                        int threadGroupX = (clusterSizeInPass.x + 7) / 8;
                        int threadGroupY = (clusterSizeInPass.y + 7) / 8;
                        ctx.cmd.DispatchCompute(
                            data.clusterCullingReflectionProbeCS,
                            data.clusterCullingKernel,
                            threadGroupX,
                            threadGroupY,
                            clusterSizeInPass.z);

                        // ── 把需要更新的探针 cubemap blit 进其图集区域 ──
                        // baked / custom 探针内容不变，写入一次后跨帧复用；
                        // 实时探针仅在其 cubemap 被 Phase B 重渲染后重写。
                        // 逐级 mip 直接由源 cubemap 投影得到（八面体投影在每个 mip
                        // 独立进行），故无需整图 GenerateMips，也不会把未更新
                        // 探针的 mip 链重算一遍。
                        for (int i = 0; i < probeCountInPass; i++)
                        {
                            if (!this.probeDirty[i])
                            {
                                continue;
                            }

                            Texture source = this.probeTextures[i];
                            if (source == null)
                            {
                                continue;
                            }

                            TextureAllocatorResult allocation = this.probeAllocations[i];
                            int blockSize = this.probeBlockSizes[i];
                            int mipLevels = this.probeMipLevels[i];
                            int sourceMaxMip = Mathf.Max(0, source.mipmapCount - 1);
                            int texelPadding = ReflectionProbeAtlasTexelPadding;

                            for (int mip = 0; mip < mipLevels; mip++)
                            {
                                ctx.cmd.SetRenderTarget(
                                    (RenderTargetIdentifier)data.reflectionProbeAtlas,
                                    mip,
                                    CubemapFace.Unknown,
                                    allocation.SliceIndex);

                                var propertyBlock = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                                Blitter.BlitCubeToOctahedral2DQuadWithPadding(
                                    ctx.cmd,
                                    propertyBlock,
                                    source,
                                    GetTextureSizeWithoutPadding(blockSize, mip, texelPadding),
                                    allocation.ScaleOffset,
                                    Mathf.Min(mip, sourceMaxMip),
                                    true,
                                    texelPadding);
                            }
                        }

                        // 上传簇剔除参数，使片元 shader 能解析簇索引来迭代探针。
                        ConstantBuffer.PushGlobal(
                            ctx.cmd,
                            data.clusterCullingReflectionProbeParams,
                            PropertyIDs.clusterCullingReflectionProbeParamsBuffer);
                    });
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// 释放图集（持久资源）。分配表与暂存缓冲随实例一并清空。
        /// </remarks>
        public override void Cleanup()
        {
            reflectionProbeAtlas?.Release();
            reflectionProbeAtlas = null;

            textureAllocator = null;
            allocatedResolution = 0;
            allocatedSliceCount = 0;
            activeProbeIds?.Clear();
            allocateResults?.Clear();
            residentProbes?.Clear();
            freeProbeSlots?.Clear();
            atlasContentInvalid = false;
            realtimeProbeUpdates = null;
            cameraContext = null;
            computeShader = null;
        }

        /// <inheritdoc />
        /// <remarks>
        /// 图集、掩码缓冲与探针数据缓冲三者齐备（且本 pass 启用）时，开启
        /// <c>CLUSTER_CULLING_REFLECTION_PROBE</c> keyword 并绑定对应全局资源；
        /// 否则关闭该 keyword，避免全局状态残留。
        /// </remarks>
        public void BindGlobalShaderResources(CommandBuffer cmd)
        {
            bool available = IsEnabled
                && reflectionProbeAtlas != null
                && ClusterCullingReflectionProbeMaskBufferSlot != null
                && ClusterCullingReflectionProbeMaskBufferSlot.HasHandle
                && ClusterCullingReflectionProbeDatasBufferSlot != null
                && ClusterCullingReflectionProbeDatasBufferSlot.HasHandle;

            if (!available)
            {
                cmd.DisableShaderKeyword(GlobalKeywords.clusterCullingReflectionProbe);
                return;
            }

            cmd.EnableShaderKeyword(GlobalKeywords.clusterCullingReflectionProbe);
            cmd.SetGlobalTexture(
                PropertyIDs.reflectionProbeAtlas,
                (RenderTargetIdentifier)reflectionProbeAtlas);
            cmd.SetGlobalBuffer(
                PropertyIDs.clusterCullingReflectionProbeMaskBuffer,
                ClusterCullingReflectionProbeMaskBufferSlot.ReadHandle());
            cmd.SetGlobalBuffer(
                PropertyIDs.clusterCullingReflectionProbeDatasBuffer,
                ClusterCullingReflectionProbeDatasBufferSlot.ReadHandle());
        }

        // ── 资源 ──

        /// <summary>
        /// 确保图集与分配器已按当前参数分配。分辨率 / slice 数量变化时重建图集、
        /// 重建分配器并清空驻留分配。
        /// </summary>
        private void EnsureResources()
        {
            int resolution = Mathf.Clamp(
                Mathf.ClosestPowerOfTwo(atlasSliceResolution),
                MinProbeBlockSize,
                MaxAtlasResolution);
            int slices = Mathf.Clamp(atlasSliceCount, 1, MaxAtlasSliceCount);

            if (reflectionProbeAtlas != null
                && allocatedResolution == resolution
                && allocatedSliceCount == slices)
            {
                return;
            }

            reflectionProbeAtlas?.Release();

            reflectionProbeAtlas = RTHandles.Alloc(
                resolution,
                resolution,
                slices: slices,
                depthBufferBits: DepthBits.None,
                colorFormat: GraphicsFormat.B10G11R11_UFloatPack32,
                filterMode: FilterMode.Trilinear,
                wrapMode: TextureWrapMode.Clamp,
                dimension: TextureDimension.Tex2DArray,
                useMipMap: true,
                autoGenerateMips: false,
                name: "HN Reflection Probe Atlas");

            allocatedResolution = resolution;
            allocatedSliceCount = slices;

            // 分配器：brick = 256（满足 ReflectionProbe256），最大块 = 单 slice 分辨率。
            textureAllocator = new TextureAllocator(resolution, MinProbeBlockSize, resolution, slices);

            activeProbeIds?.Clear();

            needsAtlasClear = true;

            // 图集已重建：所有驻留内容失效，必须重写。
            InvalidateAllProbeContent();
        }

        // ── 分配 ──

        /// <summary>
        /// 按本帧可见探针集合同步分配器与逐探针内容状态：新增 / 改变分辨率的
        /// 重新分配，不再可见的立即释放，并判定每个驻留探针本帧是否需要
        /// 重写其图集区域。
        /// </summary>
        /// <param name="entries">已按 importance 降序排序的可见探针。</param>
        /// <remarks>
        /// 分配可能触发重组（reorg）搬移既有块，因此槽位与脏标记必须在全部
        /// 分配结束后统一回读（<see cref="TextureAllocator.Contains"/> +
        /// <see cref="TextureAllocator.TryGetResult"/>），不能在分配循环内边分配边写。
        /// </remarks>
        private void UpdateProbeAllocations(List<ProbeEntry> entries)
        {
            allocateResults.Clear();
            visibleProbeIds.Clear();

            for (int i = 0; i < entries.Count; i++)
            {
                ProbeEntry entry = entries[i];
                uint probeId = entry.ProbeId;
                int desired = ClampProbeResolution(entry.Resolution);
                if (desired <= 0)
                {
                    continue;
                }

                bool allocatedThisFrame = false;

                // 驻留判定同时要求 blocks（Contains）与分配结果存在：
                // 分配器在重组失败的路径上会残留过期的 allocatedResults 条目，
                // 只用 TryGetResult 会把已丢失的槽位当成有效槽位。
                bool resident = textureAllocator.Contains(probeId)
                    && textureAllocator.TryGetResult(probeId, out TextureAllocatorResult current)
                    && Mathf.RoundToInt(current.ScaleOffset.x * allocatedResolution) == desired;

                if (!resident)
                {
                    if (textureAllocator.Contains(probeId))
                    {
                        textureAllocator.Release(probeId);
                    }

                    textureAllocator.Allocate(ref allocateResults, probeId, desired);
                    allocatedThisFrame = true;

                    if (textureAllocator.Contains(probeId) && !activeProbeIds.Contains(probeId))
                    {
                        activeProbeIds.Add(probeId);
                    }
                }

                visibleProbeIds.Add(probeId);

                // 分配失败的探针（容量耗尽）不驻留：本帧不写入数据缓冲，
                // shader 不会引用它，下次可见时重试。
                if (!textureAllocator.Contains(probeId)
                    || !textureAllocator.TryGetResult(probeId, out TextureAllocatorResult allocation))
                {
                    ReleaseProbeSlot(probeId);
                    continue;
                }

                ProbeSlot slot = AcquireProbeSlot(probeId);
                bool isRealtime = ReflectionProbeRenderUtils.IsRealtimeProbe(entry.Probe);
                bool realtimeUpdated = isRealtime
                    && realtimeProbeUpdates != null
                    && realtimeProbeUpdates.IsProbeUpdatedThisFrame((int)probeId);

                bool stateChanged = slot.allocation.ScaleOffset != allocation.ScaleOffset
                    || slot.allocation.SliceIndex != allocation.SliceIndex
                    || slot.blockSize != desired
                    || slot.source != entry.Texture;

                slot.dirty = ShouldRewriteProbe(
                    slot, allocatedThisFrame, stateChanged, isRealtime, realtimeUpdated);

                slot.allocation = allocation;
                slot.source = entry.Texture;
                slot.blockSize = desired;

                // 本帧已决定写入（记录期判定）：内容视为有效，供后续帧复用。
                slot.hasContent = true;
            }

            // 释放本帧不再可见（或分配失败）的驻留探针。只遍历活跃项，O(驻留数)。
            for (int i = 0; i < activeProbeIds.Count;)
            {
                uint probeId = activeProbeIds[i];
                if (visibleProbeIds.Contains(probeId) && textureAllocator.Contains(probeId))
                {
                    i++;
                    continue;
                }

                textureAllocator.Release(probeId);
                ReleaseProbeSlot(probeId);
                activeProbeIds[i] = activeProbeIds[activeProbeIds.Count - 1];
                activeProbeIds.RemoveAt(activeProbeIds.Count - 1);
            }
        }

        /// <summary>
        /// 判定某驻留探针本帧是否需要重写其图集区域。
        /// </summary>
        /// <param name="slot">该探针的驻留状态（调用前的值）。</param>
        /// <param name="allocatedThisFrame">
        /// 本帧是否对该探针执行过分配（含释放后重分配）。
        /// </param>
        /// <param name="stateChanged">
        /// 槽位 / 块尺寸 / 源纹理相对上次写入是否发生变化（含被重组搬移）。
        /// </param>
        /// <param name="isRealtime">是否为实时探针。</param>
        /// <param name="realtimeUpdated">Phase B 是否在本帧重渲染过该探针的任一 cubemap 面。</param>
        /// <returns>需要重写时返回 <c>true</c>。</returns>
        /// <remarks>
        /// <para>
        /// <paramref name="allocatedThisFrame"/> 必须参与判定，不能只看槽位是否变化：
        /// 槽位被释放后可能被其它探针占用并写入，随后本探针重新可见又拿回
        /// <b>同一偏移</b>，只比偏移会漏判，从而采到其它探针的残留内容。
        /// </para>
        /// <para>
        /// baked / custom 探针内容不变，写入一次后长期复用；实时探针仅在
        /// 被重渲染后重写。无更新信号（<see cref="realtimeProbeUpdates"/> 为
        /// <c>null</c>）时对实时探针保守地每帧重写。
        /// </para>
        /// </remarks>
        private bool ShouldRewriteProbe(
            ProbeSlot slot,
            bool allocatedThisFrame,
            bool stateChanged,
            bool isRealtime,
            bool realtimeUpdated)
        {
            if (!slot.hasContent || allocatedThisFrame || stateChanged)
            {
                return true;
            }

            if (!isRealtime)
            {
                return false;
            }

            return realtimeProbeUpdates == null || realtimeUpdated;
        }

        /// <summary>
        /// 取回或新建探针驻留状态（经对象池复用，稳态零分配）。
        /// </summary>
        private ProbeSlot AcquireProbeSlot(uint probeId)
        {
            if (!residentProbes.TryGetValue(probeId, out ProbeSlot slot))
            {
                slot = freeProbeSlots.Count > 0 ? freeProbeSlots.Pop() : new ProbeSlot();
                residentProbes[probeId] = slot;
            }

            return slot;
        }

        /// <summary>
        /// 归还探针驻留状态（探针不再驻留图集时调用）。
        /// </summary>
        private void ReleaseProbeSlot(uint probeId)
        {
            if (residentProbes.TryGetValue(probeId, out ProbeSlot slot))
            {
                residentProbes.Remove(probeId);
                freeProbeSlots.Push(slot);
            }
        }

        /// <summary>
        /// 使全部驻留探针内容失效，强制下一次记录重写（图集重建 / 未写入时调用）。
        /// </summary>
        private void InvalidateAllProbeContent()
        {
            atlasContentInvalid = false;

            if (residentProbes == null)
            {
                return;
            }

            foreach (KeyValuePair<uint, ProbeSlot> pair in residentProbes)
            {
                pair.Value.hasContent = false;
            }
        }

        /// <summary>
        /// 把探针请求分辨率收敛到分配器支持的块尺寸。
        /// </summary>
        /// <param name="resolution">探针请求分辨率（通常为 2 的幂）。</param>
        /// <returns>
        /// 可用的块尺寸；请求无效（小于等于 0）时返回 0，调用方应跳过该探针。
        /// </returns>
        /// <remarks>
        /// 低于 <see cref="MinProbeBlockSize"/> 的探针（如 128）向上收敛到 256，
        /// 即按 256 块分配与八面体投影（放大采样），只浪费图集空间，不丢功能。
        /// </remarks>
        private int ClampProbeResolution(int resolution)
        {
            if (resolution <= 0)
            {
                return 0;
            }

            int maxBlockSize = Mathf.Min(MaxAtlasResolution, allocatedResolution);
            int clamped = Mathf.Clamp(resolution, MinProbeBlockSize, maxBlockSize);
            return Mathf.ClosestPowerOfTwo(clamped);
        }

        /// <summary>
        /// 探针排序比较器：importance 降序，同重要性按实例 id 升序（保证顺序稳定）。
        /// </summary>
        private static int CompareProbeEntries(ProbeEntry x, ProbeEntry y)
        {
            int weight = y.Importance.CompareTo(x.Importance);
            return weight != 0 ? weight : x.ProbeId.CompareTo(y.ProbeId);
        }

        // ── 暂存缓冲辅助 ──

        /// <summary>
        /// 懒分配每帧暂存缓冲，一次分配永久复用，使渲染循环保持零分配。
        /// </summary>
        private void EnsureScratchBuffers()
        {
            if (probeEntries == null)
            {
                probeEntries = new List<ProbeEntry>(MaxReflectionProbesOnScreen);
                activeProbeIds = new List<uint>(MaxReflectionProbesOnScreen);
                visibleProbeIds = new HashSet<uint>();
                allocateResults = new Dictionary<uint, TextureAllocatorResult>();
                probeAllocations = new TextureAllocatorResult[MaxReflectionProbesOnScreen];
                probeBlockSizes = new int[MaxReflectionProbesOnScreen];
                probeMipLevels = new int[MaxReflectionProbesOnScreen];
                probeDirty = new bool[MaxReflectionProbesOnScreen];
                cullingDatas = new ReflectionProbeData4CS[MaxReflectionProbesOnScreen];
                sampleDatas = new ClusterCullingReflectionProbeDatas[MaxReflectionProbesOnScreen];
                probeTextures = new Texture[MaxReflectionProbesOnScreen];
                residentProbes = new Dictionary<uint, ProbeSlot>();
                freeProbeSlots = new Stack<ProbeSlot>();
            }
        }

        // ── 图集布局辅助 ──

        /// <summary>
        /// 把归一化图集区域（分配器结果）转换为指定 mip 上不含 padding 的纹素尺寸。
        /// </summary>
        /// <param name="blockSize">该区域在 mip 0 的纹素边长。</param>
        /// <param name="mip">目标 mip 级别（0 起）。</param>
        /// <param name="texelPadding">每侧 padding 的纹素数量。</param>
        private static Vector2 GetTextureSizeWithoutPadding(
            int blockSize, int mip, int texelPadding)
        {
            int mipSize = blockSize >> mip;
            return new Vector2(
                mipSize - texelPadding * 2,
                mipSize - texelPadding * 2);
        }

        /// <summary>
        /// 块尺寸对应的实际 mip 级数（含 mip 0）。
        /// </summary>
        /// <remarks>
        /// 逐级投影要求 <c>(blockSize &gt;&gt; mip) &gt; 2 * padding</c>，故尾部两级
        /// 退化（padding 会吃掉整个块）不再生成：级数 = <c>log2(blockSize) - 2</c>。
        /// <see cref="ClusterCullingReflectionProbeDatas.mipCount"/> 使用同一值，
        /// 保证 shader 只采样到真实生成过的 mip。
        /// </remarks>
        private static int GetBlockMipLevels(int blockSize)
        {
            return Mathf.Max(1, (int)Mathf.Log(blockSize, 2.0f) - 2);
        }

        /// <summary>
        /// 单探针的图集驻留状态：分配槽位 + 内容有效性。
        /// </summary>
        /// <remarks>
        /// 内容有效的 baked / custom 探针跨帧复用（不重写图集）；
        /// 实时探针仅在 Phase B 重渲染其 cubemap 后重写。
        /// </remarks>
        private sealed class ProbeSlot
        {
            /// <summary>分配器给出的当前槽位。</summary>
            public TextureAllocatorResult allocation;

            /// <summary>上次写入图集所用的源 cubemap；源被替换时视为内容失效。</summary>
            public Texture source;

            /// <summary>块分辨率（决定 mip 级数）。</summary>
            public int blockSize;

            /// <summary>本帧是否需要重写该区域。</summary>
            public bool dirty;

            /// <summary>该区域内容是否已写入且仍然有效。</summary>
            public bool hasContent;
        }

        /// <summary>
        /// 本帧已排入图集打包的可见探针。
        /// </summary>
        private struct ProbeEntry
        {
            public ReflectionProbe Probe;
            public Texture Texture;
            public uint ProbeId;
            public int Resolution;
            public float Importance;
        }

        // ── 簇尺寸计算 ──

        private const int ClusterMinTileSize = 8;
        private const int ClusterMaxZSlice = 128;
        private const int ClusterMinZSlice = 16;

        /// <summary>
        /// cluster 网格计算的最小屏幕分辨率。低于此值（0、极小窗口、预览相机）
        /// 会使 tileCountPerSlice 为 0 导致除零 / 死循环，故钳制。
        /// </summary>
        private const int MinClusterScreenResolution = 128;

        private static int3 GetClusterSize(int2 screenResolution)
        {
            // 退化分辨率（0 或极小，如窗口最小化 / 预览相机）会让 clusterSizeXY
            // 坍缩为非正值，使 tileCountPerSlice 为 0 → 除零 / 死循环。
            // 钳制到最小分辨率，保证计算可终止且结果为合法正值。
            screenResolution = math.max(
                screenResolution, new int2(MinClusterScreenResolution));

            // 每个簇在掩码缓冲中存 wordsPerCluster 个 uint
            // （header + 每 32 个探针位一个字）。切片数量必须由掩码缓冲
            // 容量除以每簇字数得出，否则 compute shader 会写出缓冲末尾。
            int wordsPerCluster = (MaxReflectionProbesOnScreen + 31) / 32 + 1;
            int2 clusterSizeXY = new int2(1, 1);
            int sliceCount = ClusterMinZSlice;
            int tileWidth = ClusterMinTileSize >> 1;
            do
            {
                tileWidth <<= 1;
                clusterSizeXY = (screenResolution + tileWidth - 1) / tileWidth;
                int tileCountPerSlice = clusterSizeXY.x * clusterSizeXY.y;
                sliceCount = MaxClusterMaskWords / (tileCountPerSlice * wordsPerCluster) - 1;
            }
            while (sliceCount < ClusterMinZSlice || sliceCount > ClusterMaxZSlice);

            return new int3(clusterSizeXY.x, clusterSizeXY.y, sliceCount);
        }

        private static float2 GetClusterZScaleOffset(
            int3 clusterSize, bool isOrthographic,
            float nearClipPlane, float farClipPlane)
        {
            float2 result;
            if (isOrthographic)
            {
                result.x = (float)clusterSize.z / (farClipPlane - nearClipPlane);
                result.y = -nearClipPlane * result.x;
            }
            else
            {
                result.x = (float)clusterSize.z / (math.log2(farClipPlane) - math.log2(nearClipPlane));
                result.y = -math.log2(nearClipPlane) * result.x;
            }

            return result;
        }

        // ── Pass data ──

        /// <summary>
        /// <see cref="ClusterCullingReflectionProbePass"/> 的渲染图 pass 数据容器。
        /// </summary>
        private sealed class ClusterCullingReflectionProbePassData
        {
            /// <summary>
            /// 反射探针图集纹理句柄。
            /// </summary>
            public TextureHandle reflectionProbeAtlas;

            /// <summary>
            /// 簇剔除掩码缓冲句柄。
            /// </summary>
            public ComputeBufferHandle clusterCullingReflectionProbeMaskBuffer;

            /// <summary>
            /// 簇剔除探针剔除数据缓冲句柄
            /// （<see cref="ReflectionProbeData4CS"/> 布局，喂给 compute shader）。
            /// </summary>
            public ComputeBufferHandle cullingDatasBuffer;

            /// <summary>
            /// 簇剔除探针采样数据缓冲句柄
            /// （<see cref="ClusterCullingReflectionProbeDatas"/> 布局，由 shader 消费）。
            /// </summary>
            public ComputeBufferHandle sampleDatasBuffer;

            /// <summary>
            /// 簇剔除 compute shader。
            /// </summary>
            public ComputeShader clusterCullingReflectionProbeCS;

            /// <summary>
            /// 剔除派发使用的 kernel 索引。
            /// </summary>
            public int clusterCullingKernel;

            /// <summary>
            /// 经 <c>_ClusterCullingReflectionProbeParamsBuffer</c> 上传到
            /// shader 的簇剔除参数。
            /// </summary>
            public ClusterCullingReflectionProbeParams clusterCullingReflectionProbeParams;

            /// <summary>
            /// 本帧簇网格尺寸。
            /// </summary>
            public int3 clusterSize;

            /// <summary>
            /// 本帧打包进图集的探针数量。
            /// </summary>
            public int probeCount;

            /// <summary>
            /// 当前相机是否为正交投影。
            /// </summary>
            public bool cameraOrthographic;

            /// <summary>
            /// 裁剪空间到视图空间矩阵。
            /// </summary>
            public Matrix4x4 clipToView;

            /// <summary>
            /// 视图空间到裁剪空间矩阵。
            /// </summary>
            public Matrix4x4 viewToClip;

            /// <summary>
            /// 裁剪空间到世界空间矩阵。
            /// </summary>
            public Matrix4x4 clipToWorld;
        }

        // ── shader 属性 ID ──

        /// <summary>
        /// 簇剔除反射探针 compute shader 参数的 shader 属性标识。
        /// 与簇剔除反射探针 compute shader 使用的属性 ID 保持一致。
        /// </summary>
        public static class PropertyIDs
        {
            /// <summary>
            /// 反射探针图集纹理（Tex2DArray）。值：<c>_ReflectionProbeAtlas</c>。
            /// </summary>
            public static readonly int reflectionProbeAtlas =
                Shader.PropertyToID("_ReflectionProbeAtlas");

            /// <summary>
            /// 簇剔除反射探针掩码缓冲（RWStructuredBuffer）。
            /// 值：<c>_ClusterCullingReflectionProbeMaskBuffer</c>。
            /// </summary>
            public static readonly int clusterCullingReflectionProbeMaskBuffer =
                Shader.PropertyToID("_ClusterCullingReflectionProbeMaskBuffer");

            /// <summary>
            /// 簇剔除反射探针数据缓冲（RWStructuredBuffer）。
            /// 值：<c>_ClusterCullingReflectionProbeDatasBuffer</c>。
            /// </summary>
            public static readonly int clusterCullingReflectionProbeDatasBuffer =
                Shader.PropertyToID("_ClusterCullingReflectionProbeDatasBuffer");

            /// <summary>
            /// 剔除参数 0：x=z 缩放、y=z 偏移、z=wordsPerCluster、w=isOrthographic。
            /// 值：<c>_ClusterCullingReflectionProbeParams0</c>。
            /// </summary>
            public static readonly int cullingParams0 =
                Shader.PropertyToID("_ClusterCullingReflectionProbeParams0");

            /// <summary>
            /// 剔除参数 1：xyz=clusterSize、w=probeCount。
            /// 值：<c>_ClusterCullingReflectionProbeParams1</c>。
            /// </summary>
            public static readonly int cullingParams1 =
                Shader.PropertyToID("_ClusterCullingReflectionProbeParams1");

            /// <summary>
            /// 裁剪空间到视图空间矩阵。值：<c>_ClusterCullingReflectionProbeClipToView</c>。
            /// </summary>
            public static readonly int cullingClipToViewMatrix =
                Shader.PropertyToID("_ClusterCullingReflectionProbeClipToView");

            /// <summary>
            /// 视图空间到裁剪空间矩阵。值：<c>_ClusterCullingReflectionProbeViewToClip</c>。
            /// </summary>
            public static readonly int cullingViewToClipMatrix =
                Shader.PropertyToID("_ClusterCullingReflectionProbeViewToClip");

            /// <summary>
            /// 裁剪空间到世界空间矩阵。值：<c>_ClusterCullingReflectionProbeClipToWorld</c>。
            /// </summary>
            public static readonly int cullingClipToWorldMatrix =
                Shader.PropertyToID("_ClusterCullingReflectionProbeClipToWorld");

            /// <summary>
            /// 簇剔除反射探针参数缓冲（探针参数结构化缓冲）。
            /// 值：<c>_ClusterCullingReflectionProbeParamsBuffer</c>。
            /// </summary>
            public static readonly int clusterCullingReflectionProbeParamsBuffer =
                Shader.PropertyToID("_ClusterCullingReflectionProbeParamsBuffer");

            /// <summary>
            /// 供 compute shader 使用的反射探针数据缓冲。
            /// 值：<c>_ClusterCullingReflectionProbeDatas4CSBuffer</c>。
            /// </summary>
            public static readonly int reflectionProbeDatas4CSBuffer =
                Shader.PropertyToID("_ClusterCullingReflectionProbeDatas4CSBuffer");
        }

        // ── 簇剔除数据结构（自旧版 ClusterCullingReflectionProbePass 迁移）──
    }

    /// <summary>
    /// 供 compute shader 剔除的单探针数据。
    /// 每个元素保存单个反射探针的世界空间包围盒中心与尺寸。
    /// </summary>
    [Serializable]
    public struct ReflectionProbeData4CS
    {
        /// <summary>
        /// 反射探针世界空间包围盒中心。
        /// </summary>
        public float3 boundCenter;

        /// <summary>
        /// 反射探针世界空间包围盒尺寸。
        /// </summary>
        public float3 boundExtents;
    }

    /// <summary>
    /// 剔除后传给 shader 的单探针渲染数据。
    /// 与旧版 <c>ClusterCullingReflectionProbeDatas</c> 结构保持一致。
    /// </summary>
    /// <remarks>
    /// 结构与 HLSL 侧 <c>ClusterCullingReflectionProbeDatas</c> 严格对应（步长 80 字节），
    /// 字段顺序或类型变化必须两侧同步。
    /// </remarks>
    [Serializable]
    unsafe public struct ClusterCullingReflectionProbeDatas
    {
        /// <summary>
        /// 探针世界空间包围盒最大角点。
        /// </summary>
        public Vector3 boxMax;

        /// <summary>
        /// 探针间交叉淡化的混合距离。
        /// </summary>
        public float blendDistance;

        /// <summary>
        /// 探针世界空间包围盒最小角点。
        /// </summary>
        public Vector3 boxMin;

        /// <summary>
        /// 本探针的重要性权重。
        /// </summary>
        public float importance;

        /// <summary>
        /// 反射探针的世界空间位置。
        /// </summary>
        public Vector3 positionWS;

        /// <summary>
        /// 本探针贡献的强度倍率。
        /// </summary>
        public float intensity;

        /// <summary>
        /// 采样探针 cubemap 使用的缩放与偏移（相对图集单 slice 归一化）。
        /// </summary>
        public Vector4 scaleOffset;

        /// <summary>
        /// 当前探针所在图集块的 mip 数量。不同分辨率的块拥有不同的 mip 数量。
        /// </summary>
        public float mipCount;

        /// <summary>
        /// 当前探针所在图集的 slice 索引。
        /// </summary>
        public float sliceIndex;

        /// <summary>
        /// 未用填充。
        /// </summary>
        public Vector2 unused;
    }

    /// <summary>
    /// 传给 compute shader 的簇剔除参数。
    /// 与旧版 <c>ClusterCullingReflectionProbeParams</c> 结构保持一致。
    /// </summary>
    [Serializable]
    unsafe public struct ClusterCullingReflectionProbeParams
    {
        /// <summary>
        /// 屏幕空间簇尺寸（XY）。
        /// </summary>
        public Vector2 clusterSizeXY;

        /// <summary>
        /// 簇深度切片的 Z 轴缩放与偏移。
        /// </summary>
        public Vector2 clusterZScaleOffset;

        /// <summary>
        /// 掩码缓冲中每簇的 32 位字数。
        /// </summary>
        public int wordsPerCluster;

        /// <summary>
        /// 反射探针总数。
        /// </summary>
        public int reflectionProbeCount;

        /// <summary>
        /// 图集单 slice 分辨率（供片元 shader 归一化八面体 padding）。
        /// </summary>
        public float atlasSize;

        /// <summary>
        /// 图集 slice 数量。
        /// </summary>
        public float atlasSliceCount;
    }
}
