# YIUIMCP

> 让 AI 用更接近 CLI 的方式驱动 Unity，而不是停留在传统的一下一下 MCP tool call。

![YIUIMCP Window](./Image/YIUIMCP-Window.png)

YIUIMCP 是一个给 Unity 用的本地 AI 编排插件。  
它把 Unity 内部的原子工具、HTTP/RPC 通道、UTO 编排层、以及可直接调用的 CLI flow 串起来，让 AI 能更稳定地完成：

- 编译 Unity
- 获取编译结果
- 读取控制台日志
- 调用 Unity 菜单命令
- 在 Domain Reload 之后继续完成流程

这次开源的是 **YIUIMCP 的基础能力**。  
核心目标很明确：让 AI 真正能在 Unity 里“做事”，而不只是发几个零散指令。

## 亮点

### 1. CLI-first，而不是传统 MCP-first

YIUIMCP 不是只提供一堆原子工具，然后让 AI 自己慢慢拼流程。

它更偏向这种思路：

- 底层有 Unity 原子工具
- 中间有 UTO 负责编排、心跳检测、恢复等待
- 最上层提供 `Config/*.ps1` 这样的高聚合 CLI flow

也就是说，AI 更适合直接执行：

- 一条编译命令
- 一条获取日志命令
- 一条调用工具命令

而不是手工逐步串联每个 MCP call。

### 2. 解决 Unity Domain Reload 带来的连续执行问题

Unity 在编译时会触发 Domain Reload。  
这也是很多 AI 接入 Unity 时最容易断掉的一环。

YIUIMCP 的基础设计就是围绕这个问题展开：

- Unity 侧提供本地 HTTP JSON-RPC 服务
- UTO 负责心跳检测与恢复等待
- CLI flow 负责把多个步骤打包成一次完整执行

这样 AI 才能更接近“执行一个任务并拿到最终结果”，而不是中途掉线后流程中断。

### 3. 直接面向 Unity 工程使用

这是一个 **给 Unity 工程直接使用** 的包。  
不需要复杂安装器，直接把文件夹拷贝到项目的 `Packages` 目录下即可。

### 4. 这是基础层，不是上限

这次开源出来的不是“完整成品的全部功能”，而是 **YIUIMCP 最核心、最值得复用的底层框架能力**。

你可以把它理解成：

- 它先提供一个稳定的 Unity AI 执行底座
- 先把编译、日志、工具调用、恢复等待这些关键环节打通
- 再根据你自己的项目，持续往上叠加更贴业务的 CLI flow

也就是说，真正强大的地方不只是现在这几个脚本，而是：

- 你的项目可以基于这个框架继续扩展自己的 CLI
- 越扩越贴近项目自己的工作流
- 越扩，AI 在你的项目里就会越强

YIUIMCP 更像一个可以持续长大的底层能力框架，而不是只提供固定几个命令的工具包。

## 适用场景

如果你想让 AI 在 Unity 项目里更稳定地做这些事情，这个项目就适合你：

- 自动编译并拿到结果
- 拉取 Unity Console 信息
- 通过脚本统一调用工具
- 给 AI 提供一个稳定的 Unity CLI 层
- 为后续更复杂的 AI + Unity 自动化打基础
- 作为你自己项目定制 CLI 能力的底层框架

## 快速开始

### 1. 拷贝到 Unity 项目的 `Packages` 目录

把这个仓库里的 `cn.etetet.yiuimcp` 文件夹直接拷贝到你的 Unity 项目：

```text
YourUnityProject/
  Packages/
    cn.etetet.yiuimcp/
```

### 2. 打开 Unity 项目

Unity 启动后，YIUIMCP 会在编辑器侧初始化对应服务。

### 3. 安装 UTO 依赖

进入：

```text
Packages/cn.etetet.yiuimcp/UTO
```

执行：

```bash
npm install
npm run build
```

### 4. Odin 依赖说明

YIUIMCP 当前的可视化窗口基于 Odin Inspector 实现。

也就是说：

- 如果你希望直接使用仓库里现成的可视化窗口界面，项目里需要有 Odin 插件
- 这个依赖主要对应窗口与可视化交互部分
- YIUIMCP 的底层设计重点仍然是 CLI flow、编排能力与可扩展框架思路

### 5. 使用内置 CLI flow

例如编译：

```powershell
powershell -ExecutionPolicy Bypass -Command "& '.\Packages\cn.etetet.yiuimcp\Config\compile-unity-flow.ps1' -Force 0 -NoWait 1"
```

更多脚本说明见：

- [cn.etetet.yiuimcp/Config/README.md](./cn.etetet.yiuimcp/Config/README.md)

## 仓库内容

### `cn.etetet.yiuimcp/Editor`

Unity Editor 侧实现，包括：

- 本地 HTTP 服务
- 主线程调度
- 工具注册
- 原子工具实现

### `cn.etetet.yiuimcp/UTO`

Node.js 编排层，包括：

- HTTP 包装层
- 心跳检测
- Domain Reload 恢复等待
- 批量工具调用

### `cn.etetet.yiuimcp/Config`

面向 AI / 脚本的 CLI flow 入口，包括：

- `compile-unity-flow.ps1`
- `get_console_log.ps1`
- `get_console_error.ps1`
- `invoke-uto-tool.ps1`

### `cn.etetet.yiuimcp/skills`

包内附带的 Codex skill，用来让 AI 以 CLI-first 的方式理解和驱动这个包：

- [cn.etetet.yiuimcp/skills/yiuimcp/SKILL.md](./cn.etetet.yiuimcp/skills/yiuimcp/SKILL.md)

这个 skill 不只是给当前仓库看的，也可以直接放到你自己的项目里继续使用。

常见做法：

- 跟着 `cn.etetet.yiuimcp` 一起放进你自己的 Unity 项目
- 或者单独拷贝到你自己的 Codex skills 目录中使用

它的目标就是让你在自己的项目里，也能延续这套 CLI-first 的 YIUIMCP 工作方式。

## 文档与讲解

- 视频讲解：[YIUIMCP PPT 讲解视频](https://www.bilibili.com/video/BV1sLQmBuEYF)
- 飞书文档：[YIUIMCP 飞书文档](https://my.feishu.cn/wiki/MgFKwCSujiePvokPw7rcz46ZnSb)

如果你想系统了解这个 MCP 的设计思路、分层结构、工作流、脚本入口和扩展方式，建议直接看包内 `Docs`：

- [cn.etetet.yiuimcp/Docs](./cn.etetet.yiuimcp/Docs)
- [cn.etetet.yiuimcp/Docs/README.md](./cn.etetet.yiuimcp/Docs/README.md)

这里面整理了比较完整的设计文档，包括：

- 架构与协作方式
- HTTP 接口说明
- PowerShell flow 说明
- Unity 原子工具扩展方式
- 异步编程约束

如果你只是想快速浏览包入口，也可以直接看：

- [cn.etetet.yiuimcp/README.md](./cn.etetet.yiuimcp/README.md)

## 为什么这次只开源基础部分

这次开源的是 **YIUIMCP 的基础骨架与基础能力**：

- Unity 侧服务
- UTO 编排层
- 基础 CLI flow
- 基础 skill

这样做的目的，是先把最核心、最通用、最容易落地的部分开放出来，让更多 Unity 项目可以直接上手体验。

更重要的是，这一层一旦接进你的项目，后面就可以继续围绕它扩展：

- 你自己的编译流
- 你自己的日志分析流
- 你自己的菜单调用流
- 你自己的资源处理流
- 你自己的业务级 CLI flow

最终你项目里的 AI 能力，不会停在“能连上 Unity”这一步，而是会随着你自己的 CLI 体系一起越来越强。

如果你希望看到更多能力、更完整生态、以及和 YIUI 更深的协作方向，可以继续关注上层项目：

- [YIUI 主仓库](https://github.com/LiShengYang-yiyi/YIUI)

YIUIMCP 的开源，也是希望能让更多人通过 AI 工作流认识并使用 YIUI。

## 推荐了解 ET 与 YIUI

如果你想理解这套东西在更大体系里的位置，建议一起看这两个项目：

- [ET 主仓库](https://github.com/egametang/ET)
- [YIUI 主仓库](https://github.com/LiShengYang-yiyi/YIUI)

关系大致是这样：

- ET 框架是更上层、更完整的主体框架
- YIUI 是基于 ET 体系持续发展的框架与生态
- YIUIMCP 是 ET 9.0+ 版本中的其中一个包

所以这次开源的 YIUIMCP，可以看成是整个 ET / YIUI 体系中的基础能力包之一。

如果你本来就在关注 Unity 工具链、自动化、AI 工作流，或者准备进一步接入完整生态，那推荐优先了解：

1. [ET 主仓库](https://github.com/egametang/ET)
2. [YIUI 主仓库](https://github.com/LiShengYang-yiyi/YIUI)

YIUIMCP 只是其中一个开始。  
更多扩展能力、更多实际业务场景、以及更完整的体系，会在 ET / YIUI 生态里一起展开。

## 当前说明

- 这是一个面向 Unity 的包，不是独立桌面应用
- 当前更推荐在 Windows + PowerShell 环境下使用内置 flow
- 基础能力已经可用，但仍在持续迭代优化中

## License

- 代码协议：[Apache-2.0](./LICENSE)
- 署名说明：[NOTICE](./NOTICE)
- 品牌与命名使用规则：[TRADEMARKS.md](./TRADEMARKS.md)

你可以自由使用、修改和分发代码。  
但 `YIUI`、`YIUIMCP` 相关名称与品牌标识不等于一并开放授权。

如果你发布修改版，请：

- 保留原始许可证与署名信息
- 明确说明你的版本基于 YIUIMCP
- 不要把修改版直接继续命名为官方 `YIUI` / `YIUIMCP`
