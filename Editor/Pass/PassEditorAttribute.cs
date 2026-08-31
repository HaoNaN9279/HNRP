// <copyright file="PassEditorAttribute.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// 标注 <see cref="PassEditor"/> 子类所绑定的 <see cref="Pass"/> 具体类型。
    /// <see cref="PassEditorRegistry"/> 据此把 Pass 类型映射到对应的编辑器绘制代码。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class PassEditorAttribute : Attribute
    {
        /// <summary>
        /// 获取该编辑器负责绘制的 Pass 类型。
        /// </summary>
        public Type PassType { get; }

        /// <summary>
        /// 初始化一个新的绑定属性。
        /// </summary>
        /// <param name="passType">该编辑器负责绘制的 Pass 具体类型。</param>
        public PassEditorAttribute(Type passType)
        {
            PassType = passType;
        }
    }
}
