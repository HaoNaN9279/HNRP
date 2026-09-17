// <copyright file="DebugChannelDescriptor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

namespace HN.HNRP
{
    /// <summary>
    /// 对 pass 暴露的一条调试通道的自描述信息（用于菜单生成 / API 校验）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 除标识与显示名外，还携带该通道的<b>默认颜色映射预设</b>：不同通道的取值域差别很大
    /// （平滑度是 [0,1]、世界坐标是米级、群集计数是 0..N），用同一套 [0,1]/灰阶参数去看
    /// 只会得到整屏纯色。选中通道时应用其预设，即可直接看到有意义的热力图。
    /// </para>
    /// <para>
    /// 刻意使用 <c>int</c> 而非枚举传递通道 ID：项目已知 Mono 在「枚举作为
    /// 泛型类型实参」时会触发 native crash。
    /// </para>
    /// </remarks>
    public readonly struct DebugChannelDescriptor
    {
        /// <summary>通道 ID（传给 shader 的 <c>_HNRPDebugChannelId</c>）。</summary>
        public readonly int ChannelId;

        /// <summary>显示名（如 "Albedo"）。</summary>
        public readonly string Name;

        /// <summary>取值粒度。</summary>
        public readonly DebugValueKind Kind;

        /// <summary>该通道的默认颜色映射预设。</summary>
        public readonly DebugColorMapSettings DefaultColorMap;

        /// <summary>
        /// 初始化 <see cref="DebugChannelDescriptor"/> 的新实例（沿用默认颜色映射）。
        /// </summary>
        /// <param name="channelId">通道 ID。</param>
        /// <param name="name">显示名。</param>
        /// <param name="kind">取值粒度。</param>
        public DebugChannelDescriptor(int channelId, string name, DebugValueKind kind)
            : this(channelId, name, kind, DebugColorMapSettings.Default)
        {
        }

        /// <summary>
        /// 初始化 <see cref="DebugChannelDescriptor"/> 的新实例。
        /// </summary>
        /// <param name="channelId">通道 ID。</param>
        /// <param name="name">显示名。</param>
        /// <param name="kind">取值粒度。</param>
        /// <param name="defaultColorMap">默认颜色映射预设。</param>
        public DebugChannelDescriptor(
            int channelId,
            string name,
            DebugValueKind kind,
            DebugColorMapSettings defaultColorMap)
        {
            ChannelId = channelId;
            Name = name;
            Kind = kind;
            DefaultColorMap = defaultColorMap;
        }
    }
}
