using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HN.HNRP
{
    /// <summary>
    /// ShaderLibrary「包（Package）」的声明与注册资产。
    /// </summary>
    /// <remarks>
    /// <para>
    /// SO 承担三件事：<b>声明</b>（支持哪些基底、在各基底的哪些锚点插入、需要哪些顶点属性）、
    /// <b>注册</b>（指向该包唯一的 hlsl 文件）、以及编辑器工具（一键创建 hlsl、同步模板、功能速览）。
    /// </para>
    /// <para>
    /// 参数 / varyings / keyword 的<b>声明不在此资产内</b>：它们写在 hlsl 的声明区（见
    /// <c>Core/PackageAnchors.hlsl</c> 中的 <c>HNRP_PACKAGE_*</c> 宏），编辑器运行时解析并速览。
    /// </para>
    /// <para>
    /// <see cref="DisplayName"/> 即包 Id，也是 hlsl 文件名（去扩展名）。不再有独立的 packageId。
    /// </para>
    /// </remarks>
    [CreateAssetMenu(fileName = "HNRPPackage", menuName = "HNRP/ShaderLibrary/Package Definition")]
    public sealed class HNRPPackageDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "";
        [SerializeField] private List<HNRPSubstrateId> supportedSubstrates = new List<HNRPSubstrateId>();
        [SerializeField] private string hlslPath = "";
        [SerializeField] private List<HNRPPackageInsertion> insertions = new List<HNRPPackageInsertion>();
        [SerializeField] private List<string> requiredAttributes = new List<string>();

        /// <summary>包显示名，同时作为包 Id 与 hlsl 文件名。</summary>
        public string DisplayName
        {
            get => displayName;
            set => displayName = value;
        }

        /// <summary>该包支持的基底列表。</summary>
        public List<HNRPSubstrateId> SupportedSubstrates => supportedSubstrates;

        /// <summary>该包唯一的 hlsl 实现文件的项目相对路径（可空）。</summary>
        public string HlslPath
        {
            get => hlslPath;
            set => hlslPath = value;
        }

        /// <summary>插入点列表（每个含基底 + pass + 阶段 + 位置）。</summary>
        public List<HNRPPackageInsertion> Insertions => insertions;

        /// <summary>需要向顶点输入请求的顶点属性（<c>uv0</c>/<c>tangent</c>/<c>color</c> 等）。</summary>
        public List<string> RequiredAttributes => requiredAttributes;

        /// <summary>包的代码安全 id（sanitize 后的 <see cref="DisplayName"/>）。</summary>
        public string SafeId => SanitizeIdentifier(displayName);

        /// <summary>用于文件名 / 约定类名的 PascalCase 名。</summary>
        public string FileStem => SanitizePascalName(displayName);

        /// <summary>是否支持某基底。</summary>
        /// <param name="substrate">基底 id。</param>
        /// <returns>是否支持。</returns>
        public bool Supports(HNRPSubstrateId substrate)
        {
            return supportedSubstrates.Contains(substrate);
        }

        /// <summary>取某基底的全部插入点。</summary>
        /// <param name="substrate">基底 id。</param>
        /// <returns>插入点列表。</returns>
        public List<HNRPPackageInsertion> GetInsertions(HNRPSubstrateId substrate)
        {
            var result = new List<HNRPPackageInsertion>();
            foreach (HNRPPackageInsertion insertion in insertions)
            {
                if (insertion.Substrate == substrate)
                {
                    result.Add(insertion);
                }
            }

            return result;
        }

        /// <summary>
        /// 把任意字符串 sanitize 为合法 HLSL/C# 标识符片段：
        /// 非字母数字替换为 <c>_</c>，首字符非字母 / 下划线时前置 <c>P</c>。
        /// </summary>
        /// <param name="raw">原始字符串。</param>
        /// <returns>合法标识符。</returns>
        public static string SanitizeIdentifier(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return "Package";
            }

            var sb = new StringBuilder(raw.Length);
            foreach (char c in raw)
            {
                sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            }

            string result = sb.ToString();
            char first = result[0];
            if (!char.IsLetter(first) && first != '_')
            {
                result = "P" + result;
            }

            return result;
        }

        /// <summary>
        /// 把任意字符串 sanitize 为 PascalCase 文件名片段（仅保留字母数字，
        /// 以非字母数字为分词边界，每段首字母大写）。
        /// </summary>
        /// <param name="raw">原始字符串。</param>
        /// <returns>PascalCase 文件名片段。</returns>
        public static string SanitizePascalName(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return "Package";
            }

            var sb = new StringBuilder(raw.Length);
            bool atWordStart = true;
            foreach (char c in raw)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(atWordStart ? char.ToUpperInvariant(c) : c);
                    atWordStart = false;
                }
                else
                {
                    atWordStart = true;
                }
            }

            return sb.Length == 0 ? "Package" : sb.ToString();
        }
    }
}
