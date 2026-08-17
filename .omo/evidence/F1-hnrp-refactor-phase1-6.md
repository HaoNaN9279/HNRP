# Evidence: F1 - Plan Compliance Audit

## Summary

对比架构文档（`架构~/架构文档.md`）第 10 节设计决策记录 (ADR) 中的 15 条 ADR，逐条确认对应实现在代码库中存在且实现正确。

## ADR 验证结果

### ADR 001: Pass 为纯 C# 类（非 ScriptableObject）
- **文件**: `Runtime/Core/Pass.cs`
- **验证**: ✅ 通过
- **检查项**: `public abstract class Pass` — 纯 C# 抽象类，不继承 `ScriptableObject`
- **代码证据**: 第 23 行 `public abstract class Pass` — 无 MonoBehaviour/ScriptableObject 基类
- **架构文档引用**: 第 111 行 "Pass 是纯 C# 抽象类"

### ADR 002: 管线图外部化为 RenderGraphAsset
- **文件**: `Runtime/Config/RenderGraphAsset.cs`
- **验证**: ✅ 通过
- **检查项**: `public class RenderGraphAsset : ScriptableObject` — 独立的 ScriptableObject 资产
- **代码证据**: 第 52 行 `public class RenderGraphAsset : ScriptableObject`，包含 `PassDefinition[]` + `SlotConnection[]` + `RenderGraphSettings`
- **架构文档引用**: 第 90-96 行 (RenderGraphAsset 设计)

### ADR 003: Pass 连线用名称匹配（非 index）
- **文件**: `Runtime/Config/SlotConnection.cs`, `Runtime/Core/PassSlot.cs`
- **验证**: ✅ 通过
- **检查项**: SlotConnection 使用 `SourcePass`/`SourceSlot`/`TargetPass`/`TargetSlot` 字符串名称；PassSlot 使用 `slotName` 字符串标识
- **代码证据**: 
  - `SlotConnection.cs` 第 30-68 行 — 全部 4 个字段为 string name
  - `PassSlot.cs` 第 52 行 `public string SlotName { get; }` — name 是 slot 的唯一标识
- **架构文档引用**: 第 113 行 "name-based 连接配置外部化"，ADR 003 列明 "Pass 连线用名称匹配"

### ADR 004: 每 Camera 独立 CameraRenderer
- **文件**: `Runtime/Core/CameraRenderer.cs`, `Runtime/HNRenderPipeline.cs`
- **验证**: ✅ 通过
- **检查项**: CameraRenderer 是独立类，HNRenderPipeline 为每 Camera 创建实例
- **代码证据**:
  - `CameraRenderer.cs` 第 35-36 行 — `public class CameraRenderer` 独立类，每实例持有自己的 `Passes` + `Context`
  - `HNRenderPipeline.cs` 第 116 行 — `var cameraRenderer = new CameraRenderer(cameraContext);` — 每 Camera 创建新实例
- **架构文档引用**: 第 50-54 行 "每 Camera 独立 CameraRenderer 实例"

### ADR 005: 用 Unity RenderGraph 模块
- **文件**: `Runtime/Core/Pass.cs`, `Runtime/Passes/ForwardOpaquePassV2.cs`, 各 V2 Pass
- **验证**: ✅ 通过
- **检查项**: Pass.Record() 签名使用 `UnityEngine.Experimental.Rendering.RenderGraphModule.RenderGraph`
- **代码证据**:
  - `Pass.cs` 第 77 行 `public abstract void Record(RenderGraph renderGraph)` — 使用 RenderGraph API
  - `ForwardOpaquePassV2.cs` 第 1 行 `using UnityEngine.Experimental.Rendering.RenderGraphModule;`
  - `HNRenderPipeline.cs` 第 219 行 `internal RenderGraph renderGraph` — 持有共享 RenderGraph 实例
- **架构文档引用**: ADR 005 "用 Unity RenderGraph 模块"

### ADR 006: CameraPipelineConfig 为中间层
- **文件**: `Runtime/Core/CameraPipelineConfig.cs`
- **验证**: ✅ 通过
- **检查项**: Camera 不直接引用 RenderGraphAsset，通过 CameraPipelineConfig 间接引用
- **代码证据**:
  - `CameraPipelineConfig.cs` 第 36-39 行 — `public class CameraPipelineConfig : ScriptableObject` 持有 `RenderGraphAsset m_RenderGraph`
  - `HNRenderPipeline.cs` 第 79 行 — `CameraPipelineConfig pipelineConfig = SelectPipelineConfig(camera, cameraData);` — 通过 Config 获取 RenderGraph
- **架构文档引用**: 第 79-88 行 "Camera 不直接引用 RenderGraphAsset，通过 CameraPipelineConfig 间接指定管线图"

### ADR 007: PassConfigBase 为独立 ScriptableObject
- **文件**: `Runtime/Core/PassConfigBase.cs`
- **验证**: ✅ 通过
- **检查项**: `public abstract class PassConfigBase : ScriptableObject`
- **代码证据**:
  - `PassConfigBase.cs` 第 15 行 `public abstract class PassConfigBase : ScriptableObject`
  - 子类示例: `Runtime/Configs/ForwardOpaqueConfig.cs` 第 23 行 `public sealed class ForwardOpaqueConfig : PassConfigBase`
- **架构文档引用**: 第 121-125 行 "PassConfigBase 为统一参数接口，ScriptableObject"

### ADR 008: Cluster Culling
- **范围说明**: 此 ADR 涉及渲染算法（Cluster Culling 计算着色器），不在本次架构重构范围。ComputeShader 文件保持不变（计划明确禁止修改 ShaderLibrary/ 目录）。
- **验证**: ⚠️ 跳过（渲染算法，非重构范畴）
- **检查项**: 相关 Pass 已迁移到新架构：`ClusterCullingLightPassV2.cs`, `ClusterCullingReflectionProbePassV2.cs` ← 这些是 Pass 基类迁移，算法内核不变
- **架构文档引用**: 第 279 行 ADR 008

### ADR 009: Reflection Probe Octahedral → 2D Atlas
- **范围说明**: 此 ADR 涉及渲染算法（Reflection Probe 编码方案），不在本次架构重构范围。
- **验证**: ⚠️ 跳过（渲染算法，非重构范畴）
- **检查项**: ComputeShader/Shader 文件未修改，相关 Pass 已迁移
- **架构文档引用**: 第 280 行 ADR 009

### ADR 010: GlobalConstantBuffer 统一管理
- **验证**: ✅ 通过
- **检查项**: `Runtime/ConstantBuffer.cs` 存在；`CameraContext.cs` 第 72 行包含 `public GlobalConstantBuffer ConstantBuffer`
- **架构文档引用**: 第 281 行 ADR 010

### ADR 011: RenderGraphAsset 模板, passes 实例
- **文件**: `Runtime/Config/RenderGraphAsset.cs`, `Runtime/Core/CameraRenderer.cs`
- **验证**: ✅ 通过
- **检查项**: RenderGraphAsset 是静态模板（磁盘 .asset），CameraRenderer.passes 是运行时实例（内存 List<Pass>）
- **代码证据**:
  - `RenderGraphAsset.cs` 第 30-52 行 — ScriptableObject 静态模板
  - `CameraRenderer.cs` 第 41 行 `public List<Pass> Passes` — 运行时实例列表
  - `CameraRenderer.cs` 第 83-91 行 `Build()` — 从模板构建运行时实例
- **架构文档引用**: 第 98-107 行 "模板与实例"

### ADR 012: [Pass] Attribute 反射自动发现
- **文件**: `Runtime/Core/PassAttribute.cs`, `Runtime/Core/PassRegistry.cs`
- **验证**: ✅ 通过
- **检查项**: `[Pass("DisplayName")]` 属性 + PassRegistry 通过反射自动发现
- **代码证据**:
  - `PassAttribute.cs` 第 14 行 `public sealed class PassAttribute : Attribute` — 完整的 Attribute 定义
  - `PassRegistry.cs` 第 86-119 行 `RegisterAll()` — 反射扫描 + Partial class 生成注册表
  - 各 V2 Pass 使用 `[Pass("Pass Name")]` 标注
- **架构文档引用**: 第 117-119 行 "[Pass] Attribute + 注册策略"

### ADR 013: 不自行实现拓扑排序
- **文件**: 全 Runtime 目录
- **验证**: ✅ 通过
- **检查项**: 搜索 `topolog|TopoS|sort.*pass|pass.*sort|TopologicalSort` 无匹配
- **代码证据**: grep 搜索 Runtime/ 目录下无任何拓扑排序实现
- **架构文档引用**: 第 284 行 ADR 013

### ADR 014: Transient 资源全权交 RenderGraph
- **文件**: `Runtime/Passes/ForwardOpaquePassV2.cs`, 各 V2 Pass
- **验证**: ✅ 通过
- **检查项**: Pass.Record() 中使用 `renderGraph.CreateTexture/CreateComputeBuffer`，无 `new ComputeBuffer/New RenderTexture`
- **代码证据**:
  - `ForwardOpaquePassV2.cs` 第 161 行 `TextureHandle colorTarget = renderGraph.CreateTexture(colorDesc);`
  - `BuiltinSkyPassV2.cs` 第 132 行 `TextureHandle colorTarget = renderGraph.CreateTexture(colorDesc);`
  - grep 搜索 Runtime/Passes/ 中无 `new (ComputeBuffer|RenderTexture|Texture2D)` 模式
- **架构文档引用**: 第 152-160 行 "资源生命周期"

### ADR 015: PassConfig 为 Pass 统一参数接口
- **文件**: `Runtime/Core/PassConfigBase.cs`, `Runtime/Configs/ForwardOpaqueConfig.cs`
- **验证**: ✅ 通过
- **检查项**: `PassConfigBase.ApplyToPass(Pass pass)` 方法存在
- **代码证据**:
  - `PassConfigBase.cs` 第 42 行 `public abstract void ApplyToPass(Pass pass);` — 统一接口
  - `ForwardOpaqueConfig.cs` 第 75-86 行 — `override void ApplyToPass(Pass pass)` — 具体实现
- **架构文档引用**: 第 122-125 行 "PassConfig 为 Pass 统一参数接口"

## 结果统计

| 状态 | 数量 | ADR 编号 |
|------|------|----------|
| ✅ 通过 | 13 | 001, 002, 003, 004, 005, 006, 007, 010, 011, 012, 013, 014, 015 |
| ⚠️ 跳过（算法/非重构范畴） | 2 | 008, 009 |
| ❌ 失败 | 0 | — |

## 结论

**F1 合规审计：PASS** — 所有 13 个适用于架构重构的 ADR 通过验证。ADR 008 和 ADR 009 为渲染算法相关决策（Cluster Culling、Reflection Probe 编码），不在本次重构范围，ComputeShader 和 Shader 文件保持不动，按计划要求跳过。

## Evidence Date

2026-07-09
