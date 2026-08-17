// <copyright file="RenderGraphAssetEditorTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HN.HNRP;
using HN.HNRP.Editor;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for <see cref="RenderGraphAssetEditor"/> in <c>Editor/Config/RenderGraphAssetEditor.cs</c>.
    /// Verifies the custom Inspector can correctly manipulate <see cref="RenderGraphAsset"/>
    /// serialized data through <see cref="SerializedObject"/> and <see cref="SerializedProperty"/>.
    /// </summary>
    public sealed class RenderGraphAssetEditorTests
    {
        #region Setup

        private RenderGraphAsset m_Asset;

        [SetUp]
        public void SetUp()
        {
            m_Asset = ScriptableObject.CreateInstance<RenderGraphAsset>();
            PassRegistry.RegisterAll();
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Asset != null)
            {
                Object.DestroyImmediate(m_Asset);
                m_Asset = null;
            }
        }

        #endregion

        #region SerializedObject — Passes List

        /// <summary>
        /// A <see cref="SerializedObject"/> wrapping a <see cref="RenderGraphAsset"/>
        /// can find the <c>m_Passes</c> property and its <see cref="PassDefinition"/>
        /// serialized fields.
        /// </summary>
        [Test]
        public void SerializedObject_FindsPassesProperty()
        {
            var so = new SerializedObject(m_Asset);
            var passesProp = so.FindProperty("m_Passes");

            Assert.That(passesProp, Is.Not.Null,
                "SerializedObject should find the m_Passes property.");
            Assert.That(passesProp.isArray, Is.True,
                "m_Passes should be an array property.");
            Assert.That(passesProp.arraySize, Is.Zero,
                "m_Passes should start empty.");
        }

        /// <summary>
        /// Adding a <see cref="PassDefinition"/> to <c>m_Passes</c> via
        /// <see cref="SerializedProperty"/> correctly initializes its fields
        /// and reflects in the underlying <see cref="RenderGraphAsset"/>.
        /// </summary>
        [Test]
        public void PassesList_AddElement_ReflectsInAsset()
        {
            var so = new SerializedObject(m_Asset);
            var passesProp = so.FindProperty("m_Passes");

            // Insert a new element.
            passesProp.InsertArrayElementAtIndex(0);
            SerializedProperty element = passesProp.GetArrayElementAtIndex(0);

            element.FindPropertyRelative("m_PassType").stringValue = "TestPassA";
            element.FindPropertyRelative("m_InstanceName").stringValue = "MyPass";
            element.FindPropertyRelative("m_Config").objectReferenceValue = null;

            so.ApplyModifiedProperties();

            Assert.That(m_Asset.Passes.Count, Is.EqualTo(1),
                "Asset should contain one PassDefinition after adding.");
            Assert.That(m_Asset.Passes[0].PassType, Is.EqualTo("TestPassA"),
                "PassType should match the serialized value.");
            Assert.That(m_Asset.Passes[0].InstanceName, Is.EqualTo("MyPass"),
                "InstanceName should match the serialized value.");
            Assert.That(m_Asset.Passes[0].Config, Is.Null,
                "Config should be null by default.");
        }

        /// <summary>
        /// Removing a <see cref="PassDefinition"/> from <c>m_Passes</c> via
        /// <see cref="SerializedProperty"/> correctly reduces the list size.
        /// </summary>
        [Test]
        public void PassesList_RemoveElement_ReflectsInAsset()
        {
            m_Asset.Passes.Add(PassDefinition.Create("TestPassA", "ToRemove"));
            var so = new SerializedObject(m_Asset);
            var passesProp = so.FindProperty("m_Passes");

            Assume.That(passesProp.arraySize, Is.EqualTo(1),
                "Precondition: should have one element.");

            passesProp.DeleteArrayElementAtIndex(0);
            so.ApplyModifiedProperties();

            Assert.That(m_Asset.Passes.Count, Is.Zero,
                "Asset should have zero PassDefinitions after removal.");
        }

        /// <summary>
        /// Modifying <see cref="PassDefinition"/> fields via
        /// <see cref="SerializedProperty"/> propagates to the underlying asset.
        /// </summary>
        [Test]
        public void PassesList_ModifyElement_ReflectsInAsset()
        {
            m_Asset.Passes.Add(PassDefinition.Create("TestPassA", "Original"));
            var so = new SerializedObject(m_Asset);
            var passesProp = so.FindProperty("m_Passes");

            SerializedProperty element = passesProp.GetArrayElementAtIndex(0);
            element.FindPropertyRelative("m_InstanceName").stringValue = "Modified";
            so.ApplyModifiedProperties();

            Assert.That(m_Asset.Passes[0].InstanceName, Is.EqualTo("Modified"),
                "InstanceName should reflect the modification.");
        }

        #endregion

        #region SerializedObject — Connections List

        /// <summary>
        /// A <see cref="SerializedObject"/> wrapping a <see cref="RenderGraphAsset"/>
        /// can find the <c>m_Connections</c> property and its <see cref="SlotConnection"/>
        /// serialized fields.
        /// </summary>
        [Test]
        public void SerializedObject_FindsConnectionsProperty()
        {
            var so = new SerializedObject(m_Asset);
            var connProp = so.FindProperty("m_Connections");

            Assert.That(connProp, Is.Not.Null,
                "SerializedObject should find the m_Connections property.");
            Assert.That(connProp.isArray, Is.True,
                "m_Connections should be an array property.");
            Assert.That(connProp.arraySize, Is.Zero,
                "m_Connections should start empty.");
        }

        /// <summary>
        /// Adding a <see cref="SlotConnection"/> to <c>m_Connections</c> via
        /// <see cref="SerializedProperty"/> correctly initializes its four name fields.
        /// </summary>
        [Test]
        public void ConnectionsList_AddElement_ReflectsInAsset()
        {
            var so = new SerializedObject(m_Asset);
            var connProp = so.FindProperty("m_Connections");

            connProp.InsertArrayElementAtIndex(0);
            SerializedProperty element = connProp.GetArrayElementAtIndex(0);

            element.FindPropertyRelative("m_SourcePass").stringValue = "PassA";
            element.FindPropertyRelative("m_SourceSlot").stringValue = "ColorOut";
            element.FindPropertyRelative("m_TargetPass").stringValue = "PassB";
            element.FindPropertyRelative("m_TargetSlot").stringValue = "ColorIn";

            so.ApplyModifiedProperties();

            Assert.That(m_Asset.Connections.Count, Is.EqualTo(1),
                "Asset should contain one SlotConnection after adding.");
            Assert.That(m_Asset.Connections[0].SourcePass, Is.EqualTo("PassA"),
                "SourcePass should match the serialized value.");
            Assert.That(m_Asset.Connections[0].SourceSlot, Is.EqualTo("ColorOut"),
                "SourceSlot should match the serialized value.");
            Assert.That(m_Asset.Connections[0].TargetPass, Is.EqualTo("PassB"),
                "TargetPass should match the serialized value.");
            Assert.That(m_Asset.Connections[0].TargetSlot, Is.EqualTo("ColorIn"),
                "TargetSlot should match the serialized value.");
        }

        /// <summary>
        /// Removing a <see cref="SlotConnection"/> from <c>m_Connections</c> via
        /// <see cref="SerializedProperty"/> correctly reduces the list.
        /// </summary>
        [Test]
        public void ConnectionsList_RemoveElement_ReflectsInAsset()
        {
            m_Asset.Connections.Add(SlotConnection.Create(
                "A", "AOut", "B", "BIn"));
            var so = new SerializedObject(m_Asset);
            var connProp = so.FindProperty("m_Connections");

            Assume.That(connProp.arraySize, Is.EqualTo(1),
                "Precondition: should have one element.");

            connProp.DeleteArrayElementAtIndex(0);
            so.ApplyModifiedProperties();

            Assert.That(m_Asset.Connections.Count, Is.Zero,
                "Asset should have zero SlotConnections after removal.");
        }

        #endregion

        #region SerializedObject — Settings

        /// <summary>
        /// A <see cref="SerializedObject"/> wrapping a <see cref="RenderGraphAsset"/>
        /// can find the <c>m_Settings</c> property and its nested fields.
        /// </summary>
        [Test]
        public void SerializedObject_FindsSettingsProperty()
        {
            var so = new SerializedObject(m_Asset);
            var settingsProp = so.FindProperty("m_Settings");

            Assert.That(settingsProp, Is.Not.Null,
                "SerializedObject should find the m_Settings property.");
        }

        /// <summary>
        /// Modifying <see cref="RenderGraphSettings.SHEvalMode"/> via
        /// <see cref="SerializedProperty"/> propagates to the underlying asset.
        /// </summary>
        [Test]
        public void Settings_ModifySHEvalMode_ReflectsInAsset()
        {
            var so = new SerializedObject(m_Asset);
            var settingsProp = so.FindProperty("m_Settings");
            var shEvalProp = settingsProp.FindPropertyRelative(nameof(RenderGraphSettings.SHEvalMode));

            shEvalProp.enumValueIndex = (int)SHEvalMode.PerPixel;
            so.ApplyModifiedProperties();

            Assert.That(m_Asset.Settings.SHEvalMode, Is.EqualTo(SHEvalMode.PerPixel),
                "SHEvalMode should reflect the serialized change.");
        }

        /// <summary>
        /// Modifying <see cref="RenderGraphSettings.AllowHDR"/> via
        /// <see cref="SerializedProperty"/> propagates to the underlying asset.
        /// </summary>
        [Test]
        public void Settings_ModifyAllowHDR_ReflectsInAsset()
        {
            var so = new SerializedObject(m_Asset);
            var settingsProp = so.FindProperty("m_Settings");
            var allowHDRProp = settingsProp.FindPropertyRelative(nameof(RenderGraphSettings.AllowHDR));

            Assume.That(m_Asset.Settings.AllowHDR, Is.False,
                "Default AllowHDR should be false.");

            allowHDRProp.boolValue = true;
            so.ApplyModifiedProperties();

            Assert.That(m_Asset.Settings.AllowHDR, Is.True,
                "AllowHDR should reflect the serialized change.");
        }

        #endregion

        #region SerializedObject — Roundtrip

        /// <summary>
        /// A complete <see cref="RenderGraphAsset"/> configuration can be
        /// round-tripped through <see cref="SerializedObject"/>: adding
        /// passes, connections, and settings, applying, and verifying the
        /// asset's data matches.
        /// </summary>
        [Test]
        public void SerializedObject_Roundtrip_AllProperties()
        {
            var so = new SerializedObject(m_Asset);

            // ── Add two PassDefinitions ──
            var passesProp = so.FindProperty("m_Passes");
            passesProp.InsertArrayElementAtIndex(0);
            var pass0 = passesProp.GetArrayElementAtIndex(0);
            pass0.FindPropertyRelative("m_PassType").stringValue = "TestPassA";
            pass0.FindPropertyRelative("m_InstanceName").stringValue = "Alpha";

            passesProp.InsertArrayElementAtIndex(1);
            var pass1 = passesProp.GetArrayElementAtIndex(1);
            pass1.FindPropertyRelative("m_PassType").stringValue = "TestPassB";
            pass1.FindPropertyRelative("m_InstanceName").stringValue = "Beta";

            // ── Add one SlotConnection ──
            var connProp = so.FindProperty("m_Connections");
            connProp.InsertArrayElementAtIndex(0);
            var conn0 = connProp.GetArrayElementAtIndex(0);
            conn0.FindPropertyRelative("m_SourcePass").stringValue = "Alpha";
            conn0.FindPropertyRelative("m_SourceSlot").stringValue = "Output";
            conn0.FindPropertyRelative("m_TargetPass").stringValue = "Beta";
            conn0.FindPropertyRelative("m_TargetSlot").stringValue = "Input";

            // ── Modify Settings ──
            var settingsProp = so.FindProperty("m_Settings");
            settingsProp.FindPropertyRelative(nameof(RenderGraphSettings.SHEvalMode)).enumValueIndex
                = (int)SHEvalMode.Mixed;
            settingsProp.FindPropertyRelative(nameof(RenderGraphSettings.AllowHDR)).boolValue = true;

            so.ApplyModifiedProperties();

            // ── Verify Passes ──
            Assert.That(m_Asset.Passes.Count, Is.EqualTo(2),
                "Should have two passes after roundtrip.");
            Assert.That(m_Asset.Passes[0].PassType, Is.EqualTo("TestPassA"));
            Assert.That(m_Asset.Passes[0].InstanceName, Is.EqualTo("Alpha"));
            Assert.That(m_Asset.Passes[1].PassType, Is.EqualTo("TestPassB"));
            Assert.That(m_Asset.Passes[1].InstanceName, Is.EqualTo("Beta"));

            // ── Verify Connections ──
            Assert.That(m_Asset.Connections.Count, Is.EqualTo(1),
                "Should have one connection after roundtrip.");
            Assert.That(m_Asset.Connections[0].SourcePass, Is.EqualTo("Alpha"));
            Assert.That(m_Asset.Connections[0].SourceSlot, Is.EqualTo("Output"));
            Assert.That(m_Asset.Connections[0].TargetPass, Is.EqualTo("Beta"));
            Assert.That(m_Asset.Connections[0].TargetSlot, Is.EqualTo("Input"));

            // ── Verify Settings ──
            Assert.That(m_Asset.Settings.SHEvalMode, Is.EqualTo(SHEvalMode.Mixed),
                "SHEvalMode should be Mixed.");
            Assert.That(m_Asset.Settings.AllowHDR, Is.True,
                "AllowHDR should be true.");
        }

        #endregion

        #region Editor — Type Resolution

        /// <summary>
        /// The <see cref="RenderGraphAssetEditor"/> type can be resolved via reflection,
        /// confirming it is correctly decorated and in the Editor assembly.
        /// </summary>
        [Test]
        public void EditorType_IsCustomEditor_MatchesRenderGraphAsset()
        {
            // Verify the CustomEditor attribute is present and targets RenderGraphAsset.
            var attrs = typeof(RenderGraphAssetEditor).GetCustomAttributes(
                typeof(CustomEditor), inherit: false);

            Assert.That(attrs, Is.Not.Null.And.Length.GreaterThan(0),
                "RenderGraphAssetEditor should have a [CustomEditor] attribute.");

            if (attrs.Length > 0 && attrs[0] is CustomEditor ce)
            {
                // CustomEditor(Type inspectedType) — verify the inspected type field.
                bool targetsRenderGraph = false;
                foreach (var field in typeof(CustomEditor).GetFields(
                    System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Public))
                {
                    var val = field.GetValue(ce);
                    if (val is System.Type t)
                    {
                        targetsRenderGraph = t == typeof(RenderGraphAsset);
                        if (targetsRenderGraph) break;
                    }
                }

                Assert.That(targetsRenderGraph, Is.True,
                    "CustomEditor attribute should target RenderGraphAsset.");
            }
        }

        #endregion
    }
}
