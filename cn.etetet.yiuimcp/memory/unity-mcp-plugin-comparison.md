---
name: unity-mcp-plugin-comparison
description: 三类 Unity-MCP 插件在 ET 工程(Play/域重载频繁)下的对比——为什么 cn.etetet.yiuimcp 的 CLI-first/无长连接设计在 ET 里更稳。
metadata:
  type: reference
---

# Unity-MCP 插件对比：为什么 yiuimcp 在 ET 里更稳

ET 工程的脆弱点是 **Play / 域重载**（很多 ET 工程 `EnterPlayModeOptionsEnabled: 0`，每次 Play 都整域重载）。三类插件在这一点上的表现：

## 1 & 2. Coplay (com.coplaydev.unity-mcp) / ivanmurzak (com.ivanmurzak.unity.mcp) —— MCP-first，长连接

- 通过一条**长连接 socket**(SignalR/WebSocket)把原子 MCP 工具实时暴露给 AI 客户端。
- socket 的连接/握手延续运行在 .NET 主线程 `SynchronizationContext` 上，而 ET 的 `MainThreadScheduler` 会劫持它。
- 结果：**每次 Play/编译域重载连接就断**；Play 期间无法重连(握手延续永远落不下来 → 整个 Play 期间 MCP 命令返回 `Response data is null`)；Stop 后能否自愈取决于是否打了 patch1(ET `MainThreadScheduler` 构造时捕获、`Dispose()` 时还原 `previousSynchronizationContext`)。
- 无论是否 patch1，**Play 期间都不可用**。

## 3. cn.etetet.yiuimcp —— CLI-first，无长连接，结构上免疫

- Unity 侧是**无状态 `HttpListener` JSON-RPC**(`/health`、`/rpc`，默认端口 3212)，自带 `YIUIMCPDispatcher` 在 `EditorApplication.update` 上 pump 到主线程——**不依赖 .NET `SynchronizationContext`**，所以 ET 的劫持与它无关；没有长连接可断。
- 每次域重载后 `[InitializeOnLoad]`(`YIUIMCPServerHelper`)自动重启监听(`[YIUIMCP] 启动成功，端口: 3212`)。
- 上层用 PowerShell flow / 直接 `/rpc` 驱动；可选的 Node "UTO" 层加心跳+恢复等待+批量。
- **实测**(stock MainThreadScheduler，无 patch1)：Edit→Play→Stop 全程可用，**Play 期间也能用**，自动恢复，**无需 patch1**——这是相对前两者的关键优势。

## 取舍

yiuimcp 不向 AI 客户端暴露 live MCP tool，需要用 `Config/*.ps1` flow 或直接 `/rpc` 驱动。快速自测：`GET /health` → 然后 `POST /rpc`。注意 `UTO/.port` 文件可能缺失但服务仍健康——以 `/health` 为准。
