// <copyright file="PassDebugSelection.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

namespace HN.HNRP
{
    /// <summary>
    /// 本帧在某 pass 上选中的调试通道。
    /// </summary>
    /// <remarks>
    /// 由 <see cref="CameraRenderer"/> 在 PreRecord 之后统一推导并下发，
    /// pass 只负责把选择结果体现在自己的绘制命令里（绑定 keyword / uniform），
    /// 不修改 <see cref="Pass.IsEnabled"/>（ADR-021）。
    /// </remarks>
    public readonly struct PassDebugSelection
    {
        /// <summary>本帧该 pass 是否被选中调试。</summary>
        public readonly bool Active;

        /// <summary>选中的通道 ID；未选中时为 -1。</summary>
        public readonly int ChannelId;

        /// <summary>
        /// 初始化 <see cref="PassDebugSelection"/> 的新实例。
        /// </summary>
        /// <param name="active">是否选中。</param>
        /// <param name="channelId">通道 ID。</param>
        public PassDebugSelection(bool active, int channelId)
        {
            Active = active;
            ChannelId = channelId;
        }

        /// <summary>未选中状态。</summary>
        public static PassDebugSelection None => new PassDebugSelection(false, -1);
    }
}
