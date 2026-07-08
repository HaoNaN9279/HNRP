import { tool } from "@opencode-ai/plugin"
import { join, dirname } from "path"
import { existsSync, writeFileSync, unlinkSync } from "fs"

const MCP_BASE_URL = "http://127.0.0.1:8080/mcp"
const HEALTH_CHECK_TIMEOUT = 4000
const COMPILE_WAIT_MS = 10000
const SETTLE_WAIT_MS = 6000

async function checkMCPHealth(): Promise<boolean> {
  try {
    const controller = new AbortController()
    const timer = setTimeout(() => controller.abort(), HEALTH_CHECK_TIMEOUT)
    const response = await fetch(MCP_BASE_URL, { signal: controller.signal })
    clearTimeout(timer)
    return response.ok
  } catch {
    return false
  }
}

async function isUnityEditorRunning(): Promise<boolean> {
  try {
    const out = await Bun.$`powershell -NoProfile -Command "&{ if (Get-Process Unity -ErrorAction SilentlyContinue) { '1' } }"`.text()
    return out.trim() === "1"
  } catch {
    return false
  }
}

/**
 * Walk up from `start` to find the Unity project root (where ProjectSettings/ exists).
 */
function findUnityProjectRoot(start: string): string | null {
  let current = start
  for (let i = 0; i < 12; i++) {
    if (existsSync(join(current, "ProjectSettings"))) {
      return current
    }
    const parent = dirname(current)
    if (parent === current) break
    current = parent
  }
  return null
}

export default tool({
  description: "Restart Unity MCP server when connection fails or times out. Triggers script recompilation in the Unity Editor to restart the MCP server. Use this as the first recovery step when Unity MCP tools return connection errors.",
  args: {
    force: tool.schema.boolean().optional().default(false).describe("Force restart even if MCP is currently healthy"),
  },
  async execute(args, context) {
    // ── Step 1: Health check ──────────────────────────────────────────
    const cwd = context?.directory || process.cwd()
    const isHealthy = await checkMCPHealth()

    if (isHealthy && !args.force) {
      return [
        "✅ Unity MCP 运行正常，无需重启。",
        `  - MCP 端点: ${MCP_BASE_URL}`,
        "  - 如需强制重启，请设置 force=true",
      ].join("\n")
    }

    // ── Step 2: Verify Unity Editor is running ────────────────────────
    const unityRunning = await isUnityEditorRunning()
    if (!unityRunning) {
      return [
        "❌ Unity Editor 进程未运行，无法重启 MCP。",
        "  - 请先使用 unity-restart-instance 工具重新打开 Unity：",
        "    game-dev_unity_unity-restart-instance",
        "",
        "  或在 Unity Hub 中手动打开项目。",
      ].join("\n")
    }

    // ── Step 3: Try HTTP restart endpoint ─────────────────────────────
    const statusMsg = isHealthy ? "强制重启" : "MCP 无响应"
    try {
      const controller = new AbortController()
      const timer = setTimeout(() => controller.abort(), 5000)
      const resp = await fetch("http://127.0.0.1:8080/restart", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ action: "restart" }),
        signal: controller.signal,
      })
      clearTimeout(timer)

      if (resp.ok) {
        for (let i = 0; i < 15; i++) {
          await new Promise((r) => setTimeout(r, 2000))
          if (await checkMCPHealth()) {
            return "✅ Unity MCP 已通过 HTTP 重启端点成功重启。"
          }
        }
        return "⚠️ HTTP 重启端点已响应，但 MCP 未在预期时间内恢复连接，请稍后手动检查。"
      }
    } catch {
      // HTTP restart endpoint not available — fallback to script recompile
    }

    // ── Step 4: Fallback — trigger script recompile via temp .cs file ──
    const projectRoot = findUnityProjectRoot(cwd)
    if (!projectRoot) {
      return [
        `⚠️ [${statusMsg}] 无法自动检测 Unity 项目路径。`,
        "   请尝试以下方法：",
        "   1. 在 Unity Editor 中手动触发脚本重新编译（例如修改任意脚本文件后保存）",
        "   2. 或确认当前工作目录在 Unity 项目内",
      ].join("\n")
    }

    const triggerFile = join(projectRoot, "Assets", "__mcp_restart_trigger.cs")

    try {
      // Create a minimal trigger file to force Unity to recompile
      writeFileSync(triggerFile, "// Unity MCP restart trigger — created by vibe-stack, safe to delete\n", "utf-8")

      // Wait for Unity to detect the new file and recompile
      await new Promise((r) => setTimeout(r, COMPILE_WAIT_MS))

      // Remove the trigger file (cleanup)
      try { unlinkSync(triggerFile) } catch { /* file may already be gone */ }

      // Wait for the compilation after deletion to settle
      await new Promise((r) => setTimeout(r, SETTLE_WAIT_MS))

      // ── Step 5: Verify ──────────────────────────────────────────────
      const finalHealth = await checkMCPHealth()
      if (finalHealth) {
        return [
          "✅ Unity MCP 已通过脚本重新编译成功重启。",
          "   触发文件已自动清理。",
        ].join("\n")
      }

      return [
        "⚠️ 脚本重新编译已触发，但 MCP 仍未响应。",
        "   可能的原因：",
        "   - Unity Editor 中存在编译错误（请检查 Unity Console 窗口）",
        "   - Unity MCP 插件未正确安装",
        "   - MCP 端口 (8080) 被其他程序占用",
        "   建议：在 Unity Editor 中打开 Window → Unity MCP → 检查状态面板",
      ].join("\n")
    } catch (error) {
      // Cleanup on failure
      try { unlinkSync(triggerFile) } catch { /* ignore */ }
      return `❌ 重启 MCP 时出错: ${error instanceof Error ? error.message : String(error)}`
    }
  },
})
