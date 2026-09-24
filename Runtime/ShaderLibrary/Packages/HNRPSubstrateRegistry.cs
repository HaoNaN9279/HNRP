using System.Collections.Generic;

namespace HN.HNRP
{
    /// <summary>
    /// 基底的静态描述：定位 shader 模板、类型前导 include 与自定义面板类型。
    /// </summary>
    public sealed class HNRPSubstrateDescriptor
    {
        /// <summary>基底 id。</summary>
        public HNRPSubstrateId Id;

        /// <summary>基底字符串 id（用于 override tag 与生成 shader 名，如 <c>Lit</c>）。</summary>
        public string IdString;

        /// <summary>面板显示名。</summary>
        public string DisplayName;

        /// <summary>基底 shader 模板资产路径（项目相对）。</summary>
        public string ShaderTemplatePath;

        /// <summary>基底自定义 ShaderGUI 类型全名（生成 shader 沿用）。</summary>
        public string CustomEditorType;

        /// <summary>
        /// 类型前导 include（项目相对，按依赖顺序）。组合头在包含包实现之前先包含
        /// 这些头，使包函数能引用基底的上下文结构体类型。
        /// </summary>
        public string[] PreludeIncludes;
    }

    /// <summary>
    /// 基底注册表：id → 描述。当前实现 Lit；Unlit / Cel 为预留。
    /// </summary>
    public static class HNRPSubstrateRegistry
    {
        private static readonly List<HNRPSubstrateDescriptor> Descriptors = Build();
        private static readonly Dictionary<HNRPSubstrateId, HNRPSubstrateDescriptor> ById = BuildIndex();

        /// <summary>全部已注册基底。</summary>
        public static IReadOnlyList<HNRPSubstrateDescriptor> All => Descriptors;

        /// <summary>按 id 获取基底描述。</summary>
        /// <param name="id">基底 id。</param>
        /// <param name="descriptor">输出描述。</param>
        /// <returns>是否存在。</returns>
        public static bool TryGet(HNRPSubstrateId id, out HNRPSubstrateDescriptor descriptor)
        {
            return ById.TryGetValue(id, out descriptor);
        }

        /// <summary>按 id 获取基底描述（不存在时返回 <c>null</c>）。</summary>
        /// <param name="id">基底 id。</param>
        /// <returns>基底描述。</returns>
        public static HNRPSubstrateDescriptor Get(HNRPSubstrateId id)
        {
            return ById.TryGetValue(id, out HNRPSubstrateDescriptor descriptor) ? descriptor : null;
        }

        private static List<HNRPSubstrateDescriptor> Build()
        {
            return new List<HNRPSubstrateDescriptor>
            {
                new HNRPSubstrateDescriptor
                {
                    Id = HNRPSubstrateId.Lit,
                    IdString = "Lit",
                    DisplayName = "Lit",
                    ShaderTemplatePath = HNRPProjectPaths.Substrates + "/Lit.shader",
                    CustomEditorType = "HN.HNRP.Editor.LitGUI",
                    PreludeIncludes = new[]
                    {
                        HNRPProjectPaths.ShaderLibrary + "/Lit/LitInput.hlsl",
                        HNRPProjectPaths.ShaderLibrary + "/Lit/LitVaryingsDefine.hlsl",
                        HNRPProjectPaths.ShaderLibrary + "/Lit/LitVaryings.hlsl",
                        HNRPProjectPaths.ShaderLibrary + "/Lit/LitSurfaceData.hlsl",
                        HNRPProjectPaths.ShaderLibrary + "/Lit/LitLighting.hlsl",
                    },
                },
            };
        }

        private static Dictionary<HNRPSubstrateId, HNRPSubstrateDescriptor> BuildIndex()
        {
            var map = new Dictionary<HNRPSubstrateId, HNRPSubstrateDescriptor>();
            foreach (HNRPSubstrateDescriptor descriptor in Descriptors)
            {
                map[descriptor.Id] = descriptor;
            }

            return map;
        }
    }
}
