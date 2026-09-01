// <copyright file="RenderGraphAsset.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using UnityEngine;

namespace HN.HNRP
{
    /// <summary>
    /// Spherical harmonics evaluation mode used by lighting passes.
    /// </summary>
    public enum SHEvalMode
    {
        /// <summary>
        /// Evaluate SH per-vertex.
        /// </summary>
        PerVertex,

        /// <summary>
        /// Mixed per-vertex and per-pixel SH evaluation.
        /// </summary>
        Mixed,

        /// <summary>
        /// Evaluate SH per-pixel.
        /// </summary>
        PerPixel,
    }

    /// <summary>
    /// Serializable struct holding per-asset render graph settings.
    /// Mirrors the top-level settings that influence how passes execute.
    /// </summary>
    [Serializable]
    public struct RenderGraphSettings
    {
        /// <summary>
        /// Spherical harmonics evaluation mode used by lighting passes.
        /// </summary>
        public SHEvalMode SHEvalMode;

        /// <summary>
        /// When <c>true</c>, the render graph may allocate HDR render targets.
        /// When <c>false</c>, all targets are LDR.
        /// </summary>
        public bool AllowHDR;
    }

    /// <summary>
    /// Render graph template <see cref="ScriptableObject"/>.
    /// Represents a static pipeline graph blueprint — defines which passes exist,
    /// how their slots connect, and bundled render-graph-wide settings.
    /// The runtime counterpart is <c>CameraRenderer.passes</c> (a
    /// <see cref="List{Pass}"/> per camera).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Architecture note</b> (ADR-002, ADR-011):
    /// <see cref="RenderGraphAsset"/> is a static template (ScriptableObject on disk);
    /// <see cref="CameraRenderer"/> holds the runtime pass instances for each camera.
    /// </para>
    /// <para>
    /// Cameras reference <see cref="RenderGraphAsset"/> directly — either through
    /// <see cref="HNAdditionalCameraData.pipelineConfigOverride"/> or through the
    /// default render graph fields on <see cref="HNRenderPipelineAsset"/>
    /// (e.g. <c>DefaultGameRenderGraph</c>).
    /// </para>
    /// <para>
    /// All rendering resources are owned by the passes themselves: a pass consumes
    /// a connected input slot when available and allocates its own resource from
    /// its parameters otherwise (ADR-017). There is no separate resource node
    /// layer in the asset.
    /// </para>
    /// </remarks>
    public class RenderGraphAsset : ScriptableObject
    {
        [SerializeReference]
        private List<Pass> m_Passes = new();

        [SerializeField]
        private List<SlotConnection> m_Connections = new();

        [SerializeField]
        private RenderGraphSettings m_Settings;

        /// <summary>
        /// Gets the ordered list of pass templates in this graph.
        /// Each element is a concrete <see cref="Pass"/> instance holding its own
        /// serialized parameters.
        /// </summary>
        public List<Pass> Passes => m_Passes;

        /// <summary>
        /// Gets the ordered list of slot connections wiring passes together.
        /// </summary>
        public List<SlotConnection> Connections => m_Connections;

        /// <summary>
        /// Gets or sets the bundled render graph settings.
        /// </summary>
        public RenderGraphSettings Settings
        {
            get => m_Settings;
            set => m_Settings = value;
        }

        /// <summary>
        /// 用模板定义覆盖本资源的全部序列化内容（passes/connections/settings）。
        /// 由 <see cref="RenderGraphTemplates"/> 在创建/重置模板资源时调用。
        /// </summary>
        /// <param name="passes">pass 模板列表。</param>
        /// <param name="connections">slot 连接列表。</param>
        /// <param name="settings">渲染图设置。</param>
        public void SetDefinition(
            List<Pass> passes,
            List<SlotConnection> connections,
            RenderGraphSettings settings)
        {
            m_Passes = passes;
            m_Connections = connections;
            m_Settings = settings;
        }

        /// <summary>
        /// Builds the runtime <see cref="Pass"/> list from this asset's pass templates.
        /// Clones each template through <see cref="Pass.CreateRuntimeClone"/>,
        /// wires up slots via <see cref="SlotConnection"/>,
        /// and returns only passes whose <see cref="Pass.IsEnabled"/> is <c>true</c>.
        /// </summary>
        /// <param name="renderer">
        /// The camera renderer that will own the built passes.
        /// </param>
        /// <returns>
        /// A new <see cref="List{Pass}"/> containing all enabled passes,
        /// or an empty list if no templates are configured.
        /// </returns>
        public List<Pass> Build(object renderer)
        {
            var passMap = new Dictionary<string, Pass>();

            // ── Phase 1: Clone runtime passes from templates ──
            foreach (Pass template in m_Passes)
            {
                if (template == null)
                {
                    Debug.LogWarning("RenderGraphAsset.Build: Skipping null pass template.");
                    continue;
                }

                if (string.IsNullOrEmpty(template.PassName))
                {
                    Debug.LogWarning(
                        $"RenderGraphAsset.Build: Skipping pass template with null/empty PassName.");
                    continue;
                }

                Pass pass = template.CreateRuntimeClone();
                if (pass == null)
                {
                    Debug.LogWarning(
                        $"RenderGraphAsset.Build: Failed to clone pass template " +
                        $"'{template.GetType().Name}' instance '{template.PassName}'.");
                    continue;
                }

                // Declare the pass's slots once at build time so that Phase 2
                // (ConnectPassSlots) wires up the same slot instances that Record
                // will use each frame.
                pass.SetupSlots();

                passMap[template.PassName] = pass;
            }

            // ── Phase 2: Wire up slot connections ──
            foreach (SlotConnection conn in m_Connections)
            {
                if (!conn.IsValid())
                {
                    Debug.LogWarning(
                        $"RenderGraphAsset.Build: Skipping invalid SlotConnection " +
                        $"(SourcePass='{conn.SourcePass}', SourceSlot='{conn.SourceSlot}', " +
                        $"TargetPass='{conn.TargetPass}', TargetSlot='{conn.TargetSlot}').");
                    continue;
                }

                if (!passMap.TryGetValue(conn.SourcePass, out Pass sourcePass))
                {
                    Debug.LogWarning(
                        $"RenderGraphAsset.Build: SlotConnection references unknown SourcePass " +
                        $"'{conn.SourcePass}'.");
                    continue;
                }

                if (!passMap.TryGetValue(conn.TargetPass, out Pass targetPass))
                {
                    Debug.LogWarning(
                        $"RenderGraphAsset.Build: SlotConnection references unknown TargetPass " +
                        $"'{conn.TargetPass}'.");
                    continue;
                }

                ConnectPassSlots(sourcePass, conn.SourceSlot, targetPass, conn.TargetSlot);
            }

            // ── Phase 3: Topologically sort passes (dependency order) ──
            return TopologicalSort(passMap);
        }

        /// <summary>
        /// Topologically sorts all built passes so dependency edges are honored.
        /// Returns only enabled passes.
        /// </summary>
        /// <param name="passMap">
        /// Passes instantiated in <see cref="Build"/>, keyed by instance name.
        /// </param>
        /// <returns>
        /// The enabled passes in a stable topological order (definition insertion
        /// order as tie-breaker), or all enabled passes in definition order when a
        /// cycle is detected.
        /// </returns>
        /// <remarks>
        /// Dependencies come from <see cref="SlotConnection"/> edges: the source
        /// pass (producer of an output slot) is ordered before the target pass
        /// (consumer of the matching input slot).
        /// </remarks>
        private List<Pass> TopologicalSort(Dictionary<string, Pass> passMap)
        {
            // Stable base order: definition insertion order, restricted to passes
            // that were actually built (passMap may omit failed instantiations).
            var order = new List<Pass>(passMap.Count);
            var index = new Dictionary<Pass, int>();
            foreach (Pass template in m_Passes)
            {
                if (template == null || string.IsNullOrEmpty(template.PassName))
                {
                    continue;
                }

                if (passMap.TryGetValue(template.PassName, out Pass pass))
                {
                    index[pass] = order.Count;
                    order.Add(pass);
                }
            }

            int count = order.Count;
            var adjacency = new Dictionary<int, HashSet<int>>();
            var inDegree = new int[count];

            // ── Build dependency edges from SlotConnection entries ──
            foreach (SlotConnection conn in m_Connections)
            {
                if (!conn.IsValid())
                {
                    continue;
                }

                if (!passMap.TryGetValue(conn.SourcePass, out Pass source)
                    || !passMap.TryGetValue(conn.TargetPass, out Pass target)
                    || source == target)
                {
                    continue;
                }

                if (index.TryGetValue(source, out int sourceIndex)
                    && index.TryGetValue(target, out int targetIndex))
                {
                    AddEdge(adjacency, inDegree, sourceIndex, targetIndex);
                }
            }

            // ── Kahn's algorithm with stable (insertion-order) tie-breaking ──
            var result = new List<Pass>(count);
            var visited = new bool[count];
            bool progressed = true;
            while (progressed)
            {
                progressed = false;
                for (int i = 0; i < count; i++)
                {
                    if (visited[i] || inDegree[i] != 0)
                    {
                        continue;
                    }

                    visited[i] = true;
                    result.Add(order[i]);
                    progressed = true;

                    if (adjacency.TryGetValue(i, out HashSet<int> targets))
                    {
                        foreach (int target in targets)
                        {
                            if (!visited[target])
                            {
                                inDegree[target]--;
                            }
                        }
                    }

                    break;
                }
            }

            // ── Cycle detection: append remaining nodes so nothing is dropped ──
            if (result.Count < count)
            {
                Debug.LogWarning(
                    $"RenderGraphAsset.TopologicalSort: cycle detected in render graph " +
                    $"'{name}'. Appending remaining passes in definition order.");
                for (int i = 0; i < count; i++)
                {
                    if (!visited[i])
                    {
                        result.Add(order[i]);
                    }
                }
            }

            // ── Filter to enabled passes only ──
            var enabled = new List<Pass>(result.Count);
            foreach (Pass pass in result)
            {
                if (pass.IsEnabled)
                {
                    enabled.Add(pass);
                }
            }

            return enabled;
        }

        /// <summary>
        /// Adds a dependency edge from index <paramref name="from"/> to
        /// <paramref name="to"/>, deduplicating parallel edges.
        /// </summary>
        private static void AddEdge(
            Dictionary<int, HashSet<int>> adjacency,
            int[] inDegree,
            int from,
            int to)
        {
            if (!adjacency.TryGetValue(from, out HashSet<int> targets))
            {
                targets = new HashSet<int>();
                adjacency[from] = targets;
            }

            if (targets.Add(to))
            {
                inDegree[to]++;
            }
        }

        /// <summary>
        /// Wires an output slot of <paramref name="source"/> to an input slot of
        /// <paramref name="target"/> by name.
        /// </summary>
        /// <param name="source">The source pass whose output slot is being connected.</param>
        /// <param name="sourceSlot">The name of the output slot on the source pass.</param>
        /// <param name="target">The target pass whose input slot is being connected.</param>
        /// <param name="targetSlot">The name of the input slot on the target pass.</param>
        /// <remarks>
        /// Fails silently when directions don't match (e.g. legacy output→output
        /// definitions) or when either named slot is missing. Successful connections
        /// make the target input slot's <see cref="PassSlot.IsConnected"/> <c>true</c>
        /// so the target pass reads the source's resource handle during <see cref="Pass.Record"/>.
        /// </remarks>
        private static void ConnectPassSlots(
            Pass source,
            string sourceSlot,
            Pass target,
            string targetSlot)
        {
            source.TryConnect(sourceSlot, target, targetSlot);
        }
    }
}
