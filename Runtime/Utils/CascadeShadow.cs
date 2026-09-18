using System;
using System.Collections.Generic;
using UnityEngine;

namespace HN.HNRP
{
    /// <summary>
    /// cascade 级数，仅支持 1、2、4、8 四级。
    /// </summary>
    public enum CascadeCountType
    {
        One = 1,
        Two = 2,
        Four = 4,
        Eight = 8
    }

    /// <summary>
    /// 每级 cascade 的分辨率。
    /// </summary>
    public enum ResolutionType
    {
        Low = 512,
        Medium = 1024,
        High = 2048,
        Ultra = 4096
    }

    /// <summary>
    /// 阴影更新模式。
    /// </summary>
    public enum ShadowUpdateModeType
    {
        EveryFrame,
        OnDemand,
        Custom
    }

    /// <summary>
    /// 光源阴影模式：硬阴影直接比较深度；软阴影走 PCSS（VSSM 优化）。
    /// </summary>
    public enum LightShadowType
    {
        /// <summary>硬阴影。</summary>
        Hard = 0,

        /// <summary>软阴影。</summary>
        Soft = 1
    }

    /// <summary>
    /// 标记 <see cref="CascadeShadowSettings"/> 字段，交给 CascadeShadowDrawer 自定义绘制。
    /// </summary>
    public class CascadeShadowAttribute : PropertyAttribute
    {
    }

    /// <summary>
    /// 标记 <see cref="ShadowCameraSettings"/> 字段，交给 ShadowCameraDrawer 自定义绘制。
    /// </summary>
    public class ShadowCameraAttribute : PropertyAttribute
    {
    }

    /// <summary>
    /// 灯光侧阴影设置：单张阴影 map 的分辨率。
    /// 级联相关参数（级数、分割、更新模式）属于相机，见 <see cref="ShadowCameraSettings"/>。
    /// </summary>
    [Serializable]
    public struct CascadeShadowSettings
    {
        /// <summary>默认分辨率。</summary>
        public static CascadeShadowSettings Default
        {
            get
            {
                CascadeShadowSettings settings = new CascadeShadowSettings
                {
                    cascadeResolution = ResolutionType.Medium
                };
                settings.EnsureValid();
                return settings;
            }
        }

        [SerializeField]
        private ResolutionType cascadeResolution;

        /// <summary>每张阴影 map 的分辨率。</summary>
        public ResolutionType CascadeResolution
        {
            get => cascadeResolution;
            set => cascadeResolution = value;
        }

        /// <summary>
        /// 修正缺失或非法的数据，保证枚举取值合法。
        /// 用于兼容新增字段后的旧序列化数据与运行时新建组件。
        /// </summary>
        public void EnsureValid()
        {
            switch (cascadeResolution)
            {
                case ResolutionType.Low:
                case ResolutionType.Medium:
                case ResolutionType.High:
                case ResolutionType.Ultra:
                    break;

                default:
                    cascadeResolution = ResolutionType.Medium;
                    break;
            }
        }
    }

    /// <summary>
    /// 相机侧级联阴影设置：级数、各级远边界（相机视轴深度）、更新模式与更新帧间隔。
    /// </summary>
    [Serializable]
    public struct ShadowCameraSettings
    {
        /// <summary>cascade 最大级数，级联边界数组固定长度。</summary>
        public const int MaxCascadeCount = 8;

        /// <summary>
        /// 8 级 cascade 的分组交界节点数量（固定 3 个，把 8 级切成 4 组）。
        /// 仅 8 级时使用：分组只影响剔除体积的复用，不改变级数与选级。
        /// </summary>
        public const int GroupNodeCount = 3;

        /// <summary>分组数量上限（= 引擎方向光级联剔除上限 4）。</summary>
        public const int MaxGroupCount = GroupNodeCount + 1;

        /// <summary>默认的 8 级 cascade 远边界（沿相机视轴，单位米）。</summary>
        public static readonly float[] DefaultSplits =
        {
            1f, 4f, 10f, 30f, 50f, 100f, 300f, 1000f
        };

        /// <summary>
        /// 默认的 8 级分组交界（cascade 边界索引，升序）：每 2 级一组。
        /// </summary>
        public static readonly int[] DefaultSplitGroups =
        {
            2, 4, 6
        };

        /// <summary>默认的 8 级 cascade 更新帧间隔。</summary>
        public static readonly int[] DefaultTimeSlices =
        {
            0, 0, 2, 2, 4, 4, 8, 8
        };

        /// <summary>
        /// 默认的 8 级 cascade 更新相位帧。
        /// </summary>
        /// <remarks>
        /// 相位决定"间隔内的第几帧更新"，取值按级配置、最终对更新间隔取模。
        /// 默认值按 <see cref="DefaultTimeSlices"/> 的间隔错开同级间隔的两级
        /// （间隔 2 → 相位 0/1，间隔 4 → 0/2，间隔 8 → 0/4），
        /// 避免同间隔的多级落在同一帧集中重绘；用户可按需改为任意值。
        /// </remarks>
        public static readonly int[] DefaultPhaseFrames =
        {
            0, 0, 0, 1, 0, 2, 0, 4
        };

        [SerializeField]
        private CascadeCountType cascadeCount;

        [SerializeField]
        private List<float> cascadeSplits;

        [SerializeField]
        private List<int> cascadeSplitGroups;

        [SerializeField]
        private ShadowUpdateModeType shadowUpdateMode;

        [SerializeField]
        private List<int> cascadeTimeSlices;

        [SerializeField]
        private List<int> cascadePhaseFrames;

        /// <summary>
        /// 创建一个使用默认值的新实例。
        /// </summary>
        public static ShadowCameraSettings Default
        {
            get
            {
                ShadowCameraSettings settings = new ShadowCameraSettings
                {
                    cascadeCount = CascadeCountType.Four,
                    cascadeSplits = new List<float>(DefaultSplits),
                    cascadeSplitGroups = new List<int>(DefaultSplitGroups),
                    shadowUpdateMode = ShadowUpdateModeType.EveryFrame,
                    cascadeTimeSlices = new List<int>(DefaultTimeSlices),
                    cascadePhaseFrames = new List<int>(DefaultPhaseFrames)
                };
                settings.EnsureValid();
                return settings;
            }
        }

        /// <summary>当前 cascade 级数。</summary>
        public CascadeCountType CascadeCount
        {
            get => cascadeCount;
            set => cascadeCount = value;
        }

        /// <summary>最大 8 级 cascade 时每级 cascade 的远边界（相机视轴深度）。</summary>
        public List<float> CascadeSplits
        {
            get => cascadeSplits;
            set => cascadeSplits = value;
        }

        /// <summary>
        /// 8 级 cascade 的分组交界（cascade 边界索引，升序，长度 <see cref="GroupNodeCount"/>）。
        /// 仅当 <see cref="CascadeCount"/> 为 8 时生效；其余级数每级自成一组，忽略该值。
        /// </summary>
        public List<int> CascadeSplitGroups
        {
            get => cascadeSplitGroups;
            set => cascadeSplitGroups = value;
        }

        /// <summary>阴影更新模式。</summary>
        public ShadowUpdateModeType ShadowUpdateMode
        {
            get => shadowUpdateMode;
            set => shadowUpdateMode = value;
        }

        /// <summary>当更新模式为 Custom 时，每级 cascade 的更新帧间隔。</summary>
        public List<int> CascadeTimeSlices
        {
            get => cascadeTimeSlices;
            set => cascadeTimeSlices = value;
        }

        /// <summary>
        /// 当更新模式为 Custom 时，每级 cascade 的更新相位帧。
        /// 相位决定该级在更新间隔内的第几帧更新（对间隔取模），用于把同间隔的各级错开到不同帧。
        /// </summary>
        public List<int> CascadePhaseFrames
        {
            get => cascadePhaseFrames;
            set => cascadePhaseFrames = value;
        }

        /// <summary>
        /// 修正缺失或非法的数据，保证数组长度与取值合法。
        /// 用于兼容新增字段后的旧序列化数据与运行时新建组件。
        /// </summary>
        public void EnsureValid()
        {
            if (cascadeCount != CascadeCountType.One
                && cascadeCount != CascadeCountType.Two
                && cascadeCount != CascadeCountType.Four
                && cascadeCount != CascadeCountType.Eight)
            {
                cascadeCount = CascadeCountType.Four;
            }

            if (cascadeSplits == null)
            {
                cascadeSplits = new List<float>(DefaultSplits);
            }
            else
            {
                EnsureFloatList(cascadeSplits, DefaultSplits);
            }

            if (cascadeTimeSlices == null)
            {
                cascadeTimeSlices = new List<int>(DefaultTimeSlices);
            }
            else
            {
                EnsureIntList(cascadeTimeSlices, DefaultTimeSlices);
            }

            if (cascadePhaseFrames == null)
            {
                cascadePhaseFrames = new List<int>(DefaultPhaseFrames);
            }
            else
            {
                EnsureIntList(cascadePhaseFrames, DefaultPhaseFrames);
            }

            for (int i = 0; i < cascadePhaseFrames.Count; i++)
            {
                // 相位为负等效于按间隔取模后的其它相位，直接收敛到 0 避免歧义。
                if (cascadePhaseFrames[i] < 0)
                {
                    cascadePhaseFrames[i] = 0;
                }
            }

            if (cascadeSplitGroups == null)
            {
                cascadeSplitGroups = new List<int>(DefaultSplitGroups);
            }
            else
            {
                EnsureGroupList(cascadeSplitGroups);
            }
        }

        /// <summary>
        /// 修正 8 级分组交界：补齐长度、夹取到合法交界范围并保证严格递增。
        /// 严格递增是硬约束——分组节点重合会产生零宽组，向引擎传退化剔除区间。
        /// </summary>
        private static void EnsureGroupList(List<int> values)
        {
            for (int i = values.Count; i < GroupNodeCount; i++)
            {
                values.Add(DefaultSplitGroups[i < DefaultSplitGroups.Length ? i : DefaultSplitGroups.Length - 1]);
            }

            if (values.Count > GroupNodeCount)
            {
                values.RemoveRange(GroupNodeCount, values.Count - GroupNodeCount);
            }

            for (int i = 0; i < values.Count; i++)
            {
                values[i] = Mathf.Clamp(values[i], 1, MaxCascadeCount - 1);
            }

            values.Sort();

            // 先自左向右推开重复项，再从右向左压回合法上界，保证落在 [1, MaxCascadeCount-1] 内严格递增。
            for (int i = 1; i < values.Count; i++)
            {
                if (values[i] <= values[i - 1])
                {
                    values[i] = values[i - 1] + 1;
                }
            }

            for (int i = values.Count - 1; i > 0; i--)
            {
                if (values[i] > MaxCascadeCount - 1)
                {
                    values[i] = MaxCascadeCount - 1;
                }

                if (values[i - 1] >= values[i])
                {
                    values[i - 1] = values[i] - 1;
                }
            }

            if (values[0] < 1)
            {
                values[0] = 1;
            }
        }

        private static void EnsureFloatList(List<float> values, float[] defaults)
        {
            for (int i = values.Count; i < MaxCascadeCount; i++)
            {
                values.Add(defaults[i]);
            }

            if (values.Count > MaxCascadeCount)
            {
                values.RemoveRange(MaxCascadeCount, values.Count - MaxCascadeCount);
            }
        }

        private static void EnsureIntList(List<int> values, int[] defaults)
        {
            for (int i = values.Count; i < MaxCascadeCount; i++)
            {
                values.Add(defaults[i]);
            }

            if (values.Count > MaxCascadeCount)
            {
                values.RemoveRange(MaxCascadeCount, values.Count - MaxCascadeCount);
            }
        }
    }

    /// <summary>
    /// <see cref="ShadowCameraSettings"/> 的纯逻辑校验工具，便于单独测试。
    /// </summary>
    public static class CascadeShadowUtils
    {
        /// <summary>
        /// 判断给定级数是否支持指定分辨率：
        /// 4096 仅支持 1 级，2048 最大支持 4 级，1024 与 512 支持最大 8 级。
        /// </summary>
        public static bool IsResolutionSupported(CascadeCountType count, ResolutionType resolution)
        {
            switch (resolution)
            {
                case ResolutionType.Ultra:
                    return count == CascadeCountType.One;
                case ResolutionType.High:
                    return (int)count <= (int)CascadeCountType.Four;
                default:
                    return true;
            }
        }

        /// <summary>
        /// 把分辨率收敛到当前级数支持的最大分辨率。
        /// </summary>
        public static ResolutionType ClampResolution(CascadeCountType count, ResolutionType resolution)
        {
            if (IsResolutionSupported(count, resolution))
            {
                return resolution;
            }

            if (count == CascadeCountType.One)
            {
                return ResolutionType.Ultra;
            }

            if ((int)count <= (int)CascadeCountType.Four)
            {
                return ResolutionType.High;
            }

            return ResolutionType.Medium;
        }

        /// <summary>
        /// 判断 Custom 更新模式下某级 cascade 在当前帧是否应更新。
        /// </summary>
        /// <param name="frameCount">当前帧号。</param>
        /// <param name="interval">更新帧间隔；<c>&lt;= 0</c> 表示每帧更新。</param>
        /// <param name="phaseFrame">该级配置的相位帧，对 <paramref name="interval"/> 取模后作为帧内偏移。</param>
        /// <returns>本帧是否应更新该级。</returns>
        public static bool ShouldUpdateOnFrame(int frameCount, int interval, int phaseFrame)
        {
            if (interval <= 0)
            {
                return true;
            }

            int phase = phaseFrame % interval;
            if (phase < 0)
            {
                phase += interval;
            }

            return ((frameCount + phase) % interval) == 0;
        }
    }
}
