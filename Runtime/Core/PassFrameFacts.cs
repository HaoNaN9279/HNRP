// <copyright file="PassFrameFacts.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;

namespace HN.HNRP
{
    /// <summary>
    /// 帧级事实契约的根标记（F 通道）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 这是「帧级事实」通道的扩展点：每个事实由<b>特征自己</b>定义一个派生接口
    /// （如 <see cref="IReflectionProbeUpdateSource"/>）并由生产者发布实例；
    /// 消费者用 <c>is</c> / <see cref="PassFrameFacts.TryGetFact{T}"/> 查询。
    /// 核心不枚举具体事实（开闭）。
    /// </para>
    /// <para>
    /// <b>与 R 通道的分工：</b>R（<see cref="IResourceTraits"/>）描述「某个资源的内容
    /// 语义」并随句柄传递；F 描述「与具体资源无关、属于整帧的事实」。
    /// 凡不能挂到某个资源上的跨 pass 信息走 F。
    /// </para>
    /// </remarks>
    public interface IFrameFact
    {
    }

    /// <summary>
    /// 帧级事实（F 通道）：本帧内由生产者（宿主管线或某个 pass）发布、
    /// 供其后拓扑顺序的 pass 消费的「本帧整体状态」。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 采用<b>混合形态</b>：真·全局、几乎每个图都用的帧状态用强类型字段；
    /// 生产者特有、图可能用不到的事实走<b>注册区</b>（<see cref="Publish"/> /
    /// <see cref="TryGetFact{T}"/>），未发布即零存储 —— 既避免容器随 pass 种类
    /// 单调膨胀，又避免为通用事实引入间接层（见架构文档 ADR-040）。
    /// </para>
    /// <para>
    /// <b>所有权与生命周期：</b>事实板由宿主 <see cref="CameraRenderer"/>（每相机）
    /// 跨帧持有，不是每帧新建 —— 注册区数组因此一次分配、长期复用，达成严格零 GC。
    /// <see cref="CameraContext"/> 每帧/每渲染单元构造时绑定该板并调用
    /// <see cref="Reset"/>（绑定即清空），保证不残留上一帧已发布事实。
    /// </para>
    /// <para>
    /// <b>顺序：</b>事实按发布顺序可见。由 pass 发布的事实在拓扑上必须先于其消费者
    /// （经 <see cref="SlotConnection"/> 保证），否则读取到的是未发布状态。
    /// </para>
    /// <para>
    /// <b>零 GC：</b>注册区仅在首次发布时分配一次小数组，跨帧复用；未发布事实零存储、
    /// 零分配。不使用字典 / 装箱 / 泛型键。
    /// </para>
    /// </remarks>
    public sealed class PassFrameFacts
    {
        /// <summary>注册区初始容量。</summary>
        private const int InitialFactCapacity = 4;

        /// <summary>
        /// 本帧相机是否为「共享渲染图的预览相机」
        /// （<see cref="UnityEngine.CameraType.Preview"/> 且名为
        /// <see cref="HNRenderPipelineUtils.PREVIEW_CAMERA_NAME"/>）。
        /// </summary>
        /// <remarks>
        /// 几乎每个图都会消费，且与具体资源无关，故为强类型字段。
        /// 消费方（如 <see cref="ForwardPass"/>）读本事实，而不是自行从相机对象推断，
        /// 避免同一判定散落在各 pass。
        /// </remarks>
        public bool IsPreviewCamera { get; set; }

        /// <summary>
        /// 已发布事实的注册区（首次发布时才分配）。
        /// </summary>
        private IFrameFact[] facts;

        /// <summary>注册区中已发布事实的数量。</summary>
        private int count;

        /// <summary>
        /// 发布一个帧级事实。同一类型可发布多个（按发布顺序返回首个匹配）。
        /// </summary>
        /// <param name="fact">要发布的事实实例；为 <c>null</c> 时忽略。</param>
        public void Publish(IFrameFact fact)
        {
            if (fact == null)
            {
                return;
            }

            if (facts == null)
            {
                facts = new IFrameFact[InitialFactCapacity];
            }
            else if (count == facts.Length)
            {
                Array.Resize(ref facts, count * 2);
            }

            facts[count++] = fact;
        }

        /// <summary>
        /// 查询已发布的帧级事实中首个类型为 <typeparamref name="T"/> 者。
        /// </summary>
        /// <typeparam name="T">事实契约类型（实现了 <see cref="IFrameFact"/> 的接口）。</typeparam>
        /// <param name="fact">匹配到的事实；未匹配时为 <c>null</c>。</param>
        /// <returns>存在匹配事实时返回 <c>true</c>。</returns>
        public bool TryGetFact<T>(out T fact)
            where T : class, IFrameFact
        {
            if (facts != null)
            {
                for (int i = 0; i < count; i++)
                {
                    if (facts[i] is T typed)
                    {
                        fact = typed;
                        return true;
                    }
                }
            }

            fact = null;
            return false;
        }

        /// <summary>
        /// 复位全部事实为「未发布」状态。
        /// </summary>
        /// <remarks>
        /// 先清引用再归零计数，避免注册区持有过期对象引用。
        /// 已分配的数组予以保留以便复用，不被回收。
        /// </remarks>
        public void Reset()
        {
            IsPreviewCamera = false;

            if (facts != null)
            {
                Array.Clear(facts, 0, count);
            }

            count = 0;
        }
    }
}
