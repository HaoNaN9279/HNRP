namespace HN.HNRP
{
    /// <summary>
    /// 包 varyings 字段描述。由 hlsl 声明区（<c>HNRP_PACKAGE_VARYING</c>）解析得到。
    /// 生成器把字段注入基底 varyings 结构体并生成打包 / 解包语句；字段名强制加前缀
    /// <c>_&lt;SafeId&gt;_</c>。
    /// </summary>
    public sealed class HNRPPackageVarying
    {
        /// <summary>字段短名（不含包前缀）。</summary>
        public string Name = "Value";

        /// <summary>字段分量数（1..4），对应 float / float2 / float3 / float4。</summary>
        public int Size = 1;

        /// <summary>字段用途说明。</summary>
        public string Doc = "";

        /// <summary>拼接完整 varyings 字段名：<c>_&lt;safeId&gt;_&lt;Name&gt;</c>。</summary>
        /// <param name="safeId">包的代码安全 id。</param>
        /// <returns>完整字段名。</returns>
        public string GetFieldName(string safeId)
        {
            return "_" + safeId + "_" + Name;
        }

        /// <summary>该字段的 HLSL 类型名（依据 <see cref="Size"/>）。</summary>
        public string GetHlslType()
        {
            int size = Size < 1 ? 1 : (Size > 4 ? 4 : Size);
            return size == 1 ? "float" : "float" + size;
        }
    }
}
