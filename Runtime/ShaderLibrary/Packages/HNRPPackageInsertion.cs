using System;

namespace HN.HNRP
{
    /// <summary>
    /// 包的插入点：<see cref="Substrate"/> + <see cref="Pass"/> + <see cref="Stage"/> +
    /// <see cref="Position"/> 四元组。生成器据此为该锚点生成包函数调用，并把同锚点的多个包
    /// 聚合为一条语句。
    /// </summary>
    [Serializable]
    public sealed class HNRPPackageInsertion
    {
        /// <summary>归属基底（该包只为其支持的基底声明插入点）。</summary>
        public HNRPSubstrateId Substrate = HNRPSubstrateId.Lit;

        /// <summary>目标 pass。</summary>
        public HNRPPassKind Pass = HNRPPassKind.Forward;

        /// <summary>目标着色阶段。</summary>
        public HNRPShaderStage Stage = HNRPShaderStage.Fragment;

        /// <summary>阶段内的插入位置。</summary>
        public HNRPAnchorPosition Position = HNRPAnchorPosition.AfterSurfaceData;
    }
}
