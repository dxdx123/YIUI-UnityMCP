---
name: yiuimcp
description: 面向嵌入 `Packages/cn.etetet.yiuimcp` 的 ET/YIUI Unity 工程的 UI 自动化 skill。通过 HTTP JSON-RPC(默认 http://127.0.0.1:3212/rpc)驱动 Unity 完成全部 YIUI UI 操作——创建模块/面板/视图/列表、编辑节点结构与布局、设置文本/图形/布局组件、添加 YIUI 组件/数据/事件绑定、导出代码、渲染 prefab/运行时 Game 视图为 PNG 做布局自检、模拟点击做运行态验证，以及编译、读控制台、执行菜单。适用于"让 AI 自动搭 YIUI 界面并自检"的场景。
---

# YIUI MCP — Unity UI 自动化 skill

让 AI 通过 `cn.etetet.yiuimcp` 自动化 ET/YIUI 工程的**全部 UI 操作**：建面板/视图/列表 → 编辑布局样式 → 绑定组件/数据/事件 → 导出代码 → 截图自检 → 运行态验证。

> 这个 skill 只适用于包含 `Packages/cn.etetet.yiuimcp` 的 ET/YIUI 工程，不是通用 Unity skill。

## 1. 适用判断

工作区存在以下任一即适用：
- `Packages/cn.etetet.yiuimcp/Editor/UnityMCP`（C# 工具实现）
- `Packages/cn.etetet.yiuimcp/Config`（PowerShell flow）

健康检查：`GET http://127.0.0.1:3212/health` 返回 `{"status":"ok",...}` 即服务在线。
（注意：`UTO/.port` 文件可能缺失但服务仍健康，**以 `/health` 为准**，不要依赖 `.port` 是否存在。）

## 2. 怎么驱动 —— `/rpc` 是主干

**绝大多数操作直接 POST 到 `http://127.0.0.1:3212/rpc`**，body 为 JSON-RPC：

```
{"jsonrpc":"2.0","id":"1","method":"<工具名>","params":{...}}
```

PowerShell helper（处理 byte[] 返回）或 curl 均可。`Config/*.ps1` 只是少数基础流程(编译/控制台)的封装，**不覆盖 YIUI 操作工具**——别误以为只能用 PS1。

PS1 辅助入口（仅这几类用）：
- `Config/compile-unity-flow.ps1` → `StopPlayMode → TriggerCompile → GetCompileResult`
- `Config/get_console_log.ps1` → `GetConsoleLog`
- `Config/invoke-uto-tool.ps1` → 带参数调任意工具的通用兜底（参数需 UTF-8 Base64）
- `Config/gen-skill-tools.ps1` → 重新生成本文件第 5 节的工具速查表（纯 ASCII 脚本，勿加中文）
- `Config/proto2cs-flow.ps1` / `excel-export-flow.ps1` → 一条命令跑 codegen + 重编译（引擎 `menu-codegen-flow.ps1 -MenuPath <菜单>`）

### 调用 Unity 菜单命令

任何 `[MenuItem]` 注册的菜单都能用 `ExecuteMenu{menuPath:"..."}` 触发，例如 ET 常用 codegen：
- `ET/Proto/Proto2CS`、`ET/Excel/ExcelExporter`（生成代码后会触发重编译 → 用 `GetCompileResult` 确认；`ExecuteMenu` 的 success 只表示菜单被触发，生成结果看控制台）
- 这两个建议直接用上面的 `proto2cs-flow.ps1` / `excel-export-flow.ps1`（已封装 退出Play→codegen→编译→取结果）

### 域重载处理（重要）

`TriggerCompile` / `YIUIExportCode` / `YIUISourceSplit` 会触发**域重载** → 该次 `/rpc` 请求超时或返回空，**但 Unity 已完成工作**。处理方式：超时后轮询 `/health` 直到 200，再 `GetCompileResult` 确认，或在磁盘上 grep prefab 验证。serverId 变化即代表发生过重载。

## 3. 标准工作流：搭一个 Panel→View→Item 列表 demo

YIUI 列表是三层：**Panel → View(持有 LoopScrollRect) → Item(每行, codeType=Common)**。

1. **建面板源**：`YIUICreatePanelSource{moduleName,resName}` → `Source/{Res}PanelSource.prefab`（含 UIBlockBG / AllViewParent / AllPopupViewParent）
2. **编辑源**：加按钮节点(`YIUICreateChildNode`)、事件(`YIUIAddEventBinding` 或 `YIUIBindClickEventToNode`)、布局
3. **拆分**：`YIUISourceSplit{sourcePrefabPath}` → `Prefabs/{Res}Panel.prefab` + 抽出的 View prefab
4. **建视图里的列表**：`YIUICreateView` 建 View(在 AllViewParent 下，名 `{View}Parent`)，View 内放 LoopScroll；`YIUIAddComponentBinding` 把 LoopVerticalScrollRect 绑为 `u_ComXxx`
5. **建 Item**（codeType=**Common**）：`YIUICreatePrefab(codeType=Common)` → `YIUICreateChildNode` 加文本节点 → `YIUIAddDataBinding(dataName="u_DataIndex",type=Int)` → `YIUIAddTextBindingToNode(dataName="u_DataIndex")` → **`YIUISetLayoutElement(preferredWidth/Height=item宽高)` ← 必须！**
6. **导出代码**：`YIUIExportCode` 对 Panel / View / Item 各跑一次（生成 4 类 .cs）
7. **手写逻辑**（ET System，YIUI 规范由人写）：View 的 `YIUIInitialize` 里 `AddChild<YIUILoopScrollChild,LoopScrollRect,Type>(self.u_ComXxx, typeof(ItemComponent))`；`Refresh(count)` 里 `SetDataRefresh`；`YIUILoopRenderer` 里 `item.u_DataIndex.SetValue(index)`
8. **编译 + 验证**：`compile-unity-flow` → `EnterPlayMode` → `YIUISimulateClick` 逐级点开 → `YIUICaptureGameView` 截图读图确认

### 自检闭环（强烈建议每次改完布局都做）

- **静态自检**：改完 prefab → `YIUICapturePrefab{prefabPath,width,height}` 渲染成 PNG → 用 Read 工具看图 → 不合理就改 → 再截。Panel 用设计分辨率(本工程 **1920×1080 横屏**)，View/Common/Item 用各自尺寸(非全屏拉伸的根会自动居中预览)。
- **运行态验证**：`EnterPlayMode` → 等到目标界面 → `YIUISimulateClick{name}` 按节点名模拟真实点击(走 IPointerClickHandler) → `YIUICaptureGameView{outputPath}`(下一帧落盘，稍等 ~1s 再 Read)。

## 4. 关键踩坑（本项目血泪，务必遵守）

1. **LoopScroll 的 Item 必须有 `LayoutElement`**（`preferredWidth/Height`）。否则 loop 算不出行高，所有 item 堆在 y=0(视口外)，列表看起来**全空**，但 `SetDataRefresh`/业务日志一切正常——极易误判为逻辑 bug。
2. **中文/非 ASCII 参数经本机 git-bash/curl 传 `/rpc` 会乱码**(shell 层破坏字节)。→ 中文用 JSON `\uXXXX` 转义传，例 `测试` → `测试`。服务端已强制 `Encoding.UTF8` 读 body。
3. **PowerShell 5.1 的 .ps1 必须纯 ASCII**（无 BOM 时按 GBK 解析，中文字面量/注释都会乱码甚至破坏解析）；要输出中文就从 UTF-8 文件里 `Get-Content -Encoding UTF8` 读，别写死在脚本里。
4. **组件绑定走 `m_AllBindPair` 源数据**(`YIUIAddComponentBinding` 已实现)，别直接写派生缓存 `m_AllBindDic`——导出时 AutoCheck 会清空重建导致丢失。
5. **Loop item 的 codeType 是 `Common`**，不是 View。
6. **data 名传全名**(带 `u_Data` 前缀，如 `u_DataIndex`)，不要传 `index`——否则导出改名后数据表 key 与 Text 绑定 key 不一致。
7. **Play 模式下改 prefab 资源可能在域重载后回滚** → 改 prefab 一律先 `StopPlayMode` 在 edit 模式下做，改完 grep 磁盘确认落盘。
8. **`GetConsoleLog` 的 `logType`** 用 `ErrorMask`/`WarningMask`/`LogMask`/`All`(位标志枚举)，不是 `"Error"`/`"Warning"`。
9. **`find_gameobjects` 目前找不到嵌套 YIUI 节点**(只搜有限范围)，别依赖它做运行时层级查询；运行态找节点用 `YIUISimulateClick`(内部 GameObject.Find + 全量遍历)间接验证。
10. **端口 3212 多 ET 编辑器共享**，同时开多个工程会抢端口 → 关掉其他工程，确保目标编辑器绑定 3212。
11. 编译结果里出现 `SerializedObjectNotCreatableException` / 域重载期的 `NullReferenceException(SerializedObject Disposed)` 多为 Odin Inspector 重载假阳性，**只看最新一次编译有没有 `error CS`**(Editor.log 里旧的 CS 行是历史残留)。

## 5. 工具速查

> 下表由 `Config/gen-skill-tools.ps1` 从 `[YIUIMCPTools]` 特性自动生成，**勿手改**；新增工具后重跑该脚本即可同步。

<!-- AUTOGEN:TOOLS:START -->

_43 tools - auto-generated by gen-skill-tools.ps1, do not edit by hand_

**Basic (compile / console / play / menu)**

| Tool | Desc |
|---|---|
| `AssertConsoleContains` | 断言控制台日志包含关键词 |
| `EnterPlayMode` | 进入运行模式 |
| `ExecuteMenu` | 执行Unity菜单命令 |
| `GetCompileResult` | 获取编译结果 |
| `GetConsoleLog` | 获取控制台日志 |
| `Log` | 打印日志 |
| `LogError` | 打印错误日志 |
| `StopPlayMode` | 退出运行模式 |
| `TriggerCompile` | 触发编译 |

**YIUI ops (create / node / layout / binding / capture)**

| Tool | Desc |
|---|---|
| `YIUIAddComponent` | 添加Unity组件到Prefab节点 |
| `YIUIAddComponentBinding` | 通过YIUI API给Prefab添加Component绑定 |
| `YIUIAddDataBindActive` | 添加UIDataBindActive组件控制节点显隐 |
| `YIUIAddDataBindColor` | 添加UIDataBindColor组件控制Graphic颜色 |
| `YIUIAddDataBindImage` | 添加UIDataBindImage组件控制Image sprite |
| `YIUIAddDataBinding` | 通过YIUI API给Prefab添加Data变量 |
| `YIUIAddEventBinding` | 通过YIUI API给Prefab添加Event事件 |
| `YIUIAddTextBindingToNode` | 在节点上添加文本绑定到YIUI Data |
| `YIUIBindClickEventToNode` | 在指定节点上添加UIEventBindClick并绑定事件 |
| `YIUICaptureGameView` | 截取运行时Game视图为PNG(需Play) |
| `YIUICapturePrefab` | 渲染UI Prefab为PNG(布局自检) |
| `YIUICreateChildNode` | 在Prefab中创建子节点 |
| `YIUICreateLoopScroll` | 在Prefab节点下创建YIUI LoopScroll滚动列表(克隆官方模板, 支持水平/垂直/分组等) |
| `YIUICreateModule` | 通过YIUI自动化工具创建UI模块目录 |
| `YIUICreatePanelSource` | 创建标准YIUI Panel源数据(Source,含UIBlockBG/AllViewParent/AllPopupViewParent) |
| `YIUICreatePrefab` | 通过Unity/YIUI API创建Panel/View/Common Prefab |
| `YIUICreateView` | 在Panel源数据的AllViewParent下创建View(含ViewParent, 注册AllCreateView) |
| `YIUIDeleteChildNode` | 删除Prefab子节点 |
| `YIUIDuplicateNode` | 复制Prefab节点 |
| `YIUIExportCode` | 调用YIUI自动化工具为Prefab导出生成代码 |
| `YIUIInspectPrefab` | 读取Prefab上的YIUI CDE绑定信息 |
| `YIUIMoveNode` | 移动Prefab节点到新父节点或调整同级顺序 |
| `YIUIOpenAutoTool` | 打开YIUI自动化工具窗口 |
| `YIUIRemoveComponent` | 从Prefab节点移除Unity组件 |
| `YIUISetContentSizeFitter` | 设置Prefab节点的ContentSizeFitter |
| `YIUISetGraphic` | 设置节点的图形组件 |
| `YIUISetLayerRecursive` | 设置Prefab节点Layer，可递归设置子节点 |
| `YIUISetLayoutElement` | 设置Prefab节点的LayoutElement属性 |
| `YIUISetLayoutGroup` | 设置Prefab节点的LayoutGroup |
| `YIUISetNodeActive` | 设置Prefab节点的激活状态 |
| `YIUISetRectTransform` | 设置Prefab节点的RectTransform |
| `YIUISetText` | 设置节点的Text组件(字号/对齐/颜色/溢出) |
| `YIUISimulateClick` | Play模式下按名字模拟点击UI节点 |
| `YIUISourceSplit` | 源数据拆分: 将Source面板拆分生成到Prefabs目录(等价CDE的源数据拆分按钮) |
<!-- AUTOGEN:TOOLS:END -->

Coplay 风格工具（snake_case，返回 `{success,message,data}`）：
- `find_gameobjects` — 按名查找 GameObject（注意上面第 9 条限制）

## 6. 扩展工具时

- **优先复用**已有工具组合，而不是教 AI 手工拼接。
- 新增 C# 工具：在 `Editor/UnityMCP/Tools/YIUI/` 下加 `[YIUIMCPTools("Name","desc")]` 的 `YIUIMCPBaseExecutor<T>`，编辑 prefab 用 `YIUIMCPYIUIHelper.EditPrefab`(LoadPrefabContents→编辑→SaveAsPrefabAsset)。加完重跑 `gen-skill-tools.ps1` 同步本文件工具表。
- 新增高聚合 CLI flow：在 `Config/` 加 `.ps1` 并更新 `Config/README.md`。
- 改完务必用 `compile-unity-flow.ps1` 验证编译通过，并读真实控制台输出而非只看 exit code。

## 7. 边界

- 用 `/rpc` 直接调工具是主用法；UTO `/call`、`/batch` 与 `Config/*.ps1` 是其上的封装层。
- CLI flow 能解决就别把项目重新理解成传统 MCP-first。
