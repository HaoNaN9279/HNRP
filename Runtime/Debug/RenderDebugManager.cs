// <copyright file="RenderDebugManager.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    /// <summary>
    /// 渲染调试的中央管理器：持有跨帧的 GPU 资源（单值缓冲、标签缓冲、
    /// 字模图集、颜色渐变 LUT）与单值注册表，并把相机级配置解析为帧级
    /// <see cref="RenderDebugState"/>。
    /// </summary>
    /// <remarks>
    /// <para><b>零回读原则：</b>数值与文本格式化全部在 GPU 完成；CPU 只提供
    /// 「显示什么」的元信息（标签、类型、格式）并上传一次数据，不做任何
    /// <c>AsyncGPUReadback</c>。</para>
    /// <para><b>零成本原则：</b>调试未启用时不做任何 GPU 绑定，也不产生显示 pass；
    /// 缓冲与纹理惰性创建，首次启用后才分配。</para>
    /// <para><b>两种数值来源：</b></para>
    /// <list type="bullet">
    ///   <item><b>CPU 拥有</b>（<see cref="SetValue"/>）—— 每帧由 C# 上传，适合
    ///     逻辑层关心的数值。</item>
    ///   <item><b>GPU 拥有</b>（<see cref="RegisterShaderValue"/>）—— CPU 只上传
    ///     标签与类型，值由 shader / compute 经 <c>_HNRPDebugValuesRW</c> 直接写入，
    ///     适合中间值（如光栅计数、聚类结果）。上传按条目做部分更新，不会覆盖
    ///     GPU 写入的值。</item>
    /// </list>
    /// </remarks>
    public static class RenderDebugManager
    {
        /// <summary>单值通道支持的最大注册条目数。</summary>
        public const int MaxEntries = 32;

        /// <summary>
        /// GPU 缓冲支持的最大显示行数。
        /// </summary>
        /// <remarks>
        /// 显示行 ≠ 注册条目：分组路径的每一级标题各占一行，
        /// 因此行数上限要显著大于条目上限（32 条目 × 最多 3 级分组 + 32 值 ≈ 128）。
        /// </remarks>
        public const int MaxRows = 128;

        /// <summary>单值注册表（跨帧保留）。</summary>
        private static readonly List<DebugDisplayEntry> entries = new();

        /// <summary>标签 → 注册表下标的索引（大小写敏感）。</summary>
        private static readonly Dictionary<string, int> entryIndices =
            new(StringComparer.Ordinal);

        /// <summary>各槽位的值是否需要重新上传。</summary>
        private static readonly bool[] valueDirty = new bool[MaxEntries];

        /// <summary>各条目的标签 / 头部是否需要重新上传。</summary>
        private static readonly bool[] headerDirty = new bool[MaxEntries];

        /// <summary>槽位布局（哪些条目可见 / 顺序 / 分组标题）是否变化，变化时下一次上传整体重传。</summary>
        private static bool layoutDirty = true;

        /// <summary>显示行计划是否需要重建。</summary>
        private static bool rowPlanDirty = true;

        // ── 显示行计划（由可见条目 + 分组标题推导，仅在布局变化时重建） ──

        /// <summary>每行对应的条目下标；分组标题行为 -1。</summary>
        private static readonly int[] rowEntryIndex = new int[MaxRows];

        /// <summary>每行的显示文本（分组名或条目叶子名）。</summary>
        private static readonly string[] rowText = new string[MaxRows];

        /// <summary>每行的缩进层级。</summary>
        private static readonly int[] rowIndent = new int[MaxRows];

        /// <summary>当前显示行数。</summary>
        private static int rowCount;

        /// <summary>排序用的可见条目下标暂存（复用，避免布局重建时分配）。</summary>
        private static readonly List<int> visibleOrderScratch = new();


        // ── GPU 资源 ──

        private static GraphicsBuffer valuesBuffer;
        private static GraphicsBuffer stringsBuffer;
        private static uint[] valuesStaging;
        private static uint[] stringsStaging;
        private static Texture2D colorMapLut;
        private static int colorMapSignature = int.MinValue;
        private static Texture fontAtlas;

        /// <summary>当前管线资源引用（用于惰性取字模图集）。</summary>
        private static HNRenderPipelineRuntimeResources runtimeResources;

        /// <summary>
        /// 全局运行时调试配置（进程级，所有相机共享）。
        /// </summary>
        /// <remarks>
        /// 刻意不做成相机级：调试显示是「看渲染结果」的手段，一次开关即应对全部相机
        /// 生效。相机维度的差异只保留 SceneView 相机的独立开关。
        /// </remarks>
        public static RenderDebugSettings Settings { get; set; } = RenderDebugSettings.Default;

        /// <summary>
        /// 以「读改写」方式修改全局配置，避免调用方手写样板。
        /// </summary>
        /// <param name="mutator">就地修改委托。</param>
        public static void MutateSettings(SettingsMutator mutator)
        {
            if (mutator == null)
            {
                return;
            }

            RenderDebugSettings settings = Settings;
            mutator(ref settings);
            Settings = settings;
        }

        /// <summary>全局配置的就地修改委托。</summary>
        /// <param name="settings">待修改的配置。</param>
        public delegate void SettingsMutator(ref RenderDebugSettings settings);

        /// <summary>已注册的单值条目总数（含被隐藏的）。</summary>
        public static int EntryCount => entries.Count;

        /// <summary>
        /// 当前参与显示的条目数（即显示 pass 的行数）。
        /// </summary>
        /// <remarks>
        /// 实时计算而非缓存：可见开关可能在任意时刻被 Editor 面板改动，
        /// 缓存值只在下一次 <see cref="UploadFrameData"/> 时才更新，会读到过期数字。
        /// 条目上限仅 <see cref="MaxEntries"/>，循环代价可忽略。
        /// </remarks>
        public static int VisibleEntryCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].Visible)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>
        /// 当前显示行数（可见值行 + 分组标题行）。显示 pass 的行循环上限。
        /// </summary>
        public static int RowCount
        {
            get
            {
                EnsureRowPlan();
                return rowCount;
            }
        }

        /// <summary>单值缓冲（<c>_HNRPDebugValues</c>）；未初始化时为 <c>null</c>。</summary>
        public static GraphicsBuffer ValuesBuffer => valuesBuffer;

        /// <summary>标签缓冲（<c>_HNRPDebugStrings</c>）；未初始化时为 <c>null</c>。</summary>
        public static GraphicsBuffer StringsBuffer => stringsBuffer;

        /// <summary>颜色渐变 LUT；未初始化时为 <c>null</c>。</summary>
        public static Texture ColorMapLut => colorMapLut;

        /// <summary>字模图集；未初始化时为 <c>null</c>。</summary>
        public static Texture FontAtlas => fontAtlas;

        // ── 单值注册表 ──

        /// <summary>
        /// 取指定下标的注册条目。
        /// </summary>
        /// <param name="index">下标。</param>
        /// <returns>条目；越界时返回 <c>null</c>。</returns>
        public static DebugDisplayEntry GetEntry(int index)
        {
            return (index >= 0 && index < entries.Count) ? entries[index] : null;
        }

        /// <summary>
        /// 遍历当前全部条目（只读，供 Editor 面板使用）。
        /// </summary>
        /// <returns>条目只读列表。</returns>
        public static IReadOnlyList<DebugDisplayEntry> GetEntries()
        {
            return entries;
        }

        /// <summary>
        /// 注册或更新一个 CPU 拥有的单值条目。
        /// </summary>
        /// <param name="group">
        /// 分组路径（<c>"/"</c> 分隔，可多级，如 <c>"Animation/Value"</c>）；空串表示不分组。
        /// </param>
        /// <param name="name">条目名称（显示时的叶子名）；<c>null</c>/空时不注册。</param>
        /// <param name="type">值类型。</param>
        /// <param name="value">数值载荷。</param>
        /// <param name="decimalDigits">浮点显示位数。</param>
        /// <param name="source">注册点（调用方源文件名）。</param>
        /// <returns>注册成功返回 <c>true</c>；已达容量上限且为新条目时返回 <c>false</c>。</returns>
        public static bool SetValue(
            string group,
            string name,
            DebugValueType type,
            Vector4 value,
            int decimalDigits,
            string source = null)
        {
            int index = EnsureEntry(group, name, type, decimalDigits, source);
            if (index < 0)
            {
                return false;
            }

            entries[index].Type = type;
            entries[index].Value = value;
            entries[index].DecimalDigits = Mathf.Clamp(decimalDigits, 0, 15);
            valueDirty[index] = true;
            headerDirty[index] = true;
            return true;
        }

        /// <summary>
        /// 设置单个条目的显示开关。
        /// </summary>
        /// <param name="label">注册唯一键（<c>"Group/Name"</c> 或 <c>"Name"</c>）。</param>
        /// <param name="visible">是否显示。</param>
        /// <returns>成功修改返回 <c>true</c>；条目不存在时返回 <c>false</c>。</returns>
        public static bool SetVisible(string label, bool visible)
        {
            int index = GetSlot(label);
            if (index < 0)
            {
                return false;
            }

            if (entries[index].Visible == visible)
            {
                return true;
            }

            entries[index].Visible = visible;
            MarkLayoutDirty();
            return true;
        }

        /// <summary>
        /// 设置某个注册点下全部条目的显示开关。
        /// </summary>
        /// <param name="source">注册点（调用方源文件名）。</param>
        /// <param name="visible">是否显示。</param>
        /// <returns>实际被修改的条目数。</returns>
        public static int SetSourceVisible(string source, bool visible)
        {
            if (string.IsNullOrEmpty(source))
            {
                return 0;
            }

            int changed = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Source != source || entries[i].Visible == visible)
                {
                    continue;
                }

                entries[i].Visible = visible;
                changed++;
            }

            if (changed > 0)
            {
                MarkLayoutDirty();
            }

            return changed;
        }

        /// <summary>
        /// 设置某个<b>分组</b>（含其子分组）下全部条目的显示开关。
        /// </summary>
        /// <remarks>
        /// 匹配规则：条目分组路径等于 <paramref name="groupPath"/> 或以其加 <c>"/"</c> 为前缀。
        /// </remarks>
        /// <param name="groupPath">分组路径（如 <c>"Animation"</c> 或 <c>"Animation/Value"</c>）。</param>
        /// <param name="visible">是否显示。</param>
        /// <returns>实际被修改的条目数。</returns>
        public static int SetGroupVisible(string groupPath, bool visible)
        {
            string prefix = (groupPath ?? string.Empty).Trim('/');
            string withSlash = prefix.Length == 0 ? string.Empty : prefix + "/";

            int changed = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                string group = entries[i].Group ?? string.Empty;
                bool inGroup = prefix.Length == 0
                    || group == prefix
                    || group.StartsWith(withSlash, System.StringComparison.Ordinal);

                if (!inGroup || entries[i].Visible == visible)
                {
                    continue;
                }

                entries[i].Visible = visible;
                changed++;
            }

            if (changed > 0)
            {
                MarkLayoutDirty();
            }

            return changed;
        }

        /// <summary>
        /// 按标签取槽位下标。
        /// </summary>
        /// <param name="label">显示标签。</param>
        /// <returns>槽位下标；不存在时返回 -1。</returns>
        public static int GetSlot(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return -1;
            }

            return entryIndices.TryGetValue(label, out int index) ? index : -1;
        }

        /// <summary>
        /// 修改已注册条目的浮点显示位数。
        /// </summary>
        /// <param name="label">标签。</param>
        /// <param name="decimalDigits">显示位数（0..15）。</param>
        /// <returns>条目存在并更新时返回 <c>true</c>。</returns>
        public static bool SetFormat(string label, int decimalDigits)
        {
            int index = GetSlot(label);
            if (index < 0)
            {
                return false;
            }

            entries[index].DecimalDigits = Mathf.Clamp(decimalDigits, 0, 15);
            headerDirty[index] = true;
            return true;
        }

        /// <summary>
        /// 移除指定标签的条目。
        /// </summary>
        /// <param name="label">要移除的标签。</param>
        /// <returns>存在并移除时返回 <c>true</c>。</returns>
        public static bool Clear(string label)
        {
            int index = GetSlot(label);
            if (index < 0)
            {
                return false;
            }

            entries.RemoveAt(index);

            // 条目被移除后，其后所有槽位的数据都要前移重传。
            for (int i = index; i < entries.Count; i++)
            {
                headerDirty[i] = true;
                valueDirty[i] = true;
            }

            RebuildIndex();
            MarkLayoutDirty();
            return true;
        }

        /// <summary>
        /// 通知槽位布局已变化（由 Editor 直接改写条目可见性后调用）。
        /// </summary>
        public static void MarkLayoutDirty()
        {
            layoutDirty = true;
            rowPlanDirty = true;
        }

        // ── 资源生命周期 ──

        /// <summary>
        /// 确保 GPU 资源可用。资源缺失或失效时重建。
        /// </summary>
        /// <param name="resources">管线运行时资源；可为 <c>null</c>（沿用上次引用）。</param>
        /// <returns>资源齐备时返回 <c>true</c>。</returns>
        public static bool EnsureResources(HNRenderPipelineRuntimeResources resources)
        {
            if (resources != null)
            {
                runtimeResources = resources;
            }

            if (valuesBuffer == null || !valuesBuffer.IsValid())
            {
                valuesBuffer = new GraphicsBuffer(
                    GraphicsBuffer.Target.Structured, MaxRows, DebugValueCodec.EntryStride)
                {
                    name = "HNRPDebugValues",
                };

                // 缓冲重建后全部槽位都需重新上传。
                for (int i = 0; i < MaxEntries; i++)
                {
                    valueDirty[i] = true;
                    headerDirty[i] = true;
                }
            }

            if (stringsBuffer == null || !stringsBuffer.IsValid())
            {
                stringsBuffer = new GraphicsBuffer(
                    GraphicsBuffer.Target.Structured,
                    MaxRows * DebugStringCodec.UintsPerLabel,
                    sizeof(uint))
                {
                    name = "HNRPDebugStrings",
                };

                // 标签缓冲重建：所有条目的标签都要重传。
                for (int i = 0; i < MaxEntries; i++)
                {
                    headerDirty[i] = true;
                }
            }

            if (valuesStaging == null || valuesStaging.Length != MaxRows * DebugValueCodec.UintsPerEntry)
            {
                valuesStaging = new uint[MaxRows * DebugValueCodec.UintsPerEntry];

                for (int i = 0; i < MaxEntries; i++)
                {
                    headerDirty[i] = true;
                    valueDirty[i] = true;
                }
            }

            if (stringsStaging == null || stringsStaging.Length != MaxRows * DebugStringCodec.UintsPerLabel)
            {
                stringsStaging = new uint[MaxRows * DebugStringCodec.UintsPerLabel];
            }

            EnsureFontAtlas();
            EnsureColorMap();

            return valuesBuffer != null && stringsBuffer != null && fontAtlas != null;
        }

        /// <summary>
        /// 把注册表中发生变化的条目上传到 GPU。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 逐条目部分上传（<c>SetData</c> 带区间重载），因此 GPU 独占槽位
        /// （<see cref="RegisterShaderValue"/>）的值不会被 CPU 数据覆盖。
        /// </para>
        /// <para>
        /// 槽位按<b>可见</b>条目紧凑排布：被隐藏的条目完全不占显示行，
        /// 显示 pass 只需遍历 <c>[0, RowCount)</c>（行 = 可见值 + 分组标题）。
        /// </para>
        /// </remarks>
        public static void UploadFrameData()
        {
            if (valuesBuffer == null || stringsBuffer == null)
            {
                return;
            }

            bool fullUpload = layoutDirty;
            if (fullUpload)
            {
                // 可见集合 / 顺序 / 分组结构变化会让所有行前移，必须整体重传。
                for (int i = 0; i < entries.Count; i++)
                {
                    headerDirty[i] = true;
                    valueDirty[i] = true;
                }

                layoutDirty = false;
            }

            EnsureRowPlan();

            for (int row = 0; row < rowCount; row++)
            {
                int entryIndex = rowEntryIndex[row];

                if (entryIndex < 0)
                {
                    // 分组标题行：只在布局变化时重写（其余时候内容不变）。
                    if (fullUpload)
                    {
                        WriteRow(
                            row, DebugValueType.None, rowText[row], 0, Vector4.zero,
                            rowIndent[row], isGroupHeader: true);
                    }

                    continue;
                }

                if (!fullUpload && !headerDirty[entryIndex] && !valueDirty[entryIndex])
                {
                    continue;
                }

                DebugDisplayEntry entry = entries[entryIndex];
                WriteRow(
                    row, entry.Type, entry.Name, entry.DecimalDigits, entry.Value,
                    rowIndent[row], isGroupHeader: false);

                headerDirty[entryIndex] = false;
                valueDirty[entryIndex] = false;
            }
        }

        /// <summary>
        /// 写一行到 GPU 缓冲。
        /// </summary>
        /// <param name="row">行号。</param>
        /// <param name="type">值类型；分组标题行用 <see cref="DebugValueType.None"/>。</param>
        /// <param name="text">本行显示文本（分组名或条目叶子名）。</param>
        /// <param name="decimalDigits">浮点显示位数。</param>
        /// <param name="value">数值载荷。</param>
        /// <param name="indent">缩进层级。</param>
        /// <param name="isGroupHeader">是否为分组标题行。</param>
        private static void WriteRow(
            int row,
            DebugValueType type,
            string text,
            int decimalDigits,
            Vector4 value,
            int indent,
            bool isGroupHeader)
        {
            int offset = row * DebugValueCodec.UintsPerEntry;
            int labelOffset = row * DebugStringCodec.UintsPerLabel;

            int charCount = DebugStringCodec.GetCharCount(text);
            valuesStaging[offset] = DebugValueCodec.PackHeader(
                type, charCount, decimalDigits, indent, isGroupHeader);
            valuesStaging[offset + 1] = 0;

            uint[] packed = DebugValueCodec.PackValue(type, value);
            valuesStaging[offset + 2] = packed[0];
            valuesStaging[offset + 3] = packed[1];
            valuesStaging[offset + 4] = packed[2];
            valuesStaging[offset + 5] = packed[3];

            valuesBuffer.SetData(valuesStaging, offset, offset, DebugValueCodec.UintsPerEntry);

            DebugStringCodec.Encode(text, stringsStaging, labelOffset);
            stringsBuffer.SetData(
                stringsStaging, labelOffset, labelOffset, DebugStringCodec.UintsPerLabel);
        }

        /// <summary>
        /// 重建显示行计划：可见条目按分组路径字典序排列，并为每一级分组插入标题行。
        /// </summary>
        /// <remarks>
        /// 「从上往下按分组排列」由路径字典序天然得到 —— <c>"/"</c>（0x2F）小于所有
        /// 大写字母，因此未分组的条目排在所有分组之前。
        /// </remarks>
        private static void EnsureRowPlan()
        {
            if (!rowPlanDirty)
            {
                return;
            }

            rowPlanDirty = false;
            rowCount = 0;

            visibleOrderScratch.Clear();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Visible)
                {
                    visibleOrderScratch.Add(i);
                }
            }

            visibleOrderScratch.Sort(CompareByLabel);

            string[] previousSegments = System.Array.Empty<string>();

            for (int k = 0; k < visibleOrderScratch.Count && rowCount < MaxRows; k++)
            {
                int entryIndex = visibleOrderScratch[k];
                DebugDisplayEntry entry = entries[entryIndex];
                string[] segments = entry.GroupSegments;

                // 与上一条目的分组路径比较，只为「新出现 / 变化的层级」发标题行。
                int common = 0;
                int maxCommon = Mathf.Min(previousSegments.Length, segments.Length);
                while (common < maxCommon && previousSegments[common] == segments[common])
                {
                    common++;
                }

                for (int level = common; level < segments.Length && rowCount < MaxRows; level++)
                {
                    rowEntryIndex[rowCount] = -1;
                    rowText[rowCount] = segments[level];
                    rowIndent[rowCount] = level;
                    rowCount++;
                }

                if (rowCount >= MaxRows)
                {
                    break;
                }

                rowEntryIndex[rowCount] = entryIndex;
                rowText[rowCount] = entry.Name;
                rowIndent[rowCount] = segments.Length;
                rowCount++;

                previousSegments = segments;
            }
        }

        /// <summary>按注册唯一键字典序比较条目（用于稳定的分组排列）。</summary>
        private static int CompareByLabel(int a, int b)
        {
            return string.CompareOrdinal(entries[a].Label, entries[b].Label);
        }

        /// <summary>
        /// 按配置重建颜色渐变 LUT（配置未变化时为空操作）。
        /// </summary>
        /// <param name="settings">颜色映射配置。</param>
        public static void UpdateColorMap(in DebugColorMapSettings settings)
        {
            int signature = settings.ComputeSignature();
            if (colorMapLut != null && signature == colorMapSignature)
            {
                return;
            }

            if (colorMapLut == null || !colorMapLut)
            {
                colorMapLut = DebugColorMap.CreateLut(settings);
            }
            else
            {
                DebugColorMap.UpdateLut(colorMapLut, settings);
            }

            colorMapSignature = signature;
        }

        /// <summary>
        /// 释放全部 GPU 资源。管线销毁时调用。
        /// </summary>
        public static void Dispose()
        {
            valuesBuffer?.Release();
            valuesBuffer = null;

            stringsBuffer?.Release();
            stringsBuffer = null;

            colorMapLut = null;
            fontAtlas = null;
            colorMapSignature = int.MinValue;
            runtimeResources = null;
        }

        // ── 帧级状态解析 ──

        /// <summary>
        /// 解析相机级配置，填充 <see cref="CameraContext.DebugState"/>，
        /// 并确保 GPU 资源与单值数据已就绪。
        /// </summary>
        /// <param name="context">当前帧的相机上下文。</param>
        public static void ResolveState(CameraContext context)
        {
            if (context == null || context.DebugState == null)
            {
                return;
            }

            RenderDebugState state = context.DebugState;
            state.Reset();

            RenderDebugSettings settings = Settings;
            if (!settings.Enabled || !IsRuntimeEnabled())
            {
                return;
            }

            Camera camera = context.Camera;
            if (camera == null)
            {
                return;
            }

            // 调试配置是进程级的：开关一开，运行时所有相机都显示叠层。
            // 仅两条例外：
            //   · SceneView 相机 —— 由独立开关控制（编辑时经常不希望叠层挡住视口）；
            //   · 反射 / 预览相机 —— 输出被其它系统消费（探针烘焙、材质预览），
            //     叠加调试信息会污染结果，永不参与。
            if (camera.cameraType == CameraType.Reflection || camera.cameraType == CameraType.Preview)
            {
                return;
            }

            if (camera.cameraType == CameraType.SceneView && !settings.AffectsSceneView)
            {
                return;
            }

            if (!EnsureResources(context.RuntimeResources))
            {
                return;
            }

            // 布局类参数（文本区位置 / 字号 / 预览边长 / mip / 色带越界）统一取
            // GlobalSettings 的默认值，不在浮动面板里暴露。
            RenderDebugDefaults defaults = GetDebugDefaults();

            state.Settings = settings;
            state.FlipY = !context.Flip;
            state.OverlayRect = defaults.OverlayRect;
            state.FontScale = Mathf.Max(1.0f, defaults.FontScale);
            state.PreviewSize = Mathf.Max(16f, defaults.PreviewSize);
            state.PreviewMip = Mathf.Max(0, defaults.PreviewMip);

            state.EntryCount = Mathf.Min(RowCount, MaxRows);
            state.SingleValuesActive = settings.DrawSingleValues && state.EntryCount > 0;
            state.PerPixelActive = settings.DrawPerPixel;
            state.PerPixelPassName = settings.PerPixelPassName;
            state.PerPixelChannelId = settings.PerPixelChannelId;

            if (settings.DrawPerPixel)
            {
                DebugColorMapSettings colorMap = settings.ColorMap;
                colorMap.Wrap = defaults.ColorMapWrap;
                UpdateColorMap(colorMap);
            }

            if (settings.DrawTexturePreview)
            {
                ResolvePreviewTexture(context);
            }

            UploadFrameData();
        }

        /// <summary>
        /// 取 GlobalSettings 上的调试默认值；全局设置缺失时回退到文档化默认。
        /// </summary>
        /// <returns>调试默认值。</returns>
        public static RenderDebugDefaults GetDebugDefaults()
        {
            HNRenderPipelineGlobalSettings globalSettings = HNRenderPipelineGlobalSettings.Instance;
            return globalSettings != null
                ? globalSettings.DebugDefaults
                : RenderDebugDefaults.CreateDefault();
        }

        /// <summary>
        /// 运行时（非 Editor）是否允许调试显示生效。
        /// </summary>
        /// <returns>允许时返回 <c>true</c>。</returns>
        public static bool IsRuntimeEnabled()
        {
#if UNITY_EDITOR
            return true;
#else
            HNRenderPipelineGlobalSettings settings = HNRenderPipelineGlobalSettings.Instance;
            return settings != null && settings.SupportRuntimeDebugDisplay;
#endif
        }

        // ── 全局绑定 ──

        /// <summary>
        /// 绑定单值文本显示所需的全部全局资源与 uniform。
        /// </summary>
        /// <param name="cmd">接收绑定命令的命令缓冲。</param>
        /// <param name="state">当前帧的调试状态。</param>
        /// <param name="screenWidth">目标宽度（像素）。</param>
        /// <param name="screenHeight">目标高度（像素）。</param>
        public static void BindTextGlobals(CommandBuffer cmd, RenderDebugState state, int screenWidth, int screenHeight)
        {
            if (cmd == null || state == null || valuesBuffer == null || stringsBuffer == null)
            {
                return;
            }

            cmd.SetGlobalBuffer(DebugPropertyIDs.Values, valuesBuffer);
            cmd.SetGlobalBuffer(DebugPropertyIDs.Strings, stringsBuffer);
            cmd.SetGlobalTexture(DebugPropertyIDs.Font, fontAtlas);
            cmd.SetGlobalInt(DebugPropertyIDs.EntryCount, state.EntryCount);

            float originX = state.OverlayRect.x * screenWidth;
            float originY = state.OverlayRect.y * screenHeight;

            // zw 分别携带 Y 翻转开关与目标高度，供 shader 把像素坐标换算到显示空间。
            cmd.SetGlobalVector(DebugPropertyIDs.TextOrigin, new Vector4(
                originX, originY, state.FlipY ? 1f : 0f, screenHeight));
            cmd.SetGlobalFloat(DebugPropertyIDs.FontScale, Mathf.Max(0.5f, state.FontScale));
            cmd.SetGlobalColor(DebugPropertyIDs.FontColor, Color.white);
        }

        /// <summary>
        /// 绑定每像素颜色映射所需的全局资源与 uniform（由生产者 pass 在绘制前调用）。
        /// </summary>
        /// <param name="cmd">接收绑定命令的命令缓冲。</param>
        /// <param name="settings">颜色映射配置。</param>
        /// <param name="channelId">选中的通道 ID。</param>
        public static void BindPerPixelGlobals(CommandBuffer cmd, in DebugColorMapSettings settings, int channelId)
        {
            if (cmd == null)
            {
                return;
            }

            if (colorMapLut == null || !colorMapLut)
            {
                UpdateColorMap(settings);
            }

            cmd.SetGlobalTexture(DebugPropertyIDs.ColorMap, colorMapLut);
            cmd.SetGlobalInt(DebugPropertyIDs.ChannelId, channelId);
            cmd.SetGlobalInt(DebugPropertyIDs.ColorChannel, (int)settings.Channel);
            cmd.SetGlobalVector(
                DebugPropertyIDs.ColorRange,
                new Vector4(settings.RangeMin, settings.RangeMax, 0f, 0f));
            cmd.SetGlobalFloat(DebugPropertyIDs.ColorWrap, settings.Wrap ? 1f : 0f);
            cmd.SetGlobalFloat(DebugPropertyIDs.ColorOpacity, Mathf.Clamp01(settings.Opacity));
            cmd.EnableShaderKeyword(GlobalKeywords.debugPerPixel);
        }

        /// <summary>
        /// 关闭每像素颜色映射的全局 keyword（生产者绘制完成后调用）。
        /// </summary>
        /// <param name="cmd">接收命令的命令缓冲。</param>
        public static void UnbindPerPixelGlobals(CommandBuffer cmd)
        {
            cmd?.DisableShaderKeyword(GlobalKeywords.debugPerPixel);
        }

        /// <summary>
        /// 绑定纹理预览所需的全局资源与 uniform。
        /// </summary>
        /// <param name="cmd">接收绑定命令的命令缓冲。</param>
        /// <param name="state">当前帧的调试状态。</param>
        /// <param name="screenWidth">目标宽度（像素）。</param>
        /// <param name="screenHeight">目标高度（像素）。</param>
        public static void BindPreviewGlobals(CommandBuffer cmd, RenderDebugState state, int screenWidth, int screenHeight)
        {
            if (cmd == null || state == null || state.PreviewTexture == null)
            {
                return;
            }

            float size = Mathf.Max(16f, state.PreviewSize);

            cmd.SetGlobalTexture(DebugPropertyIDs.Preview, state.PreviewTexture);
            cmd.SetGlobalFloat(DebugPropertyIDs.PreviewSlice, state.PreviewSlice);
            cmd.SetGlobalFloat(DebugPropertyIDs.PreviewMip, state.PreviewMip);
            cmd.SetGlobalFloat(DebugPropertyIDs.FlipY, state.FlipY ? 1f : 0f);
            cmd.SetGlobalVector(
                DebugPropertyIDs.PreviewRect,
                new Vector4(size, size, Mathf.Max(1f, screenWidth), Mathf.Max(1f, screenHeight)));
        }

        // ── 内部 ──

        /// <summary>
        /// 取或新建条目，返回其下标。
        /// </summary>
        private static int EnsureEntry(
            string group, string name, DebugValueType type, int decimalDigits, string source)
        {
            if (string.IsNullOrEmpty(name))
            {
                return -1;
            }

            string normalizedGroup = (group ?? string.Empty).Trim('/');
            string label = normalizedGroup.Length == 0 ? name : $"{normalizedGroup}/{name}";

            if (entryIndices.TryGetValue(label, out int index))
            {
                return index;
            }

            if (entries.Count >= MaxEntries)
            {
                Debug.LogWarning(
                    $"[HNRP] 渲染调试单值条目已达上限 {MaxEntries}，'{label}' 被忽略。");
                return -1;
            }

            index = entries.Count;
            entryIndices[label] = index;

            var entry = new DebugDisplayEntry(
                normalizedGroup, name, type, Vector4.zero, decimalDigits, source)
            {
                // 沿用上次会话在 GlobalSettings 里保存的显示开关（默认显示）。
                Visible = !IsHiddenInGlobalSettings(label),
            };

            entries.Add(entry);
            headerDirty[index] = true;
            valueDirty[index] = true;
            MarkLayoutDirty();
            return index;
        }

        /// <summary>
        /// 查询 GlobalSettings 里记录的「默认隐藏」单值集合。
        /// </summary>
        private static bool IsHiddenInGlobalSettings(string label)
        {
            HNRenderPipelineGlobalSettings globalSettings = HNRenderPipelineGlobalSettings.Instance;
            return globalSettings != null && globalSettings.HiddenDebugValues != null
                && globalSettings.HiddenDebugValues.Contains(label);
        }

        /// <summary>
        /// 解析纹理预览源（外部纹理优先，其次 pass 自描述预览）。
        /// </summary>
        /// <param name="context">当前帧的相机上下文。</param>
        private static void ResolvePreviewTexture(CameraContext context)
        {
            RenderDebugState state = context.DebugState;

            // 预览源只能是「pass 自描述的内部渲染目标」（阴影图集 / 反射探针图集）。
            // mip 级别由 GlobalSettings 的默认值决定，此处只接受解析出的 slice。
            if (previewResolver != null
                && previewResolver.TryResolvePreview(context, out Texture texture, out int slice, out _)
                && texture != null)
            {
                state.PreviewTexture = texture;
                state.PreviewSlice = slice;
                state.TexturePreviewActive = true;
            }
        }

        /// <summary>
        /// 由宿主（管线）注入的预览解析器。避免 RenderDebugManager 直接依赖 pass 列表。
        /// </summary>
        private static IDebugPreviewResolver previewResolver;

        /// <summary>
        /// 注册纹理预览解析器（由管线在初始化时调用）。
        /// </summary>
        /// <param name="resolver">解析器；<c>null</c> 表示清除。</param>
        public static void SetPreviewResolver(IDebugPreviewResolver resolver)
        {
            previewResolver = resolver;
        }

        /// <summary>
        /// 重建标签索引（删除条目后调用）。
        /// </summary>
        private static void RebuildIndex()
        {
            entryIndices.Clear();
            for (int i = 0; i < entries.Count; i++)
            {
                entryIndices[entries[i].Label] = i;
            }
        }

        /// <summary>
        /// 确保字模图集可用；首次使用时程序化生成内置 5×7 位图字模。
        /// </summary>
        private static void EnsureFontAtlas()
        {
            if (fontAtlas != null && fontAtlas)
            {
                return;
            }

            fontAtlas = DebugFontAtlas.CreateDefault();
        }

        /// <summary>
        /// 确保颜色渐变 LUT 已创建。
        /// </summary>
        private static void EnsureColorMap()
        {
            if (colorMapLut == null || !colorMapLut)
            {
                colorMapLut = DebugColorMap.CreateLut(DebugColorMapSettings.Default);
                colorMapSignature = DebugColorMapSettings.Default.ComputeSignature();
            }
        }
    }
}
