using System.Collections.Generic;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// 从包 hlsl 声明区解析出的内容（参数 / varyings / keywords）与解析诊断。
    /// 非资产、非序列化：每次解析按需重建。
    /// </summary>
    public sealed class HNRPPackageContent
    {
        /// <summary>参数列表（顺序与声明一致）。</summary>
        public readonly List<HNRPPackageParameter> Parameters = new List<HNRPPackageParameter>();

        /// <summary>varyings 列表（顺序与声明一致）。</summary>
        public readonly List<HNRPPackageVarying> Varyings = new List<HNRPPackageVarying>();

        /// <summary>局部关键字列表。</summary>
        public readonly List<HNRPPackageKeyword> Keywords = new List<HNRPPackageKeyword>();

        /// <summary>解析诊断（未知宏 / 参数个数不符 / 非法数值等）。</summary>
        public readonly List<string> Diagnostics = new List<string>();
    }

    /// <summary>
    /// 包定义 + 解析内容的成对条目。数据库与生成器以此为单位工作。
    /// </summary>
    public sealed class HNRPPackageEntry
    {
        /// <summary>包声明资产。</summary>
        public HNRPPackageDefinition Definition;

        /// <summary>从 hlsl 解析出的内容。</summary>
        public HNRPPackageContent Content;

        /// <summary>包 Id（= 显示名）。</summary>
        public string Id => Definition != null ? Definition.DisplayName : string.Empty;

        /// <summary>代码安全 id。</summary>
        public string SafeId => Definition != null ? Definition.SafeId : "Package";
    }
}
