# memory/ — 随插件移植的 AI 记忆

这里是 `cn.etetet.yiuimcp` 在实战中沉淀的、**与具体项目无关、可移植**的 AI 知识，目的是：当你把本插件放进**另一个 ET/YIUI 工程**时，让那个工程里的 AI（Claude Code / Codex 等）能直接复用这些经验，而不用从零踩坑。

## 两种加载方式

1. **SKILL.md 作为 skill 加载（主要能力）**
   `skills/yiuimcp/SKILL.md` 里已包含 `/rpc` 主干用法、全工具速查、Panel→View→Item 工作流、关键踩坑。本包根目录带 `.claude-plugin/plugin.json`，是一个 Claude Code plugin——在工程根 `claude --plugin-dir Packages/cn.etetet.yiuimcp` 即可加载（skill 为 `/yiuimcp:yiuimcp`，并按 description 自动按需调用）。Claude Code **不会**自动扫描 Unity 包内的 `skills/`，所以必须经 plugin 或拷到 `.claude/skills/` 才能用（详见包内 README「在 Claude Code 中使用」）。Codex 则直接识别本 skill。

2. **memory/ 手动导入（可选，增强上下文）**
   Claude Code 的长期记忆位于用户机器的 `~/.claude/projects/<工程哈希>/memory/`，**不会**从仓库自动加载。若想让新工程的 AI 拥有下面这些记忆，把本目录的 `*.md` 拷进该工程对应的 memory 目录即可（`MEMORY.md` 作为索引，可与已有 `MEMORY.md` 合并）。
   - Codex 等其他客户端：可直接把这些文件作为参考文档喂给 AI。

## 文件

- `MEMORY.md` — 记忆索引（一行一条）
- `yiuimcp-knowledge.md` — 本插件是什么 / 与原生区别 / YIUI 架构 / 关键踩坑（核心，建议必带）
- `unity-mcp-plugin-comparison.md` — 与 coplay / ivanmurzak 等 Unity-MCP 插件的对比（为什么 CLI-first 在 ET 里更稳）

> 这些是"经验/背景"，会随版本演进；具体工具的权威说明以 `skills/yiuimcp/SKILL.md`（自动生成的工具表）为准。
