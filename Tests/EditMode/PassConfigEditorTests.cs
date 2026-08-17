using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace HN.HNRP.Editor.Tests
{
    /// <summary>
    /// Stub tests for <see cref="PassConfigEditor"/>.
    /// These tests validate that the custom Inspector correctly handles
    /// <see cref="PassConfigBase"/> subclasses and their serialized properties.
    /// </summary>
    /// <remarks>
    /// Full test coverage depends on the test assembly (Todo 4) and concrete
    /// <see cref="PassConfigBase"/> subclasses being available at test time.
    /// </remarks>
    public class PassConfigEditorTests
    {
        /// <summary>
        /// A minimal concrete <see cref="PassConfigBase"/> subclass for testing.
        /// </summary>
        private class TestPassConfig : PassConfigBase
        {
            [SerializeField]
            public float testFloatValue = 1.0f;

            [SerializeField]
            public string testStringValue = "default";

            /// <inheritdoc />
            public override void ApplyToPass(Pass pass)
            {
                // Stub implementation for testing.
            }
        }

        [Test]
        public void PassConfigBase_IsScriptableObject()
        {
            var config = ScriptableObject.CreateInstance<TestPassConfig>();
            Assert.That(config, Is.Not.Null);
            Assert.That(config, Is.InstanceOf<ScriptableObject>());
            Object.DestroyImmediate(config);
        }

        [Test]
        public void PassConfigBase_PassName_DefaultIsNull()
        {
            var config = ScriptableObject.CreateInstance<TestPassConfig>();
            Assert.That(config.PassName, Is.Null.Or.Empty);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void PassConfigBase_PassName_CanBeSet()
        {
            var config = ScriptableObject.CreateInstance<TestPassConfig>();
            const string expected = "TestPass";
            config.PassName = expected;
            Assert.That(config.PassName, Is.EqualTo(expected));
            Object.DestroyImmediate(config);
        }

        [Test]
        public void PassConfigBase_SerializedFields_RetainValues()
        {
            var config = ScriptableObject.CreateInstance<TestPassConfig>();
            config.testFloatValue = 42.0f;
            config.testStringValue = "hello";

            Assert.That(config.testFloatValue, Is.EqualTo(42.0f));
            Assert.That(config.testStringValue, Is.EqualTo("hello"));
            Object.DestroyImmediate(config);
        }

        [Test]
        public void PassConfigEditor_TargetType_IsPassConfigBase()
        {
            // Verifies the CustomEditor attribute targets the correct type
            var attributes = typeof(PassConfigEditor)
                .GetCustomAttributes(typeof(CustomEditor), inherit: false);

            Assert.That(attributes.Length, Is.GreaterThan(0));

            var attr = (CustomEditor)attributes[0];
            var inspectedType = typeof(CustomEditor)
                .GetField("m_InspectedType", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(attr) as System.Type;
            Assert.That(inspectedType, Is.EqualTo(typeof(PassConfigBase)));
        }
    }
}
