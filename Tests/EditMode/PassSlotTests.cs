// <copyright file="PassSlotTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using NUnit.Framework;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for the name-based <see cref="PassSlot"/> system in <c>Runtime/Core/PassSlot.cs</c>.
    /// Verifies slot naming, direction, handle semantics, connection, and input-output linking.
    /// </summary>
    public sealed class PassSlotTests
    {
        #region Slot Name Validation

        /// <summary>
        /// A slot must not be created with an empty or null name.
        /// </summary>
        [Test]
        public void Slot_NameMustNotBeEmpty()
        {
            Assert.Throws<ArgumentException>(() => new TextureSlot(string.Empty, SlotDirection.Output),
                "Constructor should reject empty string as slot name.");
            Assert.Throws<ArgumentException>(() => new TextureSlot(null!, SlotDirection.Input),
                "Constructor should reject null as slot name.");
            Assert.Throws<ArgumentException>(() => new ComputeBufferSlot(" \t\n", SlotDirection.Output),
                "Constructor should reject whitespace-only string as slot name.");
        }

        #endregion

        #region Direction

        /// <summary>
        /// The <see cref="SlotDirection"/> enum correctly distinguishes Input from Output slots.
        /// </summary>
        [Test]
        public void Slot_Direction_InputOutput()
        {
            var input = new TextureSlot("ColorIn", SlotDirection.Input);
            var output = new ComputeBufferSlot("LightList", SlotDirection.Output);
            var rlOutput = new RendererListSlot("OpaqueList", SlotDirection.Output);

            Assert.That(input.Direction, Is.EqualTo(SlotDirection.Input));
            Assert.That(output.Direction, Is.EqualTo(SlotDirection.Output));
            Assert.That(rlOutput.Direction, Is.EqualTo(SlotDirection.Output));
        }

        #endregion

        #region Handle Semantics (Output)

        /// <summary>
        /// An output slot has no handle before <see cref="PassSlot{T}.SetHandle"/> is called.
        /// </summary>
        [Test]
        public void OutputSlot_HasHandle_False_BeforeSet()
        {
            var output = new TextureSlot("MainColor", SlotDirection.Output);
            Assert.That(output.HasHandle, Is.False);
        }

        /// <summary>
        /// Setting a default (invalid) handle must not mark the slot as having a valid handle.
        /// </summary>
        [Test]
        public void OutputSlot_SetHandle_DefaultValue_IsNotValid()
        {
            var output = new TextureSlot("MainColor", SlotDirection.Output);
            output.SetHandle(default(TextureHandle));
            Assert.That(output.HasHandle, Is.False,
                "A default TextureHandle is not valid, so HasHandle must be false.");
        }

        /// <summary>
        /// <see cref="PassSlot{T}.ResetHandle"/> clears the stored handle and resets the slot state.
        /// </summary>
        [Test]
        public void OutputSlot_ResetHandle_ClearsHandle()
        {
            var output = new ComputeBufferSlot("Buf", SlotDirection.Output);
            output.SetHandle(default(ComputeBufferHandle));
            output.ResetHandle();
            Assert.That(output.HasHandle, Is.False);
        }

        #endregion

        #region Connection & Handle Reading (Input)

        /// <summary>
        /// After connecting an output to an input, the input slot can read the output's handle.
        /// </summary>
        [Test]
        public void InputSlot_ReadsConnectedHandle()
        {
            var output = new RendererListSlot("MainList", SlotDirection.Output);
            var input = new RendererListSlot("ListIn", SlotDirection.Input);
            var value = default(RendererListHandle);
            output.SetHandle(value);
            output.Connect(input);
            Assert.That(input.ReadHandle(), Is.EqualTo(value));
        }

        /// <summary>
        /// An input slot that is not connected should throw when reading a handle.
        /// </summary>
        [Test]
        public void InputSlot_ThrowsWhenNotConnected()
        {
            var input = new ComputeBufferSlot("BufIn", SlotDirection.Input);

            Assert.Throws<InvalidOperationException>(() => input.ReadHandle(),
                "Unconnected input slot should throw when attempting to read handle.");
        }

        /// <summary>
        /// Connecting Input→Output (wrong direction) is not allowed.
        /// </summary>
        [Test]
        public void InputSlot_CannotConnect()
        {
            var input = new TextureSlot("In", SlotDirection.Input);
            var other = new TextureSlot("Other", SlotDirection.Input);

            Assert.Throws<InvalidOperationException>(() => input.Connect(other),
                "Input slot should not be allowed to initiate Connect.");
        }

        /// <summary>
        /// Connecting slots that carry different resource types throws an
        /// <see cref="ArgumentException"/> at connect time.
        /// </summary>
        [Test]
        public void Connect_TypeMismatch_Throws()
        {
            var output = new TextureSlot("Tex", SlotDirection.Output);
            var input = new ComputeBufferSlot("Buf", SlotDirection.Input);
            Assert.Throws<ArgumentException>(() => output.Connect(input));
        }

        /// <summary>
        /// A failed type-mismatched connect must not mark the input slot as connected.
        /// </summary>
        [Test]
        public void Connect_TypeMismatch_DoesNotMarkConnected()
        {
            var output = new TextureSlot("Tex", SlotDirection.Output);
            var input = new ComputeBufferSlot("Buf", SlotDirection.Input);
            Assert.Throws<ArgumentException>(() => output.Connect(input));
            Assert.That(input.IsConnected, Is.False);
        }

        #endregion

        #region Connected Flag

        /// <summary>
        /// After connecting output to input, the input slot's <see cref="PassSlot.IsConnected"/> flag is set.
        /// </summary>
        [Test]
        public void Slot_ConnectedFlag()
        {
            var output = new RendererListSlot("OpaqueList", SlotDirection.Output);
            var input = new RendererListSlot("OpaqueIn", SlotDirection.Input);

            Assert.That(input.IsConnected, Is.False,
                "Input slot should not be connected before Connect is called.");

            output.Connect(input);

            Assert.That(input.IsConnected, Is.True,
                "Input slot should be connected after Connect is called.");
        }

        #endregion

        #region Slot Types

        /// <summary>
        /// Each concrete slot type correctly reports its slot name.
        /// </summary>
        [Test]
        public void Slot_ReportsSlotName()
        {
            var tex = new TextureSlot("MyTex", SlotDirection.Output);
            var buf = new ComputeBufferSlot("MyBuf", SlotDirection.Input);
            var rl = new RendererListSlot("MyList", SlotDirection.Output);

            Assert.That(tex.SlotName, Is.EqualTo("MyTex"));
            Assert.That(buf.SlotName, Is.EqualTo("MyBuf"));
            Assert.That(rl.SlotName, Is.EqualTo("MyList"));
        }

        #endregion
    }
}
