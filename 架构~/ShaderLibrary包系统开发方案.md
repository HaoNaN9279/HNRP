# HNRP ShaderLibrary 包（Package）系统开发方案

> 目标：在 ShaderLibrary 中建立「基底（Substrate）+ 包（Package）」体系。
> 基底 = Lit / 未来 Unlit、Cel；包 = 可注入基底 vs/ps 各阶段的 hlsl 扩展单元。
> 状态：已实施（P1–P4）。实施偏差见文末「实施说明」。

---

## 1. 术语

| 术语 | 定义 |
|---|---|
| 基底 Substrate | 一个 shader 家族 + 一套 hlsl + 一组锚点。如 Lit（Forward/ShadowCaster/DepthOnly/DepthNormal） |
| 包 Package | 一段 hlsl + 声明式元数据（插入点 / 参数 / varyings / keyword）。只归属一个基底 |
| 插入点 Insertion Point | `(Pass, Stage, Position)` 三元组，如 `(Forward, Fragment, AfterSurfaceData)` |
| 组合 Combination | `基底 + 有序包列表`。同一组合的签名确定 |
| 生成 Shader | 组合对应的最终 shader 资产，材质实际引用它 |

---

## 2. 现状基线

- `Shaders/Lit.shader`：4 个 Pass（Forward / ShadowCaster / DepthOnly / DepthNormal），`HLSLINCLUDE` 为空，`CustomEditor = HN.HNRP.Editor.LitGUI`。
- `Lit/LitForward.hlsl`：`VertMain` 与 `FragMain` 内是一串 `BuildXxx` 调用，天然插入点。
- `Lit/LitVaryings.hlsl`：`BuildPackVaryings` 用 `pointer=0` 递增 SET；`BuildUnpackVaryings` 用 `pointer=VARYINGS_FLOAT_COUNT-1` 递减 GET，两侧顺序镜像。
- `Lit/LitVaryingsDefine.hlsl`：varying 计数总和已含 `VARYINGS_FLOAT_COUNT_PACKAGES`。
- `Core/PackageRegistry.hlsl`：现硬编码 `VARYINGS_FLOAT_COUNT_PACKAGES 0`（预留位）。
- `Core/PackedVaryings.hlsl`：已实现 TEXCOORD0..7（最多 32 float）的 `SetVaryingsFloatN` / `GetVaryingsFloatN`。
- `Core/KeywordsList.hlsl`：为基底 / 包提供属性宏的 undef 入口。
- 编辑器：`MaterialGUI → MaterialGUIBlockList → MaterialGUIBlock`（`MaterialHeaderScope` 折叠）；`LitGUI` 注册 3 个 block。
- 代码生成先例：`PassRegistryGenerator`（`[InitializeOnLoad]` + `IPreprocessBuildWithReport`，内容不变不写盘）。
- 管线：`HNRenderPipeline.cs` 开启 `GraphicsSettings.useScriptableRenderPipelineBatching = true`（**SRP Batcher 生效**）。
- 已知小问题：`Core/VertexInput.hlsl:34` 的 `attruvytes.uv1` 拼写错误（仅 `LIGHTMAP_ON` 路径编译，顺带修）。

---

## 3. 关键决策

### D1 组合 → 生成 shader 资产
Unity 无运行时 shader 编译，且需求要求「组合为最终 shader」，故由基底模板生成 shader 资产。
无包组合不生成，直接复用基底 shader。

### D2 参数 CB：并入 `UnityPerMaterial`（已确认）
SRP Batcher 硬约束：所有 MaterialProperty 必须声明在唯一 `UnityPerMaterial` 中，任何外部材质属性都会使 shader 变为 incompatible。
故每个包的参数**在源码上独立成段，物理上拼进该组合的 `UnityPerMaterial`**。
「节省资源」由组合级生成天然达成：生成的 shader 只含该组合用到的包，未使用的包不占空间；同组合布局一致，SRP Batcher 完整生效。

### D3 包定义：ScriptableObject + hlsl
`CreateAssetMenu` 可视化配置；hlsl 为独立资产。保留自定义 GUI 类型扩展点。

### D4 材质包列表：`Material.SetOverrideTag`
`material.SetOverrideTag("HNRPPackages", "<substrateId>|<pkgId1>|<pkgId2>")`，随材质复制 / 预制体实例携带。变更后 `EditorUtility.SetDirty`。

### D5 锚点双写 + 校验
C# 的 `HNRPSubstrateAnchors` 为锚点签名唯一真源；hlsl 只写默认空宏。EditMode 测试校验两端一致，防止漂移。

### D6 多包同位置：`do{}while(0)` 聚合
同一锚点上多个包调用由生成器包成单条语句，保证在任意语句上下文中安全；`inout` 引用保证按包顺序依次可见。

---

## 4. 详细设计

### 4.1 包定义资产

```
HNRPPackageDefinition : ScriptableObject
  string              DisplayName          // 可读名，用于生成 shader 文件名
  string              PackageId            // 稳定唯一 id（默认资产 GUID），参数/函数前缀
  HNRPSubstrateId     Substrate            // Lit / Unlit / Cel
  ShaderInclude       Hlsl                 // 包实现（可空，面板可一键从模板创建）
  PackageInsertion[]  Insertions           // (Pass, Stage, Position)
  PackageParameter[]  Parameters           // (Name, Type, Default, Display, Tooltip)
  PackageVarying[]    Varyings             // (Name, Size 1..4)
  PackageKeyword[]    Keywords             // 局部关键字
  string[]            RequiredAttributes   // uv0/tangent/color… → ATTRIBUTES_NEED_*
  string              GuiTypeName          // 自定义面板类型（可空 → 声明式）
```

参数命名强制前缀 `_<PackageId>_`；生成器做重名 / 非法字符校验。
参数类型：`Float / Range / Int / Toggle / Color / Vector / Texture2D / Cube / Enum`。

### 4.2 包资产面板与 hlsl 模板

`HNRPPackageDefinitionEditor` 绘制：

- 基础：DisplayName、PackageId、Substrate 下拉。
- **HLSL 引用槽 + 操作按钮**：
  - 有引用：`Open` 按钮直接打开该 hlsl 编辑。
  - 无引用：`Create HLSL` 按钮从模板在同目录生成 `<PackageId>.hlsl`，回填引用并打开。
- 插入点列表（Pass / Stage / Position 下拉，选项来自 `HNRPSubstrateAnchors`）。
- 参数、varyings、keyword、RequiredAttributes 列表。
- `Sync Template` 按钮：按当前插入点补齐缺失的函数骨架，**不覆盖已有实现**。

**模板内容**：为每个插入点生成一个函数骨架，函数名
`Pkg<PackageId>_<Substrate>_<Pass>_<Stage>_<Position>`；
参数为该锚点声明的上下文结构体（`inout`）；函数头注释内列全**输入 / 输出结构体的每个字段**（字段名、类型、语义说明），数据取自 `HNRPSubstrateAnchors` 的锚点元数据。示例如：

```hlsl
// 插入点：Forward / Fragment / AfterSurfaceData
// 输入输出：
//   LitSurfaceData surface
//     float3 albedo      基础色（已乘 _BaseColor）
//     float  alpha       透明度
//     float3 normalTS    切线空间法线
//     float  smoothness  平滑度
//     float  metallic    金属度
//     float  occlusion   环境光遮蔽
//     float3 emission    自发光
//   LitVaryings varyings
//     float3 positionWS  世界空间位置
//     ...
void Pkg<PkgId>_Lit_Forward_Fragment_AfterSurfaceData(inout LitSurfaceData surface, in LitVaryings varyings)
{
    // TODO: 在此修改 surface / varyings
}
```

### 4.3 基底锚点改造

在基底 hlsl 的阶段之间插入默认空宏（`#ifndef` 保护）：

```hlsl
// LitForward.hlsl
HNRP_PKG_EMIT_LIT_FORWARD_FRAGMENT_AFTERSURFACEDATA(litSurfaceData, litVaryings)
BuildPreBRDFData(litVaryings, preBRDFData);
HNRP_PKG_EMIT_LIT_FORWARD_FRAGMENT_AFTERBRDFDATA(brdfData, litSurfaceData)
```

基础锚点清单（Forward 为核心，其余按需扩展）：

| Pass | Stage | Positions |
|---|---|---|
| Forward | Vertex | `Begin` `AfterVertexInput(vertexInput)` `AfterVaryings(litVaryings)` `End` |
| Forward | Fragment | `Begin` `AfterUnpackVaryings(litVaryings)` `AfterSurfaceData(surface,varyings)` `AfterBRDFData(brdf,surface)` `AfterLightingInput(lightingInput)` `AfterLighting(lighting,brdf,surface)` `AfterOutput(output,surface)` `End` |
| DepthNormal | Vertex / Fragment | `AfterVaryings` / `AfterSurfaceData` |
| DepthOnly / ShadowCaster | Vertex / Fragment | `Begin` / `AfterVertex` / `End` |

**材质属性锚点**（保证 SRP Batcher 布局一致）：

- `Lit/LitInput.hlsl` 的 `CBUFFER_START(UnityPerMaterial)` 内加 `HNRP_PKG_MATERIAL_DECLARATIONS` 锚点（包参数声明段，**无条件编译**，不随 keyword 变化）。
- 同文件加 `HNRP_PKG_TEXTURE_DECLARATIONS` 锚点（包纹理 / 采样器声明，CBUFFER 之外）。
- `Lit/LitVaryings.hlsl`：`BuildPackVaryings` 末尾加 `HNRP_PKG_SET_VARYINGS(packed, lit, pointer)`；`BuildUnpackVaryings` 开头加 `HNRP_PKG_GET_VARYINGS(packed, lit, pointer)`（与 SET 镜像）。
- `Core/PackageRegistry.hlsl` 改为兜底：`#ifndef VARYINGS_FLOAT_COUNT_PACKAGES → 0`，实际值由组合头先定义。

**多包同位置展开**（D6）：

```hlsl
    #define HNRP_PKG_EMIT_LIT_FORWARD_FRAGMENT_AFTERSURFACEDATA(surface, varyings) \
    do { \
        PkgA_Forward_Fragment_AfterSurfaceData(surface, varyings); \
        PkgB_Forward_Fragment_AfterSurfaceData(surface, varyings); \
    } while (0)
```

约束：锚点仅出现在语句位置；同一包在同一锚点只声明一次（生成器去重校验）；无包的锚点展开为空。

### 4.4 组合生成

生成两份产物，均在 `Assets/HNRP/Runtime/ShaderLibrary/Generated/`：

1. **组合头 `<Substrate>_<Pkg>_<Pkg>_<hash>.hlsl`**
   - `#include` 各包 hlsl 实现；
   - `#define VARYINGS_FLOAT_COUNT_PACKAGES <n>`；
   - 定义被使用的锚点宏（`do{}while(0)` 聚合，按包顺序）；
   - 定义包材质属性 / 纹理声明段；
   - 定义 `HNRP_PKG_SET/GET_VARYINGS`（依据各包 Varyings 字段）。

2. **生成 Shader `<Substrate>_<Pkg>_<Pkg>_<hash>.shader`**
   - Shader 名 `Hidden/HNRP/Generated/<Substrate>_<Pkg>_<Pkg>_<hash>`；
   - 由基底 shader 模板替换而来：
     - `Properties` = 基底属性 + 各包参数属性；
     - `HLSLINCLUDE` 内 `#include "<组合头>"` + 各包 keyword 的 `#pragma`；
     - `CustomEditor` 沿用基底 GUI（`LitGUI`）。

**命名**：`<Substrate>_<Pkg>_<Pkg>_<hash>`，包名取 `DisplayName` 的 sanitize 结果（去非法字符、PascalCase）；`hash` 为签名短哈希（8 hex）。
**签名** = `substrateId + 有序 (packageId, 包内容 hash) + 各包元数据规范化串 + 模板版本`。

**缓存与流畅性**：
- 确定性路径，存在即复用，不重生成 / 不重导入。
- 内容一致不写盘（沿用 `PassRegistryGenerator` 做法）。
- **改参数值不触发生成**，仅包列表 / 包定义变化才触发。
- 批量用 `AssetDatabase.StartAssetEditing/StopAssetEditing`；生成延迟到编辑器空闲 + 进度条。
- 菜单 `HNRP/Clean Unused Generated Shaders` 清理无引用产物。

### 4.5 材质面板

`PackageListBlock : MaterialGUIBlock`，置于基底 block 之后：

- 包列表：每行显示包名 + 上移 / 下移 / 删除。
- `Add Package` 下拉：按基底过滤、排除已加。
- 每包可展开子面板：有 `GuiTypeName` 时用自定义 `PackageGUI`，否则声明式按类型绘制参数。
- 变更流程：写 tag → 生成 / 定位 shader → `material.shader = generatedShader`。
- `MaterialHeaderScope` 的 `expandableBit` 仅 32 位，包数 >28 时回退 `BeginFoldoutHeaderGroup` + `SessionState` 存展开态。

`LitGUI.DrawGUI` 末尾追加包块；`LitGUI` 从 tag 解析当前包列表。
未来 Unlit / Cel 各自 GUI 复用同一 `PackageListBlock`。

---

## 5. 文件变更清单

**新增（Runtime）**

- `Runtime/ShaderLibrary/Packages/HNRPPackageDefinition.cs`
- `Runtime/ShaderLibrary/Packages/HNRPPackageParameter.cs`
- `Runtime/ShaderLibrary/Packages/HNRPPackageInsertion.cs`
- `Runtime/ShaderLibrary/Packages/HNRPPackageVarying.cs`
- `Runtime/ShaderLibrary/Packages/HNRPPackageKeyword.cs`
- `Runtime/ShaderLibrary/Packages/HNRPSubstrateAnchors.cs`（锚点签名与结构体字段元数据，唯一真源）
- `Runtime/ShaderLibrary/Packages/HNRPSubstrateRegistry.cs`

**新增（Editor）**

- `Editor/ShaderGUI/PackageListBlock.cs`
- `Editor/ShaderGUI/PackageGUI.cs`
- `Editor/ShaderGUI/PackagePropertyDrawer.cs`
- `Editor/Packages/HNRPPackageGenerator.cs`（生成组合头 / shader，`[InitializeOnLoad]` + `IPreprocessBuildWithReport`）
- `Editor/Packages/HNRPPackageDefinitionEditor.cs`（包面板 + 模板生成 + Open/Create/Sync 按钮）
- `Editor/Packages/HNRPPackageTemplateWriter.cs`（模板拼装）
- `Editor/Packages/SubstrateAnchorValidator.cs`（测试用校验）

**修改（hlsl / shader）**

- `Runtime/ShaderLibrary/Core/PackageRegistry.hlsl`（兜底默认）
- `Runtime/ShaderLibrary/Lit/LitInput.hlsl`（材质属性 / 纹理锚点）
- `Runtime/ShaderLibrary/Lit/LitVaryings.hlsl`（SET / GET 锚点）
- `Runtime/ShaderLibrary/Lit/LitForward.hlsl`（阶段锚点）
- `Runtime/ShaderLibrary/Lit/LitDepthNormal.hlsl`、`LitDepthOnly.hlsl`、`LitShadowCaster.hlsl`（阶段锚点）
- `Runtime/ShaderLibrary/Shaders/Substrates/Lit.shader`（基底模板标记位；基底单独放 `Shaders/Substrates/`）
- `Runtime/Core/HNStringConstants.cs`（tag 名 / 包相关常量）

**修改（编辑器）**

- `Editor/ShaderGUI/MaterialGUI.cs`、`LitGUI.cs`（接入包块）

**顺带修复**

- `Runtime/ShaderLibrary/Core/VertexInput.hlsl:34` 的 `attruvytes.uv1` → `attributes.uv1`

---

## 6. 实施阶段

1. **P1 骨架**：包定义 SO + 面板 + hlsl 模板 + 基底锚点宏 + 生成器 + 组合缓存 + 无参示例包，产出可渲染的 `Lit_<Pkg>_<hash>.shader`。
2. **P2 参数**：参数声明、`UnityPerMaterial` 注入、Properties 注入、包面板增删排序。
3. **P3 varyings**：vs→ps 传递、`VARYINGS_FLOAT_COUNT_PACKAGES` 补齐、32 float 上限校验。
4. **P4 收尾**：Depth / Shadow pass 锚点、keyword、自定义 GUI、示例包、EditMode 测试、架构文档同步。

测试遵循 AGENTS.md：TDD 编写的测试为临时产物，完成后删除；仅保留核心冒烟测试（生成 shader 可编译、包参数生效、多包同位置顺序正确）。

---

## 7. 风险

- 生成 shader 首次编译有卡顿，缓存后消除；缓存命中率取决于组合复用度。
- 包参数若随 keyword 条件编译，会破坏同组合内 SRP Batcher 布局一致性 —— 故强制包参数段无条件编译。
- `PackedVaryings` 仅 8 个 TEXCOORD（32 float），基底 + 包共用，超限需报错。
- 包 hlsl 引入 `ATTRIBUTES_NEED_*` / `USE_*_VARYING` 时，需经 `KeywordsList.hlsl` 统一 undef / define，避免重定义与跨 pass 不一致。

---

## 9. 重构修订（v2）：SO 声明 + hlsl 单文件 DSL

> 本节为最新修订；与 §1–§8 冲突处以本节为准。

### 9.1 模型

- Package = **SO 资产（声明 / 注册 / 工具）+ 唯一 hlsl（内容）**。
- SO 字段：`DisplayName`（= Id = hlsl 文件名）、`SupportedSubstrates`、`Insertions`（每项带基底）、`RequiredAttributes`、`HlslPath`。
- **删除**：`PackageId`、单基底 `Substrate`、SO 内 `Parameters/Varyings/Keywords` 结构化编辑、`GuiTypeName`。
- 参数 / varyings / keyword 的**声明写在 hlsl**（`HNRP_PACKAGE_BEGIN..END` 区间，DSL 宏，编译期空宏）。
- 插入点 / 顶点属性归 SO（结构声明）。
- SO 面板承担：`Create HLSL` / `Sync Template` / `Open` + 解析 hlsl 的**功能速览**与校验。

### 9.2 hlsl DSL

```
HNRP_PACKAGE_BEGIN
HNRP_PACKAGE_COLOR(Tint, "Tint", 1, 1, 1, 1)
HNRP_PACKAGE_RANGE(Strength, "Strength", 0, 1, 0.5)
HNRP_PACKAGE_TEXTURE(Map, "Map")
HNRP_PACKAGE_TOOLTIP(Strength, "…")
HNRP_PACKAGE_VARYING(float2, uv, "UV")
HNRP_PACKAGE_KEYWORD(FEATURE, "…")
HNRP_PACKAGE_END
```

宏清单（空定义在 `Core/PackageAnchors.hlsl`）：`FLOAT / RANGE / INT / TOGGLE / TOGGLE_KEYWORD / COLOR / VECTOR / TEXTURE / CUBE / ENUM / TOOLTIP / VARYING / KEYWORD`。固定元数（HLSL 不支持变参宏）；解析器 `HNRPPackageParser` 产出 `HNRPPackageContent`。

### 9.3 多基底

- 组合仍为「单基底 + 有序包」；包可声明支持多个基底。
- 组合头在 include 包实现前定义 `HNRP_ACTIVE_SUBSTRATE_<SUB>`；包实现按该守卫分段，只编译当前基底。
- 生成器只取 `Insertion.Substrate == 当前基底`；包须 `Supports(基底)`，否则忽略 + 警告。

### 9.4 命名与面板

- Id / DisplayName = hlsl 文件名；`SafeId` = sanitize(名)。
- 自定义面板类名约定 `Package<Name>Editor : PackageGUI`（反射按简单类名查找），未定义退回 `DefaultPackageGUI`。
- 参数按 type 只显示相关项：SO 速览（只读）+ 材质面板（按类型绘制，仅纹理显示 tiling/offset）。

### 9.5 受影响的旧条目

- §4.1 字段 `PackageId / Substrate / Parameters / Varyings / Keywords / GuiTypeName` 作废。
- §5 文件清单中的 `PackagePropertyDrawer` 删除；新增 `HNRPPackageContent / HNRPPackageParser / HNRPPackageDatabase`。

---

## 8. 实施说明（与方案的偏差）

1. **hlsl 引用以项目相对路径字符串存储**（`HNRPPackageDefinition.HlslPath`），而非序列化 `ShaderInclude`：该类型仅 `UnityEditor` 程序集可用，而定义资产位于 Runtime 程序集；编辑器用 `ShaderInclude` ObjectField 选择并写入路径。
2. **锚点调用不用变参分发宏**：Unity shader 预处理器不支持 Variadic macros（实测报 `Variadic macros are not supported`），故基底 hlsl 直接调用 `HNRP_PKG_EMIT_<PASS>_<STAGE>_<POSITION>(...)`，`Core/PackageAnchors.hlsl` 为每个锚点提供默认空宏。
3. **`PackageId` 默认取资产名 sanitize 结果**（便于可读），生成器对全工程包 id 做唯一性校验；未使用资产 GUID。
4. **漂移防护**以 `SubstrateAnchorValidator` + 菜单 `HNRP/Validate Substrate Anchors` 形式保留（不长期保留测试文件）。临时 TDD 测试按 AGENTS.md 约定在验证后删除。
5. 生成产物目录 `Runtime/ShaderLibrary/Generated/` 由生成器按需创建。
6. **基底 shader 移动到 `Runtime/ShaderLibrary/Shaders/Substrates/`**（基底单独成文件夹）。因目录深度变化，基底 shader 内的 include 改为项目绝对路径（`Assets/HNRP/Runtime/ShaderLibrary/Lit/...`），使基底 shader 与生成 shader（位于 `Generated/`）均可正确定位；生成 shader 直接沿用同一模板文本，无需重写 include。
7. **锚点宏与包函数名均带基底前缀**：宏 `HNRP_PKG_EMIT_<SUBSTRATE>_<PASS>_<STAGE>_<POSITION>`、函数 `Pkg<Id>_<Substrate>_<Pass>_<Stage>_<Position>`（如 `HNRP_PKG_EMIT_LIT_FORWARD_VERTEX_BEGIN` / `PkgSnow_Lit_Forward_Fragment_AfterSurfaceData`）。`HNRPAnchorDescriptor.Token` 含基底，生成器与校验器自动随之更新。
