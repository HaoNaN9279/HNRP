// <copyright file="ConnectedProducerQuery.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace HN.HNRP
{
    /// <summary>
    /// 「上游生产者」发现工具：按消费者 pass 声明的显式依赖边
    /// （<see cref="Pass.Dependencies"/>）收集其上游 pass。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 这是「生产者自描述 + 消费者通用发现」模式的统一发现原语（ADR-023 / ADR-040）：
    /// 消费者无需硬编码任何具体生产者类型，只遍历自身显式依赖的上游 pass。
    /// 依赖边由 <see cref="RenderGraphBuilder"/> 从蓝图 <see cref="PassDependency"/>
    /// 解析并注入，不再从 slot 连接推断 —— slot 只表达资源内容。
    /// </para>
    /// <para>
    /// <b>零 GC：</b>结果写入调用方提供的可复用列表，本类不分配。
    /// 同一生产者可能经多条依赖边指向消费者（如探针的图集 / 掩码 / 数据），
    /// 收集时按引用去重，保证只出现一次。
    /// </para>
    /// </remarks>
    public static class ConnectedProducerQuery
    {
        /// <summary>
        /// 收集 <paramref name="consumer"/> 显式依赖的上游 pass 中，
        /// 类型为 <typeparamref name="T"/> 的全部生产者（按声明顺序、引用去重）。
        /// </summary>
        /// <typeparam name="T">要筛选的生产者契约类型（如 <see cref="IGlobalShaderResource"/>）。</typeparam>
        /// <param name="consumer">要查询的消费者 pass。</param>
        /// <param name="results">
        /// 接收结果的列表。调用时会被清空，随后按发现顺序填充去重后的生产者。
        /// </param>
        public static void CollectOwners<T>(Pass consumer, List<T> results)
            where T : class
        {
            if (results == null)
            {
                return;
            }

            results.Clear();

            if (consumer == null)
            {
                return;
            }

            IReadOnlyList<Pass> dependencies = consumer.Dependencies;
            for (int i = 0; i < dependencies.Count; i++)
            {
                if (dependencies[i] is T owner && !results.Contains(owner))
                {
                    results.Add(owner);
                }
            }
        }
    }
}
