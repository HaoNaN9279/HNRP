namespace HN.HNRP
{
    /// <summary>
    /// ShaderLibrary「基底（Substrate）」标识。包只归属一个基底；
    /// 组合（基底 + 有序包列表）确定生成 shader 的签名。
    /// </summary>
    public enum HNRPSubstrateId
    {
        /// <summary>Lit 基底（Forward / ShadowCaster / DepthOnly / DepthNormal）。</summary>
        Lit = 0,

        /// <summary>Unlit 基底（Forward / ShadowCaster / DepthOnly / DepthNormal）。</summary>
        Unlit = 1,

        /// <summary>Cel 基底（预留）。</summary>
        Cel = 2,
    }
}
