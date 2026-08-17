// <copyright file="PassSlotTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using NUnit.Framework;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for the name-based <see cref="PassSlot"/> system in <c>Runtime/Core/PassSlot.cs</c>.
    /// Verifies slot naming, direction, handle creation, connection, and input-output linking.
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

        #region Handle Creation (Output)

        /// <summary>
        /// An output slot can create a resource handle via <see cref="PassSlot.CreateHandle"/>.
        /// </summary>
        [Test]
        public void OutputSlot_CreatesHandle()
        {
            var output = new TextureSlot("MainColor", SlotDirection.Output);

            Assert.That(output.HasHandle, Is.False,
                "Handle should not exist before CreateHandle is called.");

            output.CreateHandle();

            Assert.That(output.HasHandle, Is.True,
                "Handle should exist after CreateHandle is called.");
            Assert.That(output.ReadHandle(), Is.Not.Null,
                "ReadHandle on an output should return the created handle.");
        }

        /// <summary>
        /// An input slot cannot create a handle — only outputs can.
        /// </summary>
        [Test]
        public void InputSlot_CannotCreateHandle()
        {
            var input = new TextureSlot("ColorIn", SlotDirection.Input);

            Assert.Throws<InvalidOperationException>(() => input.CreateHandle(),
                "Input slot should not be allowed to create a handle.");
        }

        #endregion

        #region Connection & Handle Reading (Input)

        /// <summary>
        /// After connecting an output to an input, the input slot can read the output's handle.
        /// </summary>
        [Test]
        public void InputSlot_ReadsConnectedHandle()
        {
            var output = new TextureSlot("MainColor", SlotDirection.Output);
            var input = new TextureSlot("ColorIn", SlotDirection.Input);

            output.CreateHandle();
            output.Connect(input);

            var readHandle = input.ReadHandle();
            Assert.That(readHandle, Is.Not.Null,
                "InputReadHandle should return a non-null handle after connection.");
            Assert.That(readHandle, Is.SameAs(output.ReadHandle()),
                "Input should read exactly the same handle instance created by output.");
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
