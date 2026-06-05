# 从 Coplay 搬运工具到 YIUIMCP

## 背景与目标

YIUIMCP 的稳定性来自「无状态 HttpListener（默认 `:3212`）+ 自带 `YIUIMCPDispatcher`（挂 `EditorApplication.update` 把调用抽水到主线程）」，不依赖 .NET `SynchronizationContext`，因此**编译 / 进出 Play / domain reload 后都能自动恢复**（不需要改 ET 的 `MainThreadScheduler`）。

它的短板是**内置工具少**。本方案把 **CoplayDev/unity-mcp（MIT）** 丰富的编辑器工具**搬运**过来，得到「稳定底座 + 工具齐全」。选 Coplay 而非 ivanmurzak：MIT 最宽松；工具是 `JObject 进 / object 出` 的纯逻辑、用 Newtonsoft（与本包同栈）、零传输耦合；ivanmurzak 强耦合重型反射库 ReflectorNet。

## 设计：Coplay 工具 = 第二种工具类型

不改写 Coplay 工具，**保留其原命名空间** `MCPForUnity.Editor.*`，使工具/助手文件能近乎原样落地（便于日后跟上游更新）。新增一条与原生平行的发现 + 调用通路，并在 RPC 入口分支。

### 两种工具对比

| | 原生 YIUIMCP 工具 | 搬运自 Coplay 的工具 |
|---|---|---|
| 标记 | `[YIUIMCPTools("name","desc")]` | `[McpForUnityTool("name")]` |
| 形态 | 继承 `YIUIMCPBaseExecutor<T>`，`Run(T):Task<YIUIMCPResult>` | `static object HandleCommand(JObject)` |
| 返回 | `YIUIMCPResult { success, message }` | `SuccessResponse{success,message,data}` / `ErrorResponse` |
| 注册表 | `YIUIMCPToolsRegistry` | `YIUIMCPCoplayRegistry` |

### RPC 分发优先级（`YIUIMCPServer.HandleRpc`）

1. 命中原生工具 → 原生 `Invoke`（行为不变）
2. 否则命中 Coplay 工具 → `YIUIMCPCoplayRegistry.Invoke(method, paramsJObject)`，返回对象直接作为 JSON-RPC `result`（保留 `data` 结构化数据）
3. 都未命中 → 回到原生 `Invoke` 产出统一的「没有这个MCP工具」错误

两条路径都经 `YIUIMCPDispatcher.Dispatch` 在主线程执行；同步返回对象或 `Task<object>` 都被 Dispatcher 统一处理。**未修改 `YIUIMCPResult`，原生工具不受影响。**

## 目录结构（镜像 Coplay 分类）

```
cn.etetet.yiuimcp/Editor/UnityMCP/
├── Core/
│   ├── YIUIMCPServer.cs            (改: HandleRpc 加 Coplay 分支)
│   ├── YIUIMCPServerHelper.cs      (改: 静态构造加 YIUIMCPCoplayRegistry.Initialize())
│   ├── YIUIMCPToolsRegistry.cs     (改: 增加 Has())
│   └── YIUIMCPCoplayRegistry.cs    (新: Coplay 工具发现/调用，原创代码)
└── Coplay/                         (搬运自 CoplayDev/unity-mcp, MIT)
    ├── Tools/
    │   ├── McpForUnityToolAttribute.cs
    │   └── FindGameObjects.cs
    ├── Helpers/
    │   ├── Response.cs / ToolParams.cs / ParamCoercion.cs
    │   ├── StringCaseUtility.cs / Pagination.cs
    │   ├── McpLog.cs               (改: 去掉 Constants.EditorPrefKeys 依赖)
    │   ├── UnityTypeResolver.cs / GameObjectLookup.cs
    └── Runtime/Helpers/
        ├── UnityObjectIdCompat.cs
        └── UnityAssembliesCompat.cs
```

## 如何再搬一个 Coplay 工具

1. 从 `CoplayDev/unity-mcp` 的 `MCPForUnity/Editor/Tools/...` 复制工具 `.cs` 到 `Coplay/Tools/`（保留 `MCPForUnity.Editor.Tools` 命名空间 + 加 MIT 归属头）。
2. 把它依赖的 `Helpers/` / `Runtime/Helpers/` 文件一并补到对应子目录（已存在的不重复）。
3. **不要搬**带 `InitializeOnLoad` 或副作用静态构造的文件（如 `McpLogRecord`、`ProjectIdentityUtility`、`ExecuteCode`、`UnityReflect`），以及 Coplay 自己的 `CommandRegistry`/服务器/传输层。
4. 触发编译，无错即自动被 `YIUIMCPCoplayRegistry` 发现（按 `[McpForUnityTool]` 的名字，或类名 snake_case）。

> 注意：在 ET 项目里，操作**裸 Unity GameObject/Component** 的工具未必有意义；优先搬「框架无关」的（场景查询、Transform、资源、相机、动画等），ET/YIUI 专属操作建议另写原生工具。

## 验证

1. 编译干净：`Config/compile-unity-flow.ps1` 或 `get_console_error.ps1` 无报错。
2. 直接打 `/rpc`（默认 `http://127.0.0.1:3212/rpc`，JSON-RPC `{jsonrpc,method,params,id}`）：

```powershell
$body = '{"jsonrpc":"2.0","id":"1","method":"find_gameobjects","params":{"searchTerm":"Main Camera","searchMethod":"by_name"}}'
Invoke-WebRequest -Uri "http://127.0.0.1:3212/rpc" -Method Post -Body $body -ContentType "application/json" -UseBasicParsing
```

3. 鲁棒性：`EnterPlayMode` → Play 期间再调 `find_gameobjects`（应成功）→ `StopPlayMode` → 再调（自动恢复）。全程不需要 patch ET 的 `MainThreadScheduler`。
