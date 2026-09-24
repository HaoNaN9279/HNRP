namespace HN.HNRP
{
    /// <summary>
    /// 包的局部关键字描述。由 hlsl 声明区（<c>HNRP_PACKAGE_KEYWORD</c>）解析得到。
    /// 关键字全名带包前缀 <c>_&lt;SafeId&gt;_</c>，避免跨包冲突。
    /// </summary>
    public sealed class HNRPPackageKeyword
    {
        /// <summary>关键字短名（不含包前缀）。</summary>
        public string Name = "FEATURE";

        /// <summary>用途说明。</summary>
        public string Doc = "";

        /// <summary>拼接完整局部关键字名：<c>_&lt;safeId&gt;_&lt;Name&gt;</c>。</summary>
        /// <param name="safeId">包的代码安全 id。</param>
        /// <returns>完整关键字名。</returns>
        public string GetKeywordName(string safeId)
        {
            return "_" + safeId + "_" + Name;
        }
    }
}
