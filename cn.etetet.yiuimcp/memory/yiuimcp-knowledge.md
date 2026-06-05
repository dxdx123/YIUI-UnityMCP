---
name: yiuimcp-knowledge
description: cn.etetet.yiuimcp 插件的核心知识——是什么、与原生 YIUIMCP 的区别、怎么驱动、YIUI 的 Panel→View→Item 架构、以及一组关键踩坑。放入任意 ET/YIUI 工程后可让 AI 复用。
metadata:
  type: reference
---

# cn.etetet.yiuimcp 核心知识（可移植）

> 操作细节与完整工具表以同包 `skills/yiuimcp/SKILL.md` 为准；本文是背景与经验。

## 是什么 / 与原生 YIUIMCP 的区别

- **原生 YIUIMCP（基础层）**：Unity 内一个无状态 `HttpListener` 本地服务（默认 `http://127.0.0.1:3212`，`/health` + `/rpc`），CLI-first；提供 9 个基础工具：`Log` `LogError` `EnterPlayMode` `StopPlayMode` `TriggerCompile` `GetCompileResult` `GetConsoleLog` `ExecuteMenu` `AssertConsoleContains`，外加 UTO 编排层与 `Config/*.ps1` flow。
- **本仓库的扩展**：在基础层之上加了整套 **YIUI UI 操作工具**（创建链 / 节点 / 布局样式 / 组件·数据·事件绑定 / 截图自检 / 运行态验证），当前共 43 个。

## 怎么驱动

- **主干 = 直接 `POST /rpc`**：`{"jsonrpc":"2.0","id":"1","method":"<工具名>","params":{...}}`。YIUI 操作工具基本都这么调，不是只能用 PS1。
- **PS1 仅辅助**：`compile-unity-flow.ps1`(编译) / `get_console_log.ps1` / `invoke-uto-tool.ps1`(带参兜底) / `proto2cs-flow.ps1`·`excel-export-flow.ps1`(codegen+重编译)。
- **域重载**：`TriggerCompile` / `YIUIExportCode` / `YIUISourceSplit` 会触发域重载 → 该次 `/rpc` 超时但工作已完成；轮询 `/health` 恢复后再 `GetCompileResult` 确认。serverId 变化=发生过重载。
- **为什么 CLI-first 在 ET 里更稳**：见 `unity-mcp-plugin-comparison.md`（长连接型 MCP 插件在 ET 的 Play/域重载下会断；yiuimcp 无长连接、自恢复）。

## YIUI 架构（关键）

- **CDE 表**：每个 YIUI prefab 根上有 `UIBindCDETable`，挂三张子表 `UIBindComponentTable`(组件) / `UIBindDataTable`(数据) / `UIBindEventTable`(事件)，均为 Odin `SerializedMonoBehaviour`。
- **codeType**：`Panel`(整屏面板) / `View`(面板内视图) / `Common`(可复用件，**列表项就是 Common**)。
- **列表三层 Panel→View→Item**：Panel 的 `AllViewParent` 下放 `{View}Parent`→`{View}`(View，持有 `LoopVerticalScrollRect`)；运行时 `OpenViewAsync` 把 View 克隆进去；View 里 `AddChild<YIUILoopScrollChild,LoopScrollRect,Type>(u_Com滚动, typeof(ItemComponent))` 驱动循环列表，`SetDataRefresh(list)` 填充，`YIUILoopRenderer` 渲染每一行。
- **导出代码**：`YIUIExportCode` 每个 prefab 生成 4 个文件（ModelView 的 Gen+手写 Component、HotfixView 的 Gen+手写 System）。绑定命名 `u_Com*`(组件) / `u_Data*`(数据) / `u_Event*`(事件)。
- **绑定注册**：组件类型 → prefab 的映射由 SG 从 `[YIUI(codeType)]` 特性生成（`YIUIBindProvider`）。`u_Event*` 同步事件用 `void`+`.NoContext()` fire-and-forget；`UITaskEvent*` 才用 `async ETTask`。

## 关键踩坑（血泪，务必遵守）

1. **LoopScroll 的 Item 必须有 `LayoutElement`(preferredWidth/Height)**。否则 loop 算不出行高，所有 item 堆在 y=0(视口外)，列表看起来全空，但 `SetDataRefresh`/业务日志正常——极易误判为逻辑 bug。
2. **组件绑定写 `m_AllBindPair`(源)，不是 `m_AllBindDic`(派生缓存)**。后者导出时 AutoCheck 会清空重建 → 直接写会丢。`YIUIAddComponentBinding` 已实现正确写法。(Data/Event 表的 dict 本身即源，无此问题。)
3. **Loop item 的 codeType 是 `Common`**，不是 View。
4. **data 名传全名**(带 `u_Data` 前缀，如 `u_DataIndex`)，别传 `index`——否则导出改名后数据表 key 与 Text 绑定 key 不一致。
5. **导出需把 prefab 持久化**：AutoCheck 在内存里规范化的字段(如拆分后从节点名重算的 `ResName`)必须落盘，否则 prefab 上残留旧值与生成代码不一致。`YIUIExportCode` 已在 AutoCheck 后 `SaveAssets`。
6. **Play 模式下改 prefab 资源可能在域重载后回滚** → 改 prefab 一律先 `StopPlayMode` 在 edit 模式做，改完 grep 磁盘确认落盘。
7. **中文/非 ASCII 经 git-bash/curl 传 `/rpc` 会乱码**(shell 层破坏字节) → 用 JSON `\uXXXX` 转义。服务端已强制 `Encoding.UTF8` 读 body。
8. **PowerShell 5.1 的 .ps1 必须纯 ASCII**(无 BOM 按 GBK 解析，中文字面量/注释都可能乱码甚至破坏解析)；要输出中文就从 UTF-8 文件 `Get-Content -Encoding UTF8` 读，别写死在脚本里。
9. **`GetConsoleLog` 的 `logType`** 用位标志枚举 `ErrorMask`/`WarningMask`/`LogMask`/`All`，不是 `"Error"`/`"Warning"`。它的 error 计数会把运行时异常也算上——长时间 Play 刷 NRE 后看编译结果要以最新一次重编译为准。
10. **`find_gameobjects` 当前找不到嵌套节点**(只搜有限范围)，别用它做运行时层级查询；运行态用 `YIUISimulateClick`(内部 GameObject.Find+全量遍历)间接验证。
11. **端口 3212 多 ET 编辑器共享**，同开多个工程会抢端口 → 关掉其他工程。
12. 编译结果里 `SerializedObjectNotCreatableException` / 域重载期 `NullReferenceException(SerializedObject Disposed)` 多为 Odin 重载假阳性，**只看最新一次有没有 `error CS`**。

## 自检闭环（强烈建议）

- 静态：改完 prefab → `YIUICapturePrefab` 渲染 PNG → 读图判断 → 不对就改再截。Panel 用设计分辨率；View/Common/Item 非全屏拉伸的根会自动居中预览。
- 运行态：`EnterPlayMode` → `YIUISimulateClick`(按节点名走 IPointerClickHandler 真实点击) → `YIUICaptureGameView`(下一帧落盘，稍等再读)。
