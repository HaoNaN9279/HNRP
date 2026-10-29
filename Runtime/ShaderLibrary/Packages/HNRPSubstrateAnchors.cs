using System.Collections.Generic;

namespace HN.HNRP
{
    /// <summary>锚点上下文参数的传递方式，映射到 HLSL 参数修饰符。</summary>
    public enum HNRPContextAccess
    {
        /// <summary>只读（in）。</summary>
        In = 0,

        /// <summary>可读写（inout）。</summary>
        InOut = 1,

        /// <summary>仅输出（out）。</summary>
        Out = 2,
    }

    /// <summary>
    /// 锚点上下文中的一个参数（结构体类型 + 参数名 + 传递方式）。
    /// 生成包函数骨架时作为签名参数。
    /// </summary>
    public sealed class HNRPAnchorContext
    {
        /// <summary>结构体类型名（如 <c>LitSurfaceData</c>）。</summary>
        public string StructName;

        /// <summary>参数名（如 <c>surface</c>）。</summary>
        public string ParamName;

        /// <summary>传递方式。</summary>
        public HNRPContextAccess Access;

        /// <summary>构造一个锚点参数。</summary>
        public HNRPAnchorContext(string structName, string paramName, HNRPContextAccess access)
        {
            StructName = structName;
            ParamName = paramName;
            Access = access;
        }
    }

    /// <summary>
    /// 一个基底锚点的完整签名：基底 + pass + 阶段 + 位置，以及上下文参数列表。
    /// 这是锚点签名的唯一真源（与 <c>Core/PackageAnchors.hlsl</c> 的默认空宏一一对应）。
    /// </summary>
    public sealed class HNRPAnchorDescriptor
    {
        /// <summary>归属基底。</summary>
        public HNRPSubstrateId Substrate;

        /// <summary>目标 pass。</summary>
        public HNRPPassKind Pass;

        /// <summary>目标阶段。</summary>
        public HNRPShaderStage Stage;

        /// <summary>阶段内位置。</summary>
        public HNRPAnchorPosition Position;

        /// <summary>上下文参数列表。</summary>
        public HNRPAnchorContext[] Contexts;

        /// <summary>
        /// 宏 / 函数名后缀 token（全大写，含基底，如
        /// <c>LIT_FORWARD_FRAGMENT_AFTERSURFACEDATA</c>）。
        /// </summary>
        public string Token =>
            Substrate.ToString().ToUpperInvariant() + "_" +
            Pass.ToString().ToUpperInvariant() + "_" +
            Stage.ToString().ToUpperInvariant() + "_" +
            Position.ToString().ToUpperInvariant();

        /// <summary>hlsl 默认空宏名（如 <c>HNRP_PKG_EMIT_LIT_FORWARD_FRAGMENT_AFTERSURFACEDATA</c>）。</summary>
        public string MacroName => "HNRP_PKG_EMIT_" + Token;

        /// <summary>包函数名（如 <c>PkgSnow_Lit_Forward_Fragment_AfterSurfaceData</c>）。</summary>
        /// <param name="safeId">包的代码安全 id。</param>
        /// <returns>包函数名。</returns>
        public string GetFunctionName(string safeId)
        {
            return "Pkg" + safeId + "_" + Substrate + "_" + Pass + "_" + Stage + "_" + Position;
        }

        /// <summary>包函数签名的括号内参数列表文本。</summary>
        /// <returns>如 <c>inout LitSurfaceData surface, in LitVaryings varyings</c>。</returns>
        public string GetParameterList()
        {
            if (Contexts == null || Contexts.Length == 0)
            {
                return string.Empty;
            }

            var parts = new List<string>(Contexts.Length);
            foreach (HNRPAnchorContext context in Contexts)
            {
                parts.Add(GetAccessKeyword(context.Access) + " " + context.StructName + " " + context.ParamName);
            }

            return string.Join(", ", parts);
        }

        /// <summary>调用包函数时传入的实参列表文本。</summary>
        /// <returns>如 <c>surface, varyings</c>。</returns>
        public string GetArgumentList()
        {
            if (Contexts == null || Contexts.Length == 0)
            {
                return string.Empty;
            }

            var parts = new List<string>(Contexts.Length);
            foreach (HNRPAnchorContext context in Contexts)
            {
                parts.Add(context.ParamName);
            }

            return string.Join(", ", parts);
        }

        /// <summary>把传递方式映射为 HLSL 关键字。</summary>
        /// <param name="access">传递方式。</param>
        /// <returns>HLSL 关键字。</returns>
        public static string GetAccessKeyword(HNRPContextAccess access)
        {
            switch (access)
            {
                case HNRPContextAccess.InOut:
                    return "inout";
                case HNRPContextAccess.Out:
                    return "out";
                default:
                    return "in";
            }
        }
    }

    /// <summary>结构体字段元数据，用于生成函数头的字段说明注释。</summary>
    public sealed class HNRPStructField
    {
        /// <summary>字段名。</summary>
        public string Name;

        /// <summary>字段类型。</summary>
        public string Type;

        /// <summary>字段说明。</summary>
        public string Doc;

        /// <summary>构造字段元数据。</summary>
        public HNRPStructField(string name, string type, string doc)
        {
            Name = name;
            Type = type;
            Doc = doc;
        }
    }

    /// <summary>结构体元数据（类型名 + 字段列表）。</summary>
    public sealed class HNRPStructInfo
    {
        /// <summary>结构体类型名。</summary>
        public string Name;

        /// <summary>字段列表。</summary>
        public HNRPStructField[] Fields;

        /// <summary>构造结构体元数据。</summary>
        public HNRPStructInfo(string name, HNRPStructField[] fields)
        {
            Name = name;
            Fields = fields;
        }
    }

    /// <summary>
    /// 基底锚点签名的唯一真源：定义每个基底可用的锚点及其上下文参数，
    /// 并提供锚点上下文所涉及结构体的字段元数据（供模板注释使用）。
    /// </summary>
    /// <remarks>
    /// hlsl 侧（<c>Core/PackageAnchors.hlsl</c>）只写与本文档对应的默认空宏；
    /// EditMode 测试校验两端一一对应，防止签名漂移（方案 D5）。
    /// </remarks>
    public static class HNRPSubstrateAnchors
    {
        private static readonly Dictionary<HNRPSubstrateId, List<HNRPAnchorDescriptor>> AnchorsBySubstrate =
            new Dictionary<HNRPSubstrateId, List<HNRPAnchorDescriptor>>();

        private static readonly Dictionary<string, HNRPStructInfo> StructInfos =
            new Dictionary<string, HNRPStructInfo>();

        static HNRPSubstrateAnchors()
        {
            BuildStructInfos();
            AnchorsBySubstrate[HNRPSubstrateId.Lit] = BuildLitAnchors();
            AnchorsBySubstrate[HNRPSubstrateId.Unlit] = BuildUnlitAnchors();
        }

        /// <summary>获取某基底的全部锚点。</summary>
        /// <param name="substrate">基底 id。</param>
        /// <returns>锚点列表；未知基底返回空列表。</returns>
        public static IReadOnlyList<HNRPAnchorDescriptor> GetAnchors(HNRPSubstrateId substrate)
        {
            return AnchorsBySubstrate.TryGetValue(substrate, out List<HNRPAnchorDescriptor> list)
                ? list
                : (IReadOnlyList<HNRPAnchorDescriptor>)new List<HNRPAnchorDescriptor>();
        }

        /// <summary>获取某基底 + 某 pass 的全部锚点。</summary>
        /// <param name="substrate">基底 id。</param>
        /// <param name="pass">pass 种类。</param>
        /// <returns>锚点列表。</returns>
        public static List<HNRPAnchorDescriptor> GetAnchors(HNRPSubstrateId substrate, HNRPPassKind pass)
        {
            var result = new List<HNRPAnchorDescriptor>();
            foreach (HNRPAnchorDescriptor anchor in GetAnchors(substrate))
            {
                if (anchor.Pass == pass)
                {
                    result.Add(anchor);
                }
            }

            return result;
        }

        /// <summary>按四元组精确查找锚点。</summary>
        /// <param name="substrate">基底 id。</param>
        /// <param name="pass">pass 种类。</param>
        /// <param name="stage">着色阶段。</param>
        /// <param name="position">阶段内位置。</param>
        /// <returns>锚点；不存在时返回 <c>null</c>。</returns>
        public static HNRPAnchorDescriptor Find(
            HNRPSubstrateId substrate,
            HNRPPassKind pass,
            HNRPShaderStage stage,
            HNRPAnchorPosition position)
        {
            foreach (HNRPAnchorDescriptor anchor in GetAnchors(substrate))
            {
                if (anchor.Pass == pass && anchor.Stage == stage && anchor.Position == position)
                {
                    return anchor;
                }
            }

            return null;
        }

        /// <summary>获取结构体字段元数据。</summary>
        /// <param name="structName">结构体类型名。</param>
        /// <returns>结构体元数据；未知时返回 <c>null</c>。</returns>
        public static HNRPStructInfo GetStructInfo(string structName)
        {
            if (string.IsNullOrEmpty(structName))
            {
                return null;
            }

            return StructInfos.TryGetValue(structName, out HNRPStructInfo info) ? info : null;
        }

        /// <summary>获取全部结构体元数据。</summary>
        public static IReadOnlyDictionary<string, HNRPStructInfo> AllStructInfos => StructInfos;

        private static HNRPAnchorContext Ctx(string structName, string paramName, HNRPContextAccess access)
        {
            return new HNRPAnchorContext(structName, paramName, access);
        }

        private static HNRPAnchorDescriptor Anchor(
            HNRPSubstrateId substrate,
            HNRPPassKind pass,
            HNRPShaderStage stage,
            HNRPAnchorPosition position,
            params HNRPAnchorContext[] contexts)
        {
            return new HNRPAnchorDescriptor
            {
                Substrate = substrate,
                Pass = pass,
                Stage = stage,
                Position = position,
                Contexts = contexts ?? new HNRPAnchorContext[0],
            };
        }

        private static List<HNRPAnchorDescriptor> BuildLitAnchors()
        {
            const HNRPSubstrateId lit = HNRPSubstrateId.Lit;

            var list = new List<HNRPAnchorDescriptor>
            {
                // Forward / Vertex
                Anchor(lit, HNRPPassKind.Forward, HNRPShaderStage.Vertex, HNRPAnchorPosition.Begin),
                Anchor(lit, HNRPPassKind.Forward, HNRPShaderStage.Vertex, HNRPAnchorPosition.AfterVertexInput,
                    Ctx("VertexInput", "vertexInput", HNRPContextAccess.InOut)),
                Anchor(lit, HNRPPassKind.Forward, HNRPShaderStage.Vertex, HNRPAnchorPosition.AfterVaryings,
                    Ctx("LitVaryings", "varyings", HNRPContextAccess.InOut)),
                Anchor(lit, HNRPPassKind.Forward, HNRPShaderStage.Vertex, HNRPAnchorPosition.End),

                // Forward / Fragment
                Anchor(lit, HNRPPassKind.Forward, HNRPShaderStage.Fragment, HNRPAnchorPosition.Begin),
                Anchor(lit, HNRPPassKind.Forward, HNRPShaderStage.Fragment, HNRPAnchorPosition.AfterUnpackVaryings,
                    Ctx("LitVaryings", "varyings", HNRPContextAccess.InOut)),
                Anchor(lit, HNRPPassKind.Forward, HNRPShaderStage.Fragment, HNRPAnchorPosition.AfterSurfaceData,
                    Ctx("LitSurfaceData", "surface", HNRPContextAccess.InOut),
                    Ctx("LitVaryings", "varyings", HNRPContextAccess.In)),
                Anchor(lit, HNRPPassKind.Forward, HNRPShaderStage.Fragment, HNRPAnchorPosition.AfterBRDFData,
                    Ctx("BRDFData", "brdf", HNRPContextAccess.InOut),
                    Ctx("LitSurfaceData", "surface", HNRPContextAccess.In)),
                Anchor(lit, HNRPPassKind.Forward, HNRPShaderStage.Fragment, HNRPAnchorPosition.AfterLightingInput,
                    Ctx("LightingInputData", "lightingInput", HNRPContextAccess.InOut)),
                Anchor(lit, HNRPPassKind.Forward, HNRPShaderStage.Fragment, HNRPAnchorPosition.AfterLighting,
                    Ctx("LightingData", "lighting", HNRPContextAccess.InOut),
                    Ctx("BRDFData", "brdf", HNRPContextAccess.In),
                    Ctx("LitSurfaceData", "surface", HNRPContextAccess.In)),
                Anchor(lit, HNRPPassKind.Forward, HNRPShaderStage.Fragment, HNRPAnchorPosition.AfterOutput,
                    Ctx("LightingOutputData", "output", HNRPContextAccess.InOut),
                    Ctx("LitSurfaceData", "surface", HNRPContextAccess.In)),
                Anchor(lit, HNRPPassKind.Forward, HNRPShaderStage.Fragment, HNRPAnchorPosition.End),

                // DepthNormal / Vertex
                Anchor(lit, HNRPPassKind.DepthNormal, HNRPShaderStage.Vertex, HNRPAnchorPosition.Begin),
                Anchor(lit, HNRPPassKind.DepthNormal, HNRPShaderStage.Vertex, HNRPAnchorPosition.AfterVaryings,
                    Ctx("LitVaryings", "varyings", HNRPContextAccess.InOut)),
                Anchor(lit, HNRPPassKind.DepthNormal, HNRPShaderStage.Vertex, HNRPAnchorPosition.End),

                // DepthNormal / Fragment
                Anchor(lit, HNRPPassKind.DepthNormal, HNRPShaderStage.Fragment, HNRPAnchorPosition.Begin),
                Anchor(lit, HNRPPassKind.DepthNormal, HNRPShaderStage.Fragment, HNRPAnchorPosition.AfterUnpackVaryings,
                    Ctx("LitVaryings", "varyings", HNRPContextAccess.InOut)),
                Anchor(lit, HNRPPassKind.DepthNormal, HNRPShaderStage.Fragment, HNRPAnchorPosition.AfterSurfaceData,
                    Ctx("LitSurfaceData", "surface", HNRPContextAccess.InOut),
                    Ctx("LitVaryings", "varyings", HNRPContextAccess.In)),
                Anchor(lit, HNRPPassKind.DepthNormal, HNRPShaderStage.Fragment, HNRPAnchorPosition.End),

                // DepthOnly / Vertex
                Anchor(lit, HNRPPassKind.DepthOnly, HNRPShaderStage.Vertex, HNRPAnchorPosition.Begin),
                Anchor(lit, HNRPPassKind.DepthOnly, HNRPShaderStage.Vertex, HNRPAnchorPosition.AfterVertex,
                    Ctx("DepthOnlyVaryings", "varyings", HNRPContextAccess.InOut)),
                Anchor(lit, HNRPPassKind.DepthOnly, HNRPShaderStage.Vertex, HNRPAnchorPosition.End),

                // DepthOnly / Fragment
                Anchor(lit, HNRPPassKind.DepthOnly, HNRPShaderStage.Fragment, HNRPAnchorPosition.Begin),
                Anchor(lit, HNRPPassKind.DepthOnly, HNRPShaderStage.Fragment, HNRPAnchorPosition.End),

                // ShadowCaster / Vertex
                Anchor(lit, HNRPPassKind.ShadowCaster, HNRPShaderStage.Vertex, HNRPAnchorPosition.Begin),
                Anchor(lit, HNRPPassKind.ShadowCaster, HNRPShaderStage.Vertex, HNRPAnchorPosition.AfterVertex,
                    Ctx("ShadowCasterVaryings", "varyings", HNRPContextAccess.InOut)),
                Anchor(lit, HNRPPassKind.ShadowCaster, HNRPShaderStage.Vertex, HNRPAnchorPosition.End),

                // ShadowCaster / Fragment
                Anchor(lit, HNRPPassKind.ShadowCaster, HNRPShaderStage.Fragment, HNRPAnchorPosition.Begin),
                Anchor(lit, HNRPPassKind.ShadowCaster, HNRPShaderStage.Fragment, HNRPAnchorPosition.End),
            };

            return list;
        }

        private static List<HNRPAnchorDescriptor> BuildUnlitAnchors()
        {
            const HNRPSubstrateId unlit = HNRPSubstrateId.Unlit;

            var list = new List<HNRPAnchorDescriptor>
            {
                // Forward / Vertex
                Anchor(unlit, HNRPPassKind.Forward, HNRPShaderStage.Vertex, HNRPAnchorPosition.Begin),
                Anchor(unlit, HNRPPassKind.Forward, HNRPShaderStage.Vertex, HNRPAnchorPosition.AfterVertexInput,
                    Ctx("VertexInput", "vertexInput", HNRPContextAccess.InOut)),
                Anchor(unlit, HNRPPassKind.Forward, HNRPShaderStage.Vertex, HNRPAnchorPosition.AfterVaryings,
                    Ctx("UnlitVaryings", "varyings", HNRPContextAccess.InOut)),
                Anchor(unlit, HNRPPassKind.Forward, HNRPShaderStage.Vertex, HNRPAnchorPosition.End),

                // Forward / Fragment（无光照 / GI 锚点）
                Anchor(unlit, HNRPPassKind.Forward, HNRPShaderStage.Fragment, HNRPAnchorPosition.Begin),
                Anchor(unlit, HNRPPassKind.Forward, HNRPShaderStage.Fragment, HNRPAnchorPosition.AfterUnpackVaryings,
                    Ctx("UnlitVaryings", "varyings", HNRPContextAccess.InOut)),
                Anchor(unlit, HNRPPassKind.Forward, HNRPShaderStage.Fragment, HNRPAnchorPosition.AfterSurfaceData,
                    Ctx("UnlitSurfaceData", "surface", HNRPContextAccess.InOut),
                    Ctx("UnlitVaryings", "varyings", HNRPContextAccess.In)),
                Anchor(unlit, HNRPPassKind.Forward, HNRPShaderStage.Fragment, HNRPAnchorPosition.AfterOutput,
                    Ctx("UnlitOutputData", "output", HNRPContextAccess.InOut),
                    Ctx("UnlitSurfaceData", "surface", HNRPContextAccess.In)),
                Anchor(unlit, HNRPPassKind.Forward, HNRPShaderStage.Fragment, HNRPAnchorPosition.End),

                // DepthNormal / Vertex
                Anchor(unlit, HNRPPassKind.DepthNormal, HNRPShaderStage.Vertex, HNRPAnchorPosition.Begin),
                Anchor(unlit, HNRPPassKind.DepthNormal, HNRPShaderStage.Vertex, HNRPAnchorPosition.AfterVaryings,
                    Ctx("UnlitVaryings", "varyings", HNRPContextAccess.InOut)),
                Anchor(unlit, HNRPPassKind.DepthNormal, HNRPShaderStage.Vertex, HNRPAnchorPosition.End),

                // DepthNormal / Fragment
                Anchor(unlit, HNRPPassKind.DepthNormal, HNRPShaderStage.Fragment, HNRPAnchorPosition.Begin),
                Anchor(unlit, HNRPPassKind.DepthNormal, HNRPShaderStage.Fragment, HNRPAnchorPosition.AfterUnpackVaryings,
                    Ctx("UnlitVaryings", "varyings", HNRPContextAccess.InOut)),
                Anchor(unlit, HNRPPassKind.DepthNormal, HNRPShaderStage.Fragment, HNRPAnchorPosition.AfterSurfaceData,
                    Ctx("UnlitSurfaceData", "surface", HNRPContextAccess.InOut),
                    Ctx("UnlitVaryings", "varyings", HNRPContextAccess.In)),
                Anchor(unlit, HNRPPassKind.DepthNormal, HNRPShaderStage.Fragment, HNRPAnchorPosition.End),

                // DepthOnly / Vertex
                Anchor(unlit, HNRPPassKind.DepthOnly, HNRPShaderStage.Vertex, HNRPAnchorPosition.Begin),
                Anchor(unlit, HNRPPassKind.DepthOnly, HNRPShaderStage.Vertex, HNRPAnchorPosition.AfterVertex,
                    Ctx("UnlitDepthOnlyVaryings", "varyings", HNRPContextAccess.InOut)),
                Anchor(unlit, HNRPPassKind.DepthOnly, HNRPShaderStage.Vertex, HNRPAnchorPosition.End),

                // DepthOnly / Fragment
                Anchor(unlit, HNRPPassKind.DepthOnly, HNRPShaderStage.Fragment, HNRPAnchorPosition.Begin),
                Anchor(unlit, HNRPPassKind.DepthOnly, HNRPShaderStage.Fragment, HNRPAnchorPosition.End),

                // ShadowCaster / Vertex
                Anchor(unlit, HNRPPassKind.ShadowCaster, HNRPShaderStage.Vertex, HNRPAnchorPosition.Begin),
                Anchor(unlit, HNRPPassKind.ShadowCaster, HNRPShaderStage.Vertex, HNRPAnchorPosition.AfterVertex,
                    Ctx("UnlitShadowCasterVaryings", "varyings", HNRPContextAccess.InOut)),
                Anchor(unlit, HNRPPassKind.ShadowCaster, HNRPShaderStage.Vertex, HNRPAnchorPosition.End),

                // ShadowCaster / Fragment
                Anchor(unlit, HNRPPassKind.ShadowCaster, HNRPShaderStage.Fragment, HNRPAnchorPosition.Begin),
                Anchor(unlit, HNRPPassKind.ShadowCaster, HNRPShaderStage.Fragment, HNRPAnchorPosition.End),
            };

            return list;
        }

        private static void BuildStructInfos()
        {
            Add("VertexInput", new[]
            {
                F("positionOS", "float3", "物体空间位置"),
                F("normalOS", "float3", "物体空间法线"),
                F("tangentOS", "float4", "物体空间切线（w 为手性符号）"),
                F("uv0", "float2", "主 UV（已应用 _BaseMap ST）"),
                F("uv1", "float2", "次 UV / 静态光照贴图 UV"),
                F("uv2", "float2", "动态光照贴图 UV"),
                F("uv3", "float2", "第三套 UV"),
                F("color", "float4", "顶点色"),
                F("staticLightmapUV", "float2", "静态光照贴图 UV（已应用 unity_LightmapST）"),
                F("dynamicLightmapUV", "float2", "动态光照贴图 UV"),
            });

            Add("LitVaryings", new[]
            {
                F("positionCS", "float4", "裁剪空间位置"),
                F("positionWS", "float3", "世界空间位置"),
                F("normalWS", "float3", "世界空间法线"),
                F("tangentWS", "float4", "世界空间切线（w 为手性符号）"),
                F("uv0", "float2", "主 UV"),
                F("uv1", "float2", "次 UV"),
                F("uv2", "float2", "动态光照贴图 UV"),
                F("uv3", "float2", "第三套 UV"),
                F("color", "float4", "顶点色"),
                F("staticLightmapUV", "float2", "静态光照贴图 UV（已应用 ST）"),
                F("vertexSH", "float3", "顶点球谐（SH）"),
            });

            Add("LitSurfaceData", new[]
            {
                F("albedo", "float3", "基础色（已乘 _BaseColor）"),
                F("alpha", "float", "透明度"),
                F("normalTS", "float3", "切线空间法线"),
                F("smoothness", "float", "平滑度"),
                F("metallic", "float", "金属度"),
                F("occlusion", "float", "环境光遮蔽"),
                F("emission", "float3", "自发光"),
            });

            Add("BRDFData", new[]
            {
                F("normalTS", "float3", "切线空间法线"),
                F("normalWS", "float3", "世界空间法线（已应用法线贴图）"),
                F("oneMinusReflectivity", "float", "1 - 反射率"),
                F("reflectivity", "float", "反射率"),
                F("diffuse", "float3", "漫反射系数（反照率 × (1-反射率)）"),
                F("specular", "float3", "镜面反射系数"),
                F("perceptualRoughness", "float", "感知粗糙度"),
                F("roughness", "float", "粗糙度"),
                F("roughness2", "float", "粗糙度平方"),
                F("grazingTerm", "float", "掠射项（smoothness + reflectivity）"),
                F("normalizationTerm", "float", "法线分布归一化项"),
                F("roughness2MinusOne", "float", "粗糙度平方 - 1"),
                F("refViewDirectionWS", "float3", "反射视线方向"),
                F("NdotV", "float", "法线·视线"),
                F("saturateNdotV", "float", "saturate(法线·视线)"),
                F("fresnelTerm", "float", "菲涅尔项"),
            });

            Add("LightingInputData", new[]
            {
                F("positionWS", "float3", "世界空间位置"),
                F("mainLight", "Light", "主光"),
                F("mainUV", "float2", "主光 UV"),
                F("bakedGI", "float3", "烘焙全局光照"),
                F("normalizedScreenSpaceUV", "float2", "归一化屏幕空间 UV"),
            });

            Add("LightingData", new[]
            {
                F("mainDirectLight", "DirectLightingData", "主光直接光照"),
                F("additionalDirectLight", "DirectLightingData", "附加光直接光照（累加）"),
                F("indirectLight", "IndirectLightingData", "间接光照"),
            });

            Add("LightingOutputData", new[]
            {
                F("lightingColor", "float3", "最终光照颜色"),
                F("alpha", "float", "透明度"),
            });

            Add("DepthOnlyVaryings", new[]
            {
                F("positionCS", "float4", "裁剪空间位置"),
                F("uv0", "float2", "主 UV（用于 alpha clip）"),
            });

            Add("ShadowCasterVaryings", new[]
            {
                F("positionCS", "float4", "光源裁剪空间位置"),
                F("uv0", "float2", "主 UV（用于 alpha clip）"),
                F("positionWS", "float3", "世界空间位置"),
                F("normalWS", "float3", "世界空间法线"),
            });

            Add("UnlitVaryings", new[]
            {
                F("positionCS", "float4", "裁剪空间位置"),
                F("positionWS", "float3", "世界空间位置"),
                F("normalWS", "float3", "世界空间法线"),
                F("tangentWS", "float4", "世界空间切线（w 为手性符号）"),
                F("uv0", "float2", "主 UV"),
                F("uv1", "float2", "次 UV"),
                F("uv2", "float2", "动态光照贴图 UV"),
                F("uv3", "float2", "第三套 UV"),
                F("color", "float4", "顶点色"),
            });

            Add("UnlitSurfaceData", new[]
            {
                F("color", "float3", "颜色参数 × 贴图采样结果"),
                F("alpha", "float", "透明度"),
            });

            Add("UnlitOutputData", new[]
            {
                F("color", "float3", "输出颜色（透明预乘时已乘 alpha）"),
                F("alpha", "float", "透明度"),
            });

            Add("UnlitDepthOnlyVaryings", new[]
            {
                F("positionCS", "float4", "裁剪空间位置"),
                F("uv0", "float2", "主 UV（用于 alpha clip）"),
            });

            Add("UnlitShadowCasterVaryings", new[]
            {
                F("positionCS", "float4", "光源裁剪空间位置"),
                F("uv0", "float2", "主 UV（用于 alpha clip）"),
                F("positionWS", "float3", "世界空间位置"),
                F("normalWS", "float3", "世界空间法线"),
            });
        }

        private static HNRPStructField F(string name, string type, string doc)
        {
            return new HNRPStructField(name, type, doc);
        }

        private static void Add(string structName, HNRPStructField[] fields)
        {
            StructInfos[structName] = new HNRPStructInfo(structName, fields);
        }
    }
}
