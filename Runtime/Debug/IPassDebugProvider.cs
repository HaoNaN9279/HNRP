// <copyright file="IPassDebugProvider.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace HN.HNRP
{
    /// <summary>
    /// 由「可被调试查看中间值」的 pass 实现的契约。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 沿用 <see cref="IGlobalShaderResource"/> / <c>PassEditorRegistry</c> 的
    /// 「pass 自描述能力 + 宿主统一触发」哲学：新增一个可调试 pass 只需实现本接口，
    /// 不需要修改管线、<see cref="CameraRenderer"/> 或显示 pass。
    /// </para>
    /// <para>
    /// <b>时序：</b><see cref="CameraRenderer.Render"/> 在建立 pass 列表后遍历全部启用
    /// pass，对实现本接口者调用一次 <see cref="ApplyDebug"/>；pass 随后在自己的
    /// render func 内把选择结果反映到绘制命令（开/关 keyword、写 uniform）。
    /// 之所以不在宿主侧直接下命令：渲染命令必须记录在与绘制同一个命令缓冲上，
    /// 只有 pass 自己知道那是不是 RenderGraph 的 <c>ctx.cmd</c>。
    /// </para>
    /// </remarks>
    public interface IPassDebugProvider
    {
        /// <summary>
        /// 本 pass 暴露的全部调试通道。
        /// </summary>
        IReadOnlyList<DebugChannelDescriptor> DebugChannels { get; }

        /// <summary>
        /// 接收本帧的调试选择。实现只应记录选择供后续 render func 使用，
        /// 不得在此记录渲染命令，也不得修改 <see cref="Pass.IsEnabled"/>。
        /// </summary>
        /// <param name="selection">本帧选中的通道；未选中时 <c>Active</c> 为 <c>false</c>。</param>
        void ApplyDebug(in PassDebugSelection selection);
    }
}
