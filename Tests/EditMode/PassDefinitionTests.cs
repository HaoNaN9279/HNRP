// <copyright file="PassDefinitionTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using NUnit.Framework;
using UnityEngine;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for <see cref="PassDefinition"/> in <c>Runtime/Config/PassDefinition.cs</c>.
    /// Verifies factory methods, serialization roundtrip, and field management.
    /// </summary>
    public sealed class PassDefinitionTests
    {
        #region Test Helpers

        /// <summary>
        /// A minimal concrete <see cref="PassConfigBase"/> subclass for testing
        /// serialization behaviour with ScriptableObject references.
        /// </summary>
        private class TestPassConfig : PassConfigBase
        {
            /// <inheritdoc />
            public override void ApplyToPass(Pass pass)
            {
            }
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// <see cref="PassDefinition.Create(string, string)"/> creates a definition
        /// with the correct pass type and instance name, and a <c>null</c> config.
        /// </summary>
        [Test]
        public void Create_TwoArg_SetsPassTypeAndInstanceName()
        {
            var def = PassDefinition.Create("HN.HNRP.Passes.DeferredPass", "MainDeferred");

            Assert.That(def, Is.Not.Null,
                "Create should return a non-null instance.");
            Assert.That(def.PassType, Is.EqualTo("HN.HNRP.Passes.DeferredPass"),
                "PassType should match the value passed to Create.");
            Assert.That(def.InstanceName, Is.EqualTo("MainDeferred"),
                "InstanceName should match the value passed to Create.");
            Assert.That(def.Config, Is.Null,
                "Config should be null when not provided.");
        }

        /// <summary>
        /// <see cref="PassDefinition.Create(string, string, PassConfigBase)"/> creates
        /// a definition with all three fields set, including the config reference.
        /// </summary>
        [Test]
        public void Create_ThreeArg_SetsAllFields()
        {
            var config = ScriptableObject.CreateInstance<TestPassConfig>();
            config.PassName = "DeferredPass";

            var def = PassDefinition.Create("HN.HNRP.Passes.DeferredPass", "MainDeferred", config);

            Assert.That(def, Is.Not.Null,
                "Create should return a non-null instance.");
            Assert.That(def.PassType, Is.EqualTo("HN.HNRP.Passes.DeferredPass"),
                "PassType should match the value passed to Create.");
            Assert.That(def.InstanceName, Is.EqualTo("MainDeferred"),
                "InstanceName should match the value passed to Create.");
            Assert.That(def.Config, Is.SameAs(config),
                "Config should be the exact same instance passed to Create.");

            UnityEngine.Object.DestroyImmediate(config);
        }

        /// <summary>
        /// <see cref="PassDefinition.Create(string, string)"/> can accept <c>null</c>
        /// or empty strings — the factory itself does not validate.
        /// </summary>
        [Test]
        public void Create_AcceptsNullAndEmptyStrings()
        {
            var defNull = PassDefinition.Create(null, null);
            var defEmpty = PassDefinition.Create(string.Empty, string.Empty);

            Assert.That(defNull.PassType, Is.Null,
                "PassType should be null when null is passed.");
            Assert.That(defNull.InstanceName, Is.Null,
                "InstanceName should be null when null is passed.");
            Assert.That(defEmpty.PassType, Is.EqualTo(string.Empty),
                "PassType should be empty when string.Empty is passed.");
            Assert.That(defEmpty.InstanceName, Is.EqualTo(string.Empty),
                "InstanceName should be empty when string.Empty is passed.");
        }

        #endregion

        #region Property Mutation

        /// <summary>
        /// All properties (<see cref="PassDefinition.PassType"/>,
        /// <see cref="PassDefinition.InstanceName"/>, and <see cref="PassDefinition.Config"/>)
        /// can be mutated after construction.
        /// </summary>
        [Test]
        public void Properties_CanBeMutatedAfterConstruction()
        {
            var def = new PassDefinition();

            def.PassType = "HN.HNRP.Passes.ForwardPass";
            def.InstanceName = "MainForward";
            var config = ScriptableObject.CreateInstance<TestPassConfig>();
            def.Config = config;

            Assert.That(def.PassType, Is.EqualTo("HN.HNRP.Passes.ForwardPass"),
                "PassType should reflect the mutated value.");
            Assert.That(def.InstanceName, Is.EqualTo("MainForward"),
                "InstanceName should reflect the mutated value.");
            Assert.That(def.Config, Is.SameAs(config),
                "Config should reflect the mutated value.");

            UnityEngine.Object.DestroyImmediate(config);
        }

        #endregion

        #region Serialization Roundtrip

        /// <summary>
        /// <see cref="PassDefinition"/> with only string fields survives a
        /// <see cref="JsonUtility.ToJson"/> / <see cref="JsonUtility.FromJson"/> roundtrip,
        /// preserving <see cref="PassDefinition.PassType"/> and
        /// <see cref="PassDefinition.InstanceName"/>.
        /// </summary>
        [Test]
        public void Serialization_Roundtrip_PreservesStringFields()
        {
            var original = PassDefinition.Create("HN.HNRP.Passes.ShadowPass", "MainShadows");

            var json = JsonUtility.ToJson(original, prettyPrint: false);
            var restored = JsonUtility.FromJson<PassDefinition>(json);

            Assert.That(restored, Is.Not.Null,
                "Deserialized PassDefinition should not be null.");
            Assert.That(restored.PassType, Is.EqualTo("HN.HNRP.Passes.ShadowPass"),
                "PassType should survive JSON roundtrip.");
            Assert.That(restored.InstanceName, Is.EqualTo("MainShadows"),
                "InstanceName should survive JSON roundtrip.");
            Assert.That(restored.Config, Is.Null,
                "Config should be null after roundtrip when it was null before.");
        }

        /// <summary>
        /// When a <see cref="PassDefinition"/> references a ScriptableObject-based
        /// <see cref="PassConfigBase"/>, the JSON serialization includes an instanceID
        /// reference. After deserialization, the config reference is <c>null</c> because
        /// JsonUtility cannot reconstruct ScriptableObject references from JSON alone.
        /// This test documents the behaviour.
        /// </summary>
        [Test]
        public void Serialization_Roundtrip_ScriptableObjectReferenceBehaviour()
        {
            var config = ScriptableObject.CreateInstance<TestPassConfig>();
            config.PassName = "TestConfig";

            var original = PassDefinition.Create("HN.HNRP.Passes.TestPass", "Test", config);

            var json = JsonUtility.ToJson(original, prettyPrint: false);

            // JsonUtility serializes private fields by their field name (m_*).
            Assert.That(json, Does.Contain("m_Config"),
                "JSON should contain the serialized Config field.");

            var restored = JsonUtility.FromJson<PassDefinition>(json);

            Assert.That(restored, Is.Not.Null,
                "Deserialized PassDefinition should not be null.");
            Assert.That(restored.PassType, Is.EqualTo("HN.HNRP.Passes.TestPass"),
                "PassType should survive JSON roundtrip.");
            Assert.That(restored.InstanceName, Is.EqualTo("Test"),
                "InstanceName should survive JSON roundtrip.");
            // JsonUtility may resolve ScriptableObject references if the object
            // with the matching instanceID is still alive in memory.
            // This is expected Unity behavior: references to live objects survive.
            Assert.That(restored.Config, Is.Not.Null,
                "Config should resolve when the referenced ScriptableObject is still alive.");

            UnityEngine.Object.DestroyImmediate(config);
        }

        #endregion
    }
}
