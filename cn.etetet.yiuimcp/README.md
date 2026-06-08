# cn.etetet.yiuimcp

> YIUI MCP 包：在 Unity Editor 内提供本地 HTTP JSON-RPC 服务，并附带一个可选的 UTO HTTP 编排层，用来处理编译、域重载和批量调用。

## 当前实现概览

- Unity 端服务入口位于 `Editor/UnityMCP/Core/`，默认监听 `127.0.0.1:3212`
- UTO 位于 `UTO/`，启动 HTTP 模式后监听 `Unity端口 + 1`，默认是 `3213`
- Unity 端健康检查接口为 `GET /health`
- Unity 端 RPC 接口为 `POST /rpc`
- UTO 提供 `GET /health`、`POST /call`、`POST /batch`、`GET /tools`

## 当前可用工具

当前共 **43 个**工具，分两类：

**基础层（9 个，原生 YIUIMCP 能力）**：`Log` / `LogError` / `EnterPlayMode` / `StopPlayMode` / `TriggerCompile` / `GetCompileResult` / `GetConsoleLog` / `ExecuteMenu` / `AssertConsoleContains`

**YIUI 操作层（34 个，本仓库扩展）**：YIUI 创建链、节点/布局/样式、组件/数据/事件绑定、`YIUICapturePrefab` / `YIUICaptureGameView` / `YIUISimulateClick` 截图自检与运行态验证等。

> 完整工具速查表见 [skills/yiuimcp/SKILL.md](skills/yiuimcp/SKILL.md) 第 5 节，由 `Config/gen-skill-tools.ps1` 从 `[YIUIMCPTools]` 特性自动生成，新增工具后重跑即同步。

说明：
- 工具通过 `[YIUIMCPTools(...)]` 特性自动注册
- **基础层**可用 `Config/*.ps1` flow 驱动；**YIUI 操作层**几乎都是直接 `POST /rpc`（见 SKILL.md「`/rpc` 是主干」）

## 依赖

本包当前显式依赖：

```json
"com.unity.nuget.newtonsoft-json": "3.2.1"
```

如果项目里缺少该依赖，会出现 `using Newtonsoft.Json;` 相关的编译错误。

## 快速开始

### 1. 打开 Unity 工程

Unity 启动后，`YIUIMCPServerHelper` 会根据配置自动拉起 Unity 侧服务。

### 2. 安装并构建 UTO（仅在需要 HTTP 编排层时）

```bash
cd Packages/cn.etetet.yiuimcp/UTO
npm install
npm run build
```

### 3. 启动 UTO HTTP

```bash
cd Packages/cn.etetet.yiuimcp/UTO
npm run start:http
```

### 4. 调用示例

单工具调用：

```powershell
$body = @{
    tool = "Log"
    params = @{
        message = "Hello from AI"
    }
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:3213/call" -Method Post -Body $body -ContentType "application/json"
```

编译流程脚本：

```powershell
powershell -ExecutionPolicy Bypass -Command "& '.\Packages\cn.etetet.yiuimcp\Config\compile-unity-flow.ps1' -Force 0 -NoWait 1"
```

## 文档索引

- [Docs/README.md](Docs/README.md): 文档总览
- [Docs/UTO与UnityMCP协作手册.md](Docs/UTO与UnityMCP协作手册.md): 架构与协作方式
- [Docs/README_HTTP.md](Docs/README_HTTP.md): HTTP 接口说明
- [Docs/README-Flow.md](Docs/README-Flow.md): PowerShell 流程脚本说明
- [Docs/如何扩展Unity原子工具.md](Docs/如何扩展Unity原子工具.md): 扩展工具指南
- [Docs/UnityMCP异步编程指南.md](Docs/UnityMCP异步编程指南.md): Unity 主线程异步约束
- [Docs/强制编译规则.md](Docs/强制编译规则.md): 修改 C# 后的验证要求

## Skill

包内附带了一个可分享的 Codex skill：

- [skills/yiuimcp/SKILL.md](skills/yiuimcp/SKILL.md)

用途：

- 让 AI 以 CLI-first 的方式驱动这个包
- 优先使用 `Config/*.ps1` 作为高聚合入口
- 避免把工作方式退化成传统逐个 MCP tool call

如果别人把这个 skill 安装到自己的 Codex skills 目录里，就可以直接这样引用：

```text
Use $yiuimcp to compile this Unity project through its bundled CLI flow.
```

对应的 UI 元数据位于：

- [skills/yiuimcp/agents/openai.yaml](skills/yiuimcp/agents/openai.yaml)（仅 Codex 使用，Claude Code 会忽略）

### 在 Claude Code 中使用

本包同时是一个 **Claude Code plugin**：根目录有 `.claude-plugin/plugin.json`，skill 位于 `skills/yiuimcp/`。Claude Code 不会自动扫描 Unity 包内的 `skills/`，需用以下任一方式启用：

- **作为 plugin 加载（推荐，随包移植）**——在工程根目录执行：
  ```bash
  claude --plugin-dir Packages/cn.etetet.yiuimcp
  ```
  之后 skill 以 `/yiuimcp:yiuimcp` 提供，Claude 也会按 `description` 自动按需调用。
- **或拷进项目级 skills 目录**：
  ```bash
  mkdir -p .claude/skills/yiuimcp
  cp Packages/cn.etetet.yiuimcp/skills/yiuimcp/SKILL.md .claude/skills/yiuimcp/
  ```

> `.claude-plugin/` 以点开头，Unity 会忽略该目录（不导入、不生成 .meta），不影响 Unity 工程。

## 随身记忆（memory/）

`memory/` 收录了与具体项目无关、可移植的 AI 经验（插件定位、YIUI 架构、关键踩坑、插件对比），方便把本插件放进新工程时让该工程的 AI 直接复用：

- [memory/README.md](memory/README.md): 使用方式（SKILL.md 自动加载；memory 可手动导入新工程的 `~/.claude/.../memory/`）
- [memory/yiuimcp-knowledge.md](memory/yiuimcp-knowledge.md): 核心知识与踩坑
- [memory/unity-mcp-plugin-comparison.md](memory/unity-mcp-plugin-comparison.md): 与其他 Unity-MCP 插件对比

## 维护与扩展注意事项

### 新增 C# 工具后的完整联动

新增一个带 `[YIUIMCPTools]` 特性的 C# 工具后，需要同步更新以下 **4 处**，缺一不可：

| 步骤 | 操作 | 文件 |
|---|---|---|
| 1 | 重跑工具表生成脚本 | `Config/gen-skill-tools.ps1` → 更新 `skills/yiuimcp/SKILL.md` 第 5 节 |
| 2 | 同步 UTO 静态工具表 | `UTO/src/index.ts` → `STATIC_TOOLS` 数组追加新工具 |
| 3 | 重新编译 UTO | `cd UTO && npm run build` |
| 4 | 同步 AI 客户端上下文 | `Config/cursor/yiuimcp.mdc` 和 `Config/codex/AGENTS.yiuimcp.mdc` 的工具速查节 |

**为什么要更新 STATIC_TOOLS？**
Cursor / Codex 显示的工具列表来自 `index.ts` 里的静态表，不是 Unity 运行时动态返回的（Unity 的 `ListTools` RPC 可能只返回部分工具）。静态表是完整工具列表对外暴露的权威来源。

### 修改 UTO（TypeScript）后必须重新 build

`UTO/src/*.ts` 文件被修改后，Cursor / Codex 实际运行的是 `UTO/build/index.js`（编译产物），不是 `.ts` 源文件。每次修改 TS 都必须：

```bash
cd Packages/cn.etetet.yiuimcp/UTO
npm run build
```

然后在 Cursor 里 `Ctrl+Shift+P → Developer: Reload Window` 让 MCP 客户端重连，才会加载新版本。

> `tsconfig.json` 在仓库根已补充，初次 build 前先 `npm install`。

### PowerShell 脚本必须纯 ASCII

`Config/` 下所有 `.ps1` 文件**不能写中文字面量或中文注释**（PowerShell 5.1 无 BOM 时按 GBK 解析，中文会乱码）。需要输出中文时，把内容存入 UTF-8 的 `.md` / `.txt` 文件，然后在 `.ps1` 里用：

```powershell
Get-Content "path\to\file.md" -Encoding UTF8
```

`Config/cursor/yiuimcp.mdc` 和 `Config/codex/AGENTS.yiuimcp.mdc` 是纯 Markdown，可以包含中文，不受此限制。

### AI 客户端重连

修改 `UTO/src/index.ts` 并 build 后，需要重启或重新加载 AI 客户端才能使新工具列表生效：

- **Cursor**：`Ctrl+Shift+P → Developer: Reload Window`
- **Codex**：重新执行 `codex` 命令
- **Claude Code**：重新执行 `claude --plugin-dir Packages/cn.etetet.yiuimcp`

### 新建文件与 Unity .meta

在包目录内新建文件后，Unity 会自动生成 `.meta` 文件。如需把这些文件一起提交到版本库，记得把对应的 `.meta` 一起加入 git。

---

## 重要说明

- `UTO/.port` 记录 Unity 端口；UTO HTTP 端口按 `Unity端口 + 1` 计算
- `GET /tools` 目前返回的是最小演示列表，不是 Unity 侧所有工具的权威来源
- 如果只需要 Unity 原子能力，不一定必须启动 UTO；但编译和域重载场景更适合走 UTO
