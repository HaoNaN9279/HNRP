import { tool } from "@opencode-ai/plugin"
import { join, dirname, resolve } from "path"
import { existsSync } from "fs"

const UNITY_HUB_COMMON_PATHS: string[] = [
  "C:\\Program Files\\Unity Hub\\Unity Hub.exe",
  "C:\\Program Files (x86)\\Unity Hub\\Unity Hub.exe",
]

const MCP_BASE_URL = "http://127.0.0.1:8080/mcp"
const MAX_WAIT_SECONDS = 120
const POLL_INTERVAL_MS = 3000

// ── Helpers ─────────────────────────────────────────────────────────────

async function checkMCPHealth(): Promise<boolean> {
  try {
    const controller = new AbortController()
    const timer = setTimeout(() => controller.abort(), 3000)
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

function findUnityProjectRoot(start: string): string | null {
  let current = resolve(start)
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

async function findUnityHub(): Promise<string | null> {
  // Check common installation paths first
  for (const p of UNITY_HUB_COMMON_PATHS) {
    if (existsSync(p)) return p
  }
  // Fallback: search PATH via PowerShell
  try {
    const out = await Bun.$`powershell -NoProfile -Command "&{ $p = Get-Command 'Unity Hub.exe' -ErrorAction SilentlyContinue; if ($p) { $p.Source } }"`.text()
    const trimmed = out.trim()
    if (trimmed) return trimmed
  } catch {
    // Not found
  }
  return null
}

async function waitForUnityStartup(initialMCP: boolean): Promise<string> {
  const startTime = Date.now()
  let unityStarted = false
  let mcpStarted = false

  while (Date.now() - startTime < MAX_WAIT_SECONDS * 1000) {
    await new Promise((r) => setTimeout(r, POLL_INTERVAL_MS))

    // Check Unity process
    if (!unityStarted) {
      unityStarted = await isUnityEditorRunning()
    }

    // Check MCP
    if (!mcpStarted) {
      mcpStarted = await checkMCPHealth()
    }

    if (unityStarted && mcpStarted) break
  }

  const elapsed = Math.round((Date.now() - startTime) / 1000)
  const parts: string[] = []

  if (unityStarted) {
    parts.push(`✅ Unity Editor 进程已启动 (${elapsed}s)`)
  } else {
    parts.push(`⚠️ Unity Editor 进程未在 ${elapsed}s 内检测到`)
  }

  if (mcpStarted) {
    parts.push(`✅ Unity MCP 已连接 (${elapsed}s)`)
  } else {
    parts.push("⚠️ Unity MCP 未响应")
  }

  // If MCP came back but we had initial MCP (restart case)
  if (initialMCP && !await checkMCPHealth()) {
    parts.push("ℹ️ Unity 已重新启动，建议等待几秒后再次检查 MCP 状态。")
  }

  return parts.join("\n")
}

// ── Tool definition ─────────────────────────────────────────────────────

export default tool({
  description: "Restart Unity Editor via Unity Hub when the Unity instance crashes or becomes unresponsive. Opens the current Unity project through Unity Hub, then waits for Unity and MCP to come back online.",
  args: {
    projectPath: tool.schema.string().optional().describe("Unity 项目路径。留空时自动从当前目录向上查找"),
    unityHubPath: tool.schema.string().optional().describe("Unity Hub 可执行文件路径。留空时自动检测"),
    force: tool.schema.boolean().optional().default(false).describe("即使 Unity 或 MCP 仍在运行也强制重启"),
  },
  async execute(args, context) {
    // ── Step 1: Find project path ─────────────────────────────────────
    const cwd = context?.directory || process.cwd()
    const projectPath = args.projectPath
      ? resolve(args.projectPath)
      : findUnityProjectRoot(cwd)

    if (!projectPath) {
      return [
        "❌ 无法确定 Unity 项目路径。",
        "   请通过 projectPath 参数指定项目路径，",
        "   或在 Unity 项目目录中运行此工具。",
      ].join("\n")
    }

    if (!existsSync(join(projectPath, "ProjectSettings"))) {
      return [
        `❌ 指定路径不是有效的 Unity 项目: ${projectPath}`,
        "   缺少 ProjectSettings 目录。",
      ].join("\n")
    }

    // ── Step 2: Find Unity Hub ────────────────────────────────────────
    const hubPath = args.unityHubPath || await findUnityHub()
    if (!hubPath) {
      return [
        "❌ 未找到 Unity Hub。",
        "   请确保 Unity Hub 已安装。",
        "   如果安装路径非标准，请通过 unityHubPath 参数指定。",
      ].join("\n")
    }

    // ── Step 3: Check current state ────────────────────────────────────
    const unityRunning = await isUnityEditorRunning()
    const mcpHealthy = await checkMCPHealth()

    if (unityRunning && mcpHealthy && !args.force) {
      return [
        "✅ Unity Editor 和 MCP 均正常运行，无需重启。",
        "   如需强制重启，请设置 force=true。",
      ].join("\n")
    }

    // ── Step 4: Close old Unity if running (only with force) ──────────
    if (unityRunning && args.force) {
      try {
        await Bun.$`powershell -NoProfile -Command "&{ Stop-Process -Name Unity -Force -ErrorAction SilentlyContinue }"`.text()
        await new Promise((r) => setTimeout(r, 3000))
      } catch {
        // Non-critical, continue
      }
    }

    // ── Step 5: Launch Unity Hub with the project ─────────────────────
    const projectName = projectPath.split("\\").pop() || projectPath.split("/").pop() || "Unknown"

    try {
      // Use Bun.spawn to launch Unity Hub directly (bypassing shell quoting issues)
      const proc = Bun.spawn([hubPath, "--", "--no-wait", "--open-project-path", projectPath])
      // Give it a moment to fail-fast (e.g., file not found)
      await new Promise((r) => setTimeout(r, 2000))

      if (proc.killed || proc.exitCode === null) {
        // Process is running — good
      } else if (proc.exitCode !== 0) {
        const stderr = await new Response(proc.stderr).text()
        return [
          `❌ Unity Hub 启动失败 (exit code: ${proc.exitCode})`,
          `   ${stderr.trim()}`,
        ].join("\n")
      }
    } catch (error) {
      return [
        `❌ 启动 Unity Hub 时出错: ${error instanceof Error ? error.message : String(error)}`,
        `   Unity Hub 路径: ${hubPath}`,
        `   项目路径: ${projectPath}`,
        "   请检查 Unity Hub 安装和项目路径。",
      ].join("\n")
    }

    // ── Step 6: Wait for Unity and MCP ────────────────────────────────
    const result = await waitForUnityStartup(mcpHealthy)

    return [
      `🚀 Unity 实例重启完成 (项目: ${projectName})`,
      `   项目路径: ${projectPath}`,
      `   Unity Hub: ${hubPath}`,
      "",
      result,
      "",
      `如果 Unity 已打开但 MCP 仍无响应，请使用 unity-restart-mcp 工具重启 MCP：`,
      `   game-dev_unity_unity-restart-mcp`,
    ].join("\n")
  },
})
