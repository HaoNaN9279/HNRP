using System.Collections.Generic;
using UnityEngine;

namespace HN.HNRP
{
    /// <summary>
    /// 一个 cascade 分组：覆盖 <c>[Start, End)</c> 区间的 cascade 级。
    /// 组内各级共享同一份剔除结果（引擎级 <c>ShadowSplitData</c>）。
    /// </summary>
    public readonly struct CascadeGroup
    {
        /// <summary>组内第一级 cascade 的索引（含）。</summary>
        public readonly int Start;

        /// <summary>组内最后一级 cascade 的索引 + 1（不含）。</summary>
        public readonly int End;

        /// <summary>
        /// 初始化 <see cref="CascadeGroup"/> 的新实例。
        /// </summary>
        /// <param name="start">组内第一级 cascade 的索引（含）。</param>
        /// <param name="end">组内最后一级 cascade 的索引 + 1（不含）。</param>
        public CascadeGroup(int start, int end)
        {
            Start = start;
            End = end;
        }

        /// <summary>组内的 cascade 级数。</summary>
        public int Count => End - Start;
    }

    /// <summary>
    /// cascade 分组纯逻辑：把级数切成"剔除结果共享组"，并生成引擎所需的归一化分割比例。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 引擎的方向光剔除接口 <c>ComputeDirectionalShadowMatricesAndCullingPrimitives</c>
    /// 只支持 1/2/4 级，因此 8 级必须合并为 ≤4 组才能逐组取得剔除结果。
    /// </para>
    /// <para>
    /// 分界节点在生成时即被夹取为严格递增，故组内级数恒 ≥1——不存在零宽组，
    /// 也就不会向引擎传退化的剔除区间。
    /// </para>
    /// </remarks>
    public static class CascadeShadowGrouping
    {
        /// <summary>
        /// 按级数与分组交界生成分组列表（复用外部列表，避免每帧分配）。
        /// </summary>
        /// <remarks>
        /// 级数 ≤ <see cref="ShadowCameraSettings.MaxGroupCount"/> 时每级自成一组
        /// （组数 = 级数，与引擎 1/2/4 级一一对应）。
        /// </remarks>
        /// <param name="count">cascade 级数。</param>
        /// <param name="nodes">8 级时的分组交界（cascade 边界索引）；其余级数忽略，可为 <c>null</c>。</param>
        /// <param name="groups">输出的分组列表，函数内清空并填充。</param>
        public static void BuildGroups(CascadeCountType count, List<int> nodes, List<CascadeGroup> groups)
        {
            if (groups == null)
            {
                return;
            }

            groups.Clear();

            int cascadeCount = Mathf.Clamp((int)count, 1, ShadowCameraSettings.MaxCascadeCount);
            if (cascadeCount <= ShadowCameraSettings.MaxGroupCount)
            {
                for (int i = 0; i < cascadeCount; i++)
                {
                    groups.Add(new CascadeGroup(i, i + 1));
                }

                return;
            }

            // 8 级：3 个交界节点切成 4 组。每个边界至少留 1 级给后续每组，
            // 保证严格递增且无空组。
            int previous = 0;
            for (int i = 0; i < ShadowCameraSettings.GroupNodeCount; i++)
            {
                int remainingGroups = ShadowCameraSettings.GroupNodeCount - i;
                int boundary = nodes != null && i < nodes.Count
                    ? nodes[i]
                    : ShadowCameraSettings.DefaultSplitGroups[
                        i < ShadowCameraSettings.DefaultSplitGroups.Length
                            ? i
                            : ShadowCameraSettings.DefaultSplitGroups.Length - 1];

                boundary = Mathf.Clamp(boundary, previous + 1, cascadeCount - remainingGroups);
                groups.Add(new CascadeGroup(previous, boundary));
                previous = boundary;
            }

            groups.Add(new CascadeGroup(previous, cascadeCount));
        }

        /// <summary>
        /// 生成引擎所需的归一化分割比例：<c>ratios[k]</c> 为第 k 组的远边界占 <paramref name="shadowDistance"/> 的比例。
        /// </summary>
        /// <remarks>
        /// 填法与 URP 对齐：1 组 <c>(1,0,0)</c>；2 组 <c>(b0,1,0)</c>；3 组 <c>(b0,b1,0)</c>；4 组 <c>(b0,b1,b2)</c>。
        /// 末组的远边界由引擎隐式取 <paramref name="shadowDistance"/>，故 4 组时第三分量之外无需填写。
        /// 分母必须与 Cull 期实际生效的 <c>shadowDistance</c> 一致，否则引擎算出的绝对边界与本项目 splits 错位。
        /// </remarks>
        /// <param name="splits">cascade 各级远边界（沿相机视轴，单位米）。</param>
        /// <param name="groups">分组列表。</param>
        /// <param name="shadowDistance">Cull 期实际生效的阴影距离（米）。</param>
        /// <returns>归一化的分割比例。</returns>
        public static Vector3 BuildSplitRatio(List<float> splits, List<CascadeGroup> groups, float shadowDistance)
        {
            Vector3 ratios = Vector3.zero;
            if (groups == null || groups.Count == 0 || shadowDistance <= 0f)
            {
                return ratios;
            }

            int groupCount = Mathf.Min(groups.Count, ShadowCameraSettings.MaxGroupCount);
            for (int i = 0; i < groupCount - 1; i++)
            {
                ratios[i] = GetGroupFarRatio(splits, groups[i], shadowDistance);
            }

            // 与 URP 的填法一致：单组与双组的末边界显式写 1。
            if (groupCount == 1)
            {
                ratios.x = 1f;
            }
            else if (groupCount == 2)
            {
                ratios.y = 1f;
            }

            return ratios;
        }

        /// <summary>
        /// 查找某级 cascade 所属的组下标；不属于任何组时返回 <c>-1</c>。
        /// </summary>
        /// <param name="groups">分组列表。</param>
        /// <param name="cascadeIndex">cascade 级索引。</param>
        /// <returns>组下标；越界时为 <c>-1</c>。</returns>
        public static int FindGroup(List<CascadeGroup> groups, int cascadeIndex)
        {
            if (groups == null)
            {
                return -1;
            }

            for (int i = 0; i < groups.Count; i++)
            {
                if (cascadeIndex >= groups[i].Start && cascadeIndex < groups[i].End)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 取某组远边界（该组最后一级 cascade 的远边界）占阴影距离的比例。
        /// </summary>
        private static float GetGroupFarRatio(List<float> splits, CascadeGroup group, float shadowDistance)
        {
            if (splits == null || splits.Count == 0)
            {
                return 0f;
            }

            int index = Mathf.Clamp(group.End - 1, 0, splits.Count - 1);
            return Mathf.Clamp01(splits[index] / shadowDistance);
        }
    }
}
