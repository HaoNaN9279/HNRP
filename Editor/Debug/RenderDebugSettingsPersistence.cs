// <copyright file="RenderDebugSettingsPersistence.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using UnityEditor;
using UnityEngine;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// SceneView 调试面板设置的持久化（<see cref="EditorPrefs"/>，跨脚本重载 / 会话保留）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 进程级的 <see cref="RenderDebugManager.Settings"/> 与面板折叠状态都是 static，
    /// 每次脚本编译（domain reload）都会回到默认值。本类在重载后
    /// （<see cref="InitializeOnLoadMethodAttribute"/>）自动回灌，并在面板改动 /
    /// 重载前 / 退出编辑器时写回 <see cref="EditorPrefs"/>。
    /// </para>
    /// <para>
    /// 刻意<b>不</b>新建 Project 资源：这些属于「本机开发者的 UI 偏好」，
    /// 与 <c>RenderGraphAssetEditor</c> 的折叠状态持久化保持同一策略。
    /// 键按项目数据路径哈希作用域，避免同一机器上的多个项目互相串状态。
    /// </para>
    /// </remarks>
    internal static class RenderDebugSettingsPersistence
    {
        /// <summary>调试配置的 EditorPrefs 键前缀。</summary>
        private const string SettingsKeyPrefix = "HNRP.Debug.Settings.";

        /// <summary>面板折叠状态的 EditorPrefs 键前缀。</summary>
        private const string SectionsKeyPrefix = "HNRP.Debug.Sections.";

        /// <summary>各段（数值 / 每像素 / 预览）的折叠状态。</summary>
        internal static readonly bool[] SectionExpanded = { false, true, false };

        /// <summary>当前项目的调试配置键。</summary>
        private static readonly string SettingsKey = SettingsKeyPrefix + ProjectSuffix();

        /// <summary>当前项目的面板折叠状态键。</summary>
        private static readonly string SectionsKey = SectionsKeyPrefix + ProjectSuffix();

        /// <summary>
        /// 域重载后把已保存的设置与折叠状态回灌到运行时。
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Restore()
        {
            RestoreSettings();
            RestoreSections();

            // 兜底：脚本重载前与退出编辑器时落盘（面板只在其可见时才会主动保存）。
            AssemblyReloadEvents.beforeAssemblyReload += Save;
            EditorApplication.quitting += Save;
        }

        /// <summary>保存全部面板状态（配置 + 折叠状态）。</summary>
        internal static void Save()
        {
            SaveSettings();
            SaveSections();
        }

        /// <summary>保存运行时调试配置。</summary>
        internal static void SaveSettings()
        {
            var blob = new RenderDebugSettingsBlob { Settings = RenderDebugManager.Settings };
            EditorPrefs.SetString(SettingsKey, JsonUtility.ToJson(blob));
        }

        /// <summary>保存面板折叠状态。</summary>
        internal static void SaveSections()
        {
            var parts = new string[SectionExpanded.Length];
            for (int i = 0; i < SectionExpanded.Length; i++)
            {
                parts[i] = SectionExpanded[i] ? "1" : "0";
            }

            EditorPrefs.SetString(SectionsKey, string.Join(",", parts));
        }

        /// <summary>
        /// 从 EditorPrefs 恢复调试配置。
        /// </summary>
        private static void RestoreSettings()
        {
            // 以文档默认值为基线覆盖：将来新增字段能从 Default 取到合理初值，
            // 而不是结构体的零值（如 bool 全 false）。
            var blob = new RenderDebugSettingsBlob { Settings = RenderDebugSettings.Default };

            string json = EditorPrefs.GetString(SettingsKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    JsonUtility.FromJsonOverwrite(json, blob);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[HNRP] 渲染调试设置反序列化失败，回退默认值：{e.Message}");
                    blob.Settings = RenderDebugSettings.Default;
                }
            }

            RenderDebugManager.Settings = blob.Settings;
        }

        /// <summary>
        /// 从 EditorPrefs 恢复面板折叠状态。
        /// </summary>
        private static void RestoreSections()
        {
            string saved = EditorPrefs.GetString(SectionsKey, string.Empty);
            if (string.IsNullOrEmpty(saved))
            {
                return;
            }

            string[] parts = saved.Split(',');
            for (int i = 0; i < SectionExpanded.Length && i < parts.Length; i++)
            {
                SectionExpanded[i] = parts[i] == "1";
            }
        }

        /// <summary>
        /// 生成按项目作用域的键后缀（数据路径哈希）。
        /// </summary>
        /// <returns>8 位十六进制字符串。</returns>
        private static string ProjectSuffix()
        {
            string dataPath = Application.dataPath;
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < dataPath.Length; i++)
                {
                    hash = (hash * 31) + dataPath[i];
                }

                return hash.ToString("X8");
            }
        }
    }

    /// <summary>
    /// <see cref="JsonUtility"/> 的序列化外壳：<see cref="RenderDebugSettings"/> 是结构体，
    /// <c>FromJsonOverwrite</c> 直接作用于值类型会因装箱而丢失修改，故用引用类型包裹。
    /// </summary>
    [Serializable]
    internal sealed class RenderDebugSettingsBlob
    {
        /// <summary>运行时调试配置。</summary>
        public RenderDebugSettings Settings;
    }
}
