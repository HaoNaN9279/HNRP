// <copyright file="IResourceTraits.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

namespace HN.HNRP
{
    /// <summary>
    /// 资源语义契约的根标记（R 通道）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 这是「资源语义」通道的扩展点：核心（<see cref="PassSlot"/>）只持有本类型引用，
    /// <b>永不枚举具体语义</b>。每个语义由<b>特征自己</b>定义一个派生接口
    /// （如 <see cref="IDepthPrepassTraits"/>）并提供不可变实现；生产者随句柄发布，
    /// 消费者用 <c>is</c> 查询具体契约。
    /// </para>
    /// <para>
    /// <b>扩展方式：</b>新增资源语义 = 新增一个派生接口 + 生产者实现，
    /// 不修改本文件、<see cref="PassSlot"/> 或任何中心类型（开闭）。
    /// </para>
    /// <para>
    /// <b>实现约定：</b>实现必须<b>不可变</b>（用于描述资源内容属性，如覆盖掩码，
    /// 而非每帧状态）。因此实现可在 pass 构造期创建一次、长期复用，
    /// 稳态零分配，且可安全地被多个消费者共享读取。
    /// </para>
    /// <para>
    /// <b>生命周期：</b>仅随输出句柄发布、随每帧句柄重置一并清除；消费者不得跨帧缓存。
    /// </para>
    /// </remarks>
    public interface IResourceTraits
    {
    }
}
