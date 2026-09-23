// <copyright file="PassDependency.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using UnityEngine;

namespace HN.HNRP
{
    /// <summary>
    /// 描述两个 pass 之间显式执行先后关系的可序列化数据类。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 显式依赖边只表达两件事：
    /// <list type="bullet">
    ///   <item><see cref="SourcePass"/> 必须先于 <see cref="TargetPass"/> 执行；</item>
    ///   <item><see cref="TargetPass"/> 可发现 <see cref="SourcePass"/> 作为其全局
    ///     shader 资源生产者（<see cref="IGlobalShaderResource"/>）或契约提供者。</item>
    /// </list>
    /// </para>
    /// <para>
    /// 依赖边<b>不携带任何资源句柄</b>。资源内容传递只走两条路：RenderGraph 托管的
    /// 内容 slot（color / depth 链），或全局 shader 绑定（生产者
    /// <see cref="IGlobalShaderResource"/>）。这样 slot 的语义保持单一（内容），
    /// 依赖不再伪装成资源读取。
    /// </para>
    /// </remarks>
    [Serializable]
    public class PassDependency
    {
        [SerializeField]
        private string sourcePass;

        [SerializeField]
        private string targetPass;

        [SerializeField]
        private string reason;

        /// <summary>
        /// 源 pass 的实例名（依赖的上游）。不能为 <c>null</c> 或空。
        /// </summary>
        public string SourcePass
        {
            get => sourcePass;
            set => sourcePass = value;
        }

        /// <summary>
        /// 目标 pass 的实例名（依赖的下游）。不能为 <c>null</c> 或空。
        /// </summary>
        public string TargetPass
        {
            get => targetPass;
            set => targetPass = value;
        }

        /// <summary>
        /// 依赖语义的可选说明（如 <c>"LightDatas"</c> / <c>"ShadowMap"</c>），
        /// 仅供调试与编辑器展示，不参与构建逻辑。
        /// </summary>
        public string Reason
        {
            get => reason;
            set => reason = value;
        }

        /// <summary>
        /// 校验源与目标 pass 名均非 null 且非空。
        /// </summary>
        /// <returns>名字字段有效时返回 <c>true</c>；否则返回 <c>false</c>。</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(sourcePass)
                && !string.IsNullOrEmpty(targetPass);
        }

        /// <summary>
        /// 创建一条显式依赖边。
        /// </summary>
        /// <param name="sourcePass">依赖的上游 pass 实例名。</param>
        /// <param name="targetPass">依赖的下游 pass 实例名。</param>
        /// <param name="reason">可选的依赖语义说明。</param>
        /// <returns>新的 <see cref="PassDependency"/> 实例。</returns>
        public static PassDependency Create(
            string sourcePass,
            string targetPass,
            string reason = null)
        {
            return new PassDependency
            {
                SourcePass = sourcePass,
                TargetPass = targetPass,
                Reason = reason,
            };
        }
    }
}
