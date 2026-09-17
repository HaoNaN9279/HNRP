// <copyright file="PassDebugRegistry.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Reflection;

namespace HN.HNRP
{
    /// <summary>
    /// 一条 pass 的调试能力摘要（供 Editor 菜单 / API 校验使用）。
    /// </summary>
    public readonly struct PassDebugInfo
    {
        /// <summary>pass 的注册显示名（<see cref="PassAttribute.DisplayName"/>）。</summary>
        public readonly string PassName;

        /// <summary>pass 的具体类型。</summary>
        public readonly Type PassType;

        /// <summary>该 pass 暴露的调试通道。</summary>
        public readonly IReadOnlyList<DebugChannelDescriptor> Channels;

        /// <summary>
        /// 初始化 <see cref="PassDebugInfo"/> 的新实例。
        /// </summary>
        /// <param name="passName">pass 注册显示名。</param>
        /// <param name="passType">pass 具体类型。</param>
        /// <param name="channels">暴露的通道。</param>
        public PassDebugInfo(string passName, Type passType, IReadOnlyList<DebugChannelDescriptor> channels)
        {
            PassName = passName;
            PassType = passType;
            Channels = channels;
        }
    }

    /// <summary>
    /// 收集全部 pass 的自描述调试能力。
    /// </summary>
    /// <remarks>
    /// 与 <see cref="PassRegistry"/> 同构：pass 类型来自 <see cref="PassRegistry"/>
    /// 的注册表，通道来自实例上的 <see cref="IPassDebugProvider.DebugChannels"/>。
    /// 结果按类型缓存 —— 通道是编译期固定信息，扫描一次即可。
    /// </remarks>
    public static class PassDebugRegistry
    {
        /// <summary>pass 类型 → 通道列表缓存。</summary>
        private static readonly Dictionary<Type, IReadOnlyList<DebugChannelDescriptor>> channelsByType = new();

        /// <summary>pass 类型 → 注册显示名缓存。</summary>
        private static readonly Dictionary<Type, string> nameByType = new();

        /// <summary>
        /// 取指定 pass 类型暴露的调试通道。
        /// </summary>
        /// <param name="passType">pass 具体类型。</param>
        /// <returns>通道列表；该类型不实现 <see cref="IPassDebugProvider"/> 时为空。</returns>
        public static IReadOnlyList<DebugChannelDescriptor> GetChannels(Type passType)
        {
            if (passType == null)
            {
                return Array.Empty<DebugChannelDescriptor>();
            }

            if (channelsByType.TryGetValue(passType, out IReadOnlyList<DebugChannelDescriptor> cached))
            {
                return cached;
            }

            var channels = new List<DebugChannelDescriptor>();

            if (typeof(IPassDebugProvider).IsAssignableFrom(passType) && !passType.IsAbstract)
            {
                IPassDebugProvider probe = TryCreateProvider(passType);
                if (probe != null)
                {
                    channels.AddRange(probe.DebugChannels);
                }
            }

            channelsByType[passType] = channels;
            return channels;
        }

        /// <summary>
        /// 取已构建 pass 实例暴露的调试通道。
        /// </summary>
        /// <param name="pass">pass 实例。</param>
        /// <returns>通道列表；实例未实现 <see cref="IPassDebugProvider"/> 时为空。</returns>
        public static IReadOnlyList<DebugChannelDescriptor> GetChannels(Pass pass)
        {
            return pass is IPassDebugProvider provider
                ? provider.DebugChannels
                : Array.Empty<DebugChannelDescriptor>();
        }

        /// <summary>
        /// 取指定类型的注册显示名。
        /// </summary>
        /// <param name="passType">pass 具体类型。</param>
        /// <returns>显示名；无 <see cref="PassAttribute"/> 时回退为类型名。</returns>
        public static string GetPassName(Type passType)
        {
            if (passType == null)
            {
                return null;
            }

            if (nameByType.TryGetValue(passType, out string cached))
            {
                return cached;
            }

            string name = passType.GetCustomAttribute<PassAttribute>(inherit: false) is { } attribute
                ? attribute.DisplayName
                : passType.Name;

            nameByType[passType] = name;
            return name;
        }

        /// <summary>
        /// 枚举全部「实现了 <see cref="IPassDebugProvider"/>」的已注册 pass。
        /// </summary>
        /// <returns>调试能力摘要列表。</returns>
        public static List<PassDebugInfo> EnumerateAll()
        {
            var result = new List<PassDebugInfo>();

            foreach (string passName in PassRegistry.GetAllPassNames())
            {
                Type passType = PassRegistry.GetPassType(passName);
                if (passType == null)
                {
                    continue;
                }

                IReadOnlyList<DebugChannelDescriptor> channels = GetChannels(passType);
                if (channels.Count == 0)
                {
                    continue;
                }

                result.Add(new PassDebugInfo(passName, passType, channels));
            }

            return result;
        }

        /// <summary>
        /// 在指定 pass 上按通道 ID 取显示名。
        /// </summary>
        /// <param name="pass">pass 实例。</param>
        /// <param name="channelId">通道 ID。</param>
        /// <returns>显示名；未找到时返回 <c>null</c>。</returns>
        public static string GetChannelName(Pass pass, int channelId)
        {
            IReadOnlyList<DebugChannelDescriptor> channels = GetChannels(pass);
            for (int i = 0; i < channels.Count; i++)
            {
                if (channels[i].ChannelId == channelId)
                {
                    return channels[i].Name;
                }
            }

            return null;
        }

        /// <summary>
        /// 在给定通道列表中按 ID 取通道描述。
        /// </summary>
        /// <param name="channels">通道列表。</param>
        /// <param name="channelId">通道 ID。</param>
        /// <param name="channel">取到的通道描述。</param>
        /// <returns>找到时返回 <c>true</c>。</returns>
        public static bool TryGetChannel(
            IReadOnlyList<DebugChannelDescriptor> channels,
            int channelId,
            out DebugChannelDescriptor channel)
        {
            if (channels != null)
            {
                for (int i = 0; i < channels.Count; i++)
                {
                    if (channels[i].ChannelId == channelId)
                    {
                        channel = channels[i];
                        return true;
                    }
                }
            }

            channel = default;
            return false;
        }

        /// <summary>
        /// 按 pass 注册显示名 + 通道 ID 取该通道的默认颜色映射预设。
        /// </summary>
        /// <remarks>
        /// 用于 <see cref="HNRPDebug.SetPerPixelChannel"/>：选中通道时套用其预设，
        /// 避免不同取值域的通道共用同一套 [0,1] 参数而看不到内容。
        /// </remarks>
        /// <param name="passName">
        /// pass 的注册显示名（<see cref="PassAttribute.DisplayName"/>，如 "Draw Object"）。
        /// </param>
        /// <param name="channelId">通道 ID。</param>
        /// <param name="colorMap">取到的默认颜色映射。</param>
        /// <returns>找到时返回 <c>true</c>。</returns>
        public static bool TryGetChannelDefaultColorMap(
            string passName,
            int channelId,
            out DebugColorMapSettings colorMap)
        {
            colorMap = DebugColorMapSettings.Default;

            if (string.IsNullOrEmpty(passName))
            {
                return false;
            }

            foreach (PassDebugInfo info in EnumerateAll())
            {
                if (info.PassName != passName)
                {
                    continue;
                }

                if (TryGetChannel(info.Channels, channelId, out DebugChannelDescriptor channel))
                {
                    colorMap = channel.DefaultColorMap;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 用无副作用的实例名试建一个 pass 实例以读取其通道表。
        /// </summary>
        private static IPassDebugProvider TryCreateProvider(Type passType)
        {
            try
            {
                object instance = Activator.CreateInstance(passType, "DebugChannelProbe");
                return instance as IPassDebugProvider;
            }
            catch (MissingMethodException)
            {
                return null;
            }
            catch (TargetInvocationException)
            {
                return null;
            }
            catch (MemberAccessException)
            {
                return null;
            }
        }
    }
}
