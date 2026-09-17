// <copyright file="DebugDisplayPass.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

#if UNITY_EDITOR || HNRP_DEBUG_DISPLAY

using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    /// <summary>
    /// 渲染调试显示 pass：把单值文本列表与纹理预览叠加到相机后备缓冲。
    /// </summary>
    /// <remarks>
    /// <para><b>加入方式（ADR-033）：</b>本 pass <b>不</b>写入
    /// <see cref="RenderGraphTemplates"/>、不参与模板构建代码、不出现在
    /// <see cref="RenderGraphAsset"/> 参数缓存与编辑器 pass 面板中，也<b>不提供</b>
    /// 「启用开关」。它由 <see cref="CameraRenderer.Build"/> 在模板构建完成后
    /// 无条件追加到 pass 列表末尾；发布构建若未定义 <c>HNRP_DEBUG_DISPLAY</c>
    /// 则整个类不进入程序集。</para>
    /// <para><b>为何不读写上游颜色链：</b>调试资源全部由
    /// <see cref="RenderDebugManager"/> 全局持有，因此本 pass 无输入 slot、无连线，
    /// 模板结构与 <c>SlotConnection</c> 完全不受影响（不破坏 ADR-018）。
    /// 颜色目标直接取相机后备缓冲，且只写不读 —— RenderGraph 无需版本拷贝，
    /// 叠加由固定管线混合完成。</para>
    /// <para><b>每像素颜色映射：</b>不在本 pass 完成。该功能由各生产者 pass
    /// 在自己的片元着色器里直接混入颜色输出（见 <c>Lit.hlsl</c> 的
    /// <c>HN_DEBUG_PER_PIXEL</c> 分支），因此不需要额外的全屏 pass、随机写纹理
    /// 或跨 pass 依赖，透明度和几何遮蔽天然正确。</para>
    /// </remarks>
    /// <remarks>
    /// 刻意<b>不</b>加 <c>[Pass]</c> 特性：本 pass 不参与 <see cref="PassRegistry"/> 的
    /// 自动发现，因此不会出现在编辑器 pass 面板、<see cref="RenderGraphAsset"/> 参数缓存
    /// 与 Player 端生成的注册表中 ——— 它只能由 <see cref="CameraRenderer.Build"/>
    /// 强制追加（ADR-033）。
    /// </remarks>
    public sealed class DebugDisplayPass : Pass
    {
        /// <summary>用于注册与识别的常量 pass 名。</summary>
        public const string PassNameConst = "Debug Display";

        /// <summary>单值文本叠层的 shader pass 下标。</summary>
        private const int TextPassIndex = 0;

        /// <summary>2D 纹理预览的 shader pass 下标。</summary>
        private const int Preview2DPassIndex = 1;

        /// <summary>Texture2DArray 预览的 shader pass 下标。</summary>
        private const int PreviewArrayPassIndex = 2;

        /// <summary>当前帧的相机上下文。</summary>
        private CameraContext cameraContext;

        /// <summary>当前帧解析出的调试状态。</summary>
        private RenderDebugState debugState;

        /// <summary>本 pass 持有的显示材质；<see cref="Cleanup"/> 释放。</summary>
        private Material material;

        /// <summary>
        /// 初始化 <see cref="DebugDisplayPass"/> 的新实例。
        /// </summary>
        /// <remarks>
        /// 无参构造仅供 <see cref="RenderGraphAsset"/> 上参数缓存 Pass 的
        /// <c>[SerializeReference]</c> 反序列化使用。
        /// </remarks>
        public DebugDisplayPass()
            : base(string.Empty)
        {
        }

        /// <summary>
        /// 初始化 <see cref="DebugDisplayPass"/> 的新实例。
        /// </summary>
        /// <param name="passName">本 pass 的实例名。必须非 null 且在渲染图内唯一。</param>
        public DebugDisplayPass(string passName)
            : base(passName)
        {
        }

        /// <inheritdoc />
        /// <remarks>
        /// 本 pass 无输入 / 输出 slot：显示所需的全部资源经
        /// <see cref="RenderDebugManager"/> 全局绑定，详见类注释。
        /// </remarks>
        public override void SetupSlots()
        {
        }

        /// <inheritdoc />
        public override void PreRecord(RenderGraphAsset template, CameraContext context)
        {
            cameraContext = context;
            debugState = context != null ? context.DebugState : null;
        }

        /// <inheritdoc />
        public override void Record(RenderGraph renderGraph)
        {
            // ── 预检：必须在 AddRenderPass 之前完成（ADR-021）──
            // 未启用调试时不产生任何图 pass，保证「零成本」。
            if (cameraContext == null || debugState == null || !debugState.Any)
            {
                return;
            }

            Camera camera = cameraContext.Camera;
            if (camera == null)
            {
                return;
            }

            if (!RenderDebugManager.EnsureResources(cameraContext.RuntimeResources))
            {
                return;
            }

            if (!EnsureMaterial())
            {
                return;
            }

            TextureHandle backBuffer = renderGraph.ImportBackbuffer(
                new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget));

            using var builder = renderGraph.AddRenderPass<DebugDisplayPassData>(
                PassName, out var passData);

            builder.AllowPassCulling(false);

            passData.backBuffer = builder.UseColorBuffer(backBuffer, 0);
            passData.width = Mathf.Max(1, camera.pixelWidth);
            passData.height = Mathf.Max(1, camera.pixelHeight);
            passData.textActive = debugState.SingleValuesActive;
            passData.previewActive = debugState.TexturePreviewActive && debugState.PreviewTexture != null;
            passData.previewIsArray = passData.previewActive
                && debugState.PreviewTexture.dimension == TextureDimension.Tex2DArray;
            passData.material = material;

            builder.SetRenderFunc(
                (DebugDisplayPassData data, RenderGraphContext ctx) =>
                {
                    if (!IsEnabled || data.material == null)
                    {
                        return;
                    }

                    if (data.textActive)
                    {
                        RenderDebugManager.BindTextGlobals(ctx.cmd, debugState, data.width, data.height);
                        ctx.cmd.DrawProcedural(
                            Matrix4x4.identity,
                            data.material,
                            TextPassIndex,
                            MeshTopology.Triangles,
                            3,
                            1);
                    }

                    if (data.previewActive)
                    {
                        RenderDebugManager.BindPreviewGlobals(ctx.cmd, debugState, data.width, data.height);
                        ctx.cmd.DrawProcedural(
                            Matrix4x4.identity,
                            data.material,
                            data.previewIsArray ? PreviewArrayPassIndex : Preview2DPassIndex,
                            MeshTopology.Triangles,
                            3,
                            1);
                    }
                });
        }

        /// <inheritdoc />
        public override void Cleanup()
        {
            CoreUtils.Destroy(material);
            material = null;
            cameraContext = null;
            debugState = null;
        }

        /// <summary>
        /// 确保显示材质已按当前管线资源创建。
        /// </summary>
        /// <returns>材质可用时返回 <c>true</c>。</returns>
        private bool EnsureMaterial()
        {
            Shader shader = cameraContext?.RuntimeResources != null
                && cameraContext.RuntimeResources.shaderResources != null
                ? cameraContext.RuntimeResources.shaderResources.DebugDisplay
                : null;

            if (shader == null)
            {
                return false;
            }

            if (material != null && material.shader == shader)
            {
                return true;
            }

            CoreUtils.Destroy(material);

            // NOTE: 用内置 Unlit 变体名创建不受管线影响的独立材质实例，
            // 参数全部经全局 uniform 下发，材质本身不承载状态。
            material = new Material(shader)
            {
                name = "HNRPDebugDisplay",
                hideFlags = HideFlags.HideAndDontSave,
            };

            return material != null;
        }

        /// <summary>
        /// <see cref="DebugDisplayPass"/> 的渲染图 pass 数据容器。
        /// </summary>
        private sealed class DebugDisplayPassData
        {
            /// <summary>相机后备缓冲句柄（仅作颜色目标，从不读取）。</summary>
            public TextureHandle backBuffer;

            /// <summary>目标宽度（像素）。</summary>
            public int width;

            /// <summary>目标高度（像素）。</summary>
            public int height;

            /// <summary>是否绘制单值文本叠层。</summary>
            public bool textActive;

            /// <summary>是否绘制纹理预览。</summary>
            public bool previewActive;

            /// <summary>预览源是否为 Texture2DArray。</summary>
            public bool previewIsArray;

            /// <summary>显示材质。</summary>
            public Material material;
        }
    }
}

#endif // UNITY_EDITOR || HNRP_DEBUG_DISPLAY
