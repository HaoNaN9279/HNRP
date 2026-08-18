using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using HN.HNRP;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Tests for <see cref="HNRenderPipeline"/> camera rendering and render graph
    /// selection logic.
    /// Verifies that <see cref="CameraRenderer"/> is created per camera, each camera's
    /// renderer is independent, and <see cref="RenderGraphAsset"/> selection maps
    /// correctly by <see cref="CameraType"/>.
    /// </summary>
    public sealed class HNRenderPipelineTests
    {
        #region Test Helpers

        /// <summary>
        /// A minimal <see cref="Pass"/> subclass for verifying pass lists.
        /// Registered as <c>"HNRenderPipelineTestPass"</c>.
        /// </summary>
        [Pass("HNRenderPipelineTestPass")]
        private sealed class TestPass : Pass
        {
            /// <summary>
            /// Gets or sets a custom value for verifying pass independence.
            /// </summary>
            public string Tag { get; set; }

            /// <summary>
            /// Gets whether SetupSlots was called.
            /// </summary>
            public bool SetupSlotsCalled { get; private set; }

            /// <summary>
            /// Gets whether Initialize was called.
            /// </summary>
            public bool InitializeCalled { get; private set; }

            /// <summary>
            /// Gets whether Record was called.
            /// </summary>
            public bool RecordCalled { get; private set; }

            /// <summary>
            /// Initializes a new instance with the given name.
            /// </summary>
            /// <param name="name">The pass instance name.</param>
            public TestPass(string name)
                : base(name)
            {
            }

            /// <inheritdoc />
            public override void SetupSlots()
            {
                SetupSlotsCalled = true;
            }

            /// <inheritdoc />
            public override void Initialize(CameraContext context)
            {
                InitializeCalled = true;
            }

            /// <inheritdoc />
            public override void Record(RenderGraph renderGraph)
            {
                RecordCalled = true;
            }
        }

        /// <summary>
        /// Creates a <see cref="RenderGraphAsset"/> with a single test pass definition.
        /// </summary>
        /// <param name="passName">The instance name for the pass.</param>
        /// <returns>A new RenderGraphAsset with one pass.</returns>
        private static RenderGraphAsset CreateTemplate(string passName)
        {
            var asset = ScriptableObject.CreateInstance<RenderGraphAsset>();
            asset.Passes.Add(PassDefinition.Create("HNRenderPipelineTestPass", passName));
            return asset;
        }

        #endregion

        #region Setup / Teardown

        /// <summary>
        /// Ensures <see cref="PassRegistry"/> is populated before each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            PassRegistry.RegisterAll();
        }

        #endregion

        #region Render_UsesCameraRenderer

        /// <summary>
        /// Verifies that for each camera that has a valid <see cref="RenderGraphAsset"/>,
        /// a <see cref="CameraRenderer"/> is created and its passes are populated from the
        /// template. This is tested by simulating the per-camera renderer creation loop
        /// (without the full <c>Render</c> context).
        /// </summary>
        [Test]
        public void Render_UsesCameraRenderer()
        {
            // ── Arrange: create two cameras with different render graphs ──
            var template1 = CreateTemplate("Camera1_OpaquePass");
            var template2 = CreateTemplate("Camera2_TransparentPass");

            var go1 = new GameObject("Camera1");
            var go2 = new GameObject("Camera2");
            var camera1 = go1.AddComponent<Camera>();
            var camera2 = go2.AddComponent<Camera>();
            camera1.cameraType = CameraType.Game;
            camera2.cameraType = CameraType.Game;

            var data1 = go1.AddComponent<HNAdditionalCameraData>();
            var data2 = go2.AddComponent<HNAdditionalCameraData>();
            data1.PipelineConfigOverride = template1;
            data2.PipelineConfigOverride = template2;

            try
            {
                // ── Act: simulate the pipeline's per-camera loop ──
                var ctx1 = new CameraContext(camera1, default);
                var ctx2 = new CameraContext(camera2, default);

                var renderer1 = new CameraRenderer(ctx1);
                var renderer2 = new CameraRenderer(ctx2);

                renderer1.Build(template1);
                renderer2.Build(template2);

                // ── Assert: each camera got its own renderer with correct passes ──
                Assert.That(renderer1, Is.Not.Null,
                    "Camera 1 should have a renderer.");
                Assert.That(renderer2, Is.Not.Null,
                    "Camera 2 should have a renderer.");
                Assert.That(renderer1, Is.Not.SameAs(renderer2),
                    "Each camera should get its own CameraRenderer instance.");

                Assert.That(renderer1.Passes.Count, Is.EqualTo(1),
                    "Camera 1 renderer should have 1 pass from its template.");
                Assert.That(renderer2.Passes.Count, Is.EqualTo(1),
                    "Camera 2 renderer should have 1 pass from its template.");

                Assert.That(renderer1.Passes[0].PassName, Is.EqualTo("Camera1_OpaquePass"),
                    "Camera 1 pass name should match its template.");
                Assert.That(renderer2.Passes[0].PassName, Is.EqualTo("Camera2_TransparentPass"),
                    "Camera 2 pass name should match its template.");

                ctx1.Dispose();
                ctx2.Dispose();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go1);
                UnityEngine.Object.DestroyImmediate(go2);
                UnityEngine.Object.DestroyImmediate(template1);
                UnityEngine.Object.DestroyImmediate(template2);
            }
        }

        #endregion

        #region EachCamera_HasIndependentRenderer

        /// <summary>
        /// Verifies that when two cameras share the same <see cref="RenderGraphAsset"/>
        /// template, each gets its own independent <see cref="CameraRenderer"/> instance.
        /// Modifying passes on one renderer does not affect the other.
        /// </summary>
        [Test]
        public void EachCamera_HasIndependentRenderer()
        {
            // ── Arrange: two cameras sharing the same render graph ──
            var template = CreateTemplate("SharedPass");

            var go1 = new GameObject("CameraA");
            var go2 = new GameObject("CameraB");
            var camera1 = go1.AddComponent<Camera>();
            var camera2 = go2.AddComponent<Camera>();
            var data1 = go1.AddComponent<HNAdditionalCameraData>();
            var data2 = go2.AddComponent<HNAdditionalCameraData>();
            data1.PipelineConfigOverride = template;
            data2.PipelineConfigOverride = template;

            try
            {
                var ctx1 = new CameraContext(camera1, default);
                var ctx2 = new CameraContext(camera2, default);

                var renderer1 = new CameraRenderer(ctx1);
                var renderer2 = new CameraRenderer(ctx2);

                renderer1.Build(template);
                renderer2.Build(template);

                // ── Act: modify renderer1's passes ──
                renderer1.AddPass<TestPass>("ExtraPassOnCameraA");
                renderer1.SetPassEnabled("SharedPass", false);

                // ── Assert: renderer2 is unaffected ──
                Assert.That(renderer1.Passes.Count, Is.EqualTo(2),
                    "Camera A renderer should have 2 passes (1 template + 1 added).");
                Assert.That(renderer2.Passes.Count, Is.EqualTo(1),
                    "Camera B renderer should still have only 1 pass from template.");

                Assert.That(renderer1.Passes[0].IsEnabled, Is.False,
                    "SharedPass should be disabled on Camera A after SetPassEnabled(false).");
                Assert.That(renderer2.Passes[0].IsEnabled, Is.True,
                    "SharedPass should still be enabled on Camera B.");

                Assert.That(renderer2.Passes.Exists(p => p.PassName == "ExtraPassOnCameraA"), Is.False,
                    "ExtraPassOnCameraA should NOT appear in Camera B's renderer.");

                ctx1.Dispose();
                ctx2.Dispose();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go1);
                UnityEngine.Object.DestroyImmediate(go2);
                UnityEngine.Object.DestroyImmediate(template);
            }
        }

        #endregion

        #region RenderGraphSelection_ByCameraType

        /// <summary>
        /// Verifies that <see cref="HNRenderPipeline.SelectPipelineConfig"/> selects
        /// the correct default <see cref="RenderGraphAsset"/> based on the camera's
        /// <see cref="CameraType"/> when no per-camera override is set.
        /// </summary>
        [Test]
        public void RenderGraphSelection_ByCameraType_GameCamera_UsesDefaultGameRenderGraph()
        {
            var defaultGameRenderGraph = ScriptableObject.CreateInstance<RenderGraphAsset>();
            var defaultSceneRenderGraph = ScriptableObject.CreateInstance<RenderGraphAsset>();

            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            asset.DefaultGameRenderGraph = defaultGameRenderGraph;
            asset.DefaultSceneViewRenderGraph = defaultSceneRenderGraph;
            asset.DefaultPreviewRenderGraph = null;
            asset.DefaultReflectionRenderGraph = null;

            var pipeline = new HNRenderPipeline(asset);

            var go = new GameObject("TestCamera");
            var camera = go.AddComponent<Camera>();
            camera.cameraType = CameraType.Game;
            var data = go.AddComponent<HNAdditionalCameraData>();
            data.PipelineConfigOverride = null;

            try
            {
                var selected = pipeline.SelectPipelineConfig(camera, data);

                Assert.That(selected, Is.Not.Null,
                    "A render graph should be selected for a Game camera with a default set.");
                Assert.That(selected, Is.SameAs(defaultGameRenderGraph),
                    "Should select defaultGameRenderGraph for CameraType.Game.");
                Assert.That(selected, Is.Not.SameAs(defaultSceneRenderGraph),
                    "Should NOT select the SceneView render graph for a Game camera.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(asset);
                UnityEngine.Object.DestroyImmediate(defaultGameRenderGraph);
                UnityEngine.Object.DestroyImmediate(defaultSceneRenderGraph);
            }
        }

        /// <summary>
        /// Verifies that a SceneView camera selects <c>DefaultSceneViewRenderGraph</c>.
        /// </summary>
        [Test]
        public void RenderGraphSelection_ByCameraType_SceneViewCamera_UsesDefaultSceneViewRenderGraph()
        {
            var defaultSceneRenderGraph = ScriptableObject.CreateInstance<RenderGraphAsset>();
            var defaultPreviewRenderGraph = ScriptableObject.CreateInstance<RenderGraphAsset>();

            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            asset.DefaultGameRenderGraph = null;
            asset.DefaultSceneViewRenderGraph = defaultSceneRenderGraph;
            asset.DefaultPreviewRenderGraph = defaultPreviewRenderGraph;
            asset.DefaultReflectionRenderGraph = null;

            var pipeline = new HNRenderPipeline(asset);

            var go = new GameObject("SceneCamera");
            var camera = go.AddComponent<Camera>();
            camera.cameraType = CameraType.SceneView;
            var data = go.AddComponent<HNAdditionalCameraData>();

            try
            {
                var selected = pipeline.SelectPipelineConfig(camera, data);

                Assert.That(selected, Is.Not.Null);
                Assert.That(selected, Is.SameAs(defaultSceneRenderGraph),
                    "Should select defaultSceneViewRenderGraph for CameraType.SceneView.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(asset);
                UnityEngine.Object.DestroyImmediate(defaultSceneRenderGraph);
                UnityEngine.Object.DestroyImmediate(defaultPreviewRenderGraph);
            }
        }

        /// <summary>
        /// Verifies that a Preview camera selects <c>DefaultPreviewRenderGraph</c>.
        /// </summary>
        [Test]
        public void RenderGraphSelection_ByCameraType_PreviewCamera_UsesDefaultPreviewRenderGraph()
        {
            var defaultPreviewRenderGraph = ScriptableObject.CreateInstance<RenderGraphAsset>();

            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            asset.DefaultPreviewRenderGraph = defaultPreviewRenderGraph;

            var pipeline = new HNRenderPipeline(asset);

            var go = new GameObject("PreviewCamera");
            var camera = go.AddComponent<Camera>();
            camera.cameraType = CameraType.Preview;
            var data = go.AddComponent<HNAdditionalCameraData>();

            try
            {
                var selected = pipeline.SelectPipelineConfig(camera, data);

                Assert.That(selected, Is.Not.Null);
                Assert.That(selected, Is.SameAs(defaultPreviewRenderGraph),
                    "Should select defaultPreviewRenderGraph for CameraType.Preview.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(asset);
                UnityEngine.Object.DestroyImmediate(defaultPreviewRenderGraph);
            }
        }

        /// <summary>
        /// Verifies that a Reflection camera selects <c>DefaultReflectionRenderGraph</c>.
        /// </summary>
        [Test]
        public void RenderGraphSelection_ByCameraType_ReflectionCamera_UsesDefaultReflectionRenderGraph()
        {
            var defaultReflectionRenderGraph = ScriptableObject.CreateInstance<RenderGraphAsset>();

            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            asset.DefaultReflectionRenderGraph = defaultReflectionRenderGraph;

            var pipeline = new HNRenderPipeline(asset);

            var go = new GameObject("ReflectionCamera");
            var camera = go.AddComponent<Camera>();
            camera.cameraType = CameraType.Reflection;
            var data = go.AddComponent<HNAdditionalCameraData>();

            try
            {
                var selected = pipeline.SelectPipelineConfig(camera, data);

                Assert.That(selected, Is.Not.Null);
                Assert.That(selected, Is.SameAs(defaultReflectionRenderGraph),
                    "Should select defaultReflectionRenderGraph for CameraType.Reflection.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(asset);
                UnityEngine.Object.DestroyImmediate(defaultReflectionRenderGraph);
            }
        }

        /// <summary>
        /// Verifies that a camera without any matching render graph returns <c>null</c>
        /// and would be skipped during rendering.
        /// </summary>
        [Test]
        public void RenderGraphSelection_NoMatchingRenderGraph_ReturnsNull()
        {
            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            // All defaults are null.
            var pipeline = new HNRenderPipeline(asset);

            var go = new GameObject("NoConfigCamera");
            var camera = go.AddComponent<Camera>();
            camera.cameraType = CameraType.Game;
            var data = go.AddComponent<HNAdditionalCameraData>();

            try
            {
                var selected = pipeline.SelectPipelineConfig(camera, data);

                Assert.That(selected, Is.Null,
                    "Should return null when no default render graph is assigned.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        /// <summary>
        /// Verifies that <c>pipelineConfigOverride</c> takes priority over the
        /// default render graph.
        /// </summary>
        [Test]
        public void RenderGraphSelection_OverrideHasPriority_OverDefault()
        {
            var defaultRenderGraph = ScriptableObject.CreateInstance<RenderGraphAsset>();
            var overrideRenderGraph = ScriptableObject.CreateInstance<RenderGraphAsset>();

            var asset = ScriptableObject.CreateInstance<HNRenderPipelineAsset>();
            asset.DefaultGameRenderGraph = defaultRenderGraph;

            var pipeline = new HNRenderPipeline(asset);

            var go = new GameObject("OverrideCamera");
            var camera = go.AddComponent<Camera>();
            camera.cameraType = CameraType.Game;
            var data = go.AddComponent<HNAdditionalCameraData>();
            data.PipelineConfigOverride = overrideRenderGraph;

            try
            {
                var selected = pipeline.SelectPipelineConfig(camera, data);

                Assert.That(selected, Is.Not.Null);
                Assert.That(selected, Is.SameAs(overrideRenderGraph),
                    "Override should be selected even when a default exists.");
                Assert.That(selected, Is.Not.SameAs(defaultRenderGraph),
                    "Default should be ignored when override is set.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(asset);
                UnityEngine.Object.DestroyImmediate(defaultRenderGraph);
                UnityEngine.Object.DestroyImmediate(overrideRenderGraph);
            }
        }

        #endregion
    }
}
