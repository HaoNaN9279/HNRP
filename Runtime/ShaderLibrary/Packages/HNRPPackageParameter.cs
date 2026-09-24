using UnityEngine;

namespace HN.HNRP
{
    /// <summary>
    /// 包参数的类型。决定生成的材质属性声明形式与 <c>UnityPerMaterial</c> 字段类型；
    /// 也决定包 Inspector 速览与材质面板按类型只显示相关设置项。
    /// </summary>
    public enum HNRPPackageParameterType
    {
        /// <summary>浮点标量（Float）。</summary>
        Float = 0,

        /// <summary>区间浮点（Range）。</summary>
        Range = 1,

        /// <summary>整数（Int）。</summary>
        Int = 2,

        /// <summary>开关（ToggleUI），材质中以 0 / 1 浮点存储。</summary>
        Toggle = 3,

        /// <summary>颜色（Color）。</summary>
        Color = 4,

        /// <summary>向量（Vector，float4）。</summary>
        Vector = 5,

        /// <summary>二维纹理（Texture2D）。</summary>
        Texture2D = 6,

        /// <summary>立方体贴图（Cube）。</summary>
        Cube = 7,

        /// <summary>枚举（以浮点存储，配合 <see cref="HNRPPackageParameter.EnumLabels"/> 显示）。</summary>
        Enum = 8,
    }

    /// <summary>
    /// 包参数描述。由 hlsl 声明区（<c>HNRP_PACKAGE_*</c> 宏）解析得到，不作为资产序列化。
    /// 参数名在生成时强制加前缀 <c>_&lt;SafeId&gt;_</c>。
    /// </summary>
    public sealed class HNRPPackageParameter
    {
        /// <summary>参数短名（不含包前缀）。</summary>
        public string Name = "Value";

        /// <summary>参数类型。</summary>
        public HNRPPackageParameterType Type = HNRPPackageParameterType.Float;

        /// <summary>面板显示名；为空时使用 <see cref="Name"/>。</summary>
        public string DisplayName = "";

        /// <summary>面板提示。</summary>
        public string Tooltip = "";

        /// <summary>Float / Range / Toggle / Enum 的默认值。</summary>
        public float DefaultFloat;

        /// <summary>Range 下界。</summary>
        public float Min;

        /// <summary>Range 上界。</summary>
        public float Max = 1f;

        /// <summary>Int 的默认值。</summary>
        public int DefaultInt;

        /// <summary>Color 的默认值。</summary>
        public Color DefaultColor = Color.white;

        /// <summary>Vector 的默认值。</summary>
        public Vector4 DefaultVector = Vector4.zero;

        /// <summary>Enum 的选项标签（可空）。</summary>
        public string[] EnumLabels;

        /// <summary>该参数驱动的局部关键字短名（不含包前缀，可空）。</summary>
        public string Keyword = "";

        /// <summary>拼接完整材质属性名：<c>_&lt;safeId&gt;_&lt;Name&gt;</c>。</summary>
        /// <param name="safeId">包的代码安全 id。</param>
        /// <returns>完整属性名。</returns>
        public string GetPropertyName(string safeId)
        {
            return "_" + safeId + "_" + Name;
        }

        /// <summary>拼接该参数驱动的完整局部关键字名（含 <c>_</c> 前缀）。</summary>
        /// <param name="safeId">包的代码安全 id。</param>
        /// <returns>完整关键字名；<see cref="Keyword"/> 为空时返回空串。</returns>
        public string GetKeywordName(string safeId)
        {
            return string.IsNullOrEmpty(Keyword) ? string.Empty : "_" + safeId + "_" + Keyword;
        }
    }
}
