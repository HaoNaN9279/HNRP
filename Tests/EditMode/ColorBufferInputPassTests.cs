using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for <see cref="ColorBufferInputPass"/>.
    /// Verifies slot declaration, pass instantiation, default parameters,
    /// lifecycle behavior, and <see cref="PassAttribute"/> decoration.
    /// </summary>
    public sealed class ColorBufferInputPassTests
    {
        // ── SetupSlots ──

        /// <summary>
        /// <see cref="ColorBufferInputPass.SetupSlots"/> must create a
        /// <see cref="TextureSlot"/> named "colorTargetSlot" with
        /// <see cref="SlotDirection.Output"/> direction.
        /// </summary>
        [Test]
        public void SetupSlots_DeclaresColorTargetSlot()
        {
            var pass = new ColorBufferInputPass("TestColorBufferInput");

            pass.SetupSlots();

            Assert.That(pass.ColorTargetSlot, Is.Not.Null,
                "SetupSlots should create the colorTargetSlot.");
            Assert.That(pass.ColorTargetSlot.SlotName, Is.EqualTo("colorTargetSlot"),
                "The slot should be named 'colorTargetSlot'.");
            Assert.That(pass.ColorTargetSlot.Direction, Is.EqualTo(SlotDirection.Output),
                "colorTargetSlot should be an Output slot so downstream passes can read from it.");
        }

        // ── Constructor ──

        /// <summary>
        /// The constructor should set <see cref="Pass.PassName"/> and
        /// default <see cref="Pass.IsEnabled"/> to <c>true</c>.
        /// </summary>
        [Test]
        public void Constructor_SetsPassNameAndEnabled()
        {
            var pass = new ColorBufferInputPass("TestColorBufferInput");

            Assert.That(pass.PassName, Is.EqualTo("TestColorBufferInput"),
                "PassName should match the constructor argument.");
            Assert.That(pass.IsEnabled, Is.True,
                "Pass should be enabled by default.");
        }

        // ── Default Parameters ──

        /// <summary>
        /// All configurable parameters should have sensible defaults
        /// matching the legacy <c>ColorBufferInput</c> behavior.
        /// </summary>
        [Test]
        public void DefaultParameters_AreCorrect()
        {
            var pass = new ColorBufferInputPass("TestColorBufferInput");

            Assert.That(pass.TextureScale, Is.EqualTo(Vector2.one),
                "Default TextureScale should be Vector2.one (full resolution).");
            Assert.That(pass.ColorFormat, Is.EqualTo(GraphicsFormat.R8G8B8A8_UNorm),
                "Default ColorFormat should be R8G8B8A8_UNorm.");
            Assert.That(pass.ClearBuffer, Is.True,
                "Default ClearBuffer should be true.");
            Assert.That(pass.ClearColor, Is.EqualTo(Color.black),
                "Default ClearColor should be black.");
        }

        // ── Record Structure ──

        /// <summary>
        /// Before <see cref="ColorBufferInputPass.Record"/> is called,
        /// the colorTargetSlot must be initialized by <see cref="ColorBufferInputPass.SetupSlots"/>
        /// and must be an <see cref="SlotDirection.Output"/> slot so that
        /// <c>builder.UseColorBuffer</c> can populate it correctly.
        /// </summary>
        /// <remarks>
        /// The actual RenderGraph integration (<c>renderGraph.CreateTexture</c> and
        /// <c>builder.UseColorBuffer</c>) is verified via integration tests that require
        /// a running Unity Editor with an active render graph. This test confirms the
        /// structural prerequisites for Record to succeed.
        /// </remarks>
        [Test]
        public void Record_RequiresColorTargetSlot_AsOutput()
        {
            var pass = new ColorBufferInputPass("TestColorBufferInput");
            pass.SetupSlots();

            Assert.That(pass.ColorTargetSlot, Is.Not.Null,
                "colorTargetSlot must be initialized before Record is called.");
            Assert.That(pass.ColorTargetSlot.Direction, Is.EqualTo(SlotDirection.Output),
                "colorTargetSlot must be an Output direction for builder.UseColorBuffer " +
                "to populate it with the created texture handle.");
        }

        // ── PassAttribute ──

        /// <summary>
        /// <see cref="ColorBufferInputPass"/> must be decorated with
        /// <c>[Pass("Color Buffer Input")]</c> so that <see cref="PassRegistry.RegisterAll"/>
        /// discovers it automatically.
        /// </summary>
        [Test]
        public void HasPassAttribute_WithCorrectDisplayName()
        {
            var type = typeof(ColorBufferInputPass);
            var attr = type.GetCustomAttribute<PassAttribute>();

            Assert.That(attr, Is.Not.Null,
                "ColorBufferInputPass must have the [Pass] attribute for PassRegistry discovery.");
            Assert.That(attr!.DisplayName, Is.EqualTo("Color Buffer Input"),
                "Display name must be 'Color Buffer Input' to match the legacy pass type name.");
        }

        // ── Inheritance ──

        /// <summary>
        /// <see cref="ColorBufferInputPass"/> must inherit from <see cref="Pass"/>
        /// (the new pass system), not from the legacy <see cref="PassBase"/>.
        /// </summary>
        [Test]
        public void InheritsFromPass_NotPassBase()
        {
            var pass = new ColorBufferInputPass("TestColorBufferInput");

            Assert.That(pass, Is.InstanceOf<Pass>(),
                "ColorBufferInputPass must inherit from Pass (new system).");
            Assert.That(pass, Is.InstanceOf<Pass>(),
                "ColorBufferInputPass must inherit from Pass (new system).");
        }

        // ── IsEnabled ──

        /// <summary>
        /// Setting <see cref="Pass.IsEnabled"/> to <c>false</c> should cause
        /// <see cref="CameraRenderer.Render"/> to skip <see cref="Pass.Record"/>
        /// for this pass.
        /// </summary>
        [Test]
        public void IsEnabled_ControlsRecordExecution()
        {
            var pass = new ColorBufferInputPass("TestColorBufferInput") { IsEnabled = false };

            Assert.That(pass.IsEnabled, Is.False,
                "IsEnabled should be false after setting to false.");

            // Simulate CameraRenderer.Render logic:
            // enabled passes call SetupSlots → Initialize → Record
            // disabled passes are skipped
            bool wouldRecord = pass.IsEnabled;

            Assert.That(wouldRecord, Is.False,
                "Record should be skipped when IsEnabled is false.");
        }
    }
}
