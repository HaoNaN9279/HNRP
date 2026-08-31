// <copyright file="PassEditor.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// Abstract base Editor for inspecting <see cref="Pass"/> instances inside a
    /// <see cref="RenderGraphAsset"/> inspector. Provides shared drawing logic for
    /// PassName, IsEnabled, serialized parameters, and the slot list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Pass"/> is a pure serializable C# class (not a
    /// <see cref="UnityEngine.Object"/>), so this class cannot use Unity's
    /// <c>[CustomEditor]</c> mechanism. Subclasses bind to a pass type via
    /// <see cref="PassEditorAttribute"/> and are discovered by
    /// <see cref="PassEditorRegistry"/>; the container editor
    /// (<see cref="RenderGraphAssetEditor"/>) invokes <see cref="DrawPassGUI"/>
    /// with the pass's <see cref="SerializedProperty"/> so edits participate in
    /// undo/redo and serialization.
    /// </para>
    /// <para>
    /// Editors are stateless singletons: all per-pass UI state (foldouts, etc.)
    /// is owned by the container editor.
    /// </para>
    /// </remarks>
    public abstract class PassEditor
    {
        /// <summary>
        /// Gets the presets offered for the bound pass type. Presets are defined
        /// in the pass's editor (Editor-only) and applied via value copy
        /// (<see cref="Pass.CopyFrom"/>). Empty by default.
        /// </summary>
        public virtual IReadOnlyList<IPassPreset> Presets => Array.Empty<IPassPreset>();

        /// <summary>
        /// Draws the full parameter panel for the given pass:
        /// <list type="bullet">
        ///   <item><b>Pass Name</b> — read-only label.</item>
        ///   <item><b>Is Enabled</b> — toggle controlling whether the pass executes.</item>
        ///   <item><b>Parameters</b> — serialized fields, drawn by <see cref="DrawParameters"/>.</item>
        ///   <item><b>Slot list</b> — runtime slot debug table (requires SetupSlots to have run).</item>
        /// </list>
        /// </summary>
        /// <param name="passProp">The <see cref="SerializedProperty"/> of the pass element.</param>
        /// <param name="pass">The <see cref="Pass"/> template instance. Must not be null.</param>
        public void DrawPassGUI(SerializedProperty passProp, Pass pass)
        {
            if (pass == null || passProp == null)
            {
                EditorGUILayout.HelpBox("No Pass instance available.", MessageType.Warning);
                return;
            }

            DrawCommonHeader(passProp);

            EditorGUILayout.Space();

            DrawParameters(passProp, pass);

            EditorGUILayout.Space();
        }

        /// <summary>
        /// Draws the serialized parameters of the pass. Override to customize how
        /// the pass's parameters appear on the panel; the base implementation
        /// draws every visible serialized field except the common header fields.
        /// </summary>
        /// <param name="passProp">The <see cref="SerializedProperty"/> of the pass element.</param>
        /// <param name="pass">The <see cref="Pass"/> template instance. Must not be null.</param>
        protected virtual void DrawParameters(SerializedProperty passProp, Pass pass)
        {
            SerializedProperty iterator = passProp.Copy();
            SerializedProperty end = iterator.GetEndProperty();

            if (iterator.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(iterator, end))
                    {
                        break;
                    }

                    // The common header fields are already drawn by DrawCommonHeader.
                    if (iterator.name == "m_PassName" || iterator.name == "m_IsEnabled")
                    {
                        continue;
                    }

                    EditorGUILayout.PropertyField(iterator, true);
                }
                while (iterator.NextVisible(false));
            }
        }

        /// <summary>
        /// Draws the shared header: PassName (read-only) and IsEnabled toggle.
        /// </summary>
        /// <param name="passProp">The <see cref="SerializedProperty"/> of the pass element.</param>
        private static void DrawCommonHeader(SerializedProperty passProp)
        {
            SerializedProperty nameProp = passProp.FindPropertyRelative("m_PassName");
            SerializedProperty enabledProp = passProp.FindPropertyRelative("m_IsEnabled");

            if (nameProp != null)
            {
                GUI.enabled = false;
                EditorGUILayout.PropertyField(nameProp, new GUIContent("Pass Name"));
                GUI.enabled = true;
            }

            if (enabledProp != null)
            {
                EditorGUILayout.PropertyField(enabledProp, new GUIContent("Is Enabled"));
            }
        }

        /// <summary>
        /// Draws a single slot entry showing its name, type, direction, and
        /// connection status (for input slots only).
        /// </summary>
        /// <param name="slot">The slot to draw.</param>
        private static void DrawSlotEntry(PassSlot slot)
        {
            EditorGUILayout.BeginHorizontal();

            // Slot name
            EditorGUILayout.LabelField(slot.SlotName, GUILayout.Width(140));

            // Slot type (short name: TextureSlot / ComputeBufferSlot / RendererListSlot)
            EditorGUILayout.LabelField(slot.GetType().Name, GUILayout.Width(120));

            // Direction
            EditorGUILayout.LabelField(slot.Direction.ToString(), GUILayout.Width(70));

            // Connection status — only meaningful for input slots
            if (slot.Direction == SlotDirection.Input)
            {
                GUI.enabled = false;
                EditorGUILayout.Toggle(slot.IsConnected, GUILayout.Width(20));
                GUI.enabled = true;
            }
            else
            {
                GUILayout.Space(20);
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Discovers all public, readable, instance <see cref="PassSlot"/>-typed
        /// properties on the given <see cref="Pass"/> via reflection.
        /// Non-null slot values are collected and returned.
        /// </summary>
        /// <param name="pass">The <see cref="Pass"/> instance to inspect.</param>
        /// <returns>
        /// A list of (property name, slot instance) tuples for all non-null
        /// <see cref="PassSlot"/> properties.
        /// </returns>
        private static List<(string Name, PassSlot Slot)> DiscoverSlots(Pass pass)
        {
            var result = new List<(string, PassSlot)>();

            if (pass == null)
            {
                return result;
            }

            Type type = pass.GetType();
            PropertyInfo[] properties = type.GetProperties(
                BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo prop in properties)
            {
                // Only properties whose type derives from PassSlot
                if (!typeof(PassSlot).IsAssignableFrom(prop.PropertyType))
                {
                    continue;
                }

                // Must have a public, non-static getter
                MethodInfo getter = prop.GetGetMethod();
                if (getter == null || getter.IsStatic)
                {
                    continue;
                }

                object value;
                try
                {
                    value = prop.GetValue(pass);
                }
                catch
                {
                    // Reflection can fail on disposed / invalid state — skip gracefully.
                    continue;
                }

                if (value is PassSlot slot)
                {
                    result.Add((prop.Name, slot));
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Default <see cref="PassEditor"/> used for pass types without a dedicated
    /// editor binding. Draws every serialized field via the base implementation.
    /// </summary>
    public sealed class DefaultPassEditor : PassEditor
    {
        /// <summary>
        /// Shared singleton instance.
        /// </summary>
        public static readonly DefaultPassEditor Instance = new DefaultPassEditor();
    }
}
