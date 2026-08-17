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
    /// Cameras do <b>not</b> reference <see cref="RenderGraphAsset"/> directly.
    /// Instead, <see cref="CameraPipelineConfig"/> acts as the intermediary,
    /// and all configs are managed through
    /// <see cref="HNRenderPipelineGlobalSettings.cameraPipelineConfigs"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="CameraPipelineConfig"/>
    /// <seealso cref="CameraPipelineConfig.renderGraph"/>
    public class RenderGraphAsset : ScriptableObject
    {
        [SerializeField]
        private List<PassDefinition> m_Passes = new();

        [SerializeField]
        private List<SlotConnection> m_Connections = new();

        [SerializeField]
        private RenderGraphSettings m_Settings;

        /// <summary>
        /// Gets the ordered list of pass definitions in this graph.
        /// </summary>
        public List<PassDefinition> Passes => m_Passes;

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

                passMap[def.InstanceName] = pass;
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

            // ── Phase 3: Filter to enabled passes only ──
            var result = new List<Pass>(passMap.Count);
            foreach (KeyValuePair<string, Pass> kvp in passMap)
            {
                if (kvp.Value.IsEnabled)
                {
                    result.Add(kvp.Value);
                }
            }

            return result;
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
        /// (Todo 14): Actual slot-level wiring will be implemented when <see cref="Pass"/>
        /// gains a named-slot API (e.g. <c>Pass.GetSlot(name)</c>). Currently this method
        /// validates that both passes exist; full data-flow wiring is deferred.
        /// </remarks>
        private static void ConnectPassSlots(
            Pass source,
            string sourceSlot,
            Pass target,
            string targetSlot)
        {
            // Placeholder: wire up slots by name.
            // When Pass exposes named slot collections, this method will:
            //   1. Call source.GetOutputSlot(sourceSlot)
            //   2. Call target.GetInputSlot(targetSlot)
            //   3. Point the input slot's data source to the output slot
        }
    }
}
