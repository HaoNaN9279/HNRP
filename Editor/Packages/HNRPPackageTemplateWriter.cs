using System.Collections.Generic;
using System.Text;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// 包 hlsl 模板拼装与骨架同步。生成：
    /// <list type="bullet">
    /// <item>声明区骨架（<c>HNRP_PACKAGE_BEGIN/END</c> 与示例宏）；</item>
    /// <item>实现区骨架：按支持的基底分组、以 <c>HNRP_ACTIVE_SUBSTRATE_*</c> 守卫，
    /// 每个插入点一个函数骨架，函数头注释内列全上下文结构体的字段。</item>
    /// </list>
    /// </summary>
    public static class HNRPPackageTemplateWriter
    {
        /// <summary>依据包定义生成完整 hlsl 模板。</summary>
        /// <param name="definition">包定义。</param>
        /// <returns>hlsl 文本。</returns>
        public static string BuildFullTemplate(HNRPPackageDefinition definition)
        {
            string guard = BuildGuard(definition);
            var sb = new StringBuilder();

            sb.AppendLine("// 由 HNRPPackageTemplateWriter 生成的包实现骨架。");
            sb.AppendLine("// 声明区：参数 / varyings / keywords（写在本区间内）。");
            sb.AppendLine("// 实现区：每个支持的基底一段（Sync Template 只补齐缺失函数，不覆盖已有实现）。");
            sb.AppendLine();
            sb.Append("#ifndef ").AppendLine(guard);
            sb.Append("#define ").AppendLine(guard);
            sb.AppendLine();
            sb.AppendLine("// ---- 声明区 ----");
            sb.AppendLine("HNRP_PACKAGE_BEGIN");
            sb.AppendLine("// TODO: 在此声明 parameters / varyings / keywords。例如：");
            sb.AppendLine("// HNRP_PACKAGE_COLOR(Tint, \"Tint\", 1, 1, 1, 1)");
            sb.AppendLine("// HNRP_PACKAGE_RANGE(Strength, \"Strength\", 0, 1, 0.5)");
            sb.AppendLine("// HNRP_PACKAGE_VARYING(float2, uv, \"UV\")");
            sb.AppendLine("// HNRP_PACKAGE_KEYWORD(FEATURE, \"开关\")");
            sb.AppendLine("HNRP_PACKAGE_END");
            sb.AppendLine();

            sb.AppendLine("// ---- 实现区 ----");
            foreach (HNRPSubstrateId substrate in definition.SupportedSubstrates)
            {
                List<string> stubs = BuildStubs(definition, substrate);
                if (stubs.Count == 0)
                {
                    continue;
                }

                sb.Append("#if defined(").Append(SubstrateGuard(substrate)).AppendLine(")");
                foreach (string stub in stubs)
                {
                    sb.AppendLine(stub);
                }

                sb.AppendLine("#endif");
                sb.AppendLine();
            }

            sb.Append("#endif // ").AppendLine(guard);
            return sb.ToString();
        }

        /// <summary>取某基底下全部插入点的函数骨架（未做重复过滤）。</summary>
        /// <param name="definition">包定义。</param>
        /// <param name="substrate">基底 id。</param>
        /// <returns>函数骨架列表。</returns>
        public static List<string> BuildStubs(HNRPPackageDefinition definition, HNRPSubstrateId substrate)
        {
            var stubs = new List<string>();
            string safeId = definition.SafeId;

            foreach (HNRPPackageInsertion insertion in definition.Insertions)
            {
                if (insertion.Substrate != substrate)
                {
                    continue;
                }

                HNRPAnchorDescriptor anchor = HNRPSubstrateAnchors.Find(
                    substrate,
                    insertion.Pass,
                    insertion.Stage,
                    insertion.Position);

                if (anchor == null)
                {
                    continue;
                }

                stubs.Add(BuildFunctionStub(anchor, safeId));
            }

            return stubs;
        }

        /// <summary>生成单个插入点的函数骨架（含字段说明注释与函数签名）。</summary>
        /// <param name="anchor">锚点描述。</param>
        /// <param name="safeId">包的代码安全 id。</param>
        /// <returns>函数骨架文本。</returns>
        public static string BuildFunctionStub(HNRPAnchorDescriptor anchor, string safeId)
        {
            var sb = new StringBuilder();

            sb.Append("// 插入点：").Append(anchor.Substrate).Append(" / ").Append(anchor.Pass).Append(" / ")
                .Append(anchor.Stage).Append(" / ").Append(anchor.Position).AppendLine();
            sb.AppendLine("// 输入输出：");

            if (anchor.Contexts != null)
            {
                foreach (HNRPAnchorContext context in anchor.Contexts)
                {
                    sb.Append("//   ").Append(context.StructName).Append(' ').Append(context.ParamName)
                        .Append("  [").Append(HNRPAnchorDescriptor.GetAccessKeyword(context.Access)).Append(']').AppendLine();

                    HNRPStructInfo structInfo = HNRPSubstrateAnchors.GetStructInfo(context.StructName);
                    if (structInfo != null)
                    {
                        foreach (HNRPStructField field in structInfo.Fields)
                        {
                            sb.Append("//     ").Append(field.Type).Append(' ').Append(field.Name)
                                .Append("  ").Append(field.Doc).AppendLine();
                        }
                    }
                }
            }

            sb.Append("void ").Append(anchor.GetFunctionName(safeId)).Append('(')
                .Append(anchor.GetParameterList()).AppendLine(")");
            sb.AppendLine("{");
            sb.AppendLine("    // TODO: 在此修改输入输出结构体字段。");
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// 按当前插入点补齐 <paramref name="existingContent"/> 中缺失的函数骨架，已有实现不变。
        /// </summary>
        /// <param name="definition">包定义。</param>
        /// <param name="existingContent">现有 hlsl 文本。</param>
        /// <returns>同步后的 hlsl 文本。</returns>
        public static string SyncTemplate(HNRPPackageDefinition definition, string existingContent)
        {
            if (string.IsNullOrEmpty(existingContent))
            {
                return BuildFullTemplate(definition);
            }

            var addition = new StringBuilder();
            foreach (HNRPSubstrateId substrate in definition.SupportedSubstrates)
            {
                var missing = new List<string>();
                foreach (string stub in BuildStubs(definition, substrate))
                {
                    string functionName = ExtractFunctionName(stub);
                    if (!string.IsNullOrEmpty(functionName) && existingContent.Contains(functionName))
                    {
                        continue;
                    }

                    missing.Add(stub);
                }

                if (missing.Count == 0)
                {
                    continue;
                }

                addition.Append("#if defined(").Append(SubstrateGuard(substrate)).AppendLine(")");
                foreach (string stub in missing)
                {
                    addition.AppendLine(stub);
                }

                addition.AppendLine("#endif");
                addition.AppendLine();
            }

            if (addition.Length == 0)
            {
                return existingContent;
            }

            var block = new StringBuilder();
            block.AppendLine();
            block.AppendLine("// ---- Sync Template 补齐 ----");
            block.Append(addition);

            string guard = BuildGuard(definition);
            string endMarker = "#endif // " + guard;
            int endIndex = existingContent.LastIndexOf(endMarker, System.StringComparison.Ordinal);
            if (endIndex >= 0)
            {
                return existingContent.Substring(0, endIndex) + block + existingContent.Substring(endIndex);
            }

            return existingContent + "\n" + block;
        }

        /// <summary>某基底的激活守卫名（由组合头定义，包实现据此分段）。</summary>
        /// <param name="substrate">基底 id。</param>
        /// <returns>守卫宏名。</returns>
        public static string SubstrateGuard(HNRPSubstrateId substrate)
        {
            return "HNRP_ACTIVE_SUBSTRATE_" + substrate.ToString().ToUpperInvariant();
        }

        private static string ExtractFunctionName(string stub)
        {
            int index = stub.IndexOf("void ", System.StringComparison.Ordinal);
            if (index < 0)
            {
                return string.Empty;
            }

            index += "void ".Length;
            int end = stub.IndexOf('(', index);
            return end < 0 ? string.Empty : stub.Substring(index, end - index);
        }

        private static string BuildGuard(HNRPPackageDefinition definition)
        {
            return "HNRP_PACKAGE_" + definition.SafeId.ToUpperInvariant() + "_INCLUDED";
        }
    }
}
