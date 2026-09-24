using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// 包数据库：扫描全部 <see cref="HNRPPackageDefinition"/> 资产，读取各自唯一 hlsl 并解析声明，
    /// 提供按 Id / 基底的查询。带短时缓存（避免面板重绘频繁扫描）。
    /// </summary>
    public static class HNRPPackageDatabase
    {
        private const double CacheSeconds = 1.0;

        private static List<HNRPPackageEntry> cache;
        private static Dictionary<string, HNRPPackageEntry> cacheIndex;
        private static double cacheTime;

        /// <summary>全部包条目（按资产路径排序）。</summary>
        /// <returns>包条目列表。</returns>
        public static List<HNRPPackageEntry> All()
        {
            EnsureCache();
            return cache;
        }

        /// <summary>按 Id（显示名）索引的包条目。</summary>
        /// <returns>索引字典。</returns>
        public static Dictionary<string, HNRPPackageEntry> BuildIndex()
        {
            EnsureCache();
            return cacheIndex;
        }

        /// <summary>按 Id 查询。</summary>
        /// <param name="id">包 Id（显示名）。</param>
        /// <param name="entry">输出条目。</param>
        /// <returns>是否找到。</returns>
        public static bool TryGet(string id, out HNRPPackageEntry entry)
        {
            EnsureCache();
            return cacheIndex.TryGetValue(id, out entry);
        }

        /// <summary>取支持某基底的全部包。</summary>
        /// <param name="substrate">基底 id。</param>
        /// <returns>包条目列表。</returns>
        public static List<HNRPPackageEntry> GetForSubstrate(HNRPSubstrateId substrate)
        {
            var result = new List<HNRPPackageEntry>();
            foreach (HNRPPackageEntry entry in All())
            {
                if (entry.Definition != null && entry.Definition.Supports(substrate))
                {
                    result.Add(entry);
                }
            }

            return result;
        }

        /// <summary>
        /// 立即解析某包定义的 hlsl（不走缓存），供定义面板做实时速览。
        /// </summary>
        /// <param name="definition">包定义。</param>
        /// <returns>解析内容。</returns>
        public static HNRPPackageContent ParseDefinition(HNRPPackageDefinition definition)
        {
            if (definition == null || string.IsNullOrEmpty(definition.HlslPath))
            {
                return new HNRPPackageContent();
            }

            string fullPath = Path.GetFullPath(definition.HlslPath);
            if (!File.Exists(fullPath))
            {
                var empty = new HNRPPackageContent();
                empty.Diagnostics.Add("hlsl 文件不存在：" + definition.HlslPath);
                return empty;
            }

            return HNRPPackageParser.Parse(File.ReadAllText(fullPath));
        }

        private static void EnsureCache()
        {
            double now = EditorApplication.timeSinceStartup;
            if (cache != null && now - cacheTime < CacheSeconds)
            {
                return;
            }

            var entries = new List<HNRPPackageEntry>();
            var index = new Dictionary<string, HNRPPackageEntry>(StringComparer.Ordinal);

            string[] guids = AssetDatabase.FindAssets("t:HNRPPackageDefinition");
            var paths = new List<string>(guids.Length);
            foreach (string guid in guids)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            paths.Sort(StringComparer.Ordinal);

            foreach (string path in paths)
            {
                var definition = AssetDatabase.LoadAssetAtPath<HNRPPackageDefinition>(path);
                if (definition == null)
                {
                    continue;
                }

                HNRPPackageContent content = ParseDefinition(definition);
                var entry = new HNRPPackageEntry
                {
                    Definition = definition,
                    Content = content,
                };

                entries.Add(entry);

                if (string.IsNullOrEmpty(definition.DisplayName))
                {
                    Debug.LogWarning($"[HNRPPackageDatabase] 包资产 '{path}' 未设置 DisplayName（即包 Id）；已跳过注册。");
                    continue;
                }

                if (index.ContainsKey(definition.DisplayName))
                {
                    Debug.LogWarning(
                        $"[HNRPPackageDatabase] 包 Id '{definition.DisplayName}' 重复：'{path}' 被忽略。");
                    continue;
                }

                index.Add(definition.DisplayName, entry);
            }

            cache = entries;
            cacheIndex = index;
            cacheTime = now;
        }
    }
}
