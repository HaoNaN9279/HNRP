// <copyright file="PassSlot.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;

namespace HN.HNRP
{
    /// <summary>
    /// Defines the direction of a <see cref="PassSlot"/>.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><see cref="Input"/> — reads the resource handle from a connected output slot.</item>
    ///   <item><see cref="Output"/> — creates a resource handle that input slots can read.</item>
    /// </list>
    /// </remarks>
    public enum SlotDirection
    {
        /// <summary>
        /// Input slot: reads from a connected output's handle.
        /// </summary>
        Input,

        /// <summary>
        /// Output slot: creates a resource handle for downstream inputs.
        /// </summary>
        Output,
    }

    /// <summary>
    /// Abstract base class for name-based render pass slots.
    /// Replaces the legacy index-based slot system with a name-driven model.
    /// </summary>
    /// <remarks>
    /// <para><b>Connection model:</b></para>
    /// <list type="bullet">
    ///   <item>Output slots call <see cref="CreateHandle"/> to produce a resource handle.</item>
    ///   <item>Output slots call <see cref="Connect"/> to link an input slot.</item>
    ///   <item>Input slots call <see cref="ReadHandle"/> to retrieve the connected output's handle.</item>
    /// </list>
    /// <para>
    /// This class is pure C# — no <c>ScriptableObject</c> inheritance and no Unity serialization
    /// attributes. It is designed to be lightweight and testable outside the Unity Editor.
    /// </para>
    /// </remarks>
    public abstract class PassSlot
    {
        /// <summary>
        /// Gets the name of this slot. Must be non-empty and unique within a pass.
        /// </summary>
        public string SlotName { get; }

        /// <summary>
        /// Gets the direction of this slot — <see cref="SlotDirection.Input"/> or
        /// <see cref="SlotDirection.Output"/>.
        /// </summary>
        public SlotDirection Direction { get; }

        /// <summary>
        /// Gets a value indicating whether this slot is connected.
        /// For input slots, <c>true</c> after a successful <see cref="Connect"/> call.
        /// For output slots, always <c>false</c> (an output can drive multiple inputs
        /// and does not track its own connection state).
        /// </summary>
        public bool IsConnected { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether this slot holds a resource handle.
        /// An output slot has a handle after <see cref="CreateHandle"/> is called.
        /// </summary>
        public bool HasHandle => handle != null;

        /// <summary>
        /// The resource handle created by this slot (output) or accessed via connection (input).
        /// May be <c>null</c> if not yet created or not yet connected.
        /// </summary>
        protected object? handle;

        /// <summary>
        /// For input slots: the output slot this input is connected to.
        /// For output slots: always <c>null</c>.
        /// </summary>
        protected PassSlot? connectedOutput;

        /// <summary>
        /// Initializes a new instance of the <see cref="PassSlot"/> class.
        /// </summary>
        /// <param name="slotName">
        /// The name of the slot. Must be non-null and non-empty (whitespace-only is rejected).
        /// </param>
        /// <param name="direction">
        /// Whether this slot is an <see cref="SlotDirection.Input"/> or
        /// <see cref="SlotDirection.Output"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="slotName"/> is <c>null</c>, empty, or whitespace-only.
        /// </exception>
        protected PassSlot(string slotName, SlotDirection direction)
        {
            if (string.IsNullOrWhiteSpace(slotName))
            {
                throw new ArgumentException(
                    "Slot name must not be null, empty, or whitespace-only.",
                    nameof(slotName));
            }

            SlotName = slotName;
            Direction = direction;
        }

        /// <summary>
        /// Creates a resource handle for this slot.
        /// Only valid for output slots.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this slot is an input slot.
        /// </exception>
        /// <remarks>
        /// The base implementation creates a placeholder <see cref="object"/> handle.
        /// Subclasses may override to create type-specific handles (e.g., via the render graph).
        /// </remarks>
        public virtual void CreateHandle()
        {
            if (Direction != SlotDirection.Output)
            {
                throw new InvalidOperationException(
                    "Only output slots can create resource handles.");
            }

            handle = new object();
        }

        /// <summary>
        /// Sets the resource handle for this slot. Output slots use this to publish
        /// the real render graph handle (TextureHandle / ComputeBufferHandle) that
        /// connected input slots read via <see cref="ReadHandle"/>.
        /// </summary>
        /// <param name="value">The real render graph resource handle.</param>
        public void SetHandle(object value)
        {
            handle = value;
        }

        /// <summary>
        /// Connects this output slot to the given input slot.
        /// After connection, the input slot can call <see cref="ReadHandle"/> to retrieve
        /// this output's resource handle.
        /// </summary>
        /// <param name="input">
        /// The input slot to connect to. Must have <see cref="SlotDirection.Input"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="input"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this slot is not an output, or <paramref name="input"/> is not an input.
        /// </exception>
        public void Connect(PassSlot input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (Direction != SlotDirection.Output)
            {
                throw new InvalidOperationException(
                    "Only output slots can initiate a connection.");
            }

            if (input.Direction != SlotDirection.Input)
            {
                throw new InvalidOperationException(
                    "Can only connect an output slot to an input slot.");
            }

            input.connectedOutput = this;
            input.IsConnected = true;
        }

        /// <summary>
        /// Reads the resource handle associated with this slot.
        /// </summary>
        /// <returns>
        /// For an output slot: its own created handle (may be <c>null</c> if
        /// <see cref="CreateHandle"/> was not called).
        /// For a connected input slot: the connected output's handle.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this is an input slot that is not yet connected.
        /// </exception>
        public object? ReadHandle()
        {
            if (Direction == SlotDirection.Output)
            {
                return handle;
            }

            // Direction is Input
            if (!IsConnected)
            {
                throw new InvalidOperationException(
                    "Input slot is not connected to an output slot. " +
                    "Call Connect() on the output slot first.");
            }

            return connectedOutput!.handle;
        }
    }

    #region Concrete Slot Types

    /// <summary>
    /// A <see cref="PassSlot"/> that represents a texture resource.
    /// </summary>
    public class TextureSlot : PassSlot
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextureSlot"/> class.
        /// </summary>
        /// <param name="slotName">The name of the texture slot.</param>
        /// <param name="direction">Whether this is an input or output slot.</param>
        public TextureSlot(string slotName, SlotDirection direction)
            : base(slotName, direction)
        {
        }
    }

    /// <summary>
    /// A <see cref="PassSlot"/> that represents a compute buffer resource.
    /// </summary>
    public class ComputeBufferSlot : PassSlot
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ComputeBufferSlot"/> class.
        /// </summary>
        /// <param name="slotName">The name of the compute buffer slot.</param>
        /// <param name="direction">Whether this is an input or output slot.</param>
        public ComputeBufferSlot(string slotName, SlotDirection direction)
            : base(slotName, direction)
        {
        }
    }

    /// <summary>
    /// A <see cref="PassSlot"/> that represents a renderer list resource.
    /// </summary>
    public class RendererListSlot : PassSlot
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RendererListSlot"/> class.
        /// </summary>
        /// <param name="slotName">The name of the renderer list slot.</param>
        /// <param name="direction">Whether this is an input or output slot.</param>
        public RendererListSlot(string slotName, SlotDirection direction)
            : base(slotName, direction)
        {
        }
    }

    #endregion
}
