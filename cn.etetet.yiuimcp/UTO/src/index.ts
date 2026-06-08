import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
} from "@modelcontextprotocol/sdk/types.js";
import axios from "axios";
import * as fs from "fs";
import * as path from "path";
import * as http from "http";

// 端口默认 3212，为支持动态端口，从 UTO 目录下的 .port 读取
function getUnityPort(): number {
    try {
        const candidates = [
            path.resolve(__dirname, "../.port"),
            path.resolve(__dirname, "../../.port"),
        ];

        for (const p of candidates) {
            if (fs.existsSync(p)) {
                const content = fs.readFileSync(p, "utf-8").trim();
                const port = parseInt(content, 10);
                if (!isNaN(port)) {
                    return port;
                }
            }
        }
    } catch (e) {
        // Ignore
    }
    return 3212;
}

function getUnityUrl(): string {
    const port = getUnityPort();
    return `http://127.0.0.1:${port}`;
}

// 辅助函数：获取 InstanceId
async function getInstanceId(): Promise<string | null> {
    try {
        const agent = new http.Agent({ keepAlive: false, maxSockets: 1 });
        const url = getUnityUrl();
        const response = await axios.get(`${url}/health`, { 
            timeout: 3000,
            httpAgent: agent
        });
        agent.destroy();
        
        return response.data?.serverId || null;
    } catch (e) {
        return null;
    }
}

// 健康检查
async function healthCheck(): Promise<boolean> {
    // 为每次请求创建新的 Agent
    const agent = new http.Agent({
        keepAlive: false,
        maxSockets: 1
    });

    try {
        const url = getUnityUrl();
        const response = await axios.get(`${url}/health`, { 
            timeout: 3000,
            httpAgent: agent
        });
        
        // 立即销毁 Agent
        agent.destroy();
        
        return response.status === 200;
    } catch (e) {
        // 确保 Agent 被销毁
        agent.destroy();
        return false;
    }
}

// Unity RPC 调用
async function callUnityRpc(method: string, params: any = {}): Promise<any> {
    const url = getUnityUrl();
    const port = getUnityPort();

    // 为每次请求创建新的 Agent
    const agent = new http.Agent({
        keepAlive: false,
        maxSockets: 1
    });

    try {
        const response = await axios.post(`${url}/rpc`, {
            jsonrpc: "2.0",
            method: method,
            params: params,
            id: Date.now(),
        }, { 
            timeout: 30000,
            httpAgent: agent,
            headers: {
                'Connection': 'close'  // 强制关闭连接
            }
        });

        // 立即销毁 Agent
        agent.destroy();

        if (response.data.error) {
            throw new Error(response.data.error.message);
        }

        return response.data.result;
    } catch (error: any) {
        // 确保 Agent 被销毁
        agent.destroy();
        
        if (error.code === 'ECONNREFUSED') {
            throw new Error(`Unity 连接被拒绝 (端口: ${port}, Unity 是否运行?)`);
        }
        if (error.code === 'ETIMEDOUT' || error.code === 'ECONNABORTED') {
            throw new Error(`Unity 响应超时 (端口: ${port})`);
        }
        throw error;
    }
}

// 静态工具表：确保 Cursor/Codex 等客户端始终能看到完整列表，
// 即使 Unity 端 ListTools 运行时只返回部分工具。
// CallTool 仍然动态转发，所以列表和实际调用互相独立。
const STATIC_TOOLS: Array<{ name: string; description: string }> = [
    // Basic
    { name: "Log",                      description: "打印日志" },
    { name: "LogError",                 description: "打印错误日志" },
    { name: "EnterPlayMode",            description: "进入运行模式" },
    { name: "StopPlayMode",             description: "退出运行模式" },
    { name: "TriggerCompile",           description: "触发编译" },
    { name: "GetCompileResult",         description: "获取编译结果" },
    { name: "GetConsoleLog",            description: "获取控制台日志" },
    { name: "ExecuteMenu",              description: "执行Unity菜单命令" },
    { name: "AssertConsoleContains",    description: "断言控制台日志包含关键词" },
    // YIUI ops
    { name: "YIUIAddComponent",             description: "添加Unity组件到Prefab节点" },
    { name: "YIUIAddComponentBinding",      description: "通过YIUI API给Prefab添加Component绑定" },
    { name: "YIUIAddDataBindActive",        description: "添加UIDataBindActive组件控制节点显隐" },
    { name: "YIUIAddDataBindColor",         description: "添加UIDataBindColor组件控制Graphic颜色" },
    { name: "YIUIAddDataBindImage",         description: "添加UIDataBindImage组件控制Image sprite" },
    { name: "YIUIAddDataBinding",           description: "通过YIUI API给Prefab添加Data变量" },
    { name: "YIUIAddEventBinding",          description: "通过YIUI API给Prefab添加Event事件" },
    { name: "YIUIAddTextBindingToNode",     description: "在节点上添加文本绑定到YIUI Data" },
    { name: "YIUIBindClickEventToNode",     description: "在指定节点上添加UIEventBindClick并绑定事件" },
    { name: "YIUICaptureGameView",          description: "截取运行时Game视图为PNG(需Play)" },
    { name: "YIUICapturePrefab",            description: "渲染UI Prefab为PNG(布局自检)" },
    { name: "YIUICreateChildNode",          description: "在Prefab中创建子节点" },
    { name: "YIUICreateLoopScroll",         description: "在Prefab节点下创建YIUI LoopScroll滚动列表" },
    { name: "YIUICreateModule",             description: "通过YIUI自动化工具创建UI模块目录" },
    { name: "YIUICreatePanelSource",        description: "创建标准YIUI Panel源数据(含UIBlockBG/AllViewParent/AllPopupViewParent)" },
    { name: "YIUICreatePrefab",             description: "通过Unity/YIUI API创建Panel/View/Common Prefab" },
    { name: "YIUICreateView",               description: "在Panel源数据的AllViewParent下创建View" },
    { name: "YIUIDeleteChildNode",          description: "删除Prefab子节点" },
    { name: "YIUIDuplicateNode",            description: "复制Prefab节点" },
    { name: "YIUIExportCode",               description: "调用YIUI自动化工具为Prefab导出生成代码" },
    { name: "YIUIInspectPrefab",            description: "读取Prefab上的YIUI CDE绑定信息" },
    { name: "YIUIMoveNode",                 description: "移动Prefab节点到新父节点或调整同级顺序" },
    { name: "YIUIOpenAutoTool",             description: "打开YIUI自动化工具窗口" },
    { name: "YIUIRemoveComponent",          description: "从Prefab节点移除Unity组件" },
    { name: "YIUISetContentSizeFitter",     description: "设置Prefab节点的ContentSizeFitter" },
    { name: "YIUISetGraphic",               description: "设置节点的图形组件" },
    { name: "YIUISetLayerRecursive",        description: "设置Prefab节点Layer，可递归设置子节点" },
    { name: "YIUISetLayoutElement",         description: "设置Prefab节点的LayoutElement属性" },
    { name: "YIUISetLayoutGroup",           description: "设置Prefab节点的LayoutGroup" },
    { name: "YIUISetNodeActive",            description: "设置Prefab节点的激活状态" },
    { name: "YIUISetRectTransform",         description: "设置Prefab节点的RectTransform" },
    { name: "YIUISetText",                  description: "设置节点的Text组件(字号/对齐/颜色/溢出)" },
    { name: "YIUISimulateClick",            description: "Play模式下按名字模拟点击UI节点" },
    { name: "YIUISourceSplit",              description: "源数据拆分: 将Source面板拆分生成到Prefabs目录" },
    // Coplay style
    { name: "find_gameobjects",             description: "按名查找 GameObject（注意：只搜有限范围，勿依赖做运行态层级查询）" },
];

// MCP Server
const server = new Server(
  {
    name: "uto",
    version: "1.0.0",
  },
  {
    capabilities: {
      tools: {},
    },
  }
);

// ListTools - 静态表保底（始终显示完整工具列表），Unity 返回的 schema 来了就合并
server.setRequestHandler(ListToolsRequestSchema, async () => {
    // 尝试从 Unity 获取带 inputSchema 的完整定义
    let unityTools: any[] = [];
    try {
        const result = await callUnityRpc("ListTools", {});
        if (result && result.tools) {
            unityTools = result.tools;
        }
    } catch (_) {
        // Unity 不在线或不支持 ListTools，用静态表兜底
    }

    const unityMap = new Map<string, any>(unityTools.map((t: any) => [t.name, t]));

    // 静态表为主（保证完整），Unity schema 来了就覆盖 description/inputSchema
    const tools = STATIC_TOOLS.map(s => {
        const u = unityMap.get(s.name);
        return {
            name: s.name,
            description: u?.description ?? s.description,
            inputSchema: u?.inputSchema ?? { type: "object" },
        };
    });

    // Unity 多返回了静态表里没有的工具，也追加进来
    for (const u of unityTools) {
        if (!tools.find(t => t.name === u.name)) {
            tools.push(u);
        }
    }

    return { tools };
});

// CallTool - 直接转发到 Unity MCP（心跳检测在 http-server 层处理）
server.setRequestHandler(CallToolRequestSchema, async (request) => {
    const { name, arguments: args } = request.params;
    
    // 直接转发到 Unity MCP
    try {
        const result = await callUnityRpc(name, args || {});
        
        if (result && result.success) {
            return {
                content: [{ type: "text", text: result.message || "Success" }]
            };
        } else {
            return {
                content: [{ type: "text", text: result?.message || "Unknown error" }],
                isError: true
            };
        }
    } catch (error: any) {
        return {
            content: [{ type: "text", text: `RPC Failed: ${error.message}` }],
            isError: true
        };
    }
});

// Start server
async function main() {
    const transport = new StdioServerTransport();
    await server.connect(transport);
    console.error("UTO Server running on stdio (pure proxy mode)");
}

// 根据启动参数决定运行模式
if (require.main === module) {
    const args = process.argv.slice(2);
    
    const isStdioMode = process.env.MCP_STDIO_MODE === '1';
    
    if (args.includes('--http') && !isStdioMode) {
        // HTTP 模式（无需传递端口，自动从 .port 文件读取）
        import('./http-server.js').then(({ startHttpServer }) => {
            startHttpServer().catch((error: any) => {
                console.error("HTTP Server 启动失败:", error);
                process.exit(1);
            });
        });
    } else {
        // Stdio 模式
        main().catch((error) => {
            console.error("Fatal error:", error);
            process.exit(1);
        });
    }
}
