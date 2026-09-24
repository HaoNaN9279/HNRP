using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// ShaderLibrary 组合生成器：把「基底 + 有序包列表」编译为最终 shader 资产
    /// （组合头 + 生成 shader），均落在 <see cref="HNRPProjectPaths.GeneratedShaders"/>。
    /// </summary>
    /// <remarks>
    /// 元数据来自 <see cref="HNRPPackageDatabase"/>（SO 声明 + hlsl 解析内容）。
    /// 流畅性约束：确定性路径，存在即复用；内容一致不写盘；仅包列表 / 包声明变化才触发重生成。
    /// </remarks>
    [InitializeOnLoad]
    public sealed class HNRPPackageGenerator : IPreprocessBuildWithReport
    {
        /// <summary>模板版本号；模板结构变化时递增以使全部组合失效重生成。</summary>
        public const int TemplateVersion = 1;

        private const string MarkerPropertyAnchor = "HNRP_PACKAGE_PROPERTY_ANCHOR";
        private const string MarkerIncludeAnchor = "HNRP_PACKAGE_INCLUDE_ANCHOR";
        private const string MarkerKeywordAnchor = "HNRP_PACKAGE_KEYWORD_ANCHOR";

        private static bool isScanning;

        static HNRPPackageGenerator()
        {
            EditorApplication.delayCall += GenerateAll;
        }

        /// <inheritdoc />
        public int callbackOrder => -90;

        /// <inheritdoc />
        public void OnPreprocessBuild(BuildReport report)
        {
            GenerateAll();
        }

        /// <summary>
        /// 扫描全部材质，为带 <see cref="HNRPPackageTags.Packages"/> tag 者确保生成 shader 存在。
        /// </summary>
        [MenuItem("HNRP/Generate Package Shaders")]
        public static void GenerateAll()
        {
            if (isScanning || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            isScanning = true;
            try
            {
                Dictionary<string, HNRPPackageEntry> index = HNRPPackageDatabase.BuildIndex();
                string[] guids = AssetDatabase.FindAssets("t:Material");

                // 不能包在 StartAssetEditing/StopAssetEditing 内：期间 ImportAsset 被延迟，
                // 生成 shader 无法立即 Load 以赋给材质。
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(GetMaterialPackageTag(material)))
                    {
                        continue;
                    }

                    EnsureShaderForMaterial(material, index);
                }

                AssetDatabase.Refresh();
            }
            finally
            {
                isScanning = false;
            }
        }

        /// <summary>
        /// 删除 <see cref="HNRPProjectPaths.GeneratedShaders"/> 下无任何材质引用的生成 shader / 组合头。
        /// </summary>
        [MenuItem("HNRP/Clean Unused Generated Shaders")]
        public static void CleanUnusedGeneratedShaders()
        {
            if (!Directory.Exists(ToFullPath(HNRPProjectPaths.GeneratedShaders)))
            {
                return;
            }

            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string guid in AssetDatabase.FindAssets("t:Material"))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material != null && material.shader != null)
                {
                    string shaderPath = AssetDatabase.GetAssetPath(material.shader);
                    if (!string.IsNullOrEmpty(shaderPath) &&
                        shaderPath.StartsWith(HNRPProjectPaths.GeneratedShaders, StringComparison.OrdinalIgnoreCase))
                    {
                        used.Add(shaderPath);
                    }
                }
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string shaderGuid in AssetDatabase.FindAssets("t:Shader", new[] { HNRPProjectPaths.GeneratedShaders }))
                {
                    string shaderPath = AssetDatabase.GUIDToAssetPath(shaderGuid);
                    if (used.Contains(shaderPath))
                    {
                        continue;
                    }

                    string headerPath = Path.ChangeExtension(shaderPath, ".hlsl");
                    AssetDatabase.DeleteAsset(shaderPath);
                    if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(headerPath) != null)
                    {
                        AssetDatabase.DeleteAsset(headerPath);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 依据材质的 override tag 确保其组合 shader 存在，并把 shader 赋给材质。
        /// </summary>
        /// <param name="material">目标材质。</param>
        /// <returns>材质应使用的 shader（无包时为基底 shader）。</returns>
        public static Shader EnsureShaderForMaterial(Material material)
        {
            return EnsureShaderForMaterial(material, HNRPPackageDatabase.BuildIndex());
        }

        private static Shader EnsureShaderForMaterial(
            Material material,
            Dictionary<string, HNRPPackageEntry> index)
        {
            string tag = GetMaterialPackageTag(material);
            if (string.IsNullOrEmpty(tag))
            {
                return material.shader;
            }

            if (!TryParseTag(tag, out HNRPSubstrateId substrate, out List<string> packageIds, out string error))
            {
                Debug.LogWarning($"[HNRPPackageGenerator] 材质 '{material.name}' 的包 tag 非法：{error}（tag='{tag}'）。");
                return material.shader;
            }

            HNRPSubstrateDescriptor descriptor = HNRPSubstrateRegistry.Get(substrate);
            if (descriptor == null)
            {
                Debug.LogWarning($"[HNRPPackageGenerator] 材质 '{material.name}' 指向未注册基底 {substrate}。");
                return material.shader;
            }

            var packages = new List<HNRPPackageEntry>();
            foreach (string packageId in packageIds)
            {
                if (!index.TryGetValue(packageId, out HNRPPackageEntry entry))
                {
                    Debug.LogWarning(
                        $"[HNRPPackageGenerator] 材质 '{material.name}' 引用了未注册的包 '{packageId}'；已忽略该包。");
                    continue;
                }

                if (!entry.Definition.Supports(substrate))
                {
                    Debug.LogWarning(
                        $"[HNRPPackageGenerator] 包 '{packageId}' 未声明支持基底 {descriptor.IdString}；已忽略该包。");
                    continue;
                }

                packages.Add(entry);
            }

            Shader shader;
            if (packages.Count == 0)
            {
                shader = AssetDatabase.LoadAssetAtPath<Shader>(descriptor.ShaderTemplatePath);
            }
            else
            {
                shader = EnsureCombination(descriptor, packages);
            }

            if (shader != null && material.shader != shader)
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
            }

            return shader;
        }

        /// <summary>
        /// 确保某基底 + 包列表对应的组合头与生成 shader 存在并返回其 shader。
        /// </summary>
        /// <param name="descriptor">基底描述。</param>
        /// <param name="packages">有序包条目（非空）。</param>
        /// <returns>生成 shader；生成失败时返回基底 shader。</returns>
        public static Shader EnsureCombination(HNRPSubstrateDescriptor descriptor, IReadOnlyList<HNRPPackageEntry> packages)
        {
            if (descriptor == null || packages == null || packages.Count == 0)
            {
                return descriptor != null ? AssetDatabase.LoadAssetAtPath<Shader>(descriptor.ShaderTemplatePath) : null;
            }

            string signature = BuildSignature(descriptor, packages);
            string hash = ComputeHash(signature);
            string stem = BuildStem(descriptor, packages);
            string headerName = stem + "_" + hash + ".hlsl";
            string shaderName = stem + "_" + hash + ".shader";
            string headerPath = HNRPProjectPaths.GeneratedShaders + "/" + headerName;
            string shaderPath = HNRPProjectPaths.GeneratedShaders + "/" + shaderName;

            string headerContent = BuildHeader(descriptor, packages, signature, hash);
            string shaderContent = BuildShader(descriptor, packages, headerName);

            WriteIfChanged(headerPath, headerContent);
            WriteIfChanged(shaderPath, shaderContent);

            return AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
        }

        /// <summary>读取材质的包 tag 值（无则返回空串）。</summary>
        /// <param name="material">目标材质。</param>
        /// <returns>tag 值。</returns>
        public static string GetMaterialPackageTag(Material material)
        {
            if (material == null)
            {
                return string.Empty;
            }

            return material.GetTag(HNRPPackageTags.Packages, false, string.Empty);
        }

        /// <summary>写入材质的包 tag 并置脏。</summary>
        /// <param name="material">目标材质。</param>
        /// <param name="substrate">基底 id。</param>
        /// <param name="packageIds">有序包 Id 列表（可为空）。</param>
        public static void SetMaterialPackageTag(Material material, HNRPSubstrateId substrate, IReadOnlyList<string> packageIds)
        {
            if (material == null)
            {
                return;
            }

            HNRPSubstrateDescriptor descriptor = HNRPSubstrateRegistry.Get(substrate);
            string substrateId = descriptor != null ? descriptor.IdString : substrate.ToString();

            var sb = new StringBuilder();
            sb.Append(substrateId);
            if (packageIds != null)
            {
                foreach (string packageId in packageIds)
                {
                    if (!string.IsNullOrEmpty(packageId))
                    {
                        sb.Append('|').Append(packageId);
                    }
                }
            }

            material.SetOverrideTag(HNRPPackageTags.Packages, sb.ToString());
            EditorUtility.SetDirty(material);
        }

        /// <summary>解析包 tag。</summary>
        /// <param name="tag">tag 值。</param>
        /// <param name="substrate">输出基底 id。</param>
        /// <param name="packageIds">输出有序包 Id 列表。</param>
        /// <param name="error">失败原因。</param>
        /// <returns>是否解析成功。</returns>
        public static bool TryParseTag(string tag, out HNRPSubstrateId substrate, out List<string> packageIds, out string error)
        {
            substrate = HNRPSubstrateId.Lit;
            packageIds = new List<string>();
            error = string.Empty;

            if (string.IsNullOrEmpty(tag))
            {
                error = "tag 为空";
                return false;
            }

            string[] parts = tag.Split('|');
            bool substrateFound = false;
            foreach (HNRPSubstrateDescriptor descriptor in HNRPSubstrateRegistry.All)
            {
                if (string.Equals(descriptor.IdString, parts[0], StringComparison.OrdinalIgnoreCase))
                {
                    substrate = descriptor.Id;
                    substrateFound = true;
                    break;
                }
            }

            if (!substrateFound && !Enum.TryParse(parts[0], true, out substrate))
            {
                error = $"未知基底 '{parts[0]}'";
                return false;
            }

            for (int i = 1; i < parts.Length; i++)
            {
                string id = parts[i].Trim();
                if (id.Length > 0)
                {
                    packageIds.Add(id);
                }
            }

            return true;
        }

        /// <summary>构建组合签名（方案 4.4）。</summary>
        /// <param name="descriptor">基底描述。</param>
        /// <param name="packages">有序包条目。</param>
        /// <returns>签名字符串。</returns>
        public static string BuildSignature(HNRPSubstrateDescriptor descriptor, IReadOnlyList<HNRPPackageEntry> packages)
        {
            var sb = new StringBuilder();
            sb.Append(descriptor.IdString).Append("|v").Append(TemplateVersion).Append('|');
            foreach (HNRPPackageEntry package in packages)
            {
                sb.Append(package.SafeId).Append(':')
                    .Append(ComputeFileHash(package.Definition.HlslPath)).Append(':')
                    .Append(BuildMetadataString(package)).Append(';');
            }

            return sb.ToString();
        }

        /// <summary>计算签名的短哈希（8 位十六进制）。</summary>
        /// <param name="signature">签名。</param>
        /// <returns>哈希。</returns>
        public static string ComputeHash(string signature)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(signature));
                var sb = new StringBuilder(8);
                for (int i = 0; i < 4; i++)
                {
                    sb.Append(bytes[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }

        /// <summary>构建生成文件名主体：<c>&lt;Substrate&gt;_&lt;Pkg&gt;_&lt;Pkg&gt;</c>。</summary>
        /// <param name="descriptor">基底描述。</param>
        /// <param name="packages">有序包条目。</param>
        /// <returns>文件名主体。</returns>
        public static string BuildStem(HNRPSubstrateDescriptor descriptor, IReadOnlyList<HNRPPackageEntry> packages)
        {
            var sb = new StringBuilder();
            sb.Append(descriptor.IdString);
            foreach (HNRPPackageEntry package in packages)
            {
                sb.Append('_').Append(package.Definition.FileStem);
            }

            return sb.ToString();
        }

        // ---------------- 内部实现 ----------------

        private static string BuildMetadataString(HNRPPackageEntry entry)
        {
            HNRPPackageDefinition definition = entry.Definition;
            HNRPPackageContent content = entry.Content;

            var sb = new StringBuilder();
            sb.Append(entry.SafeId).Append('/');

            sb.Append("sub:");
            foreach (HNRPSubstrateId substrate in definition.SupportedSubstrates)
            {
                sb.Append((int)substrate).Append(',');
            }

            sb.Append("/ins:");
            foreach (HNRPPackageInsertion insertion in definition.Insertions)
            {
                sb.Append((int)insertion.Substrate).Append(',')
                    .Append((int)insertion.Pass).Append(',')
                    .Append((int)insertion.Stage).Append(',')
                    .Append((int)insertion.Position).Append(';');
            }

            sb.Append("/attr:");
            foreach (string attribute in definition.RequiredAttributes)
            {
                sb.Append(attribute).Append(';');
            }

            sb.Append("/par:");
            foreach (HNRPPackageParameter parameter in content.Parameters)
            {
                sb.Append(parameter.Name).Append(',')
                    .Append((int)parameter.Type).Append(',')
                    .Append(parameter.DefaultFloat).Append(',')
                    .Append(parameter.Min).Append(',')
                    .Append(parameter.Max).Append(',')
                    .Append(parameter.DefaultInt).Append(',')
                    .Append(parameter.DefaultColor).Append(',')
                    .Append(parameter.DefaultVector).Append(',')
                    .Append(parameter.Keyword).Append(';');
            }

            sb.Append("/var:");
            foreach (HNRPPackageVarying varying in content.Varyings)
            {
                sb.Append(varying.Name).Append(',').Append(varying.Size).Append(';');
            }

            sb.Append("/key:");
            foreach (HNRPPackageKeyword keyword in content.Keywords)
            {
                sb.Append(keyword.Name).Append(';');
            }

            return sb.ToString();
        }

        private static string ComputeFileHash(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return "0";
            }

            string fullPath = ToFullPath(assetPath);
            if (!File.Exists(fullPath))
            {
                return "0";
            }

            using (var sha = SHA256.Create())
            {
                using (FileStream stream = File.OpenRead(fullPath))
                {
                    byte[] bytes = sha.ComputeHash(stream);
                    var sb = new StringBuilder(8);
                    for (int i = 0; i < 4; i++)
                    {
                        sb.Append(bytes[i].ToString("x2"));
                    }

                    return sb.ToString();
                }
            }
        }

        private static string BuildHeader(
            HNRPSubstrateDescriptor descriptor,
            IReadOnlyList<HNRPPackageEntry> packages,
            string signature,
            string hash)
        {
            var sb = new StringBuilder();
            string guard = "HNRP_GENERATED_" + hash.ToUpperInvariant() + "_INCLUDED";

            sb.AppendLine("// 自动生成，请勿手动编辑。");
            sb.AppendLine("// 由 HNRPPackageGenerator 依据材质包列表生成。");
            sb.Append("// 组合：").Append(descriptor.IdString);
            foreach (HNRPPackageEntry package in packages)
            {
                sb.Append(" + ").Append(package.SafeId);
            }

            sb.AppendLine();
            sb.Append("// 签名：").AppendLine(signature);
            sb.AppendLine();
            sb.Append("#ifndef ").AppendLine(guard);
            sb.Append("#define ").AppendLine(guard);
            sb.AppendLine();

            sb.AppendLine("// ---- 当前激活基底 ----");
            sb.Append("#define HNRP_ACTIVE_SUBSTRATE_")
                .Append(descriptor.IdString.ToUpperInvariant())
                .AppendLine(" 1");
            sb.AppendLine();

            sb.AppendLine("// ---- 包 varyings 浮点总数 ----");
            sb.Append("#define VARYINGS_FLOAT_COUNT_PACKAGES ").AppendLine(ComputePackageVaryingFloatCount(packages).ToString());
            sb.AppendLine();

            AppendMultiLineDefine(sb, "HNRP_PKG_VARYINGS_FIELDS", BuildVaryingFieldLines(packages));
            AppendMultiLineDefine(sb, "HNRP_PKG_MATERIAL_DECLARATIONS", BuildMaterialDeclarationLines(packages));
            AppendMultiLineDefine(sb, "HNRP_PKG_TEXTURE_DECLARATIONS", BuildTextureDeclarationLines(packages));
            AppendMultiLineDefine(sb, "HNRP_PKG_SET_VARYINGS(packed, lit, pointer)", BuildSetVaryingLines(packages));
            AppendMultiLineDefine(sb, "HNRP_PKG_GET_VARYINGS(packed, lit, pointer)", BuildGetVaryingLines(packages));

            // 锚点展开
            sb.AppendLine("// ---- 锚点展开 ----");
            AppendAnchorDefines(sb, descriptor, packages);
            sb.AppendLine();

            // 关键字统一 undef 必须在包级属性 define 之前
            sb.AppendLine("// ---- 关键字统一 undef 入口 ----");
            sb.Append("#include \"").Append(RelativeInclude(HNRPProjectPaths.ShaderLibrary + "/Core/KeywordsList.hlsl")).AppendLine("\"");
            sb.AppendLine();

            // 需要的顶点属性
            AppendAttributeDefines(sb, packages);

            // 基底类型前导
            sb.AppendLine("// ---- 基底类型前导 ----");
            foreach (string include in descriptor.PreludeIncludes)
            {
                sb.Append("#include \"").Append(RelativeInclude(include)).AppendLine("\"");
            }

            sb.AppendLine();

            // 包实现（唯一 hlsl）
            sb.AppendLine("// ---- 包实现 ----");
            foreach (HNRPPackageEntry package in packages)
            {
                if (string.IsNullOrEmpty(package.Definition.HlslPath))
                {
                    sb.Append("// 包 '").Append(package.SafeId).AppendLine("' 未提供 hlsl 实现。");
                    continue;
                }

                sb.Append("#include \"").Append(RelativeInclude(package.Definition.HlslPath)).AppendLine("\"");
            }

            sb.AppendLine();
            sb.Append("#endif // ").AppendLine(guard);
            return sb.ToString();
        }

        private static void AppendMultiLineDefine(StringBuilder sb, string defineTarget, List<string> lines)
        {
            sb.Append("#define ").Append(defineTarget);
            if (lines.Count == 0)
            {
                sb.AppendLine();
                sb.AppendLine();
                return;
            }

            sb.AppendLine(" \\");
            for (int i = 0; i < lines.Count; i++)
            {
                sb.Append("    ").Append(lines[i]);
                sb.Append(i == lines.Count - 1 ? string.Empty : " \\").AppendLine();
            }

            sb.AppendLine();
        }

        private static void AppendAnchorDefines(
            StringBuilder sb,
            HNRPSubstrateDescriptor descriptor,
            IReadOnlyList<HNRPPackageEntry> packages)
        {
            // 收集 token → 调用语句列表（按包顺序），仅取当前基底的插入点。
            var callsByToken = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var tokenOrder = new List<string>();

            foreach (HNRPPackageEntry package in packages)
            {
                foreach (HNRPPackageInsertion insertion in package.Definition.Insertions)
                {
                    if (insertion.Substrate != descriptor.Id)
                    {
                        continue;
                    }

                    HNRPAnchorDescriptor anchor = HNRPSubstrateAnchors.Find(
                        descriptor.Id,
                        insertion.Pass,
                        insertion.Stage,
                        insertion.Position);

                    if (anchor == null)
                    {
                        Debug.LogWarning(
                            $"[HNRPPackageGenerator] 包 '{package.SafeId}' 的插入点 " +
                            $"({insertion.Substrate}/{insertion.Pass}/{insertion.Stage}/{insertion.Position}) " +
                            $"不是基底 {descriptor.IdString} 的合法锚点；已跳过。");
                        continue;
                    }

                    if (!callsByToken.TryGetValue(anchor.Token, out List<string> calls))
                    {
                        calls = new List<string>();
                        callsByToken.Add(anchor.Token, calls);
                        tokenOrder.Add(anchor.Token);
                    }

                    string call = anchor.GetFunctionName(package.SafeId) + "(" + anchor.GetArgumentList() + ");";
                    if (!calls.Contains(call))
                    {
                        calls.Add(call);
                    }
                }
            }

            foreach (string token in tokenOrder)
            {
                HNRPAnchorDescriptor anchor = FindByToken(descriptor.Id, token);
                if (anchor == null)
                {
                    continue;
                }

                sb.Append("#define HNRP_PKG_EMIT_").Append(token).Append('(').Append(anchor.GetArgumentList()).Append(") \\").AppendLine();
                sb.AppendLine("    do { \\");
                foreach (string call in callsByToken[token])
                {
                    sb.Append("        ").Append(call).AppendLine(" \\");
                }

                sb.AppendLine("    } while (0)");
            }
        }

        private static HNRPAnchorDescriptor FindByToken(HNRPSubstrateId substrate, string token)
        {
            foreach (HNRPAnchorDescriptor anchor in HNRPSubstrateAnchors.GetAnchors(substrate))
            {
                if (anchor.Token == token)
                {
                    return anchor;
                }
            }

            return null;
        }

        private static void AppendAttributeDefines(StringBuilder sb, IReadOnlyList<HNRPPackageEntry> packages)
        {
            var attributeDefines = new List<string>();
            var varyingDefines = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (HNRPPackageEntry package in packages)
            {
                foreach (string raw in package.Definition.RequiredAttributes)
                {
                    string key = (raw ?? string.Empty).Trim().ToLowerInvariant();
                    string attribute = null;
                    string varying = null;
                    switch (key)
                    {
                        case "normal":
                            attribute = "ATTRIBUTES_NEED_NORMAL";
                            varying = "USE_NORMAL_WS_VARYING";
                            break;
                        case "tangent":
                            attribute = "ATTRIBUTES_NEED_TANGENT";
                            varying = "USE_TANGENT_WS_VARYING";
                            break;
                        case "uv0":
                            attribute = "ATTRIBUTES_NEED_UV0";
                            varying = "USE_UV0_VARYING";
                            break;
                        case "uv1":
                            attribute = "ATTRIBUTES_NEED_UV1";
                            varying = "USE_UV1_VARYING";
                            break;
                        case "uv2":
                            attribute = "ATTRIBUTES_NEED_UV2";
                            varying = "USE_UV2_VARYING";
                            break;
                        case "uv3":
                            attribute = "ATTRIBUTES_NEED_UV3";
                            varying = "USE_UV3_VARYING";
                            break;
                        case "color":
                            attribute = "ATTRIBUTES_NEED_COLOR";
                            varying = "USE_COLOR_VARYING";
                            break;
                        case "positionws":
                            varying = "USE_POSITION_WS_VARYING";
                            break;
                    }

                    if (attribute != null && seen.Add(attribute))
                    {
                        attributeDefines.Add(attribute);
                    }

                    if (varying != null && seen.Add(varying))
                    {
                        varyingDefines.Add(varying);
                    }
                }
            }

            if (attributeDefines.Count == 0 && varyingDefines.Count == 0)
            {
                return;
            }

            sb.AppendLine("// ---- 需要向顶点输入请求的属性 ----");
            foreach (string define in attributeDefines)
            {
                sb.Append("#define ").AppendLine(define);
            }

            foreach (string define in varyingDefines)
            {
                sb.Append("#define ").AppendLine(define);
            }

            sb.AppendLine();
        }

        private static int ComputePackageVaryingFloatCount(IReadOnlyList<HNRPPackageEntry> packages)
        {
            int total = 0;
            foreach (HNRPPackageEntry package in packages)
            {
                foreach (HNRPPackageVarying varying in package.Content.Varyings)
                {
                    total += Mathf.Clamp(varying.Size, 1, 4);
                }
            }

            if (total > 32)
            {
                Debug.LogError(
                    $"[HNRPPackageGenerator] 包 varyings 浮点总数 {total} 超过上限 32（PackedVaryings 仅 8 个 TEXCOORD）。");
            }

            return total;
        }

        private static List<string> BuildVaryingFieldLines(IReadOnlyList<HNRPPackageEntry> packages)
        {
            var lines = new List<string>();
            foreach (HNRPPackageEntry package in packages)
            {
                foreach (HNRPPackageVarying varying in package.Content.Varyings)
                {
                    lines.Add(varying.GetHlslType() + " " + varying.GetFieldName(package.SafeId) + ";");
                }
            }

            return lines;
        }

        private static List<string> BuildMaterialDeclarationLines(IReadOnlyList<HNRPPackageEntry> packages)
        {
            var lines = new List<string>();
            foreach (HNRPPackageEntry package in packages)
            {
                foreach (HNRPPackageParameter parameter in package.Content.Parameters)
                {
                    string name = parameter.GetPropertyName(package.SafeId);
                    switch (parameter.Type)
                    {
                        case HNRPPackageParameterType.Color:
                        case HNRPPackageParameterType.Vector:
                            lines.Add("float4 " + name + ";");
                            break;
                        case HNRPPackageParameterType.Texture2D:
                        case HNRPPackageParameterType.Cube:
                            lines.Add("float4 " + name + "_ST;");
                            break;
                        default:
                            lines.Add("float " + name + ";");
                            break;
                    }
                }
            }

            return lines;
        }

        private static List<string> BuildTextureDeclarationLines(IReadOnlyList<HNRPPackageEntry> packages)
        {
            var lines = new List<string>();
            foreach (HNRPPackageEntry package in packages)
            {
                foreach (HNRPPackageParameter parameter in package.Content.Parameters)
                {
                    string name = parameter.GetPropertyName(package.SafeId);
                    if (parameter.Type == HNRPPackageParameterType.Texture2D)
                    {
                        lines.Add("TEXTURE2D(" + name + ");");
                        lines.Add("SAMPLER(sampler" + name + ");");
                    }
                    else if (parameter.Type == HNRPPackageParameterType.Cube)
                    {
                        lines.Add("TEXTURECUBE(" + name + ");");
                        lines.Add("SAMPLER(sampler" + name + ");");
                    }
                }
            }

            return lines;
        }

        private static List<string> BuildSetVaryingLines(IReadOnlyList<HNRPPackageEntry> packages)
        {
            var lines = new List<string>();
            foreach (HNRPPackageEntry package in packages)
            {
                foreach (HNRPPackageVarying varying in package.Content.Varyings)
                {
                    int size = Mathf.Clamp(varying.Size, 1, 4);
                    lines.Add("SetVaryingsFloat" + (size == 1 ? string.Empty : size.ToString()) +
                              "(packed, lit." + varying.GetFieldName(package.SafeId) + ", pointer);");
                }
            }

            return lines;
        }

        private static List<string> BuildGetVaryingLines(IReadOnlyList<HNRPPackageEntry> packages)
        {
            var lines = new List<string>();
            for (int p = packages.Count - 1; p >= 0; p--)
            {
                HNRPPackageEntry package = packages[p];
                List<HNRPPackageVarying> varyings = package.Content.Varyings;
                for (int v = varyings.Count - 1; v >= 0; v--)
                {
                    HNRPPackageVarying varying = varyings[v];
                    int size = Mathf.Clamp(varying.Size, 1, 4);
                    lines.Add("GetVaryingsFloat" + (size == 1 ? string.Empty : size.ToString()) +
                              "(packed, lit." + varying.GetFieldName(package.SafeId) + ", pointer);");
                }
            }

            return lines;
        }

        private static List<string> BuildKeywordPragmas(IReadOnlyList<HNRPPackageEntry> packages)
        {
            var lines = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (HNRPPackageEntry package in packages)
            {
                foreach (HNRPPackageKeyword keyword in package.Content.Keywords)
                {
                    string full = keyword.GetKeywordName(package.SafeId);
                    if (seen.Add(full))
                    {
                        lines.Add("#pragma shader_feature_local " + full);
                    }
                }

                foreach (HNRPPackageParameter parameter in package.Content.Parameters)
                {
                    string full = parameter.GetKeywordName(package.SafeId);
                    if (!string.IsNullOrEmpty(full) && seen.Add(full))
                    {
                        lines.Add("#pragma shader_feature_local " + full);
                    }
                }
            }

            return lines;
        }

        private static string BuildShader(
            HNRPSubstrateDescriptor descriptor,
            IReadOnlyList<HNRPPackageEntry> packages,
            string headerName)
        {
            string templateText = File.ReadAllText(ToFullPath(descriptor.ShaderTemplatePath));
            string[] lines = templateText.Replace("\r\n", "\n").Split('\n');

            var propertyLines = new List<string>();
            foreach (HNRPPackageEntry package in packages)
            {
                propertyLines.Add(string.Empty);
                propertyLines.Add("        // " + package.Definition.DisplayName + " 包参数");
                foreach (HNRPPackageParameter parameter in package.Content.Parameters)
                {
                    propertyLines.Add("        " + BuildPropertyLine(package, parameter));
                }
            }

            List<string> keywordLines = BuildKeywordPragmas(packages);

            var output = new StringBuilder();
            string stem = BuildStem(descriptor, packages);
            string hash = ComputeHash(BuildSignature(descriptor, packages));

            foreach (string line in lines)
            {
                if (line.Contains(MarkerPropertyAnchor))
                {
                    foreach (string propertyLine in propertyLines)
                    {
                        output.AppendLine(propertyLine);
                    }
                }
                else if (line.Contains(MarkerIncludeAnchor))
                {
                    output.AppendLine("        #include \"" + headerName + "\"");
                }
                else if (line.Contains(MarkerKeywordAnchor))
                {
                    foreach (string keywordLine in keywordLines)
                    {
                        output.AppendLine("        " + keywordLine);
                    }
                }
                else
                {
                    output.AppendLine(line);
                }
            }

            string result = output.ToString();

            result = result.Replace(
                "Shader \"" + GetBaseShaderName(descriptor) + "\"",
                "Shader \"Hidden/HNRP/Generated/" + stem + "_" + hash + "\"");

            return result;
        }

        private static string GetBaseShaderName(HNRPSubstrateDescriptor descriptor)
        {
            string templateText = File.ReadAllText(ToFullPath(descriptor.ShaderTemplatePath));
            const string prefix = "Shader \"";
            int start = templateText.IndexOf(prefix, StringComparison.Ordinal);
            if (start < 0)
            {
                return "HNRP/" + descriptor.IdString;
            }

            start += prefix.Length;
            int end = templateText.IndexOf('"', start);
            return end < 0 ? "HNRP/" + descriptor.IdString : templateText.Substring(start, end - start);
        }

        private static string BuildPropertyLine(HNRPPackageEntry package, HNRPPackageParameter parameter)
        {
            string name = parameter.GetPropertyName(package.SafeId);
            string display = string.IsNullOrEmpty(parameter.DisplayName) ? parameter.Name : parameter.DisplayName;

            switch (parameter.Type)
            {
                case HNRPPackageParameterType.Range:
                    return name + " (\"" + display + "\", Range(" + FormatFloat(parameter.Min) + ", " +
                           FormatFloat(parameter.Max) + ")) = " + FormatFloat(parameter.DefaultFloat);

                case HNRPPackageParameterType.Int:
                    return name + " (\"" + display + "\", Int) = " + parameter.DefaultInt;

                case HNRPPackageParameterType.Toggle:
                    return "[ToggleUI] " + name + " (\"" + display + "\", Float) = " +
                           (parameter.DefaultFloat > 0.5f ? "1" : "0");

                case HNRPPackageParameterType.Color:
                    return name + " (\"" + display + "\", Color) = (" + FormatFloat(parameter.DefaultColor.r) + ", " +
                           FormatFloat(parameter.DefaultColor.g) + ", " + FormatFloat(parameter.DefaultColor.b) + ", " +
                           FormatFloat(parameter.DefaultColor.a) + ")";

                case HNRPPackageParameterType.Vector:
                    return name + " (\"" + display + "\", Vector) = (" + FormatVector(parameter.DefaultVector) + ")";

                case HNRPPackageParameterType.Texture2D:
                    return name + " (\"" + display + "\", 2D) = \"white\" {}";

                case HNRPPackageParameterType.Cube:
                    return name + " (\"" + display + "\", Cube) = \"white\" {}";

                case HNRPPackageParameterType.Enum:
                case HNRPPackageParameterType.Float:
                default:
                    return name + " (\"" + display + "\", Float) = " + FormatFloat(parameter.DefaultFloat);
            }
        }

        private static string FormatVector(Vector4 value)
        {
            return FormatFloat(value.x) + ", " + FormatFloat(value.y) + ", " +
                   FormatFloat(value.z) + ", " + FormatFloat(value.w);
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string RelativeInclude(string assetPath)
        {
            string generatedDir = ToFullPath(HNRPProjectPaths.GeneratedShaders);
            string target = ToFullPath(assetPath);
            string relative = Path.GetRelativePath(generatedDir, target);
            return relative.Replace('\\', '/');
        }

        private static void WriteIfChanged(string assetPath, string content)
        {
            string fullPath = ToFullPath(assetPath);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string normalized = content.Replace("\r\n", "\n");
            string existing = File.Exists(fullPath) ? File.ReadAllText(fullPath).Replace("\r\n", "\n") : null;
            if (existing == normalized)
            {
                return;
            }

            File.WriteAllText(fullPath, normalized, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        private static string ToFullPath(string assetPath)
        {
            return Path.GetFullPath(assetPath);
        }
    }
}
