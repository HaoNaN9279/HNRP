// <copyright file="PassConfigBaseTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using NUnit.Framework;
using UnityEngine;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for <see cref="PassConfigBase"/> in <c>Runtime/Core/PassConfigBase.cs</c>.
    /// Verifies subclassing, independent instantiation, and the <see cref="PassConfigBase.ApplyToPass"/>
    /// contract.
    /// </summary>
    public sealed class PassConfigBaseTests
    {
        #region Test Helpers

        /// <summary>
        /// A minimal concrete <see cref="PassConfigBase"/> subclass for testing.
        /// When <see cref="ApplyToPass"/> is called, it disables the target pass
        /// so that tests can verify the side effect.
        /// </summary>
        private class TestPassConfig : PassConfigBase
        {
            public bool WasApplied { get; private set; }

            /// <inheritdoc />
            public override void ApplyToPass(Pass pass)
            {
                if (pass == null)
                    throw new ArgumentNullException(nameof(pass));

                WasApplied = true;
                pass.IsEnabled = false;
            }
        }

        /// <summary>
        /// A minimal concrete <see cref="Pass"/> subclass for testing
        /// <see cref="PassConfigBase.ApplyToPass"/>.
        /// </summary>
        private class TestPass : Pass
        {
            public TestPass(string name) : base(name)
            {
            }

            public override void SetupSlots()
            {
            }

            public override void Initialize(CameraContext context)
            {
            }

            public override void Record(UnityEngine.Experimental.Rendering.RenderGraphModule.RenderGraph renderGraph)
            {
            }
        }

        #endregion

        #region Subclassing & Instantiation

        /// <summary>
        /// A concrete subclass of <see cref="PassConfigBase"/> can be created via
        /// <see cref="ScriptableObject.CreateInstance{T}"/> and is a valid ScriptableObject.
        /// </summary>
        [Test]
        public void Config_CanBeSubclassed()
        {
            var config = ScriptableObject.CreateInstance<TestPassConfig>();

            Assert.That(config, Is.Not.Null,
                "CreateInstance should return a non-null instance.");
            Assert.That(config, Is.InstanceOf<PassConfigBase>(),
                "Concrete config should be assignable to PassConfigBase.");
            Assert.That(config, Is.InstanceOf<ScriptableObject>(),
                "PassConfigBase should ultimately be a ScriptableObject.");

            UnityEngine.Object.DestroyImmediate(config);
        }

        /// <summary>
        /// <see cref="ScriptableObject.Instantiate{T}(T)"/> creates an independent copy —
        /// mutations to the copy must not affect the original.
        /// </summary>
        [Test]
        public void Config_Instantiate_CreatesIndependentCopy()
        {
            var original = ScriptableObject.CreateInstance<TestPassConfig>();
            original.PassName = "OriginalConfig";

            var copy = ScriptableObject.Instantiate(original);

            Assert.That(copy, Is.Not.Null,
                "Instantiate should return a non-null instance.");
            Assert.That(copy, Is.Not.SameAs(original),
                "Instantiate must create a new, independent object.");
            Assert.That(copy.PassName, Is.EqualTo("OriginalConfig"),
                "The copy should inherit field values from the original.");

            // Mutate the copy; original must be unaffected.
            copy.PassName = "ModifiedCopy";
            Assert.That(original.PassName, Is.EqualTo("OriginalConfig"),
                "Mutating the copy's PassName must not change the original.");
            Assert.That(copy.PassName, Is.EqualTo("ModifiedCopy"),
                "After mutation, the copy's PassName should reflect the new value.");

            UnityEngine.Object.DestroyImmediate(original);
            UnityEngine.Object.DestroyImmediate(copy);
        }

        #endregion

        #region ApplyToPass Contract

        /// <summary>
        /// Calling <see cref="PassConfigBase.ApplyToPass"/> on a concrete config
        /// must modify the target <see cref="Pass"/> instance's state as defined
        /// by the subclass implementation.
        /// </summary>
        [Test]
        public void Config_ApplyToPass_ModifiesPassState()
        {
            var config = ScriptableObject.CreateInstance<TestPassConfig>();
            var pass = new TestPass("TestPass");

            Assert.That(pass.IsEnabled, Is.True,
                "A newly created Pass should be enabled by default.");

            config.ApplyToPass(pass);

            Assert.That(config.WasApplied, Is.True,
                "ApplyToPass should have been invoked on the config.");
            Assert.That(pass.IsEnabled, Is.False,
                "ApplyToPass should have disabled the pass as implemented by TestPassConfig.");

            UnityEngine.Object.DestroyImmediate(config);
        }

        #endregion
    }
}
