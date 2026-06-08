<!-- yiuimcp:start -->
# YIUI MCP — Unity UI 自动化

> 仅适用于包含 `Packages/cn.etetet.yiuimcp` 的 ET/YIUI 工程。
> MCP 工具 `yiuimcp` 已在 `.codex/config.toml` 注册，直接调用工具名即可。

健康检查：`GET http://127.0.0.1:3212/health` 返回 `{"status":"ok"}` 即服务在线。

## 关键踩坑（务必遵守）

1. **LoopScroll Item 必须有 `LayoutElement`**（`preferredWidth/Height`），否则列表全空但业务日志正常——极易误判为逻辑 bug。
2. **中文参数用 JSON `\uXXXX` 转义**，shell 层会破坏 UTF-8 字节。
3. **`TriggerCompile` / `YIUIExportCode` / `YIUISourceSplit` 触发域重载** → 本次请求超时或返回空，但 Unity 已完成工作。处理：超时后轮询 `/health` 直到 200，再 `GetCompileResult` 确认；serverId 变化即代表发生过重载。
4. **组件绑定走 `YIUIAddComponentBinding`**，不要直接写 `m_AllBindDic`（导出时会被清空重建）。
5. **Loop Item 的 codeType = `Common`**，不是 View。
6. **data 名传全名**（如 `u_DataIndex`），不要传 `index`。
7. **Play 模式下改 prefab 会在域重载后回滚** → 改 prefab 一律先 `StopPlayMode`。
8. **`GetConsoleLog` 的 `logType`** 用 `ErrorMask`/`WarningMask`/`LogMask`/`All`，不是字符串 `"Error"`。
9. **`find_gameobjects` 找不到嵌套 YIUI 节点**，运行态找节点用 `YIUISimulateClick` 间接验证。

## 标准三层列表流程

Panel → View（含 LoopScrollRect）→ Item（codeType=Common）：

1. `YIUICreatePanelSource` → `YIUICreateChildNode` → `YIUIAddEventBinding` → `YIUISourceSplit`
2. `YIUICreateView`（AllViewParent 下）→ `YIUICreateLoopScroll` + `YIUIAddComponentBinding`
3. `YIUICreatePrefab(codeType=Common)` → `YIUICreateChildNode` → `YIUIAddDataBinding(u_DataIndex,Int)` → `YIUIAddTextBindingToNode` → **`YIUISetLayoutElement(preferredWidth/Height=item尺寸)`** ← 必须！
4. `YIUIExportCode` × 3（Panel / View / Item）
5. 编译：`powershell -ExecutionPolicy Bypass -File "Packages\cn.etetet.yiuimcp\Config\compile-unity-flow.ps1"`

## 自检闭环

- **静态**：`YIUICapturePrefab{prefabPath,width,height}` → 读图 → 调整 → 再截（Panel 用 1920×1080）
- **运行态**：`EnterPlayMode` → `YIUISimulateClick{name}` → `YIUICaptureGameView{outputPath}`（等 ~1s 再读）

## 工具速查（43 个）

**基础**：`TriggerCompile` `GetCompileResult` `GetConsoleLog` `AssertConsoleContains` `EnterPlayMode` `StopPlayMode` `ExecuteMenu` `Log` `LogError`

**YIUI 操作**：`YIUICreateModule` `YIUICreatePanelSource` `YIUICreatePrefab` `YIUICreateView` `YIUICreateChildNode` `YIUICreateLoopScroll` `YIUISourceSplit` `YIUIExportCode` `YIUIInspectPrefab` `YIUIAddComponentBinding` `YIUIAddDataBinding` `YIUIAddEventBinding` `YIUIAddTextBindingToNode` `YIUIAddDataBindActive` `YIUIAddDataBindColor` `YIUIAddDataBindImage` `YIUIBindClickEventToNode` `YIUISetRectTransform` `YIUISetLayoutElement` `YIUISetLayoutGroup` `YIUISetContentSizeFitter` `YIUISetGraphic` `YIUISetText` `YIUISetNodeActive` `YIUISetLayerRecursive` `YIUIAddComponent` `YIUIRemoveComponent` `YIUIMoveNode` `YIUIDuplicateNode` `YIUIDeleteChildNode` `YIUIOpenAutoTool` `YIUICapturePrefab` `YIUICaptureGameView` `YIUISimulateClick`

**Coplay 风格**：`find_gameobjects`（有限范围，勿依赖做运行态层级查询）
<!-- yiuimcp:end -->
