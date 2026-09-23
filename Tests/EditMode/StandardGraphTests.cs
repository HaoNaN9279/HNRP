// <copyright file="StandardGraphTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using HN.HNRP;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// 针对 <c>Runtime/Resources/RenderGraphs/StandardGraph.asset</c> 的测试。
    /// 验证资源能正确加载、<see cref="RenderGraphAsset.Build"/> 实例化预期的
    /// pass 集合、物化四个资源节点（color/depth 缓冲与两类渲染器列表）、
    /// 连接关键槽（资源节点加上光照/探针数据的槽连接链），并按拓扑排序 pass。
    /// </summary>
    public sealed class StandardGraphTests
    {
        /// <summary>
        /// StandardGraph 中各 pass 的预期实例名，按拓扑（依赖）序排列。
        /// </summary>
        private static readonly string[] ExpectedPassNames =
        {
            "buildLight", "drawShadow", "clusterProbe", "clusterLight",
            "depthNormal", "screenSpaceShadow", "forwardOpaque", "sky", "transparency", "wireOverlay", "finalBlit",
        };

        /// <summary>
        /// 每个测试前确保 <see cref="PassRegistry"/> 已填充
        /// （仅真实 pass —— 无 stub）。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            PassRegistry.RegisterAll();
        }

        /// <summary>
        /// 每个测试后恢复干净的注册表。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            PassRegistry.RegisterAll();
        }

        #region 资源加载

        /// <summary>
        /// <c>StandardGraph.asset</c> 可从 <c>Resources/RenderGraphs</c> 加载，
        /// 且是非空 <see cref="RenderGraphAsset"/>。
        /// </summary>
        [Test]
        public void Load_Asset_IsNotNullAndCorrectType()
        {
            var asset = Resources.Load<RenderGraphAsset>("RenderGraphs/StandardGraph");

            Assert.That(asset, Is.Not.Null,
                "StandardGraph.asset should be loadable from Resources/RenderGraphs.");
            Assert.That(asset, Is.InstanceOf<RenderGraphAsset>(),
                "Loaded asset should be a RenderGraphAsset.");
        }

        #endregion

        #region Build —— Pass 组成

        /// <summary>
        /// <see cref="RenderGraphAsset.Build"/> 在真实资源上成功执行，
        /// 且产出与预期完全一致、按拓扑序排列的 pass 集合。
        /// </summary>
        [Test]
        public void Build_ProducesExpectedPasses()
        {
            var asset = Resources.Load<RenderGraphAsset>("RenderGraphs/StandardGraph");
            Assume.That(asset, Is.Not.Null,
                "Test requires StandardGraph.asset to be loadable.");

            List<Pass> result = asset.Build();

            Assert.That(result, Is.Not.Null,
                "Build should return a non-null list.");
            Assert.That(result.Count, Is.EqualTo(ExpectedPassNames.Length),
                "Build should produce exactly the expected number of passes.");

            foreach (string name in ExpectedPassNames)
            {
                Assert.That(
                    result.Exists(p => p.PassName == name),
                    Is.True,
                    $"Pass '{name}' should be present in the build result.");
            }
        }

        #endregion

        #region Build —— 连接

        /// <summary>
        /// <see cref="RenderGraphAsset.Build"/> 之后，每个渲染 pass 的关键输入槽
        /// 均已连接。在 pass 自持资源模型（ADR-017）下，链头的 color/depth/
        /// 渲染器列表槽有意保持未连接 —— pass 从自身参数分配它们；
        /// 颜色目标链为 forwardOpaque → sky → transparency → wireOverlay /
        /// finalBlit。全局 shader 资源（光照 / 阴影 / 探针 / 簇剔除掩码）不再
        /// 走 slot 连接，而由显式依赖边表达（<see cref="Pass.Dependencies"/>）。
        /// </summary>
        [Test]
        public void Build_ConnectsKeyPassSlots()
        {
            var asset = Resources.Load<RenderGraphAsset>("RenderGraphs/StandardGraph");
            Assume.That(asset, Is.Not.Null,
                "Test requires StandardGraph.asset to be loadable.");

            List<Pass> result = asset.Build();

            Pass FindPass(string instanceName)
            {
                Pass pass = result.Find(p => p.PassName == instanceName);
                Assert.That(pass, Is.Not.Null,
                    $"Pass '{instanceName}' should be present in the build result.");
                return pass!;
            }

            bool HasDependency(Pass pass, string sourcePassName)
            {
                return pass.Dependencies.Any(d => d.PassName == sourcePassName);
            }

            // ── forwardOpaque：color / 渲染器列表本地分配（未连接）；
            //    depth 目标来自 depthNormal 预渲染深度；
            //    全局资源生产者经显式依赖边发现 ──

            var forwardOpaque = (ForwardPass)FindPass("forwardOpaque");
            Assert.That(forwardOpaque.ColorTargetSlot!.IsConnected, Is.False,
                "forwardOpaque.ColorTarget should be unconnected — the pass allocates it locally.");
            Assert.That(forwardOpaque.DepthTargetSlot!.IsConnected, Is.True,
                "forwardOpaque.DepthTarget should be connected through depthNormal.DepthTargetOutput.");
            Assert.That(forwardOpaque.ScreenSpaceShadowMaskSlot!.IsConnected, Is.True,
                "forwardOpaque.ScreenSpaceShadowMask should be connected through screenSpaceShadow.ShadowMaskOutput.");
            Assert.That(HasDependency(forwardOpaque, "buildLight"), Is.True,
                "forwardOpaque should depend on buildLight (LightDatas).");
            Assert.That(HasDependency(forwardOpaque, "drawShadow"), Is.True,
                "forwardOpaque should depend on drawShadow (ShadowMap).");
            Assert.That(HasDependency(forwardOpaque, "clusterProbe"), Is.True,
                "forwardOpaque should depend on clusterProbe (ReflectionProbe).");
            Assert.That(HasDependency(forwardOpaque, "clusterLight"), Is.True,
                "forwardOpaque should depend on clusterLight (LightMask).");

            // ── depthNormal：链头 —— 深度 / 法线目标本地分配（未连接），
            //    深度输出接入 forwardOpaque ──

            var depthNormal = (DepthNormalPass)FindPass("depthNormal");
            Assert.That(depthNormal.DepthTargetSlot!.IsConnected, Is.False,
                "depthNormal.DepthTarget should be unconnected — the pass allocates it locally.");
            Assert.That(depthNormal.NormalTargetSlot!.IsConnected, Is.False,
                "depthNormal.NormalTarget should be unconnected — the pass allocates it locally.");

            // ── screenSpaceShadow：深度自 depthNormal 接入，mask 输出接入 forwardOpaque；
            //    阴影图集 / 表经显式依赖边发现生产者（无 atlas slot）──

            var screenSpaceShadow = (ScreenSpaceShadowMaskPass)FindPass("screenSpaceShadow");
            Assert.That(screenSpaceShadow.DepthTargetSlot!.IsConnected, Is.True,
                "screenSpaceShadow.DepthTarget should be connected through depthNormal.DepthTargetOutput.");
            Assert.That(HasDependency(screenSpaceShadow, "drawShadow"), Is.True,
                "screenSpaceShadow should depend on drawShadow (ShadowMap).");

            // ── sky：color/depth 目标已连接（自 forwardOpaque 链式传入）──

            var sky = (BuiltinSkyPass)FindPass("sky");
            Assert.That(sky.ColorTargetSlot!.IsConnected, Is.True,
                "sky.ColorTarget should be connected through forwardOpaque.ColorTargetOutput.");
            Assert.That(sky.DepthTargetSlot!.IsConnected, Is.True,
                "sky.DepthTarget should be connected through forwardOpaque.DepthTargetOutput.");

            // ── transparency：color/depth 自 sky 链式传入，渲染器列表
            //    本地分配（未连接），全局资源生产者经显式依赖边发现 ──

            var transparency = (ForwardPass)FindPass("transparency");
            Assert.That(transparency.ColorTargetSlot!.IsConnected, Is.True,
                "transparency.ColorTarget should be connected through sky.ColorTargetOutput.");
            Assert.That(transparency.DepthTargetSlot!.IsConnected, Is.True,
                "transparency.DepthTarget should be connected through sky.DepthTargetOutput.");
            Assert.That(HasDependency(transparency, "buildLight"), Is.True,
                "transparency should depend on buildLight (LightDatas).");
            Assert.That(HasDependency(transparency, "drawShadow"), Is.True,
                "transparency should depend on drawShadow (ShadowMap).");
            Assert.That(HasDependency(transparency, "clusterProbe"), Is.True,
                "transparency should depend on clusterProbe (ReflectionProbe).");
            Assert.That(HasDependency(transparency, "clusterLight"), Is.True,
                "transparency should depend on clusterLight (LightMask).");

            // ── wireOverlay：color 目标已连接（自 transparency 链式传入）──

            var wireOverlay = (EditorWireOverlayPass)FindPass("wireOverlay");
            Assert.That(wireOverlay.ColorTargetSlot!.IsConnected, Is.True,
                "wireOverlay.ColorTarget should be connected through transparency.ColorTargetOutput.");

            // ── finalBlit：color 目标已连接（自 wireOverlay 链式传入）──

            var finalBlit = (RenderOutputPass)FindPass("finalBlit");
            Assert.That(finalBlit.ColorTargetSlot!.IsConnected, Is.True,
                "finalBlit.ColorTarget should be connected through wireOverlay.ColorTargetOutput.");
        }

        #endregion

        #region Build —— 拓扑排序

        /// <summary>
        /// <see cref="RenderGraphAsset.Build"/> 按拓扑序返回 pass。
        /// 链式模型沿 color/depth 目标链添加显式 pass 间边，因此
        /// <c>forwardOpaque</c> 必须先于 <c>sky</c>，后者先于
        /// <c>transparency</c>，后者先于 <c>wireOverlay</c>，
        /// 后者先于 <c>finalBlit</c>；<c>buildLight</c> 通过槽连接向
        /// <c>clusterLight</c> 提供 LightDatas，因此也必须最先执行。
        /// </summary>
        [Test]
        public void Build_OrdersPassesTopologically()
        {
            var asset = Resources.Load<RenderGraphAsset>("RenderGraphs/StandardGraph");
            Assume.That(asset, Is.Not.Null,
                "Test requires StandardGraph.asset to be loadable.");

            List<Pass> result = asset.Build();

            int IndexOf(string name) => result.FindIndex(p => p.PassName == name);

            Assert.That(IndexOf("buildLight"), Is.GreaterThanOrEqualTo(0),
                "buildLight should be present.");
            Assert.That(IndexOf("clusterLight"), Is.GreaterThanOrEqualTo(0),
                "clusterLight should be present.");
            Assert.That(IndexOf("forwardOpaque"), Is.GreaterThanOrEqualTo(0),
                "forwardOpaque should be present.");
            Assert.That(IndexOf("sky"), Is.GreaterThanOrEqualTo(0),
                "sky should be present.");
            Assert.That(IndexOf("transparency"), Is.GreaterThanOrEqualTo(0),
                "transparency should be present.");
            Assert.That(IndexOf("wireOverlay"), Is.GreaterThanOrEqualTo(0),
                "wireOverlay should be present.");
            Assert.That(IndexOf("finalBlit"), Is.GreaterThanOrEqualTo(0),
                "finalBlit should be present.");
            Assert.That(IndexOf("depthNormal"), Is.GreaterThanOrEqualTo(0),
                "depthNormal should be present.");

            Assert.That(IndexOf("buildLight"), Is.LessThan(IndexOf("clusterLight")),
                "buildLight (feeds LightDatas via slot connection) must be ordered before clusterLight.");

            Assert.That(IndexOf("depthNormal"), Is.LessThan(IndexOf("forwardOpaque")),
                "depthNormal (feeds the shared depth target) must be ordered before forwardOpaque.");

            Assert.That(IndexOf("depthNormal"), Is.LessThan(IndexOf("screenSpaceShadow")),
                "depthNormal (feeds the depth target) must be ordered before screenSpaceShadow.");
            Assert.That(IndexOf("drawShadow"), Is.LessThan(IndexOf("screenSpaceShadow")),
                "drawShadow (feeds the shadow atlas) must be ordered before screenSpaceShadow.");
            Assert.That(IndexOf("screenSpaceShadow"), Is.LessThan(IndexOf("forwardOpaque")),
                "screenSpaceShadow (feeds the shadow mask) must be ordered before forwardOpaque.");

            Assert.That(IndexOf("forwardOpaque"), Is.LessThan(IndexOf("sky")),
                "forwardOpaque (upstream color/depth pass) must be ordered before sky.");
            Assert.That(IndexOf("sky"), Is.LessThan(IndexOf("transparency")),
                "sky (upstream color/depth pass) must be ordered before transparency.");
            Assert.That(IndexOf("transparency"), Is.LessThan(IndexOf("wireOverlay")),
                "transparency (upstream color pass) must be ordered before wireOverlay.");
            Assert.That(IndexOf("wireOverlay"), Is.LessThan(IndexOf("finalBlit")),
                "wireOverlay (upstream color pass) must be ordered before finalBlit.");

            Assert.That(IndexOf("forwardOpaque"), Is.LessThan(IndexOf("transparency")),
                "forwardOpaque must be ordered before transparency (stable definition order).");
        }

        #endregion

        #region 设置

        /// <summary>
        /// <see cref="RenderGraphAsset.Settings"/> 反映配置值：
        /// <see cref="RenderGraphSettings.SHEvalMode"/> 为 <c>PerPixel</c>，
        /// <see cref="RenderGraphSettings.AllowHDR"/> 为 <c>true</c>。
        /// </summary>
        [Test]
        public void Settings_HasCorrectValues()
        {
            var asset = Resources.Load<RenderGraphAsset>("RenderGraphs/StandardGraph");
            Assume.That(asset, Is.Not.Null);

            Assert.That(asset.Settings.SHEvalMode, Is.EqualTo(SHEvalMode.PerPixel),
                "Standard graph should use PerPixel SH evaluation.");
            Assert.That(asset.Settings.AllowHDR, Is.True,
                "Standard graph should allow HDR.");
        }

        #endregion
    }
}
