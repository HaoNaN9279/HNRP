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
    /// </remarks>
    public class RenderGraphAsset : ScriptableObject
    {
        [SerializeField]
        private List<PassDefinition> m_Passes = new();

        [SerializeField]
        private List<SlotConnection> m_Connections = new();

        [SerializeField]
        private List<ResourceDefinition> m_Resources = new();

        [SerializeField]
        private List<ResourceConnection> m_ResourceConnections = new();

        [SerializeField]
        private RenderGraphSettings m_Settings;

        /// <summary>
        /// Runtime resource nodes built from <see cref="m_Resources"/> during
        /// <see cref="Build"/>. Not serialized.
        /// </summary>
        private List<ResourceNode> m_RuntimeResources = new();

        /// <summary>
        /// Gets the ordered list of pass definitions in this graph.
        /// </summary>
        public List<PassDefinition> Passes => m_Passes;

        /// <summary>
        /// Gets the ordered list of slot connections wiring passes together.
        /// </summary>
        public List<SlotConnection> Connections => m_Connections;

        /// <summary>
        /// Gets the ordered list of resource definitions in this graph.
        /// </summary>
        public List<ResourceDefinition> Resources => m_Resources;

        /// <summary>
        /// Gets the ordered list of resource connections in this graph.
        /// </summary>
        public List<ResourceConnection> ResourceConnections => m_ResourceConnections;

        /// <summary>
        /// Gets the runtime resource nodes built during the last <see cref="Build"/>
        /// call. Consumers (e.g. <c>CameraRenderer</c>) resolve these each frame
        /// before passes record.
        /// </summary>
        public IReadOnlyList<ResourceNode> ResourceNodes => m_RuntimeResources;

        /// <summary>
        /// Gets or sets the bundled render graph settings.
        /// </summary>
        public RenderGraphSettings Settings
        {
            get => m_Settings;
            set => m_Settings = value;
        }

        /// <summary>
        /// Builds the runtime <see cref="Pass"/> list from this asset's definitions.
        /// Uses <see cref="PassRegistry"/> to resolve pass types by name,
        /// instantiates each pass, wires up slots via <see cref="SlotConnection"/>,
        /// and returns only passes whose <see cref="Pass.IsEnabled"/> is <c>true</c>.
        /// </summary>
        /// <param name="renderer">
        /// The camera renderer that will own the built passes.
        /// (Todo 13: Change parameter type to <c>CameraRenderer</c> when that type exists.)
        /// </param>
        /// <returns>
        /// A new <see cref="List{Pass}"/> containing all enabled passes,
        /// or an empty list if no definitions are configured.
        /// </returns>
        public List<Pass> Build(object renderer)
        {
            var passMap = new Dictionary<string, Pass>();

            // ── Phase 1: Instantiate passes from definitions ──
            foreach (PassDefinition def in m_Passes)
            {
                if (string.IsNullOrEmpty(def.InstanceName))
                {
                    Debug.LogWarning(
                        $"RenderGraphAsset.Build: Skipping pass definition with null/empty InstanceName.");
                    continue;
                }

                Type passType = ResolvePassType(def.PassType);
                if (passType == null)
                {
                    Debug.LogWarning(
                        $"RenderGraphAsset.Build: Could not resolve pass type '{def.PassType}' " +
                        $"for instance '{def.InstanceName}'. Ensure the type is decorated with " +
                        $"[Pass(\"{def.PassType}\")] and PassRegistry.RegisterAll() has been called.");
                    continue;
                }

                Pass pass = InstantiatePass(passType, def.InstanceName);
                if (pass == null)
                {
                    Debug.LogWarning(
                        $"RenderGraphAsset.Build: Failed to instantiate pass of type '{passType.FullName}' " +
                        $"for instance '{def.InstanceName}'. The type must have a public constructor " +
                        $"that accepts a single string argument (the pass name).");
                    continue;
                }

                // Declare the pass's slots once at build time so that Phase 2
                // (ConnectPassSlots) wires up the same slot instances that Record
                // will use each frame. Per-frame SetupSlots calls (the old
                // CameraRenderer behavior) recreated slots and broke connections.
                pass.SetupSlots();

                passMap[def.InstanceName] = pass;
            }

            // ── Phase 1.5: Build resource nodes from definitions ──
            m_RuntimeResources = new List<ResourceNode>();
            var resourceMap = new Dictionary<string, ResourceNode>();
            foreach (ResourceDefinition def in m_Resources)
            {
                if (def == null || string.IsNullOrEmpty(def.ResourceName))
                {
                    Debug.LogWarning(
                        $"RenderGraphAsset.Build: Skipping resource definition with null/empty ResourceName.");
                    continue;
                }

                ResourceNode node = CreateResourceNode(def);
                if (node == null)
                {
                    Debug.LogWarning(
                        $"RenderGraphAsset.Build: Could not create resource node for " +
                        $"'{def.ResourceName}' (kind '{def.ResourceKind}').");
                    continue;
                }

                resourceMap[def.ResourceName] = node;
                m_RuntimeResources.Add(node);
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

            // ── Phase 2.5: Parse resource connections ──
            foreach (ResourceConnection rc in m_ResourceConnections)
            {
                if (rc == null || string.IsNullOrEmpty(rc.ResourceName))
                {
                    Debug.LogWarning(
                        $"RenderGraphAsset.Build: Skipping ResourceConnection with null/empty ResourceName.");
                    continue;
                }

                if (!resourceMap.TryGetValue(rc.ResourceName, out ResourceNode node))
                {
                    Debug.LogWarning(
                        $"RenderGraphAsset.Build: ResourceConnection references unknown ResourceName " +
                        $"'{rc.ResourceName}'.");
                    continue;
                }

                if (string.IsNullOrEmpty(rc.PassName)
                    || !passMap.TryGetValue(rc.PassName, out Pass pass))
                {
                    Debug.LogWarning(
                        $"RenderGraphAsset.Build: ResourceConnection for resource '{rc.ResourceName}' " +
                        $"references unknown PassName '{(rc != null ? rc.PassName : "<null>")}'.");
                    continue;
                }

                PassSlot slot = pass.GetSlot(rc.SlotName);
                if (slot == null)
                {
                    Debug.LogWarning(
                        $"RenderGraphAsset.Build: ResourceConnection for resource '{rc.ResourceName}' " +
                        $"references unknown slot '{rc.SlotName}' on pass '{rc.PassName}'.");
                    continue;
                }

                if (rc.Direction == ResourceConnectionDirection.ResourceToPass)
                {
                    try
                    {
                        slot.ConnectResource(node);
                        node.ConsumerSlots.Add(slot);
                    }
                    catch (ArgumentException ex)
                    {
                        Debug.LogWarning(
                            $"RenderGraphAsset.Build: ResourceConnection for resource '{rc.ResourceName}' " +
                            $"to pass '{rc.PassName}' slot '{rc.SlotName}' failed: {ex.Message}");
                    }
                }
                else
                {
                    if (slot.Direction != SlotDirection.Output)
                    {
                        Debug.LogWarning(
                            $"RenderGraphAsset.Build: PassToResource connection for resource " +
                            $"'{rc.ResourceName}' must reference an Output slot; slot " +
                            $"'{rc.SlotName}' on pass '{rc.PassName}' is {slot.Direction}.");
                        continue;
                    }

                    node.ProducerSlot = slot;
                }
            }

            // ── Phase 3: Topologically sort passes (producers before consumers) ──
            return TopologicalSort(passMap);
        }

        /// <summary>
        /// Creates the concrete <see cref="ResourceNode"/> subclass matching the
        /// definition's <see cref="ResourceKind"/>.
        /// </summary>
        /// <param name="def">The resource definition to materialize.</param>
        /// <returns>
        /// A <see cref="TextureResourceNode"/>, <see cref="ComputeBufferResourceNode"/>,
        /// or <see cref="RendererListResourceNode"/>, or <c>null</c> for unknown kinds.
        /// </returns>
        private static ResourceNode CreateResourceNode(ResourceDefinition def)
        {
            ResourceNode node;
            switch (def.ResourceKind)
            {
                case ResourceKind.Texture:
                    node = new TextureResourceNode();
                    break;
                case ResourceKind.ComputeBuffer:
                    node = new ComputeBufferResourceNode();
                    break;
                case ResourceKind.RendererList:
                    node = new RendererListResourceNode();
                    break;
                default:
                    return null;
            }

            node.ResourceName = def.ResourceName;
            node.Kind = def.ResourceKind;
            node.Definition = def;
            return node;
        }

        /// <summary>
        /// Topologically sorts all built passes so producers are recorded before
        /// their consumers. Returns only enabled passes.
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
        /// Dependencies come from two sources:
        /// <list type="bullet">
        ///   <item><b>Resource dependencies</b> — the pass owning
        ///   <see cref="ResourceNode.ProducerSlot"/> must run before every pass
        ///   owning a slot in <see cref="ResourceNode.ConsumerSlots"/> (the
        ///   consumer reads the producer's handle during <see cref="Pass.Record"/>).</item>
        ///   <item><b>SlotConnection edges</b> — legacy pass-to-pass edges from
        ///   <see cref="m_Connections"/> (source pass before target pass).</item>
        /// </list>
        /// </remarks>
        private List<Pass> TopologicalSort(Dictionary<string, Pass> passMap)
        {
            // Stable base order: definition insertion order, restricted to passes
            // that were actually built (passMap may omit failed instantiations).
            var order = new List<Pass>(passMap.Count);
            var index = new Dictionary<Pass, int>();
            foreach (PassDefinition def in m_Passes)
            {
                if (string.IsNullOrEmpty(def.InstanceName))
                {
                    continue;
                }

                if (passMap.TryGetValue(def.InstanceName, out Pass pass))
                {
                    index[pass] = order.Count;
                    order.Add(pass);
                }
            }

            int count = order.Count;
            var adjacency = new Dictionary<int, HashSet<int>>();
            var inDegree = new int[count];

            // ── Build dependency edges ──

            // Resource dependencies: producer pass → consumer pass, plus chained
            // consumer edges (definition order) so passes sharing a resource keep
            // a stable order even when the resource has no producer pass.
            foreach (ResourceNode node in m_RuntimeResources)
            {
                // Collect consumer passes (deduplicated) in definition order.
                var consumers = new List<Pass>();
                foreach (PassSlot consumer in node.ConsumerSlots)
                {
                    if (consumer.OwnerPass == null
                        || !index.TryGetValue(consumer.OwnerPass, out _)
                        || consumers.Contains(consumer.OwnerPass))
                    {
                        continue;
                    }

                    consumers.Add(consumer.OwnerPass);
                }

                consumers.Sort((a, b) => order.IndexOf(a).CompareTo(order.IndexOf(b)));

                // Producer pass (if any) must record before every consumer.
                if (node.ProducerSlot?.OwnerPass is Pass producer
                    && index.TryGetValue(producer, out int producerIndex))
                {
                    foreach (Pass consumer in consumers)
                    {
                        if (consumer == producer)
                        {
                            continue;
                        }

                        AddEdge(adjacency, inDegree, producerIndex, index[consumer]);
                    }
                }

                // Chained consumer edges keep resource consumers in definition
                // order. This matters for resources without a producer pass:
                // RenderGraph does not reorder passes that share an imported
                // (non-pass-produced) resource, so the record order here is the
                // execution order.
                for (int i = 0; i + 1 < consumers.Count; i++)
                {
                    AddEdge(adjacency, inDegree, index[consumers[i]], index[consumers[i + 1]]);
                }
            }

            // Legacy SlotConnection edges: source pass → target pass.
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
        /// Resolves a pass type string to a concrete <see cref="Type"/>.
        /// Tries <see cref="PassRegistry.GetPassType"/> first (display name lookup),
        /// then falls back to <see cref="Type.GetType(string)"/> for fully qualified names.
        /// </summary>
        /// <param name="passType">The pass type identifier (display name or full type name).</param>
        /// <returns>The resolved <see cref="Type"/>, or <c>null</c> if not found.</returns>
        private static Type ResolvePassType(string passType)
        {
            if (string.IsNullOrEmpty(passType))
            {
                return null;
            }

            // Try PassRegistry by display name first.
            Type type = PassRegistry.GetPassType(passType);
            if (type != null)
            {
                return type;
            }

            // Fallback: try as a fully qualified type name.
            type = Type.GetType(passType);
            return type;
        }

        /// <summary>
        /// Instantiates a <see cref="Pass"/> from its <see cref="Type"/>,
        /// passing the instance name to the constructor.
        /// </summary>
        /// <param name="passType">The concrete pass type.</param>
        /// <param name="instanceName">The instance name for the new pass.</param>
        /// <returns>The instantiated <see cref="Pass"/>, or <c>null</c> on failure.</returns>
        private static Pass InstantiatePass(Type passType, string instanceName)
        {
            try
            {
                return (Pass)Activator.CreateInstance(passType, instanceName);
            }
            catch (MissingMethodException)
            {
                return null;
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
            // Wires an output slot of source to an input slot of target by name.
            // Fails silently when directions don't match (e.g. legacy output→output definitions).
            source.TryConnect(sourceSlot, target, targetSlot);
        }
    }
}
