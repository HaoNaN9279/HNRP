using UnityEngine;

namespace HN.HNRP
{
    /// <summary>
    /// 按 light 的阴影元数据（供 GPU 两级查表的第一级）。
    /// 布局须与 shader 侧一致。
    /// </summary>
    public struct ShadowLightData
    {
        /// <summary>该 light 在可见光列表中的索引（= buffer 下标）。</summary>
        public int lightIndex;

        /// <summary>该 light 单张 map 的分辨率；0 表示该 light 无阴影。</summary>
        public int resolution;

        /// <summary>map 0..3 的 atlas 位置，每张 8 位 = (slice &lt;&lt; 6) | blockId。</summary>
        public uint blockDatas0;

        /// <summary>map 4..7 的 atlas 位置。</summary>
        public uint blockDatas1;

        /// <summary>
        /// 打包的光源类型 / cascade 级数 / 方向光槽位：
        /// bit0..7=lightType，bit8..15=cascadeCount
        /// </summary>
        public uint typeAndCascadeCount;

        /// <summary>
        /// 方向光在 <c>_ShadowCameraDatas</c> 中的下标；非方向光无意义。
        /// </summary>
        public int cameraDataIndex;
    }

    /// <summary>
    /// 每方向光阴影参数（按方向光槽位索引）：方向光 cascade 各级的远边界（沿相机视轴深度）。
    /// 布局须与 shader 侧一致。
    /// </summary>
    public struct ShadowCameraData
    {
        /// <summary>cascade 0..3 的远边界（沿相机视轴的深度；0 表示无该级）。</summary>
        public Vector4 cascadeSplits0;

        /// <summary>cascade 4..7 的远边界（沿相机视轴的深度；0 表示无该级）。</summary>
        public Vector4 cascadeSplits1;
    }

    /// <summary>
    /// atlas 槽的阴影数据（供 GPU 两级查表的第二级）。buffer 下标atlas 位置字段
    /// 布局须与 shader 侧一致
    /// </summary>
    public struct ShadowMapData
    {
        /// <summary>x=strength, y=soft, z=cascadeIndex/faceIndex, w=预留。</summary>
        public Vector4 shadowParams;

        /// <summary>世界 → 阴影裁剪矩阵（含 [0,1] remap，不含 atlas offset）。</summary>
        public Matrix4x4 worldToShadow;
    }

    /// <summary>
    /// 阴影全局标量参数常量缓冲。字段布局须与 shader 侧 <c>_ShadowMapParamsBuffer</c> 一致。
    /// </summary>
    /// <remarks>
    /// GPU 常量缓冲要求绑定的字节数为 16 的倍数（<c>ComputeBufferType.Constant</c> 的
    /// stride 同样受此约束），故标量后补齐 3 个 float，合计 16 字节。
    /// 未来新增标量全局参数时占用 padding；新增行时需保持总量为 16 的倍数。
    /// </remarks>
    public struct ShadowGlobalParams
    {
        /// <summary>atlas 单 slice 分辨率。</summary>
        public float _ShadowSliceResolution;

        /// <summary>对齐占位（无数据语义）。</summary>
        public float padding0;

        /// <summary>对齐占位（无数据语义）。</summary>
        public float padding1;

        /// <summary>对齐占位（无数据语义）。</summary>
        public float padding2;
    }

    /// <summary>
    /// 光源视图投影矩阵常量缓冲。字段布局须与 shader 侧 <c>_ShadowViewProjBuffer</c> 一致。
    /// </summary>
    /// <remarks>
    /// 更新频率高于 <see cref="ShadowGlobalParams"/>（每张阴影 map 一次），故独立成缓冲。
    /// 单个 4x4 矩阵 = 64 字节，天然满足 16 字节对齐。
    /// </remarks>
    public struct ShadowViewProjParams
    {
        /// <summary>世界 → 光源裁剪矩阵（阴影投射 pass 用）。</summary>
        public Matrix4x4 _ShadowViewProj;
    }
}
