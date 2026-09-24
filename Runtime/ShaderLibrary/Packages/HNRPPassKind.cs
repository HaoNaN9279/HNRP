namespace HN.HNRP
{
    /// <summary>
    /// 基底内的 shader pass 种类。锚点、包插入点均以 pass 为维度之一。
    /// 名称与 shader 的 <c>LightMode</c> 标签逐字一致，便于生成器与校验器对照。
    /// </summary>
    public enum HNRPPassKind
    {
        /// <summary>前向着色 pass（LightMode = Forward）。</summary>
        Forward = 0,

        /// <summary>阴影投射 pass（LightMode = ShadowCaster）。</summary>
        ShadowCaster = 1,

        /// <summary>仅深度 pass（LightMode = DepthOnly）。</summary>
        DepthOnly = 2,

        /// <summary>深度 + 法线 pass（LightMode = DepthNormal）。</summary>
        DepthNormal = 3,
    }

    /// <summary>
    /// 着色阶段。锚点插入位置按顶点 / 片元分别声明。
    /// </summary>
    public enum HNRPShaderStage
    {
        /// <summary>顶点着色阶段。</summary>
        Vertex = 0,

        /// <summary>片元着色阶段。</summary>
        Fragment = 1,
    }

    /// <summary>
    /// 锚点在阶段内的具体位置。<see cref="Pass"/> + <see cref="Stage"/> + <see cref="Position"/>
    /// 构成插入点三元组；转大写后拼接得到宏 / 函数名后缀 token。
    /// </summary>
    public enum HNRPAnchorPosition
    {
        /// <summary>阶段起始（任何数据构建之前）。</summary>
        Begin = 0,

        /// <summary>顶点输入构建之后。</summary>
        AfterVertexInput = 1,

        /// <summary>顶点阶段输出结构（varyings / 顶点变换结果）构建之后。</summary>
        AfterVertex = 2,

        /// <summary>LitVaryings 构建之后（顶点阶段）。</summary>
        AfterVaryings = 3,

        /// <summary>拆包 varyings 之后（片元阶段）。</summary>
        AfterUnpackVaryings = 4,

        /// <summary>表面数据（LitSurfaceData）构建之后。</summary>
        AfterSurfaceData = 5,

        /// <summary>BRDF 数据构建之后。</summary>
        AfterBRDFData = 6,

        /// <summary>光照输入数据构建之后。</summary>
        AfterLightingInput = 7,

        /// <summary>光照求值之后。</summary>
        AfterLighting = 8,

        /// <summary>输出数据构建之后。</summary>
        AfterOutput = 9,

        /// <summary>阶段结束（返回之前）。</summary>
        End = 10,
    }
}
