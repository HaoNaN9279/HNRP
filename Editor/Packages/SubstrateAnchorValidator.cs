using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// 基底锚点一致性校验：比对 C# 唯一真源 <see cref="HNRPSubstrateAnchors"/> 与
    /// hlsl 默认空宏文件 <c>Core/PackageAnchors.hlsl</c>，防止两端签名漂移（方案 D5）。
    /// </summary>
    public static class SubstrateAnchorValidator
    {
        private static readonly Regex IfndefPattern =
            new Regex(@"#ifndef\s+(HNRP_PKG_EMIT_[A-Z0-9_]+)", RegexOptions.Compiled);

        private static readonly Regex DefinePattern =
            new Regex(@"#define\s+(HNRP_PKG_EMIT_[A-Z0-9_]+)\(([^)]*)\)", RegexOptions.Compiled);

        /// <summary>
        /// 执行校验。
        /// </summary>
        /// <param name="messages">输出问题列表；为空表示通过。</param>
        /// <returns>是否通过。</returns>
        public static bool Validate(out List<string> messages)
        {
            messages = new List<string>();

            string path = HNRPProjectPaths.ShaderLibrary + "/Core/PackageAnchors.hlsl";
            string fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
            {
                messages.Add("找不到锚点文件：" + path);
                return false;
            }

            string content = File.ReadAllText(fullPath);

            var hlslIfndefs = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (Match match in IfndefPattern.Matches(content))
            {
                hlslIfndefs[match.Groups[1].Value] = CountParameters(match.Groups[1].Value, content);
            }

            var expected = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (HNRPSubstrateDescriptor descriptor in HNRPSubstrateRegistry.All)
            {
                foreach (HNRPAnchorDescriptor anchor in HNRPSubstrateAnchors.GetAnchors(descriptor.Id))
                {
                    expected[anchor.MacroName] = anchor.Contexts != null ? anchor.Contexts.Length : 0;
                }
            }

            foreach (KeyValuePair<string, int> pair in expected)
            {
                if (!hlslIfndefs.TryGetValue(pair.Key, out int arity))
                {
                    messages.Add("hlsl 缺少锚点默认宏：" + pair.Key);
                }
                else if (arity != pair.Value)
                {
                    messages.Add($"锚点参数个数不一致：{pair.Key}（C# {pair.Value} / hlsl {arity}）");
                }
            }

            foreach (KeyValuePair<string, int> pair in hlslIfndefs)
            {
                if (!expected.ContainsKey(pair.Key))
                {
                    messages.Add("C# 缺少锚点定义（hlsl 多出）：" + pair.Key);
                }
            }

            return messages.Count == 0;
        }

        /// <summary>菜单入口：在 Console 输出校验结果。</summary>
        [MenuItem("HNRP/Validate Substrate Anchors")]
        public static void ValidateFromMenu()
        {
            if (Validate(out List<string> messages))
            {
                UnityEngine.Debug.Log("[SubstrateAnchorValidator] 锚点校验通过。");
                return;
            }

            foreach (string message in messages)
            {
                UnityEngine.Debug.LogError("[SubstrateAnchorValidator] " + message);
            }
        }

        private static int CountParameters(string macroName, string content)
        {
            // 优先从 #define 行读取签名参数个数；若只有 #ifndef 则按 0 处理。
            foreach (Match match in DefinePattern.Matches(content))
            {
                if (!string.Equals(match.Groups[1].Value, macroName, StringComparison.Ordinal))
                {
                    continue;
                }

                string parameters = match.Groups[2].Value.Trim();
                if (parameters.Length == 0)
                {
                    return 0;
                }

                return parameters.Split(',').Length;
            }

            return 0;
        }
    }
}
