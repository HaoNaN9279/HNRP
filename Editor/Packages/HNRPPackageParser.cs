using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// 解析包 hlsl 声明区（<c>HNRP_PACKAGE_BEGIN</c> .. <c>HNRP_PACKAGE_END</c>）中的
    /// <c>HNRP_PACKAGE_*</c> 宏，产出参数 / varyings / keywords 与诊断。
    /// </summary>
    /// <remarks>
    /// 约定：一行一条宏、参数区在 <c>BEGIN</c>/<c>END</c> 之间、固定元数（HLSL 预处理器不支持
    /// 变参宏）。解析容错但会记录诊断；不抛异常。
    /// </remarks>
    public static class HNRPPackageParser
    {
        private const string BeginMarker = "HNRP_PACKAGE_BEGIN";
        private const string EndMarker = "HNRP_PACKAGE_END";

        private static readonly Regex LinePattern = new Regex(
            @"^\s*(HNRP_PACKAGE_[A-Z_]+)\s*\((.*)\)\s*;?\s*$",
            RegexOptions.Compiled);

        /// <summary>解析 hlsl 文本。</summary>
        /// <param name="hlslText">hlsl 全文。</param>
        /// <returns>解析内容（含诊断）。</returns>
        public static HNRPPackageContent Parse(string hlslText)
        {
            var content = new HNRPPackageContent();
            if (string.IsNullOrEmpty(hlslText))
            {
                return content;
            }

            string region = ExtractRegion(hlslText);
            var tooltips = new Dictionary<string, string>(StringComparer.Ordinal);
            var parametersByName = new Dictionary<string, HNRPPackageParameter>(StringComparer.Ordinal);

            string[] lines = region.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                Match match = LinePattern.Match(lines[i]);
                if (!match.Success)
                {
                    continue;
                }

                string macro = match.Groups[1].Value;
                List<string> args = SplitArguments(match.Groups[2].Value);

                switch (macro)
                {
                    case "HNRP_PACKAGE_FLOAT":
                        AddParameter(content, parametersByName, args, i, HNRPPackageParameterType.Float);
                        break;
                    case "HNRP_PACKAGE_RANGE":
                        AddParameter(content, parametersByName, args, i, HNRPPackageParameterType.Range);
                        break;
                    case "HNRP_PACKAGE_INT":
                        AddParameter(content, parametersByName, args, i, HNRPPackageParameterType.Int);
                        break;
                    case "HNRP_PACKAGE_TOGGLE":
                        AddParameter(content, parametersByName, args, i, HNRPPackageParameterType.Toggle);
                        break;
                    case "HNRP_PACKAGE_TOGGLE_KEYWORD":
                        AddParameter(content, parametersByName, args, i, HNRPPackageParameterType.Toggle);
                        break;
                    case "HNRP_PACKAGE_COLOR":
                        AddParameter(content, parametersByName, args, i, HNRPPackageParameterType.Color);
                        break;
                    case "HNRP_PACKAGE_VECTOR":
                        AddParameter(content, parametersByName, args, i, HNRPPackageParameterType.Vector);
                        break;
                    case "HNRP_PACKAGE_TEXTURE":
                        AddParameter(content, parametersByName, args, i, HNRPPackageParameterType.Texture2D);
                        break;
                    case "HNRP_PACKAGE_CUBE":
                        AddParameter(content, parametersByName, args, i, HNRPPackageParameterType.Cube);
                        break;
                    case "HNRP_PACKAGE_ENUM":
                        AddParameter(content, parametersByName, args, i, HNRPPackageParameterType.Enum);
                        break;
                    case "HNRP_PACKAGE_TOOLTIP":
                        if (args.Count >= 2)
                        {
                            tooltips[args[0]] = args[1];
                        }
                        else
                        {
                            content.Diagnostics.Add($"第 {i + 1} 行：HNRP_PACKAGE_TOOLTIP 需要 2 个参数。");
                        }

                        break;
                    case "HNRP_PACKAGE_VARYING":
                        AddVarying(content, args, i);
                        break;
                    case "HNRP_PACKAGE_KEYWORD":
                        AddKeyword(content, args, i);
                        break;
                    default:
                        content.Diagnostics.Add($"第 {i + 1} 行：未知宏 {macro}。");
                        break;
                }
            }

            foreach (KeyValuePair<string, string> pair in tooltips)
            {
                if (parametersByName.TryGetValue(pair.Key, out HNRPPackageParameter parameter))
                {
                    parameter.Tooltip = pair.Value;
                }
                else
                {
                    content.Diagnostics.Add($"HNRP_PACKAGE_TOOLTIP 引用了未声明的参数 '{pair.Key}'。");
                }
            }

            return content;
        }

        private static void AddParameter(
            HNRPPackageContent content,
            Dictionary<string, HNRPPackageParameter> byName,
            List<string> args,
            int lineIndex,
            HNRPPackageParameterType type)
        {
            int expected = ExpectedParameterArity(type);
            if (args.Count < expected)
            {
                content.Diagnostics.Add($"第 {lineIndex + 1} 行：{type} 参数需要 {expected} 个参数，实际 {args.Count} 个。");
                return;
            }

            var parameter = new HNRPPackageParameter
            {
                Name = args[0],
                Type = type,
            };

            switch (type)
            {
                case HNRPPackageParameterType.Float:
                    parameter.DisplayName = args[1];
                    parameter.DefaultFloat = ParseFloat(args[2], 0f);
                    break;

                case HNRPPackageParameterType.Range:
                    parameter.DisplayName = args[1];
                    parameter.Min = ParseFloat(args[2], 0f);
                    parameter.Max = ParseFloat(args[3], 1f);
                    parameter.DefaultFloat = ParseFloat(args[4], parameter.Min);
                    break;

                case HNRPPackageParameterType.Int:
                    parameter.DisplayName = args[1];
                    parameter.DefaultInt = ParseInt(args[2], 0);
                    break;

                case HNRPPackageParameterType.Toggle:
                    parameter.DisplayName = args[1];
                    parameter.DefaultFloat = ParseFloat(args[2], 0f);
                    if (args.Count >= 4)
                    {
                        parameter.Keyword = args[3];
                    }

                    break;

                case HNRPPackageParameterType.Color:
                    parameter.DisplayName = args[1];
                    parameter.DefaultColor = new Color(
                        ParseFloat(args[2], 1f),
                        ParseFloat(args[3], 1f),
                        ParseFloat(args[4], 1f),
                        ParseFloat(args[5], 1f));
                    break;

                case HNRPPackageParameterType.Vector:
                    parameter.DisplayName = args[1];
                    parameter.DefaultVector = new Vector4(
                        ParseFloat(args[2], 0f),
                        ParseFloat(args[3], 0f),
                        ParseFloat(args[4], 0f),
                        ParseFloat(args[5], 0f));
                    break;

                case HNRPPackageParameterType.Texture2D:
                case HNRPPackageParameterType.Cube:
                    parameter.DisplayName = args[1];
                    break;

                case HNRPPackageParameterType.Enum:
                    parameter.DisplayName = args[1];
                    parameter.DefaultFloat = ParseFloat(args[2], 0f);
                    if (args.Count >= 4 && !string.IsNullOrEmpty(args[3]))
                    {
                        parameter.EnumLabels = args[3].Split(';');
                    }

                    break;
            }

            if (string.IsNullOrEmpty(parameter.Name))
            {
                content.Diagnostics.Add($"第 {lineIndex + 1} 行：参数名为空。");
                return;
            }

            if (byName.ContainsKey(parameter.Name))
            {
                content.Diagnostics.Add($"参数重名：{parameter.Name}。");
                return;
            }

            byName.Add(parameter.Name, parameter);
            content.Parameters.Add(parameter);
        }

        private static void AddVarying(HNRPPackageContent content, List<string> args, int lineIndex)
        {
            if (args.Count < 3)
            {
                content.Diagnostics.Add($"第 {lineIndex + 1} 行：HNRP_PACKAGE_VARYING 需要 3 个参数。");
                return;
            }

            string type = args[0];
            int size = 1;
            switch (type)
            {
                case "float": size = 1; break;
                case "float2": size = 2; break;
                case "float3": size = 3; break;
                case "float4": size = 4; break;
                default:
                    content.Diagnostics.Add($"第 {lineIndex + 1} 行：varying 类型 '{type}' 非法（应为 float/float2/float3/float4）。");
                    return;
            }

            content.Varyings.Add(new HNRPPackageVarying
            {
                Name = args[1],
                Size = size,
                Doc = args[2],
            });
        }

        private static void AddKeyword(HNRPPackageContent content, List<string> args, int lineIndex)
        {
            if (args.Count < 1)
            {
                content.Diagnostics.Add($"第 {lineIndex + 1} 行：HNRP_PACKAGE_KEYWORD 至少需要 1 个参数。");
                return;
            }

            content.Keywords.Add(new HNRPPackageKeyword
            {
                Name = args[0],
                Doc = args.Count >= 2 ? args[1] : string.Empty,
            });
        }

        private static int ExpectedParameterArity(HNRPPackageParameterType type)
        {
            switch (type)
            {
                case HNRPPackageParameterType.Color:
                case HNRPPackageParameterType.Vector:
                    return 6;
                case HNRPPackageParameterType.Range:
                    return 5;
                case HNRPPackageParameterType.Toggle:
                    return 3;
                case HNRPPackageParameterType.Texture2D:
                case HNRPPackageParameterType.Cube:
                    return 2;
                case HNRPPackageParameterType.Enum:
                    return 4;
                default:
                    return 3;
            }
        }

        private static string ExtractRegion(string text)
        {
            int begin = text.IndexOf(BeginMarker, StringComparison.Ordinal);
            int end = begin >= 0 ? text.IndexOf(EndMarker, begin + BeginMarker.Length, StringComparison.Ordinal) : -1;
            if (begin >= 0 && end > begin)
            {
                return text.Substring(begin, end - begin);
            }

            return text;
        }

        private static List<string> SplitArguments(string body)
        {
            var result = new List<string>();
            if (body == null)
            {
                return result;
            }

            int depth = 0;
            bool inQuotes = false;
            int start = 0;

            for (int i = 0; i < body.Length; i++)
            {
                char c = body[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (!inQuotes && c == '(')
                {
                    depth++;
                }
                else if (!inQuotes && c == ')')
                {
                    depth--;
                }
                else if (!inQuotes && depth == 0 && c == ',')
                {
                    result.Add(Clean(body.Substring(start, i - start)));
                    start = i + 1;
                }
            }

            if (start <= body.Length)
            {
                string tail = body.Substring(start);
                if (tail.Trim().Length > 0 || result.Count > 0)
                {
                    result.Add(Clean(tail));
                }
            }

            return result;
        }

        private static string Clean(string value)
        {
            string trimmed = value.Trim();
            if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[trimmed.Length - 1] == '"')
            {
                return trimmed.Substring(1, trimmed.Length - 2);
            }

            return trimmed;
        }

        private static float ParseFloat(string value, float fallback)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result)
                ? result
                : fallback;
        }

        private static int ParseInt(string value, int fallback)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            {
                return result;
            }

            return Mathf.RoundToInt(ParseFloat(value, fallback));
        }
    }
}
