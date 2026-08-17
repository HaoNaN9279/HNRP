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
    /// Abstract base Editor for inspecting <see cref="Pass"/> instances.
    /// Provides shared drawing logic for PassName, IsEnabled, and the slot list.
    /// Uses reflection to discover <see cref="PassSlot"/>-typed properties on the
    /// target <see cref="Pass"/> instance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Since <see cref="Pass"/> is a pure C# class (not a <see cref="UnityEngine.Object"/>),
    /// this Editor does not carry a <c>[CustomEditor]</c> attribute. Subclasses or
    /// calling code invoke <see cref="DrawPassGUI"/> manually with a <see cref="Pass"/>
    /// instance — typically from a container inspector (e.g., a
    /// <see cref="RenderGraphAsset"/> editor).
    /// </para>
    /// </remarks>
    public abstract class PassEditor : UnityEditor.Editor
    {
        /// <summary>
        /// Draws the full Inspector GUI for the given <see cref="Pass"/>.
        /// Shows:
        /// <list type="bullet">
        ///   <item><b>PassName</b> — read-only label.</item>
        ///   <item><b>IsEnabled</b> — toggle controlling whether the pass executes.</item>
        ///   <item><b>Slot list</b> — table of all <see cref="PassSlot"/> properties
        ///   discovered via reflection (name, type, direction, connection status).</item>
        /// </list>
        /// </summary>
        /// <param name="pass">The <see cref="Pass"/> instance to inspect. Must not be null.</param>
        protected void DrawPassGUI(Pass pass)
        {
            if (pass == null)
            {
                EditorGUILayout.HelpBox("No Pass instance available.", MessageType.Warning);
                return;
            }

            EditorGUI.BeginChangeCheck();

            // ── Header: PassName (read-only) ──
            GUI.enabled = false;
            EditorGUILayout.TextField("Pass Name", pass.PassName);
            GUI.enabled = true;

            // ── IsEnabled toggle ──
            pass.IsEnabled = EditorGUILayout.Toggle("Is Enabled", pass.IsEnabled);

            EditorGUILayout.Space();

            // ── Slot list ──
            DrawSlotList(pass);

            if (EditorGUI.EndChangeCheck())
            {
                // Mark dirty if we are tracking a UnityEngine.Object target.
                if (target != null)
                {
                    EditorUtility.SetDirty(target);
                }
            }
        }

        /// <summary>
        /// Draws a table listing all slots discovered on the given <see cref="Pass"/>
        /// via reflection.
        /// </summary>
        /// <param name="pass">The <see cref="Pass"/> whose slots to enumerate.</param>
        protected void DrawSlotList(Pass pass)
        {
            var slots = DiscoverSlots(pass);

            EditorGUILayout.LabelField("Slots", EditorStyles.boldLabel);

            if (slots.Count == 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(
                    "(No slots discovered. Call SetupSlots() first.)",
                    EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
                return;
            }

            // ── Column header ──
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name", EditorStyles.miniBoldLabel, GUILayout.Width(140));
            EditorGUILayout.LabelField("Type", EditorStyles.miniBoldLabel, GUILayout.Width(120));
            EditorGUILayout.LabelField("Direction", EditorStyles.miniBoldLabel, GUILayout.Width(70));
            EditorGUILayout.LabelField("Connected", EditorStyles.miniBoldLabel, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;

            foreach (var (propertyName, slot) in slots)
            {
                DrawSlotEntry(slot);
            }

            EditorGUI.indentLevel--;
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
                MethodInfo? getter = prop.GetGetMethod();
                if (getter == null || getter.IsStatic)
                {
                    continue;
                }

                object? value;
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
}
