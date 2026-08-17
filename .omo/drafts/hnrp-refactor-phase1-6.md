---
slug: hnrp-refactor-phase1-6
status: drafting
intent: clear
pending-action: write .omo/plans/hnrp-refactor-phase1-6.md
approach: TDD-driven 6-phase refactoring per architecture doc ADRs, starting from zero tests. Each phase: test assembly setup → write failing tests → implement → verify via Unity MCP run_tests(EditMode). Old code coexists until Phase 6 cleanup.
---

# Draft: hnrp-refactor-phase1-6

## Components (topology ledger)
| id | outcome (one line) | status | evidence path |
| -- | ------------------ | ------ | ------------- |
| C1 | 测试基础设施 — HN.HNRP.Tests.EditMode 程序集 + Unity Test Framework | active | 架构~/架构文档.md:227-250 |
| C2 | Pass 核心框架 — Pass 纯C#抽象类、PassSlot(name-based)、[Pass]Attribute、PassRegistry | active | 架构~/架构文档.md:110-120 |
| C3 | RenderGraphAsset + CameraPipelineConfig — 管线图模板资源 + 中间配置层 | active | 架构~/架构文档.md:90-101 |
| C4 | CameraRenderer + CameraContext — 每Camera独立渲染器 + 上下文 | active | 架构~/架构文档.md:127-149 |
| C5 | HNRenderPipeline 重构 — 用CameraRenderer替代旧RenderRequest流程 | active | 架构~/架构文档.md:21-63 |
| C6 | 现有Pass迁移 — ForwardOpaquePass等10+个Pass从PassBase→Pass | active | 架构~/架构文档.md:164-186 |
| C7 | 旧代码清理 — 删除架构文档第9节列出的废弃文件 | active | 架构~/架构文档.md:255-265 |

## Open assumptions (announced defaults)
| assumption | adopted default | rationale | reversible? |
| ---------- | --------------- | --------- | ----------- |
| 测试框架 | Unity Test Framework (NUnit), EditMode | Unity标准；用户明确指定；架构文档未提PlayMode需求 | Yes |
| 测试程序集位置 | Tests/EditMode/HN.HNRP.Tests.EditMode.asmdef | Unity标准实践；与Runtime程序集分离 | Yes |
| Pass命名空间 | HN.HNRP（与现有一致） | 不引入断裂性命名变更 | Yes |
| RenderGraph实现 | 复用Unity内置RenderGraphModule API | ADR 005明确决策；不自行实现拓扑排序 | No |
| 每Phase可独立编译 | 是，旧代码保留到Phase 6 | 降低风险；Phase间可回滚 | No |
| Phase 1产物可编译但不可渲染 | 预期行为；Phase 1只搭骨架 | 后续Phase逐步接入 | Yes |

## Findings (cited - path:lines)
- 项目为UPM Package布局，无Assets/目录：Runtime/HN.HNRP.asmdef + Editor/HN.HNRP.Editor.asmdef
- 测试文件0个，测试程序集0个：glob **/*Test*.cs → 0 files
- PassBase extends ScriptableObject (Runtime/RenderGraph/PassBase.cs:10)
- PassSlot使用index-based连接 (Runtime/RenderGraph/PassSlot.cs:12-13)
- 管线定义硬编码在Standard.cs/Preview.cs (Runtime/RenderPipeline/Standard.cs:10-52)
- HNRenderPipeline共享RenderingData给所有Camera (Runtime/HNRenderPipeline.cs:113-119)
- HNRenderGraphBase是ScriptableObject持有passes字典 (Runtime/RenderGraph/HNRenderGraphBase.cs:14,192)
- UNITY_EDITOR条件编译：sceneView/preview的RenderGraphViewBlock仅在Editor (Runtime/HNRenderPipelineAsset.cs:28-34)
- 现有Shaders 23个.hlsl + 3个.shader + 3个.compute，重构不涉及修改
- ForwardOpaquePass.OnCreate()中实例化PassSlot (Runtime/Passes/ForwardOpaquePass.cs:15-26)

## Decisions (with rationale)
1. **6 Phase顺序执行，Phase间有硬依赖** — Phase 2依赖Phase 1产物，Phase 4依赖Phase 3，Phase 5依赖Phase 4
2. **每Phase独立测试程序集** — 测试随代码就近放置，避免跨Phase耦合
3. **Editor代码同步重构** — 每Phase包含Runtime+Editor对应变更
4. **Pass内部Record逻辑尽量不变** — ADR明确"内部Record逻辑尽量不变"，降低引入bug风险
5. **Resources/目录结构按架构文档8.文件结构创建** — CameraPipelineConfigs/, RenderGraphs/, PassConfigs/

## Scope IN
- 架构文档第1-6节定义的全部核心组件
- 架构文档第8节定义的文件结构（全部新文件）
- 架构文档第9节列出的待删除文件（Phase 6）
- TDD测试覆盖：Pass框架、RenderGraphAsset序列化/反序列化、CameraRenderer生命周期、Pass连接/断连、Config运行时副本
- Editor Inspector：RenderGraphAssetEditor、PassEditor（替代PassBaseEditor）

## Scope OUT (Must NOT have)
- 不修改任何Shader/HLSL/ComputeShader文件
- 不新增渲染功能（阴影、后处理等仍为TODO）
- 不修改现有Material资源
- 不修改Runtime/Utils/现有工具类
- 不修改HNAdditionalCameraData等的public API签名

## Open questions
- 无未解决问题

## Approval gate
status: plan-written
pending-action: none (plan complete)
approach: TDD-driven 6-phase refactoring with Phase 0 blocker fixes. 37 todos across 11 waves. Editor redesign for pure C# Pass, ComputeShader loading moved to RuntimeResources, HNAdditionalCameraData.pipelineConfigOverride added. Old code coexists until Phase 6.
