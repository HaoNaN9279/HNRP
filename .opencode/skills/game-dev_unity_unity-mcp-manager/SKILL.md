---
name: unity-mcp-manager
description: 当 Unity MCP 连接断开、超时或 Unity 实例崩溃需要重启时使用。提供 MCP 连接诊断、MCP 服务器重启和 Unity 实例恢复的标准操作流程
---

# Unity MCP 管理器

当 Unity MCP 连接不稳定或 Unity 实例崩溃时，使用此技能诊断问题并执行恢复操作。

此技能目录下包含两个工具定义文件，供 AI 智能体按需调用：
- `unity-restart-mcp.ts` — 重启 Unity 编辑器中运行的 MCP 服务器
- `unity-restart-instance.ts` — 通过 Unity Hub 重新打开 Unity 实例

## 使用时机

此技能在以下场景应自动加载：

- Unity MCP 工具调用返回连接错误、超时或无响应
- Unity Editor 进程意外退出
- Unity MCP 健康检查失败
- 用户报告 Unity 编辑器卡死或崩溃

## 诊断流程

### 第一步：判断 MCP 状态

当出现 MCP 连接错误时，首先判断是哪种情况：

1. **MCP 超时** — 工具调用等待后返回超时错误
2. **MCP 连接拒绝** — 工具调用立即返回连接被拒绝
3. **MCP 返回异常数据** — 工具调用成功但返回非预期结果

### 第二步：选择恢复工具

根据诊断结果选择对应的恢复工具：

| 场景 | 症状 | 使用工具 |
|------|------|----------|
| MCP 无响应但 Unity 运行中 | MCP 超时/连接拒绝，但 Unity Editor 进程存在 | `unity-restart-mcp` |
| Unity 已崩溃/关闭 | Unity Editor 进程不存在，Project 窗口已关闭 | `unity-restart-instance` |
| Unity 卡死（无响应） | Unity 窗口标题栏显示"无响应" | `unity-restart-instance` + force=true |
| 强制刷新 MCP | MCP 有响应但行为异常 | `unity-restart-mcp` + force=true |

### 第三步：验证恢复结果

执行恢复操作后，务必验证：

1. 调用 Unity MCP 工具做一次简单操作（如列出场景中的对象）确认 MCP 正常运行
2. 如果恢复失败，尝试另一种方案

## 工具说明

### unity-restart-mcp

重启 Unity 编辑器内的 MCP 服务器。

**参数：**

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `force` | boolean | 否 | `false` | 强制重启，即使 MCP 看起来正常运行 |

**执行流程：**

1. 健康检查 — 尝试连接 `http://127.0.0.1:8080/mcp`
2. 如果健康且非 force 模式 → 返回"无需重启"
3. 如果 Unity 未运行 → 返回提示，建议先重启 Unity
4. 尝试 HTTP 重启端点 (`POST /restart`)
5. 失败则回退：在项目 `Assets` 目录创建临时 `.cs` 文件触发脚本重新编译
6. 清理临时文件，等待编译完成
7. 最终健康检查，返回结果

**正常返回示例：**
```
✅ Unity MCP 已通过脚本重新编译成功重启。
   触发文件已自动清理。
```

### unity-restart-instance

通过 Unity Hub 重新打开 Unity Editor 实例，适用于 Unity 崩溃或无响应场景。

**参数：**

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `projectPath` | string | 否 | 自动检测 | Unity 项目路径，留空自动从当前目录向上查找 |
| `unityHubPath` | string | 否 | 自动检测 | Unity Hub 可执行文件路径 |
| `force` | boolean | 否 | `false` | 强制重启，即使 Unity 仍在运行 |

**执行流程：**

1. 确定项目路径（参数指定 → 自动检测）
2. 验证项目路径有效性（检查 `ProjectSettings` 目录）
3. 查找 Unity Hub（常见安装路径 → PATH 搜索）
4. 检查当前 Unity/MCP 状态
5. force 模式下终止旧 Unity 进程
6. 通过 Unity Hub CLI 打开项目
7. 等待 Unity 进程启动 + MCP 连接（最长 120 秒）

**正常返回示例：**
```
🚀 Unity 实例重启完成 (项目: MyGame)
   项目路径: D:\projects\MyGame

✅ Unity Editor 进程已启动 (24s)
✅ Unity MCP 已连接 (28s)
```

## 核心工作流

### 场景 A：MCP 断开但 Unity 活着

```
1. 调 unity-restart-mcp
2. 检查返回结果:
   - "成功" → 验证 MCP 功能（调用一个简单 MCP 工具）
   - "Unity 未运行" → 切换到场景 B
   - "脚本编译错误" → 通知用户检查 Unity Console
```

### 场景 B：Unity 已崩溃/关闭

```
1. 先尝试 unity-restart-mcp 检查 Unity 状态
2. 确认 Unity 未运行后 → 调 unity-restart-instance
3. 等待 Unity 启动和 MCP 连接（工具会自动等待最多 120 秒）
4. 验证 MCP 功能
```

### 场景 C：Unity 无响应（卡死）

```
1. 通知用户 Unity 进程无响应
2. 调 unity-restart-instance（force=true）
   - 工具会先终止旧 Unity 进程，再通过 Unity Hub 重新打开
3. 等待重新启动完成
4. 验证 MCP 功能
```

## 错误处理

### 编译错误导致 MCP 无法启动

如果脚本重新编译后 MCP 仍然无法连接，常见原因：

- **Unity 编译错误** — Unity Console 中存在红色编译错误，导致 MCP 插件无法加载
  - 解决：用户需先在 Unity 中修复编译错误
- **MCP 插件未安装** — Unity 项目中未安装 Unity MCP 包
  - 解决：通过 Unity Package Manager 安装 Unity MCP
- **端口冲突** — 8080 端口被其他程序占用
  - 解决：关闭占用端口的程序，或修改 MCP 配置中的端口号

### Unity Hub 无法打开项目

- 检查 Unity Hub 中是否添加了该项目的 Unity 版本
- 检查 Unity Hub 是否已登录并授权
- 手动在 Unity Hub 中打开项目进行诊断

## 最佳实践

- **优先使用 unity-restart-mcp** — 重启 MCP 比重启整个 Unity 更快、影响更小
- **避免频繁重启** — 如果 MCP 反复断开，可能是更深层的问题（编译错误、插件冲突），不要盲目重启
- **通知用户** — 在执行 Unity 重启前，告知用户保存工作（Unity 重启不会自动保存场景修改）
- **记录错误信息** — 向用户报告时包含 MCP 返回的错误信息，帮助定位根本原因
- **遵守领域规则** — 除非使用上述工具，否则不要自行关闭 Unity 进程或修改项目文件
