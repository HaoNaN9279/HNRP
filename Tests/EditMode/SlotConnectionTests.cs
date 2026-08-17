// <copyright file="SlotConnectionTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using NUnit.Framework;
using UnityEngine;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for <see cref="SlotConnection"/> in <c>Runtime/Config/SlotConnection.cs</c>.
    /// Verifies null/empty name validation, serialization roundtrip, and factory method.
    /// </summary>
    public sealed class SlotConnectionTests
    {
        #region Factory Method

        /// <summary>
        /// <see cref="SlotConnection.Create"/> creates a connection with all four
        /// name fields set correctly.
        /// </summary>
        [Test]
        public void Create_SetsAllFields()
        {
            var conn = SlotConnection.Create("DeferredPass", "ColorOutput", "PostProcessPass", "ColorInput");

            Assert.That(conn, Is.Not.Null,
                "Create should return a non-null instance.");
            Assert.That(conn.SourcePass, Is.EqualTo("DeferredPass"),
                "SourcePass should match the value passed to Create.");
            Assert.That(conn.SourceSlot, Is.EqualTo("ColorOutput"),
                "SourceSlot should match the value passed to Create.");
            Assert.That(conn.TargetPass, Is.EqualTo("PostProcessPass"),
                "TargetPass should match the value passed to Create.");
            Assert.That(conn.TargetSlot, Is.EqualTo("ColorInput"),
                "TargetSlot should match the value passed to Create.");
        }

        #endregion

        #region Validation

        /// <summary>
        /// <see cref="SlotConnection.IsValid"/> returns <c>true</c> when all fields
        /// are non-null and non-empty.
        /// </summary>
        [Test]
        public void IsValid_ReturnsTrue_WhenAllFieldsAreSet()
        {
            var conn = SlotConnection.Create("PassA", "Output", "PassB", "Input");

            Assert.That(conn.IsValid(), Is.True,
                "IsValid should return true when all fields are non-null and non-empty.");
        }

        /// <summary>
        /// <see cref="SlotConnection.IsValid"/> returns <c>false</c> when
        /// <see cref="SlotConnection.SourcePass"/> is <c>null</c>.
        /// </summary>
        [Test]
        public void IsValid_ReturnsFalse_WhenSourcePassIsNull()
        {
            var conn = SlotConnection.Create(null, "Output", "PassB", "Input");

            Assert.That(conn.IsValid(), Is.False,
                "IsValid should return false when SourcePass is null.");
        }

        /// <summary>
        /// <see cref="SlotConnection.IsValid"/> returns <c>false</c> when
        /// <see cref="SlotConnection.SourcePass"/> is empty.
        /// </summary>
        [Test]
        public void IsValid_ReturnsFalse_WhenSourcePassIsEmpty()
        {
            var conn = SlotConnection.Create(string.Empty, "Output", "PassB", "Input");

            Assert.That(conn.IsValid(), Is.False,
                "IsValid should return false when SourcePass is empty.");
        }

        /// <summary>
        /// <see cref="SlotConnection.IsValid"/> returns <c>false</c> when
        /// <see cref="SlotConnection.SourceSlot"/> is <c>null</c>.
        /// </summary>
        [Test]
        public void IsValid_ReturnsFalse_WhenSourceSlotIsNull()
        {
            var conn = SlotConnection.Create("PassA", null, "PassB", "Input");

            Assert.That(conn.IsValid(), Is.False,
                "IsValid should return false when SourceSlot is null.");
        }

        /// <summary>
        /// <see cref="SlotConnection.IsValid"/> returns <c>false</c> when
        /// <see cref="SlotConnection.SourceSlot"/> is empty.
        /// </summary>
        [Test]
        public void IsValid_ReturnsFalse_WhenSourceSlotIsEmpty()
        {
            var conn = SlotConnection.Create("PassA", string.Empty, "PassB", "Input");

            Assert.That(conn.IsValid(), Is.False,
                "IsValid should return false when SourceSlot is empty.");
        }

        /// <summary>
        /// <see cref="SlotConnection.IsValid"/> returns <c>false</c> when
        /// <see cref="SlotConnection.TargetPass"/> is <c>null</c>.
        /// </summary>
        [Test]
        public void IsValid_ReturnsFalse_WhenTargetPassIsNull()
        {
            var conn = SlotConnection.Create("PassA", "Output", null, "Input");

            Assert.That(conn.IsValid(), Is.False,
                "IsValid should return false when TargetPass is null.");
        }

        /// <summary>
        /// <see cref="SlotConnection.IsValid"/> returns <c>false</c> when
        /// <see cref="SlotConnection.TargetPass"/> is empty.
        /// </summary>
        [Test]
        public void IsValid_ReturnsFalse_WhenTargetPassIsEmpty()
        {
            var conn = SlotConnection.Create("PassA", "Output", string.Empty, "Input");

            Assert.That(conn.IsValid(), Is.False,
                "IsValid should return false when TargetPass is empty.");
        }

        /// <summary>
        /// <see cref="SlotConnection.IsValid"/> returns <c>false</c> when
        /// <see cref="SlotConnection.TargetSlot"/> is <c>null</c>.
        /// </summary>
        [Test]
        public void IsValid_ReturnsFalse_WhenTargetSlotIsNull()
        {
            var conn = SlotConnection.Create("PassA", "Output", "PassB", null);

            Assert.That(conn.IsValid(), Is.False,
                "IsValid should return false when TargetSlot is null.");
        }

        /// <summary>
        /// <see cref="SlotConnection.IsValid"/> returns <c>false</c> when
        /// <see cref="SlotConnection.TargetSlot"/> is empty.
        /// </summary>
        [Test]
        public void IsValid_ReturnsFalse_WhenTargetSlotIsEmpty()
        {
            var conn = SlotConnection.Create("PassA", "Output", "PassB", string.Empty);

            Assert.That(conn.IsValid(), Is.False,
                "IsValid should return false when TargetSlot is empty.");
        }

        /// <summary>
        /// <see cref="SlotConnection.IsValid"/> returns <c>false</c> when
        /// multiple fields are invalid.
        /// </summary>
        [Test]
        public void IsValid_ReturnsFalse_WhenMultipleFieldsAreInvalid()
        {
            var conn = new SlotConnection
            {
                SourcePass = null,
                SourceSlot = string.Empty,
                TargetPass = "PassB",
                TargetSlot = "Input",
            };

            Assert.That(conn.IsValid(), Is.False,
                "IsValid should return false when any field is invalid.");
        }

        #endregion

        #region Serialization Roundtrip

        /// <summary>
        /// <see cref="SlotConnection"/> survives a <see cref="JsonUtility.ToJson"/> /
        /// <see cref="JsonUtility.FromJson"/> roundtrip, preserving all four name fields.
        /// </summary>
        [Test]
        public void Serialization_Roundtrip_PreservesAllFields()
        {
            var original = SlotConnection.Create("DepthPass", "DepthOutput", "ColorPass", "DepthInput");

            var json = JsonUtility.ToJson(original, prettyPrint: false);
            var restored = JsonUtility.FromJson<SlotConnection>(json);

            Assert.That(restored, Is.Not.Null,
                "Deserialized SlotConnection should not be null.");
            Assert.That(restored.SourcePass, Is.EqualTo("DepthPass"),
                "SourcePass should survive JSON roundtrip.");
            Assert.That(restored.SourceSlot, Is.EqualTo("DepthOutput"),
                "SourceSlot should survive JSON roundtrip.");
            Assert.That(restored.TargetPass, Is.EqualTo("ColorPass"),
                "TargetPass should survive JSON roundtrip.");
            Assert.That(restored.TargetSlot, Is.EqualTo("DepthInput"),
                "TargetSlot should survive JSON roundtrip.");
        }

        /// <summary>
        /// After serialization roundtrip, <see cref="SlotConnection.IsValid"/> returns
        /// the same result as the original when all fields are valid.
        /// </summary>
        [Test]
        public void Serialization_Roundtrip_PreservesValidationResult()
        {
            var original = SlotConnection.Create("PassA", "Output", "PassB", "Input");
            Assert.That(original.IsValid(), Is.True,
                "Original should be valid before serialization.");

            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<SlotConnection>(json);

            Assert.That(restored.IsValid(), Is.True,
                "Restored connection should also be valid after roundtrip.");
        }

        /// <summary>
        /// <see cref="SlotConnection"/> with <c>null</c> fields survives serialization
        /// roundtrip — <c>null</c> values are preserved as <c>null</c>.
        /// </summary>
        [Test]
        public void Serialization_Roundtrip_PreservesNullFields()
        {
            var original = SlotConnection.Create(null, null, null, null);

            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<SlotConnection>(json);

            Assert.That(restored, Is.Not.Null,
                "Deserialized SlotConnection should not be null.");
            // JsonUtility serializes null strings as empty strings.
            Assert.That(restored.SourcePass, Is.Null.Or.Empty,
                "Null SourcePass should survive roundtrip.");
            Assert.That(restored.SourceSlot, Is.Null.Or.Empty,
                "Null SourceSlot should survive roundtrip.");
            Assert.That(restored.TargetPass, Is.Null.Or.Empty,
                "Null TargetPass should survive roundtrip.");
            Assert.That(restored.TargetSlot, Is.Null.Or.Empty,
                "Null TargetSlot should survive roundtrip.");
            Assert.That(restored.IsValid(), Is.False,
                "Connection with all-null fields should be invalid.");
        }

        #endregion
    }
}
