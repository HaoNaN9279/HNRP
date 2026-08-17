# hnrp-refactor-phase1-6 - Work Plan

## TL;DR (For humans)
<!-- Fill this LAST, after the detailed plan below is written, so it summarizes the REAL plan. -->
<!-- Plain English for a non-engineer: NO file paths, NO todo numbers, NO wave/agent/tool names. -->

**What you'll get:** HNRP 渲染管线从旧架构（Pass 绑定 ScriptableObject、所有 Camera 共享状态、管线硬编码在 C# 中）完整重构为新架构（纯 C# Pass、每 Camera 独立渲染器、管线图外部化为资产文件）。38 个任务，11 个执行波次，全部 TDD 驱动。

**Why this approach:** 先修复 3 个阻断器（Editor 检查器适配纯 C# Pass、ComputeShader 加载从 Editor API 迁到 RuntimeResources、补充 Camera 的管线覆写字段），然后按架构文档的 6 个 Phase 顺序推进——核心框架 → Pass 迁移 → 资源替代 → 管线重构 → Editor 重构 → 旧代码清理。旧代码与新代码共存直至最后阶段，降低回归风险。

**What it will NOT do:** 不修改任何 Shader/HLSL/ComputeShader；不新增渲染功能（阴影、后处理、Volume 集成留待后续）；不修改现有公共 API 签名；不在 Phase 6 之前删除任何旧文件。

**Effort:** XL — 38 个 TDD 任务，11 个串行依赖波次
**Risk:** Medium — 核心阻断器已在 Phase 0 消除；旧代码共存保证每阶段可编译；Unity MCP 实时验证
**Decisions to sanity-check:** 
  - Editor 检查器改为基于 PassConfigBase（ScriptableObject）而非 Pass 实例（纯 C#）
  - ComputeShader 引用通过 RuntimeResources 加载，不再依赖 Editor-only AssetDatabase API
  - HNAdditionalCameraData 新增 pipelineConfigOverride 字段，旧 renderGraphViewIndex 标记废弃

Your next move: approve to start execution, or optionally run a high-accuracy dual-Momus review first. Full execution detail follows below.

---

> TL;DR (machine): XL effort, Medium risk, 38 TDD todos across 11 waves, Phase 0→6 sequential refactoring of Unity 2022.3 HNRP custom render pipeline per architecture doc ADRs.

## Scope
### Must have
1. **Phase 1 — 核心框架**：Pass纯C#抽象类、PassSlot(name-based)、[Pass]Attribute、PassRegistry(反射+Built生成)、PassConfigBase、RenderGraphAsset、PassDefinition、SlotConnection、CameraPipelineConfig、CameraContext、CameraRenderer
2. **Phase 2 — Pass迁移**：ForwardOpaquePass、BuiltinSkyPass、TransparencyPass、ColorBufferInput、DepthBufferInput、RenderOutput、BuildLightDataPass、ClusterCullingLightPass、ClusterCullingReflectionProbePass、EditorWireOverlayPass、DrawObjectPass 从 PassBase(ScriptableObject)→Pass(纯C#)
3. **Phase 3 — 资源替代**：创建 StandardGraph.asset / PreviewGraph.asset RenderGraphAsset资源 + 对应PassConfig ScriptableObject子类资源
4. **Phase 4 — HNRenderPipeline重构**：用CameraRenderer替代RenderRequest流程，每Camera独立渲染
5. **Phase 5 — Editor重构**：RenderGraphAssetEditor、新PassEditor(替代PassBaseEditor)
6. **Phase 6 — 清理**：删除架构文档第9节列出的废弃文件
7. **TDD全覆盖**：所有新组件先写测试，EditMode通过Unity MCP验证
8. **Editor代码同步**：每Phase Runtime变更包含对应Editor Inspector变更

### Must NOT have (guardrails, anti-slop, scope boundaries)
- **禁止**修改任何Shader/HLSL/ComputeShader文件（ShaderLibrary/ 目录完全不动）
- **禁止**新增渲染功能——不做阴影、后处理、Volume等TODO列表内容
- **禁止**修改现有Material资源或Runtime/Utils/工具类
- **禁止**修改HNAdditionalCameraData/LightData/ReflectionProbeData的public API签名
- **禁止**在Pass.Record()中直接new ComputeBuffer()/new RenderTexture()——必须通过RenderGraph API
- **禁止**Phase间遗留未通过测试的代码；每个todo的测试必须全部通过才能进入下一个todo
- **禁止**在Phase 6之前删除任何旧文件——新旧代码共存，确保编译通过

## Verification strategy
> Zero human intervention - all verification is agent-executed.

- **Test decision**: TDD — 每个组件先写失败测试 → 实现 → 运行通过
- **Framework**: Unity Test Framework (NUnit), EditMode only
- **Test assembly**: Tests/EditMode/HN.HNRP.Tests.EditMode.asmdef（引用HN.HNRP + UnityEditor.TestRunner）
- **Runner**: `vibe_unityMCP_run_tests(mode="EditMode")` → `vibe_unityMCP_get_test_job(job_id)` 查询结果
- **Evidence**: `.omo/evidence/task-<N>-hnrp-refactor-phase1-6.md` — 每条包含测试运行截图/console输出
- **每Phase门禁**: Phase内所有测试通过 → 方可进入下一Phase
- **最终验证**: Phase 6后运行全量 `vibe_unityMCP_run_tests(mode="EditMode")` 确保无回归
- **编译验证**: 每todo完成后 `vibe_unityMCP_refresh_unity(scope="scripts", compile="request", wait_for_ready=true)` 确认编译通过

## Execution strategy
### Parallel execution waves
> Target 5-8 todos per wave. Fewer than 3 (except the final) means you under-split.

| Wave | Phase | Todos | Description |
|------|-------|-------|-------------|
| W0 | 0 | 1-3 | 基础修复 (Editor管道/ComputeShader加载/pipelineConfigOverride) |
| W1 | 0 | 4 | 测试基础设施搭建 |
| W2 | 1 | 5-8 | Pass核心框架 (Pass/PassSlot/PassAttribute/PassRegistry/PassConfigBase) |
| W3 | 1 | 9-11 | RenderGraphAsset + CameraPipelineConfig |
| W4 | 1 | 12-14 | CameraContext/CameraRenderer + PassRegistryGenerator |
| W5 | 2 | 15-18 | Pass迁移Part A (简单Pass: Color/Depth/RenderOutput/BuildLightData) |
| W6 | 2 | 19-22 | Pass迁移Part B (核心Pass: ForwardOpaque/Sky/Transparency) |
| W7 | 2 | 23-26 | Pass迁移Part C (Cluster/WireOverlay/DrawObject + CS加载修复) |
| W8 | 3 | 27-29 | 资源替代 (StandardGraph/PreviewGraph/PassConfig) |
| W9 | 4 | 30-32 | HNRenderPipeline重构 |
| W10 | 5 | 33-35 | Editor重构 |
| W11 | 6 | 36-37 | 清理 + 全量验证 |

### Dependency matrix
| Todo | Depends on | Blocks | Can parallelize with |
|------|-----------|--------|---------------------|
| 1 Editor重设计 | — | 15-26 | 2,3 |
| 2 CS加载迁移 | — | 23,24 | 1,3 |
| 3 pipelineConfigOverride | — | 30 | 1,2 |
| 4 测试基础设施 | — | 5-14 | — |
| 5 Pass抽象类 | 4 | 6,7,9,12 | — |
| 6 PassSlot | 5 | 7,9 | — |
| 7 [Pass]Attribute+PassRegistry | 5,6 | 8-26 | — |
| 8 PassConfigBase | 5 | 9,27 | 7 |
| 9 PassDefinition+SlotConnection | 5,6 | 10 | 8 |
| 10 RenderGraphAsset | 9 | 11,12 | — |
| 11 CameraPipelineConfig | 10 | 12,27 | — |
| 12 CameraContext | 5,10 | 13 | 11 |
| 13 CameraRenderer | 12 | 14,30 | — |
| 14 PassRegistryGenerator | 7,13 | 15-26 | — |
| 15 ColorBufferInput迁移 | 1,14 | — | 16,17,18 |
| 16 DepthBufferInput迁移 | 1,14 | — | 15,17,18 |
| 17 RenderOutput迁移 | 1,14 | — | 15,16,18 |
| 18 BuildLightDataPass迁移 | 1,14 | 19 | 15,16,17 |
| 19 ForwardOpaquePass迁移 | 1,14,18 | 20,27 | — |
| 20 BuiltinSkyPass迁移 | 1,14,19 | 21 | — |
| 21 TransparencyPass迁移 | 1,14,20 | 22 | — |
| 22 EditorWireOverlayPass迁移 | 1,14,21 | — | 23,24,25,26 |
| 23 ClusterCullingLightPass迁移 | 1,2,14,18 | — | 22,24,25,26 |
| 24 ClusterCullingReflectionProbePass迁移 | 1,2,14 | — | 22,23,25,26 |
| 25 DrawObjectPass迁移 | 1,14 | — | 22,23,24,26 |
| 26 RendererListInput处理 | 1,14 | — | 22,23,24,25 |
| 27 StandardGraph.asset | 11,19 | 30 | 28,29 |
| 28 PreviewGraph.asset | 11 | 30 | 27,29 |
| 29 PassConfig子类资源 | 8 | 30 | 27,28 |
| 30 HNRenderPipeline重构 | 3,13,27,28,29 | 31 | — |
| 31 HNRenderPipelineAsset重构 | 30 | 33 | — |
| 32 GlobalSettings更新 | 31 | 33 | — |
| 33 RenderGraphAssetEditor | 31,32 | 34 | — |
| 34 PassEditor重构 | 33 | 35 | — |
| 35 CameraDataEditor更新 | 34 | 36 | — |
| 36 删除废弃文件 | 35 | 37 | — |
| 37 全量测试验证 | 36 | — | — |

## Todos
> Implementation + Test = ONE todo. Never separate.
<!-- APPEND TASK BATCHES BELOW THIS LINE WITH edit/apply_patch - never rewrite the headers above. -->

### Wave 0: 基础修复 — 阻断器消除 (Phase 0)

> **来自 Metis 差距分析**：以下3个阻断器必须在任何新代码之前修复，否则后续Phase会因编辑器中断/运行时崩溃而无法验证。

- [x] 1. Editor 检查器重设计：纯 C# Pass 的 Editor 面
  **What to do**: 旧架构中 `PassBaseEditor` 通过 `CreateEditor(ScriptableObject)` 为 Pass 创建 Inspector。纯 C# Pass 不支持此API。新方案：**Editor 检查 PassConfigBase (ScriptableObject)** 而非 Pass 实例。创建 `Editor/Core/PassConfigEditor.cs` — 通用 PassConfigBase Inspector（显示Config属性）。每个 Pass 的编辑器在 Config 上操作；Pass 运行时状态通过 `CameraRenderer.FindPass<T>()` 查询但不做 Editor 面板。先写测试验证 ConfigEditor 可正确序列化/显示属性。
  **Must NOT do**: 不在纯 C# Pass 上调用 `CreateEditor()`；不在此阶段删除旧 PassBaseEditor（Phase 6做）。
  **Parallelization**: Wave 0 | Blocked by: — | Blocks: 15-26（所有Pass迁移依赖此Editor方案）
  **References**: Momus G1; Editor/Passes/PassBaseEditor.cs; 架构~/架构文档.md:122-126 (PassConfigBase作为统一接口)
  **Acceptance criteria**:
    - `Editor/Core/PassConfigEditor.cs` 存在
    - 选中 PassConfigBase 子类 .asset → 显示自定义Inspector
    - `Tests/EditMode/PassConfigEditorTests.cs` 测试Editor序列化
    - `vibe_unityMCP_refresh_unity(scope="scripts", compile="request")` 编译通过
  **QA scenarios**:
    - happy: 创建ForwardOpaqueConfig.asset → 选中 → Inspector显示Config属性（renderQueueRange等）
    - failure: Config为null → Editor不崩溃
    - Evidence: `.omo/evidence/task-1-hnrp-refactor-phase1-6.md`
  **Commit**: Y | `feat(editor): add PassConfigEditor for pure C# Pass inspection`

- [x] 2. ComputeShader 加载迁移至 RuntimeResources
  **What to do**: 修复阻断器：`ClusterCullingReflectionProbePass.cs:30` 和 `ClusterCullingLightPass.cs:30` 使用 `AssetDatabase.LoadAssetAtPath`（Editor-only API），导致 Player 中 CS 为 null 且聚类剔除静默失败。在 `HNRenderPipelineRuntimeResources` 中添加 `ComputeShaderResources` 子类（或直接添加字段：`clusterCullingLightCS`、`clusterCullingReflectionProbeCS`）。更新 `HNRenderPipelineRuntimeResourcesEditor` 确保资源引用可持久化。先写测试验证 CS 通过 RuntimeResources 加载。
  **Must NOT do**: 不删除旧加载代码（Phase 2迁移时替换）；不修改 ComputeShader 内容。
  **Parallelization**: Wave 0 | Blocked by: — | Blocks: 23,24（ClusterCulling Pass迁移）
  **References**: Metis G2; Runtime/HNRenderPipelineRuntimeResources.cs; Runtime/Passes/ClusterCullingReflectionProbePass.cs:28-32
  **Acceptance criteria**:
    - `HNRenderPipelineRuntimeResources` 包含 `ComputeShaderResources`（或等效字段）
    - `Tests/EditMode/ComputeShaderLoadingTests.cs` 测试CS可通过RuntimeResources访问
    - `vibe_unityMCP_run_tests(mode="EditMode", test_names=["ComputeShaderLoadingTests"])` 全部通过
  **QA scenarios**:
    - happy: RuntimeResources.clusterCullingLightCS != null → Pass可在Editor+Player中使用
    - failure: CS未分配 → 测试捕获并报告
    - Evidence: `.omo/evidence/task-0b-hnrp-refactor-phase1-6.md`
  **Commit**: Y | `fix(resources): move ComputeShader refs to RuntimeResources`

- [x] 3. 添加 HNAdditionalCameraData.pipelineConfigOverride
  **What to do**: 架构文档定义 `pipelineConfigOverride` 为 Camera 管线选择的最高优先级。在 `Runtime/HNAdditionalCameraData.cs` 中添加：`public CameraPipelineConfig pipelineConfigOverride;`（可空）。保留旧 `renderGraphViewIndex` 字段（标记 `[Obsolete]`，Phase 6删除）。实现优先级逻辑（在 CameraRenderer 中）：`pipelineConfigOverride` ?? `HNRenderPipelineAsset.defaultXxxConfig` ?? null。先写测试验证选择优先级。
  **Must NOT do**: 不删除 `renderGraphViewIndex`（Phase 6做）；不在Phase 4之前将此逻辑接入HNRenderPipeline。
  **Parallelization**: Wave 0 | Blocked by: — | Blocks: 30（HNRenderPipeline重构）
  **References**: Metis G3; 架构~/架构文档.md:85-88,196; Runtime/HNAdditionalCameraData.cs
  **Acceptance criteria**:
    - `HNAdditionalCameraData.cs` 包含 `pipelineConfigOverride` 字段
    - `Tests/EditMode/PipelineConfigSelectionTests.cs` 测试override优先于default
    - `vibe_unityMCP_run_tests(mode="EditMode", test_names=["PipelineConfigSelectionTests"])` 全部通过
  **QA scenarios**:
    - happy: Camera的pipelineConfigOverride=ConfigA → 即使defaultGameViewConfig=ConfigB也使用ConfigA
    - failure: override=null且default也为null → Camera被跳过（不崩溃）
    - Evidence: `.omo/evidence/task-0c-hnrp-refactor-phase1-6.md`
  **Commit**: Y | `feat(camera): add pipelineConfigOverride to HNAdditionalCameraData`

- [x] 4. 清理 Runtime 程序集中 Editor-only using 语句
  **What to do**: 现有 Runtime 代码存在未保护的 Editor using（`HNRenderPipeline.cs:6` 含 `using UnityEditor;` 无 `#if UNITY_EDITOR`、`HNRenderGraphBase.cs:6` 同样、`PassBase.cs:3` 含 VisualScripting using、`HNAdditionalCameraData.cs:3-6` 含悬垂 `System.Drawing`/`Codice.Client.*`）。添加 `#if UNITY_EDITOR` 保护或移除不需要的 using。运行时验证 `Runtime/HN.HNRP.asmdef` 不引用 Editor 程序集 GUID。先写测试验证编译在非Editor环境通过。
  **Must NOT do**: 不删除任何功能代码；不修改 asmdef 引用列表（仅验证）。
  **Parallelization**: Wave 0 | Blocked by: — | Blocks: — | Can parallelize with: 1,2,3
  **References**: Momus 2 Issue 2; Runtime/HNRenderPipeline.cs:4-6; Runtime/HNRenderGraphBase.cs:6; Runtime/HNAdditionalCameraData.cs:3-6; Runtime/PassBase.cs:3
  **Acceptance criteria**:
    - `HNRenderPipeline.cs` 中 `using UnityEditor;` 添加 `#if UNITY_EDITOR` 保护或移除
    - `HNRenderGraphBase.cs` 中 `using UnityEditor;` 添加 `#if UNITY_EDITOR` 保护
    - `PassBase.cs` 移除 `using Unity.VisualScripting.YamlDotNet.Core.Tokens;`
    - `vibe_unityMCP_refresh_unity(scope="scripts", compile="request")` 编译通过
  **QA scenarios**:
    - happy: 清理后编译通过，无 Editor-only 命名空间警告
    - failure: 误删需要的 using → 编译报错 → 回滚该条
    - Evidence: `.omo/evidence/task-4-hnrp-refactor-phase1-6.md`
  **Commit**: Y | `fix(runtime): remove unguarded Editor-only usings`



- [x] 4. 搭建 EditMode 测试程序集
  **What to do**: 创建 `Tests/EditMode/` 目录结构，创建 `HN.HNRP.Tests.EditMode.asmdef`（引用 HN.HNRP + UnityEditor.TestRunner），创建 `Tests/EditMode/Usings.cs`（global usings: NUnit.Framework, UnityEngine, HN.HNRP），通过 Unity MCP 验证测试程序集可被发现和运行。
  **Must NOT do**: 不创建PlayMode测试程序集；不在此todo中编写任何业务测试。
  **Parallelization**: Wave 1 | Blocked by: — | Blocks: 5-14
  **References**: Runtime/HN.HNRP.asmdef (assembly references); AGENTS.md:29-54 (MCP test flow)
  **Acceptance criteria**: 
    - `Tests/EditMode/HN.HNRP.Tests.EditMode.asmdef` 存在且引用正确
    - `vibe_unityMCP_find_in_file(uri="Tests/EditMode/HN.HNRP.Tests.EditMode.asmdef", pattern="HN.HNRP")` 找到引用
    - `vibe_unityMCP_refresh_unity(scope="all", compile="request")` 编译通过
    - `vibe_unityMCP_run_tests(mode="EditMode", assembly_names=["HN.HNRP.Tests.EditMode"])` 返回 job_id（即使0个测试）
  **QA scenarios**:
    - happy: 创建asmdef后Unity编译通过，run_tests返回有效job_id
    - failure: asmdef引用缺失 → run_tests报assembly not found
    - Evidence: `.omo/evidence/task-1-hnrp-refactor-phase1-6.md`
  **Commit**: Y | `chore(test): add EditMode test assembly for HN.HNRP`

### Wave 2: Pass 核心框架 (Phase 1 Part A)

- [x] 5. 实现 Pass 纯C#抽象类
  **What to do**: 创建 `Runtime/Core/Pass.cs` — 纯C#抽象类（不继承ScriptableObject）。包含：`abstract void SetupSlots()`、`abstract void Initialize()`、`abstract void Record(RenderGraph renderGraph)`、`abstract void Cleanup()`、`string PassName { get; }`、`bool IsEnabled { get; set; }`。先写测试：`PassTests.cs` 验证子类化、IsEnabled开关、生命周期调用顺序。注意：与旧 `PassBase` 共存，不修改旧代码。
  **Must NOT do**: 不继承ScriptableObject；不在Pass中持有HNRenderGraphBase引用；不在Record中直接new资源。
  **Parallelization**: Wave 2 | Blocked by: 5 | Blocks: 6,7,9,12
  **References**: 架构~/架构文档.md:111-118 (Pass系统设计); Runtime/RenderGraph/PassBase.cs:10-57 (旧PassBase)
  **Acceptance criteria**:
    - `Runtime/Core/Pass.cs` 存在，是纯C#抽象类（非ScriptableObject）
    - `Tests/EditMode/PassTests.cs` 包含测试：`Pass_Subclass_CanBeInstantiated`、`Pass_IsEnabled_DefaultsTrue`、`Pass_Lifecycle_SetupThenInitThenRecordThenCleanup`
    - `vibe_unityMCP_run_tests(mode="EditMode", test_names=["PassTests"])` 全部通过
  **QA scenarios**:
    - happy: 子类化Pass，依次调用SetupSlots→Initialize→Record→Cleanup，Record在IsEnabled=false时跳过
    - failure: 未调用SetupSlots直接Record → 测试捕获异常
    - Evidence: `.omo/evidence/task-2-hnrp-refactor-phase1-6.md`
  **Commit**: Y | `feat(core): add Pass abstract base class`

- [x] 6. 实现 PassSlot (name-based)
  **What to do**: 创建 `Runtime/Core/PassSlot.cs` — name-based slot系统。`TextureSlot(name, SlotDirection)`、`ComputeBufferSlot(name, SlotDirection)`、`RendererListSlot(name, SlotDirection)`。SlotDirection: Input/Output。每个slot持有name和Handle（Output slot创建Handle，Input slot通过connection读取）。先写测试：`PassSlotTests.cs` 验证name唯一性、方向枚举、Handle传递。
  **Must NOT do**: 不使用index-based连线；不与旧PassSlot.cs产生命名冲突（新类用不同类名或namespace区分期的）。
  **Parallelization**: Wave 2 | Blocked by: T2 | Blocks: T4,T6
  **References**: 架构~/架构文档.md:113-114 (Slot类型); Runtime/RenderGraph/PassSlot.cs:10-65 (旧PassSlot)
  **Acceptance criteria**:
    - `Runtime/Core/PassSlot.cs` 存在，包含TextureSlot/ComputeBufferSlot/RendererListSlot类
    - slot使用name标识，非index
    - `Tests/EditMode/PassSlotTests.cs` 包含测试：`Slot_NameMustBeUnique`、`Slot_Direction_InputOutput`、`OutputSlot_CreatesHandle`、`InputSlot_ReadsConnectedHandle`
    - `vibe_unityMCP_run_tests(mode="EditMode", test_names=["PassSlotTests"])` 全部通过
  **QA scenarios**:
    - happy: 创建Output TextureSlot → 创建Input TextureSlot → 连接 → Input读取到Output的Handle
    - failure: 同名slot → 检测到重复并报错
    - Evidence: `.omo/evidence/task-3-hnrp-refactor-phase1-6.md`
  **Commit**: Y | `feat(core): add name-based PassSlot system`

- [x] 7. 实现 [Pass]Attribute + PassRegistry
  **What to do**: 创建 `Runtime/Core/PassAttribute.cs` (`[Pass("DisplayName")]` Attribute)，创建 `Runtime/Core/PassRegistry.cs`（静态类：`RegisterAll()` 扫描程序集中带[Pass]的类，`GetPassType(name)` 按名称查找，`GetAllPassTypes()` 返回全部）。先写测试：`PassRegistryTests.cs` 用标记了[Pass]的stub类验证反射发现。
  **Must NOT do**: 不在此todo中实现Build时生成器(PassRegistryGenerator在T11)；不在Editor代码中实现注册逻辑（Runtime用反射兼容Editor，Player用生成的代码）。
  **Parallelization**: Wave 2 | Blocked by: 5,6 | Blocks: 8-26
  **References**: 架构~/架构文档.md:117-119 ([Pass]Attribute + 注册策略); Runtime/RenderGraph/PassBase.cs (旧Pass没有Attribute)
  **Acceptance criteria**:
    - `Runtime/Core/PassAttribute.cs` 存在
    - `Runtime/Core/PassRegistry.cs` 存在
    - `Tests/EditMode/PassRegistryTests.cs` 包含测试：`RegisterAll_DiscoversPassTypes`、`GetPassType_ByName_ReturnsCorrectType`、`GetAllPassTypes_ReturnsAll`
    - `vibe_unityMCP_run_tests(mode="EditMode", test_names=["PassRegistryTests"])` 全部通过
  **QA scenarios**:
    - happy: 定义`[Pass("TestPass")] class TestPass : Pass {}` → RegisterAll() → GetPassType("TestPass") == typeof(TestPass)
    - failure: 未标记[Pass]的子类不被发现
    - Evidence: `.omo/evidence/task-4-hnrp-refactor-phase1-6.md`
  **Commit**: Y | `feat(core): add [Pass] attribute and PassRegistry reflection`

- [x] 8. 实现 PassConfigBase
  **What to do**: 创建 `Runtime/Core/PassConfigBase.cs` — ScriptableObject基类，作为Pass参数的统一接口。包含：`virtual void ApplyToPass(Pass pass)`（将配置应用到Pass实例）。先写测试：`PassConfigBaseTests.cs` 验证子类化、序列化、Instantiate创建运行时副本。
  **Must NOT do**: 不在此todo中创建具体Pass的Config子类（在Phase 3 T26做）；Config不持有Pass引用（单向依赖）。
  **Parallelization**: Wave 2 | Blocked by: 5 | Blocks: 9,27 | Can parallelize with: 7
  **References**: 架构~/架构文档.md:122-126 (PassConfigBase设计); ADR 007,015
  **Acceptance criteria**:
    - `Runtime/Core/PassConfigBase.cs` 存在
    - `Tests/EditMode/PassConfigBaseTests.cs` 包含测试：`Config_CanBeSubclassed`、`Config_Instantiate_CreatesIndependentCopy`、`Config_ApplyToPass_ModifiesPassState`
    - `vibe_unityMCP_run_tests(mode="EditMode", test_names=["PassConfigBaseTests"])` 全部通过
  **QA scenarios**:
    - happy: 子类化PassConfigBase → ScriptableObject.CreateInstance → 设置属性 → ApplyToPass生效
    - failure: Instantiate后的副本修改不影响原始Config
    - Evidence: `.omo/evidence/task-5-hnrp-refactor-phase1-6.md`
  **Commit**: Y | `feat(core): add PassConfigBase ScriptableObject`

### Wave 3: RenderGraphAsset + CameraPipelineConfig (Phase 1 Part B)

- [x] 9. 实现 PassDefinition + SlotConnection 数据类
  **What to do**: 创建 `Runtime/Config/PassDefinition.cs`（`[Serializable] class PassDefinition { string passType; string instanceName; PassConfigBase config; }`）和 `Runtime/Config/SlotConnection.cs`（`[Serializable] class SlotConnection { string sourcePass; string sourceSlot; string targetPass; string targetSlot; }`）。先写测试：`PassDefinitionTests.cs` 验证序列化/反序列化；`SlotConnectionTests.cs` 验证name匹配。
  **Must NOT do**: 不在此todo中实现连接解析逻辑（在CameraRenderer中做）。
  **Parallelization**: Wave 3 | Blocked by: T2,T3 | Blocks: T7 | Can parallelize with: T5
  **References**: 架构~/架构文档.md:92-96 (RenderGraphAsset字段)
  **Acceptance criteria**:
    - `Runtime/Config/PassDefinition.cs` + `Runtime/Config/SlotConnection.cs` 存在
    - `Tests/EditMode/PassDefinitionTests.cs` 验证序列化往返
    - `Tests/EditMode/SlotConnectionTests.cs` 验证source/target name字段
    - `vibe_unityMCP_run_tests(mode="EditMode", test_names=["PassDefinitionTests","SlotConnectionTests"])` 全部通过
  **QA scenarios**:
    - happy: 创建PassDefinition → 序列化 → 反序列化 → passType/instanceName/config一致
    - failure: SlotConnection中sourcePass=null → 验证抛出
    - Evidence: `.omo/evidence/task-6-hnrp-refactor-phase1-6.md`
  **Commit**: Y | `feat(core): add PassDefinition and SlotConnection data classes`

- [x] 10. 实现 RenderGraphAsset
  **What to do**: 创建 `Runtime/Config/RenderGraphAsset.cs` — ScriptableObject，持有 `List<PassDefinition> passes` + `List<SlotConnection> connections` + `RenderGraphSettings settings`。提供 `Build(CameraRenderer renderer)` 方法：根据PassDefinition用PassRegistry实例化Pass → 根据SlotConnection连接slot → 返回List<Pass>。先写测试验证pass实例化和slot连接。
  **Must NOT do**: 不在此todo中创建任何.asset资源文件（在Phase 3做）；不包含执行逻辑（Record/Execute在CameraRenderer）。
  **Parallelization**: Wave 3 | Blocked by: T6 | Blocks: T8,T9
  **References**: 架构~/架构文档.md:90-101 (RenderGraphAsset + 模板与实例); ADR 002,003,011
  **Acceptance criteria**:
    - `Runtime/Config/RenderGraphAsset.cs` 存在，继承ScriptableObject
    - `Tests/EditMode/RenderGraphAssetTests.cs` 包含测试：`Build_InstantiatesPasses`、`Build_ConnectsSlots_ByName`、`Build_ReturnsEnabledPassesOnly`
    - `vibe_unityMCP_run_tests(mode="EditMode", test_names=["RenderGraphAssetTests"])` 全部通过
  **QA scenarios**:
    - happy: RenderGraphAsset定义2个PassDefinition + 1个SlotConnection → Build() → 返回2个已连接的Pass实例
    - failure: SlotConnection引用不存在的Pass → Build()报错
    - Evidence: `.omo/evidence/task-7-hnrp-refactor-phase1-6.md`
  **Commit**: Y | `feat(core): add RenderGraphAsset pipeline template`

- [x] 11. 实现 CameraPipelineConfig
  **What to do**: 创建 `Runtime/Config/CameraPipelineConfig.cs` — ScriptableObject中间层，持有 `RenderGraphAsset renderGraph` + `settingsOverride`（可选）。Camera通过此Config间接引用管线图。先写测试验证Config引用RenderGraphAsset、settingsOverride合并逻辑。
  **Must NOT do**: 不在此todo中创建Config资源文件（在Phase 3做）。
  **Parallelization**: Wave 3 | Blocked by: T7 | Blocks: T9,T24
  **References**: 架构~/架构文档.md:79-88 (CameraPipelineConfig中间层); ADR 006
  **Acceptance criteria**:
    - `Runtime/Config/CameraPipelineConfig.cs` 存在
    - `Tests/EditMode/CameraPipelineConfigTests.cs` 包含测试：`Config_ReferencesRenderGraphAsset`、`SettingsOverride_MergesCorrectly`
    - `vibe_unityMCP_run_tests(mode="EditMode", test_names=["CameraPipelineConfigTests"])` 全部通过
  **QA scenarios**:
    - happy: CameraPipelineConfig引用RenderGraphAsset → 通过Config获取管线图
    - failure: renderGraph为null → 正确处理null
    - Evidence: `.omo/evidence/task-8-hnrp-refactor-phase1-6.md`
  **Commit**: Y | `feat(core): add CameraPipelineConfig intermediate layer`

### Wave 4: CameraRenderer + Editor Generator (Phase 1 Part C)

- [x] 12. 实现 CameraContext
  **What to do**: 创建 `Runtime/Core/CameraContext.cs` — 每Camera独立上下文（class，非struct）。持有：`Camera camera`、`ScriptableRenderContext context`、`CullingResults cullingResults`、`CommandBuffer cmd`（通过 `CommandBufferPool.Get` 分配，`CommandBufferPool.Release` 释放）、`RenderTargetIdentifier targetId`、`NativeArray<VisibleLight> visibleLights`、`NativeArray<VisibleReflectionProbe> visibleReflectionProbes`、`VisibleReflectionProbe[] catchedReflectionProbes`、`HNRenderPipelineRuntimeResources runtimeResources`、`GlobalConstantBuffer constantBuffer`。注意：`ScriptableRenderContext` 是 Unity RenderPipeline.Render() 传入，必须传递到 CameraRenderer 用于 `RenderGraph.RecordAndExecute`。
  **Must NOT do**: 不使用struct（旧RenderingData是struct导致共享问题）；不持有对HNRenderGraphBase的引用。
  **Parallelization**: Wave 4 | Blocked by: T2,T7 | Blocks: T10 | Can parallelize with: T8
  **References**: 架构~/架构文档.md:147-149 (CameraContext); Runtime/RenderingData.cs:11-46 (旧RenderingData struct); ADR 004
  **Acceptance criteria**:
    - `Runtime/Core/CameraContext.cs` 存在，是class（非struct）
    - `Tests/EditMode/CameraContextTests.cs` 包含测试：`Context_CreatedForCamera`、`Context_StoresCullingResults`、`Context_DisposeReleasesCmd`
    - `vibe_unityMCP_run_tests(mode="EditMode", test_names=["CameraContextTests"])` 全部通过
  **QA scenarios**:
    - happy: 为Camera创建Context → 存储CullingResults → 获取visibleLights → Dispose释放Cmd
    - failure: 未初始化就访问 → 抛出
    - Evidence: `.omo/evidence/task-9-hnrp-refactor-phase1-6.md`
  **Commit**: Y | `feat(core): add per-Camera CameraContext`

- [x] 13. 实现 CameraRenderer
  **What to do**: 创建 `Runtime/Core/CameraRenderer.cs` — 每Camera独立渲染器。持有 `List<Pass> passes` + `CameraContext context`。核心API：`Build(RenderGraphAsset template)`、`AddPass<T>(name, connections)`、`RemovePass(name)`、`FindPass<T>(name)`、`GetConfig<T>(name)`（通过 `ScriptableObject.Instantiate` 创建独立副本；注意此为浅拷贝，PassConfig子类应避免可变集合类型属性）、`SetPassEnabled(name, enabled)`、`Connect()/Disconnect()`、`Reset(template)`、`Render(RenderGraph renderGraph, ScriptableRenderContext context)`。**RenderGraph 由 HNRenderPipeline 持有单例**，所有 CameraRenderer.Render() 共享同一 RenderGraph 实例，通过顺序 `RecordAndExecute` 调用（与现有模式一致，避免每 Camera 创建开销）。
  **Must NOT do**: 不在此todo中接入HNRenderPipeline（在Phase 4做）；AddPass/RemovePass不设计为每帧高频调用。
  **Parallelization**: Wave 4 | Blocked by: T9 | Blocks: T11,T27
  **References**: 架构~/架构文档.md:127-146 (CameraRenderer API); Runtime/HNRenderPipeline.cs:33-64 (旧Render流程)
  **Acceptance criteria**:
    - `Runtime/Core/CameraRenderer.cs` 存在
    - `Tests/EditMode/CameraRendererTests.cs` 包含测试：`Build_FromTemplate_InstantiatesPasses`、`AddPass_AppendsToList`、`RemovePass_ByName_Works`、`GetConfig_ReturnsIndependentCopy`、`SetPassEnabled_TogglesExecution`、`Reset_RestoresTemplateState`
    - `vibe_unityMCP_run_tests(mode="EditMode", test_names=["CameraRendererTests"])` 全部通过
  **QA scenarios**:
    - happy: CameraRenderer.Build(template) → passes列表正确 → AddPass追加 → RemovePass移除 → Reset恢复
    - failure: 重复Add同名Pass → 报错
    - Evidence: `.omo/evidence/task-10-hnrp-refactor-phase1-6.md`
  **Commit**: Y | `feat(core): add per-Camera CameraRenderer`

- [x] 14. 实现 PassRegistryGenerator (Editor/Build)
  **What to do**: 创建 `Editor/Config/PassRegistryGenerator.cs` — Editor工具/Build处理器。`[InitializeOnLoad]` 或 `IPreprocessBuildWithReport` 触发：扫描所有带[Pass]的类 → 生成 `Runtime/Core/Generated/PassRegistryGenerated.cs`（硬编码注册表，无反射）。生成的代码格式：`static void RegisterAll() { Register("PassName", typeof(XxxPass)); ... }`。`Runtime/Core/PassRegistry.cs` 在Editor用反射，在Player用Generated。先写测试验证生成的代码语法正确、注册完整。
  **Must NOT do**: 不在此todo中修改Runtime/PassRegistry.cs的核心反射逻辑（Generated是补充加速路径）；不修改.csproj或build pipeline配置。
  **Parallelization**: Wave 4 | Blocked by: T4,T10 | Blocks: W5-W8
  **References**: 架构~/架构文档.md:119 (注册策略); 架构~/架构文档.md:237 (PassRegistryGenerated.cs位置)
  **Acceptance criteria**:
    - `Editor/Config/PassRegistryGenerator.cs` 存在
    - 生成的 `Runtime/Core/Generated/PassRegistryGenerated.cs` 语法正确
    - `Tests/EditMode/PassRegistryGeneratorTests.cs` 测试：`GeneratedCode_RegistersAllPasses`、`Registry_EditorUsesReflection_PlayerUsesGenerated`
    - `vibe_unityMCP_run_tests(mode="EditMode", test_names=["PassRegistryGeneratorTests"])` 全部通过
  **QA scenarios**:
    - happy: 触发生成 → Generated文件包含所有[Pass]类 → Player中无反射调用
    - failure: 无[Pass]类 → Generated文件为空注册表（不崩溃）
    - Evidence: `.omo/evidence/task-11-hnrp-refactor-phase1-6.md`
  **Commit**: Y | `feat(editor): add PassRegistryGenerator for build-time registration`

### Wave 5: Pass 迁移 Part A — 简单 Pass (Phase 2)

- [x] 15. 迁移 ColorBufferInput（PassBase → Pass）
  **What to do**: 基于新 `Pass` 基类重写 `ColorBufferInput`。保持原有Record逻辑（`builder.UseColorBuffer`），`SetupSlots()` 中声明 `colorTargetSlot` Output，`Initialize()` 中从Config读取参数。旧 `Runtime/Passes/ColorBufferInput.cs` 保留不改，新类命名为 `ColorBufferInputPass` 放同目录。先写测试验证slot声明和Record调用。Editor对应更新：`Editor/Passes/ColorBufferInputEditor.cs` 适配新基类。
  **Must NOT do**: 不删除旧ColorBufferInput.cs（Phase 6统一删）；不修改Shader/HLSL。
  **Parallelization**: Wave 5 | Blocked by: 1,14 | Blocks: — | Can parallelize with: 16,17,18
  **References**: 架构~/架构文档.md:170-171 (ColorBufferInput在管线中的位置); Runtime/Passes/ColorBufferInput.cs
  **Acceptance criteria**:
    - `Runtime/Passes/ColorBufferInputPass.cs` 存在，继承Pass
    - `Tests/EditMode/ColorBufferInputPassTests.cs` 测试SetupSlots声明colorTargetSlot、Record正确使用UseColorBuffer
    - `vibe_unityMCP_run_tests(mode="EditMode", test_names=["ColorBufferInputPassTests"])` 全部通过
  **QA scenarios**:
    - happy: 实例化ColorBufferInputPass → SetupSlots声明Output color slot → Record中UseColorBuffer
    - failure: 未SetupSlots直接Record → 测试捕获
    - Evidence: `.omo/evidence/task-12-hnrp-refactor-phase1-6.md`
  **Commit**: Y | `refactor(pass): migrate ColorBufferInput to new Pass base`

- [x] 16. 迁移 DepthBufferInput（PassBase → Pass）
  **同 15 模式**。新类 `DepthBufferInputPass`。SetupSlots声明 `depthTargetSlot` Output。Record中使用 `builder.UseDepthBuffer`。
  **Parallelization**: Wave 5 | Blocked by: 1,14 | Blocks: — | Can parallelize with: 15,17,18
  **Acceptance criteria**: DepthBufferInputPassTests通过，slot声明+Record逻辑正确
  **Commit**: Y | `refactor(pass): migrate DepthBufferInput to new Pass base`

- [x] 17. 迁移 RenderOutput（PassBase → Pass）
  **同 15 模式**。新类 `RenderOutputPass`。SetupSlots声明 `colorTargetSlot` Input。Record中使用 `builder.UseColorBuffer` + Blitter.BlitCameraTexture。注意Blitter依赖需在Initialize中获取。
  **Parallelization**: Wave 5 | Blocked by: 1,14 | Blocks: — | Can parallelize with: 15,16,18
  **Acceptance criteria**: RenderOutputPassTests通过
  **Commit**: Y | `refactor(pass): migrate RenderOutput to new Pass base`

- [x] 18. 迁移 BuildLightDataPass（PassBase → Pass）
  **新类 `BuildLightDataPassV2`**（BuildLightDataPass已有子目录，为避名冲突加V2后缀）。SetupSlots声明 `lightDatasBufferSlot` Output（ComputeBufferSlot）。Record中保持原有灯光数据构建逻辑（BuildLightDataJob），通过CameraContext获取visibleLights。注意：此Pass的ComputeShader依赖需通过 `CameraContext.RuntimeResources` 获取。
  **Parallelization**: Wave 5 | Blocked by: 1,14 | Blocks: 19 | Can parallelize with: 15,16,17
  **Acceptance criteria**: BuildLightDataPassV2Tests通过，ComputeBuffer slot + Job逻辑正确
  **Commit**: Y | `refactor(pass): migrate BuildLightDataPass to new Pass base`

### Wave 6: Pass 迁移 Part B — 核心 Pass (Phase 2)

- [x] 19. 迁移 ForwardOpaquePass（PassBase → Pass）
  **重要**：这是最复杂的Pass。新类 `ForwardOpaquePassV2`。SetupSlots声明7个slot（colorTarget ReadWrite、depthTarget ReadWrite、lightDatasBuffer ReadOnly、reflectionProbeAtlas ReadOnly、clusterCullingReflectionProbeMaskBuffer ReadOnly、clusterCullingReflectionProbeDatasBuffer ReadOnly、clusterCullingLightMaskBuffer ReadOnly）。Record中保持原有RendererList创建+DrawRendererList逻辑。Slot连接检查替换为 `IsConnected` 属性。
  **Must NOT do**: 不修改ForwardOpaquePassData内部结构（保持PassData兼容）；不修改Shader Keywords设置逻辑。
  **Parallelization**: Wave 6 | Blocked by: 1,14,18 | Blocks: 20,27
  **References**: Runtime/Passes/ForwardOpaquePass.cs:13-149 (完整Record逻辑); 架构~/架构文档.md:175 (管线中的位置)
  **Acceptance criteria**:
    - `Runtime/Passes/ForwardOpaquePassV2.cs` 存在，继承Pass
    - `Tests/EditMode/ForwardOpaquePassV2Tests.cs` 测试：7个slot正确声明、Record使用RendererList、连接检查
    - `vibe_unityMCP_run_tests(mode="EditMode", test_names=["ForwardOpaquePassV2Tests"])` 全部通过
  **QA scenarios**:
    - happy: SetupSlots声明全部7个slot → Record中使用connected slot的Handle
    - failure: lightDatasBuffer未连接 → Record中跳过相关shader属性设置
    - Evidence: `.omo/evidence/task-16-hnrp-refactor-phase1-6.md`
  **Commit**: Y | `refactor(pass): migrate ForwardOpaquePass to new Pass base`

- [x] 20. 迁移 BuiltinSkyPass（PassBase → Pass）
  **新类 `BuiltinSkyPassV2`**。SetupSlots声明 colorTarget ReadWrite + depthTarget ReadOnly。Record中保持原有Skybox渲染逻辑。
  **Parallelization**: Wave 6 | Blocked by: 1,14,19 | Blocks: 21
  **Acceptance criteria**: BuiltinSkyPassV2Tests通过
  **Commit**: Y | `refactor(pass): migrate BuiltinSkyPass to new Pass base`

- [x] 21. 迁移 TransparencyPass（PassBase → Pass）
  **新类 `TransparencyPassV2`**。SetupSlots声明 colorTarget ReadWrite + depthTarget ReadOnly。Record中创建Transparent RendererList。
  **Parallelization**: Wave 6 | Blocked by: 1,14,20 | Blocks: 22
  **Acceptance criteria**: TransparencyPassV2Tests通过
  **Commit**: Y | `refactor(pass): migrate TransparencyPass to new Pass base`

- [x] 22. 迁移 EditorWireOverlayPass（PassBase → Pass）
  **新类 `EditorWireOverlayPassV2`**。注意：此Pass仅在UNITY_EDITOR下有效。SetupSlots声明 colorTarget ReadWrite。Record中绘制Editor UI overlay。
  **Parallelization**: Wave 6 | Blocked by: 1,14,21 | Blocks: —
  **Acceptance criteria**: EditorWireOverlayPassV2Tests通过（Editor only）
  **Commit**: Y | `refactor(pass): migrate EditorWireOverlayPass to new Pass base`

### Wave 7: Pass 迁移 Part C — Cluster/Draw (Phase 2)

- [x] 23. 迁移 ClusterCullingLightPass（PassBase → Pass）
  **新类 `ClusterCullingLightPassV2`**。依赖BuildLightDataPass的输出。SetupSlots声明 lightDatasBuffer ReadOnly + clusterCullingLightMaskBuffer Output。Record中保持Cluster Culling ComputeShader调度逻辑。
  **Parallelization**: Wave 7 | Blocked by: 1,2,14,18 | Blocks: — | Can parallelize with: 22,24,25,26
  **Acceptance criteria**: ClusterCullingLightPassV2Tests通过
  **Commit**: Y | `refactor(pass): migrate ClusterCullingLightPass to new Pass base`

- [x] 24. 迁移 ClusterCullingReflectionProbePass（PassBase → Pass）
  **新类 `ClusterCullingReflectionProbePassV2`**。SetupSlots声明3个Output：reflectionProbeAtlas、clusterCullingReflectionProbeMaskBuffer、clusterCullingReflectionProbeDatasBuffer。Record中保持原有Probe Culling ComputeShader逻辑。
  **Parallelization**: Wave 7 | Blocked by: 1,2,14 | Blocks: — | Can parallelize with: 22,23,25,26
  **Acceptance criteria**: ClusterCullingReflectionProbePassV2Tests通过
  **Commit**: Y | `refactor(pass): migrate ClusterCullingReflectionProbePass to new Pass base`

- [x] 25. 迁移 DrawObjectPass（PassBase → Pass）
  **新类 `DrawObjectPassV2`**。通用物体绘制Pass。SetupSlots声明所需slot。
  **Parallelization**: Wave 7 | Blocked by: 1,14 | Blocks: — | Can parallelize with: 22,23,24,26
  **Acceptance criteria**: DrawObjectPassV2Tests通过
  **Commit**: Y | `refactor(pass): migrate DrawObjectPass to new Pass base`

- [x] 26. 处理 RendererListInput（PassBase → 废弃或迁移）
  **What to do**: 检查 `Runtime/Passes/RendererListInput.cs` 是否被使用。通过 `vibe_unityMCP_find_in_file` 在整个项目搜索 `RendererListInput` 引用。架构文档标注为"未使用"。若确认未使用：不迁移，在Phase 6统一删除；若有使用：按 15 模式（ColorBufferInput）迁移为 `RendererListInputPass`。
  **Must NOT do**: 不在此todo删除（Phase 6做）。
  **Parallelization**: Wave 7 | Blocked by: 1,14 | Blocks: — | Can parallelize with: 22,23,24,25
  **Acceptance criteria**: 确认RendererListInput引用状态 → 记录到evidence → 若使用则迁移+测试，若不使用则标记待删除
  **Commit**: Y | `refactor(pass): audit and handle RendererListInput`

### Wave 8: 资源替代 (Phase 3)

- [x] 27. 创建 StandardGraph.asset（标准前向渲染管线图）
  **What to do**: 在 `Runtime/Resources/RenderGraphs/` 目录（若不存在则创建）通过 `vibe_unityMCP_manage_asset(action="create", ...)` 创建 `StandardGraph.asset`（RenderGraphAsset）。配置PassDefinition列表：buildLight→clusterProbe→clusterLight→colorInput→depthInput→forwardOpaque→sky→transparency→wireOverlay→finalBlit（使用新V2类名）。配置SlotConnection（参考架构文档第3节）。创建测试验证asset可被加载且Build()返回正确Pass列表。
  **Must NOT do**: 不引用旧PassBase类；不在此todo中创建PreviewGraph。
  **Parallelization**: Wave 8 | Blocked by: 11,19 | Blocks: 30 | Can parallelize with: 28,29
  **References**: 架构~/架构文档.md:164-186 (默认管线示例); Runtime/RenderPipeline/Standard.cs:10-52 (旧Standard定义)
  **Acceptance criteria**:
    - `Runtime/Resources/RenderGraphs/StandardGraph.asset` 存在
    - `Tests/EditMode/StandardGraphTests.cs` 测试：`LoadAsset_Valid`、`Build_Returns10Passes`、`Connections_AreValid`
    - `vibe_unityMCP_run_tests(mode="EditMode", test_names=["StandardGraphTests"])` 全部通过
  **QA scenarios**:
    - happy: 加载StandardGraph.asset → Build() → 返回10个已连接Pass实例
    - failure: 资源丢失 → 测试报告null
    - Evidence: `.omo/evidence/task-24-hnrp-refactor-phase1-6.md`
  **Commit**: Y | `feat(resource): create StandardGraph RenderGraphAsset`

- [x] 28. 创建 PreviewGraph.asset（预览渲染管线图）
  **同T24模式**。Preview管线通常更简单（跳过某些Pass）。创建 `PreviewGraph.asset` 引用较少的Pass。
  **Parallelization**: Wave 8 | Blocked by: 11 | Blocks: 30 | Can parallelize with: 27,29
  **References**: Runtime/RenderPipeline/Preview.cs (旧Preview定义)
  **Acceptance criteria**: PreviewGraph.asset可加载，Build()返回正确Pass列表
  **Commit**: Y | `feat(resource): create PreviewGraph RenderGraphAsset`

- [x] 29. 创建 PassConfig 子类 ScriptableObject 资源
  **What to do**: 为每个核心Pass创建对应的PassConfig子类ScriptableObject。如 `ForwardOpaqueConfig : PassConfigBase`（包含renderQueueRange、layerMask等参数）。在 `Runtime/Resources/PassConfigs/` 创建对应的.asset文件。先写测试验证Config子类的序列化和ApplyToPass。
  **Must NOT do**: 不在此todo中实现Volume覆写逻辑（TODO列表中的Volume系统）。
  **Parallelization**: Wave 8 | Blocked by: 8 | Blocks: 30 | Can parallelize with: 27,28
  **References**: 架构~/架构文档.md:122-126 (PassConfigBase); ADR 007,015
  **Acceptance criteria**:
    - `Runtime/Configs/` 下存在至少 `ForwardOpaqueConfig.cs`、`SkyConfig.cs`、`TransparencyConfig.cs`
    - `Runtime/Resources/PassConfigs/` 下对应.asset存在
    - `Tests/EditMode/PassConfigsTests.cs` 测试：`ForwardOpaqueConfig_Serialization`、`ForwardOpaqueConfig_ApplyToPass`
    - `vibe_unityMCP_run_tests(mode="EditMode", test_names=["PassConfigsTests"])` 全部通过
  **QA scenarios**:
    - happy: 加载ForwardOpaqueConfig.asset → 修改属性 → ApplyToPass生效
    - failure: Config子类缺少[Serializable] → 测试捕获
    - Evidence: `.omo/evidence/task-26-hnrp-refactor-phase1-6.md`
  **Commit**: Y | `feat(config): add PassConfig subclasses and assets`

### Wave 9: HNRenderPipeline 重构 (Phase 4)

- [x] 30. 重构 HNRenderPipeline.Render()
  **What to do**: 重写 `Runtime/HNRenderPipeline.cs` 的 `Render()` 和 `PrepareRenderRequests()` 方法。新流程：遍历cameras → 为每个Camera创建/获取 `CameraRenderer` → 根据 `CameraType` 选择 `CameraPipelineConfig`（Game→defaultGameViewConfig, SceneView→defaultSceneViewConfig, Preview→defaultPreviewConfig, Reflection→defaultReflectionConfig）→ `CameraRenderer.Build(config.renderGraph)` → `CameraRenderer.Render(renderGraph)`。移除旧的 RenderRequest 列表逻辑。先写测试验证Camera隔离、Config选择优先级。
  **Must NOT do**: 不在Phase 6之前删除RenderRequest相关旧文件；不破坏现有 `Dispose()` 逻辑。
  **Parallelization**: Wave 9 | Blocked by: 3,13,27,28,29 | Blocks: 31
  **References**: 架构~/架构文档.md:33-63 (总体架构、HNRenderPipeline); Runtime/HNRenderPipeline.cs:33-163 (旧Render流程)
  **Acceptance criteria**:
    - `Runtime/HNRenderPipeline.cs` Render()使用CameraRenderer
    - `Tests/EditMode/HNRenderPipelineTests.cs` 测试：`Render_UsesCameraRenderer`、`EachCamera_HasIndependentRenderer`、`ConfigSelection_ByCameraType`
    - 旧RenderRequest相关代码编译警告已消除
    - `vibe_unityMCP_run_tests(mode="EditMode", test_names=["HNRenderPipelineTests"])` 全部通过
  **QA scenarios**:
    - happy: 2个Camera(1 Game + 1 SceneView) → 各自独立的CameraRenderer → Render各自管线
    - failure: CameraPipelineConfig为null → 该Camera不渲染（不崩溃）
    - Evidence: `.omo/evidence/task-27-hnrp-refactor-phase1-6.md`
  **Commit**: Y | `refactor(pipeline): use CameraRenderer per camera`

- [x] 31. 重构 HNRenderPipelineAsset
  **What to do**: 更新 `Runtime/HNRenderPipelineAsset.cs`。移除旧 `RenderGraphViewBlock` 字段，添加：`CameraPipelineConfig defaultGameViewConfig`、`defaultSceneViewConfig`、`defaultPreviewConfig`、`defaultReflectionConfig`。Editor conditional下保留Editor-only Config。先写测试验证Config序列化和默认值。
  **Must NOT do**: 不修改public API签名（`CreatePipeline()`返回类型不变）；不在此todo删除旧字段（Phase 6）。
  **Parallelization**: Wave 9 | Blocked by: 30 | Blocks: 33
  **References**: 架构~/架构文档.md:24-32 (HNRenderPipelineAsset新设计); Runtime/HNRenderPipelineAsset.cs:10-58
  **Acceptance criteria**:
    - `HNRenderPipelineAsset.cs` 包含4个CameraPipelineConfig字段
    - `Tests/EditMode/HNRenderPipelineAssetTests.cs` 测试Config字段序列化
    - `vibe_unityMCP_run_tests(mode="EditMode", test_names=["HNRenderPipelineAssetTests"])` 全部通过
  **QA scenarios**:
    - happy: 在Inspector中分配Config → serialization正确 → CreatePipeline使用Config
    - failure: Config为null → 使用fallback
    - Evidence: `.omo/evidence/task-28-hnrp-refactor-phase1-6.md`
  **Commit**: Y | `refactor(pipeline): add CameraPipelineConfig fields to Asset`

- [x] 32. 更新 HNRenderPipelineGlobalSettings
  **What to do**: 添加 `List<CameraPipelineConfig> cameraPipelineConfigs` 字段到 `Runtime/HNRenderPipelineGlobalSettings.cs`。所有Config集中管理于此。更新 `Ensure()` 方法确保列表初始化。更新Editor UI（GlobalSettingsEditor）。
  **Must NOT do**: 不在此todo中创建Config资源（在Phase 3已做）。
  **Parallelization**: Wave 9 | Blocked by: 31 | Blocks: 33
  **References**: 架构~/架构文档.md:81 (Config集中于GlobalSettings); Runtime/HNRenderPipelineGlobalSettings.cs:10-247
  **Acceptance criteria**: GlobalSettings包含cameraPipelineConfigs列表；Editor中可添加/移除Config
  **Commit**: Y | `feat(settings): add cameraPipelineConfigs list to GlobalSettings`

### Wave 10: Editor 重构 (Phase 5)

- [x] 33. 实现 RenderGraphAssetEditor
  **What to do**: 创建 `Editor/Config/RenderGraphAssetEditor.cs` — 自定义Inspector，可视化编辑PassDefinition列表和SlotConnection列表。使用Unity Editor IMGUI或UI Toolkit。功能：添加/移除PassDefinition、编辑SlotConnection（source/target下拉选择）、预览连接图。
  **Must NOT do**: 不在此todo中实现可视化节点图编辑器（未来任务）；不删除旧的HNRenderGraphBaseEditor（Phase 6）。
  **Parallelization**: Wave 10 | Blocked by: T28,T29 | Blocks: T31
  **References**: 架构~/架构文档.md:244 (Editor/Config/ 目录); Editor/RenderGraph/HNRenderGraphBaseEditor.cs (旧Editor)
  **Acceptance criteria**:
    - `Editor/Config/RenderGraphAssetEditor.cs` 存在
    - 选中RenderGraphAsset时显示自定义Inspector
    - 可编辑PassDefinition列表（添加/移除/排序）
    - 可编辑SlotConnection列表（source/target name填写）
    - `vibe_unityMCP_refresh_unity(scope="scripts", compile="request")` 编译通过
  **QA scenarios**:
    - happy: 选中StandardGraph.asset → Inspector显示PassDefinition和SlotConnection编辑界面
    - failure: 空RenderGraphAsset → Inspector不崩溃
    - Evidence: `.omo/evidence/task-30-hnrp-refactor-phase1-6.md`
  **Commit**: Y | `feat(editor): add RenderGraphAsset custom inspector`

- [x] 34. 重构 Pass 编辑器（替代 PassBaseEditor）
  **What to do**: 创建 `Editor/Passes/PassEditor.cs` — 新的Pass Inspector基类（替代 `PassBaseEditor.cs`）。显示：PassName、IsEnabled toggle、Slot列表（name + type + direction + connection status）、Config引用。为每个V2 Pass创建对应的Editor（`ForwardOpaquePassV2Editor.cs` 等）。
  **Must NOT do**: 不删除旧的PassBaseEditor和现有Pass Editor（Phase 6）。
  **Parallelization**: Wave 10 | Blocked by: T30 | Blocks: T32
  **References**: Editor/Passes/PassBaseEditor.cs (旧Editor); 架构~/架构文档.md:245
  **Acceptance criteria**: PassEditor可正确显示新Pass类的属性；V2 Pass Editor编译通过
  **Commit**: Y | `feat(editor): add new Pass editor for V2 passes`

- [x] 35. 更新 HNAdditionalCameraData Editor
  **What to do**: 更新 `Editor/Camera/HNRenderPipelineAdditionalCameraDataEditor.cs`，添加 `pipelineConfigOverride` 字段的Inspector UI（下拉选择CameraPipelineConfig列表中的Config）。
  **Must NOT do**: 不修改HNAdditionalCameraData.cs的public API。
  **Parallelization**: Wave 10 | Blocked by: T31 | Blocks: T33
  **References**: 架构~/架构文档.md:86,196; Editor/Camera/HNRenderPipelineAdditionalCameraDataEditor.cs
  **Acceptance criteria**: Camera Inspector中可设置pipelineConfigOverride
  **Commit**: Y | `feat(editor): add pipelineConfigOverride to Camera editor`

### Wave 11: 清理 + 全量验证 (Phase 6)

- [x] 36. 删除废弃文件
  **What to do**: 按照架构文档第9节列表，删除以下文件：
  - `Runtime/RenderGraph/HNRenderGraphBase.cs`
  - `Runtime/RenderGraph/PassBase.cs` + `Runtime/RenderGraph/PassData.cs`
  - `Runtime/RenderGraph/RenderGraphView*.cs` + `*ViewBlock.cs` + `RenderGraphViewType.cs`
  - `Runtime/RenderPipeline/Standard.cs` + `Runtime/RenderPipeline/Preview.cs`
  - `Runtime/RenderRequest*.cs` + `Runtime/RenderingData.cs`
  - `Runtime/Passes/RendererListInput.cs`（若确认未使用）
  - `Editor/RenderGraph/HNRenderGraphBaseEditor.cs` + `StandardEditor.cs` + `PreviewEditor.cs`
  - `Editor/Passes/PassBaseEditor.cs` + 旧Pass Editor
  删除后运行编译验证 + 全量测试。
  **Must NOT do**: 不删除任何Shader/HLSL/ComputeShader文件；不删除Utils/工具类。
  **Parallelization**: Wave 11 | Blocked by: T32 | Blocks: T34
  **References**: 架构~/架构文档.md:255-265 (待删除文件列表)
  **Acceptance criteria**:
    - 列出的所有文件已删除
    - `vibe_unityMCP_refresh_unity(scope="all", compile="request")` 编译通过（无missing reference错误）
  **QA scenarios**:
    - happy: 删除所有废弃文件 → 编译通过 → 无broken reference
    - failure: 编译错误 → 检查残留引用 → 修复后重试
    - Evidence: `.omo/evidence/task-33-hnrp-refactor-phase1-6.md`
  **Commit**: Y | `chore(cleanup): remove deprecated old-architecture files`

- [x] 37. 全量 EditMode 测试 + 最终验证
  **What to do**: 运行全量 `vibe_unityMCP_run_tests(mode="EditMode")`，确认所有测试通过。运行场景加载测试（若有测试场景）。验证HNRP管线可在空场景中正常渲染（不崩溃、无红色console错误）。
  **Must NOT do**: 不运行PlayMode测试（无PlayMode测试）。
  **Parallelization**: Wave 11 | Blocked by: T33 | Blocks: —
  **References**: AGENTS.md:29-54
  **Acceptance criteria**:
    - `vibe_unityMCP_run_tests(mode="EditMode")` 返回全部通过（0 failures, 0 errors）
    - `vibe_unityMCP_read_console(types=["error"])` 无新增HNRP相关错误
  **QA scenarios**:
    - happy: 全量测试通过 → console无HNRP错误
    - failure: 测试失败 → 逐一修复 → 重新运行 → 直到全部通过
    - Evidence: `.omo/evidence/task-34-hnrp-refactor-phase1-6.md`
  **Commit**: Y | `test: full EditMode test suite verification`

## Final verification wave
> Runs in parallel after ALL todos. ALL must APPROVE. Surface results and wait for the user's explicit okay before declaring complete.

- [x] F1. **Plan compliance audit**: 对比架构文档所有ADR和组件定义，确认每个ADR对应实现存在。检查项：Pass是纯C#类(ADR 001)、RenderGraphAsset外部化(ADR 002)、name-based连线(ADR 003)、每Camera独立CameraRenderer(ADR 004)、Unity RenderGraph API(ADR 005)、CameraPipelineConfig中间层(ADR 006)、PassConfigBase独立SO(ADR 007)、[Pass]Attribute(ADR 012)、不自行实现拓扑排序(ADR 013)、Transient资源托管(ADR 014)、Config统一接口(ADR 015)
  - Evidence: `.omo/evidence/F1-hnrp-refactor-phase1-6.md`

- [x] F2. **Code quality review**: 检查所有新文件：命名空间一致性(HN.HNRP)、XML文档注释完整性、无硬编码魔法数字、无 `new ComputeBuffer()`/`new RenderTexture()` 在Pass.Record()中。汇编级检查：asmdef引用正确性。
  - Evidence: `.omo/evidence/F2-hnrp-refactor-phase1-6.md`

- [x] F3. **Real manual QA**: 通过Unity MCP在编辑器中实际操作：
  - 创建HNRenderPipelineAsset → 分配CameraPipelineConfig → 确认Inspector正常
  - 选中RenderGraphAsset → 确认自定义Inspector正常
  - 打开场景 → 确认HNRP管线渲染不崩溃
  - 切换CameraType → 确认不同管线被选中
  - Evidence: `.omo/evidence/F3-hnrp-refactor-phase1-6.md`

- [x] F4. **Scope fidelity**: 确认Scope OUT中没有被意外修改：
  - ShaderLibrary/ 目录文件无变化
  - 旧代码在Phase 6之前未被删除
  - Runtime/Utils/ 工具类未修改
  - HNAdditionalCameraData public API未变化
  - Evidence: `.omo/evidence/F4-hnrp-refactor-phase1-6.md`

## Commit strategy
- **每todo一commit**：commit message格式 `type(scope): summary`，对应todo描述
- **分支**：`refactor_v2`（已创建）
- **合并策略**：所有todo完成 + F1-F4全部通过 → Merge到 `develop`
- **不强制push**：仅在用户明确要求时push

## Success criteria
1. 所有38个todo的EditMode测试全部通过（0 failures）
2. Unity编译无错误（0 compilation errors）
3. 架构文档15个ADR全部有对应实现
4. HNRP管线在Editor中可正常渲染（无崩溃、无红色console）
5. 每Camera独立渲染（修改一个Camera的Pass不影响其他Camera）
6. RenderGraphAsset可序列化/反序列化，支持Inspector编辑
7. Pass可通过[Pass]Attribute自动发现
8. Phase 6后废弃文件全部删除，无broken reference


