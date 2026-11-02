<h1 align="center">Unity MCP + ZLua</h1>
<p align="center">
  <strong>通过 MCP 控制 Unity Editor、Play Mode 和真实手机游戏，并动态执行 Lua。</strong>
  <br />
  <em>Android · iOS · Editor Lua · Runtime Lua · UI 自动化 · 截图 · Profiler 报告 · Domain Reload 恢复</em>
</p>

<p align="center">
  <a href="#快速开始"><img src="https://img.shields.io/badge/Quick_Start-4CAF50?style=for-the-badge" alt="快速开始" /></a>
  <a href="#agent-zlua-速查"><img src="https://img.shields.io/badge/Agent_Lua_Guide-1976D2?style=for-the-badge" alt="Agent Lua 速查" /></a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Unity-2022.3_tested-black?style=flat&logo=unity&logoColor=white" alt="已在 Unity 2022.3 测试" />
  <a href="https://github.com/focus-creative-games/zlua"><img src="https://img.shields.io/badge/ZLua-1.1.0-blue?style=flat" alt="ZLua 1.1.0" /></a>
  <img src="https://img.shields.io/badge/MCP-JSON--RPC-6A5ACD?style=flat" alt="MCP JSON-RPC" />
</p>

<p align="center"><a href="README.md">English</a> · 中文</p>

---

Unity MCP 使用 [ZLua](https://github.com/focus-creative-games/zlua) 在 Unity Editor、Play Mode 和 Player 中执行 Lua。根目录 `Packages` 下有三个独立的 Unity package：

| 目录 | 包名 | 职责 |
|---|---|---|
| `Packages/unity-mcp` | `unity-mcp` | MCP 协议、Editor 和 Player 端点、内置 C# 工具、状态存储 |
| `Packages/unity-mcp-zlua-extension` | `unity-mcp-zlua-extension` | ZLua 宿主、Lua 动态执行、项目 Lua 模块执行 |
| `Packages/com.code-philosophy.zlua` | `com.code-philosophy.zlua` | 原样复制的官方 ZLua 源码和原生插件 |

## ZLua 官方项目

[focus-creative-games/zlua](https://github.com/focus-creative-games/zlua) 是 ZLua 官方仓库，提供 Unity Lua 运行时、原生插件、C# 互操作以及 Player 平台的构建流程。本仓库把官方包原样复制到 `Packages/com.code-philosophy.zlua`，对应[提交 `ad05c9af`](docs/zlua-upstream.md)。

`unity-mcp-zlua-extension` 把 MCP 脚本接口接到 ZLua 的 `LuaAppDomain`，`unity-mcp` 则通过 `IUnityMcpScriptHost` 调用扩展。Lua 使用 `CSharp['程序集名']['命名空间.类型名']` 访问公开 C# 类型。ZLua 本身的用法见[官方 README](https://github.com/focus-creative-games/zlua/blob/main/README.md) 和[示例工程](https://github.com/focus-creative-games/zlua-demo)；本仓库在它之上提供 MCP 端点与工具。

## 功能特性

| 能力 | 说明 |
|---|---|
| 手机直连调试 | Agent 可以通过 HTTP 直连 Android、iOS 或 standalone Unity Player，在运行中的游戏里执行 Lua。Player 构建需要完成下文的 ZLua 平台配置。 |
| Editor Lua 不触发常规 Domain Reload | `editor.lua.eval` 通过 ZLua 执行，不生成 C# 文件，也不调用 `AssetDatabase.Refresh`。 |
| Runtime Lua | `runtime.lua.eval` 可指向本地 Play Mode 或明确指定的远程 Player。 |
| 聚焦 Unity 窗口 | 在视觉自动化或采集前把 Unity Editor 窗口置前。 |
| Editor 和 Player 截图 | 截取 Game、Scene、Inspector、Console、Hierarchy 等 EditorWindow，也可截取 Player 画面。 |
| UI 测试自动化 | 用 snapshot、find、raycast 观察可见 UGUI 控件，再按 path、instance ID 或屏幕坐标点击。 |
| Profiler 报告 | 分析 Unity Editor Profiler 中 Editor 或已连接 Player 的帧数据，输出 marker 与 GC.Alloc 报告。 |
| 项目 MCP 工具 | 从项目扩展目录加载 Lua 工具，或通过 `IUnityMcpToolProvider` 注册 C# 工具。 |
| Domain Reload 恢复 | 持久化 operation 和编译结果，在 Unity domain reload 后恢复 Editor HTTP 端点。 |

## 它控制什么

每个可控制的 Unity 进程都有 MCP endpoint。Play Mode 使用 Unity Editor 进程中的 Runtime/Player endpoint，不是第三种 endpoint kind。

```text
Agent / MCP client
  |
  | stdio JSON-RPC
  v
Node stdio proxy
  |
  | HTTP JSON-RPC POST /mcp
  v
+----------------------+      本地 C# 路由         +-----------------------+
| Unity Editor MCP     | ------------------------> | Play Mode Runtime MCP |
| endpointKind=editor  |                           | endpointKind=player   |
| C# + ZLua            |                           | C# + ZLua             |
+----------------------+                           +-----------------------+
        |
        | 显式目标 URL
        v
+----------------------+
| Phone / Player MCP   |
| Android、iOS、构建包 |
| C# + ZLua            |
+----------------------+
```

## 快速开始

### 安装三个 package

将以下依赖合并到 Unity 工程的 `Packages/manifest.json`，保留原有依赖。三个包位于同一仓库的不同目录，工程需要分别声明：

```json
{
  "dependencies": {
    "unity-mcp": "https://github.com/killop/unity-mcp.git?path=/Packages/unity-mcp",
    "unity-mcp-zlua-extension": "https://github.com/killop/unity-mcp.git?path=/Packages/unity-mcp-zlua-extension",
    "com.code-philosophy.zlua": "https://github.com/killop/unity-mcp.git?path=/Packages/com.code-philosophy.zlua"
  }
}
```

使用本地检出时，可把依赖值改为 `file:<仓库绝对路径>/Packages/<包目录>`。为使用的构建目标设置脚本宏 `ZLUA_LUA_5_5`。三个包声明的最低 Unity 版本是 2021.3；下文测试结果来自 Unity 2022.3.60f1。

### 启动 Editor 端点

打开 **Unity MCP > Settings**，或执行 **Unity MCP > Start Editor MCP**。Editor 默认地址为 `http://127.0.0.1:18990/mcp`；`GET http://127.0.0.1:18990/health` 可检查状态。设置保存在 `<UnityProject>/unity-mcp-extension/editor-mcp-config.json`。

### 配置 Agent

如果 MCP 客户端需要 stdio 代理，另外检出本仓库，运行：

```text
node path/to/unity-mcp/Packages/unity-mcp/Tools~/unity-mcp-stdio-proxy.js --config path/to/UnityProject/unity-mcp-extension/editor-mcp-config.json
```

在 MCP 客户端中把 `node` 设为 command，把后续内容设为参数。代理会发现项目工具与 skills，并把 JSON-RPC 转发到所选 Unity 端点。脚本和扩展格式见 [Agent 配置指南](Packages/unity-mcp/setup-for-agent.md)。支持 HTTP MCP 的客户端也可直接连接 `/mcp`。

## 手机和 IL2CPP 构建

`runtimeAutoStart` 开启后，Runtime MCP host 会在 Play Mode 或 Player 中启动。构建手机包前，将配置保存到 `<UnityProject>/unity-mcp-extension/mobile-mcp-config.json`；Unity build hook 在构建期间复制到 `Assets/StreamingAssets/UnityMcp/mobile-mcp-config.json`，构建后恢复工程文件。最小配置示例：

```json
{
  "runtimeAutoStart": true,
  "runtimeBindAddress": "0.0.0.0",
  "runtimePort": 18991,
  "allowLuaEval": true
}
```

目标平台的原生运行时与 IL2CPP 代码生成按 [ZLua 官方仓库](https://github.com/focus-creative-games/zlua)的说明完成。当前仓库的 Editor 测试尚未验证 Android/iOS 的 IL2CPP 执行。

Player 端点提供 `GET /health` 和 `POST /mcp`。Agent 不打开 Unity Editor 也能直接连接手机：

```text
node path/to/unity-mcp/Packages/unity-mcp/Tools~/unity-mcp-stdio-proxy.js --config path/to/UnityProject/unity-mcp-extension/editor-mcp-config.json --target-kind player --target-url http://PHONE_IP:18991
```

远程目标必须显式提供 URL。也可以在 `editor-mcp-config.json` 里设置 `"selectedTargetKind":"player"` 和 `"selectedTargetUrl":"http://PHONE_IP:18991"`。如果手机端要运行项目 Lua 工具，需要把模块部署到该设备的 `Application.persistentDataPath/unity-mcp-extension/lua/runtime`；Unity 工程外部的扩展目录不会自动打进 Player 包。

## Editor Lua 不触发 Domain Reload

日常 Editor 自动化可用 `editor.lua.eval`。它通过 ZLua 执行 Lua，不创建或编译 C# 文件：

```json
{
  "name": "editor.lua.eval",
  "arguments": {
    "mode": "expression",
    "code": "CSharp['UnityEngine.CoreModule']['UnityEngine.Application'].isPlaying"
  }
}
```

`mode: "expression"` 传入一条 Lua 表达式，结果自动返回；`mode: "script"` 执行 Lua 代码块，需要显式 `return`。如果项目 C# 改动导致 domain reload，Editor MCP 会在 `.unity-mcp/ops` 保存 operation 状态、记录编译结果，并在程序集重载后恢复端点。

## MCP Tool 扩展

内置工具用 C# 编写。项目 Lua 与 C# 工具会出现在同一个 `tools/list` 中；项目扩展不会静默覆盖重名工具，建议用 `game.*` 或 `project.*` 前缀。

```text
<UnityProject>/unity-mcp-extension/
  lua/editor/             Editor Lua 工具
  lua/runtime/            Runtime / Player Lua 工具
  Packages/<package>/     可选的项目 C# provider 包
  skills/                 给 Agent 使用的项目说明
```

每个 Lua 工具在模块旁放一个 manifest：

```json
{
  "name": "game.echo",
  "description": "Return the request arguments.",
  "modulePath": "echo.lua",
  "functionName": "execute",
  "inputSchemaJson": "{\"type\":\"object\",\"additionalProperties\":true}"
}
```

```lua
-- echo.lua
return {
    execute = function(argsJson, contextJson)
        return argsJson or "{}"
    end
}
```

`editor.scriptTools.reload` 和 `runtime.scriptTools.reload` 会重新扫描对应目录。**Unity MCP > Create Extension Demos** 可以复制 Editor 与 Runtime Lua 示例，不覆盖已有文件。项目规则放在 `unity-mcp-extension/skills/*.md`，Agent 可通过 `agent.extension.skills.list`、`editor.skills.list` 或 `runtime.skills.list` 发现并加载。

项目 C# 工具建议放在独立 package，引用 `UnityMcp.Runtime` 和项目所需程序集。实现 `IUnityMcpToolProvider` 后，端点启动时会自动注册：

```csharp
using System.Threading.Tasks;
using UnityMcp;

public sealed class ProjectMcpTools : IUnityMcpToolProvider
{
    public string EndpointKind => "runtime"; // editor、runtime/player 或 all

    public void RegisterTools(UnityMcpToolProviderContext context)
    {
        context.TryRegister(new DelegateUnityMcpTool(
            "game.status", "Return project runtime state.", JsonSchemas.Object(),
            (ctx, args) => Task.FromResult("{\"ok\":true}")));
    }
}
```

## 内置 MCP Tools

`tools/list` 返回当前端点实际可用的工具。下表是 package 提供的工具；项目工具会另外加载。

### Editor MCP

| Tool | 用途 |
|---|---|
| `mcp.info` | 返回 Editor 端点元数据、健康状态和能力。 |
| `editor.state` | 返回 Unity Editor 状态。 |
| `editor.buildSettings.startupScene` | 返回 Build Settings 中第一个启用场景。 |
| `editor.lua.eval` | 在 Editor ZLua 宿主中执行 Lua。 |
| `editor.hierarchy.get` / `get-hierarchy` | 导出场景或 Play Mode hierarchy JSON 到 `.unity-mcp/hierarchy-results`。 |
| `editor.window.focus` / `focus-window` | 将 Unity Editor 窗口置前。 |
| `editor.window.screenshot` / `screenshot` | 截取 EditorWindow PNG 到 `.unity-mcp/editor-window-screenshots`。 |
| `editor.profiler.targets.list` | 列出 Editor Profiler 可见的目标，包括可用的已连接 Player。 |
| `editor.profiler.connect` | 选择 Editor 或已连接 Player 的 Profiler 目标。 |
| `editor.profiler.capture` | 录制并分析 Profiler 原始帧，输出 JSON、CSV 和 Markdown。 |
| `editor.profiler.analyze` | 分析已有的 Profiler 帧。 |
| `performance.hotspot.report` / `perf.hotspot.report` | 在 `.unity-mcp/perf-reports` 生成性能热点报告。 |
| `editor.scriptTools.list` / `editor.scriptTools.reload` | 列出或重新加载 Editor Lua 工具。 |
| `editor.skills.list` / `editor.skill.load` | 列出或加载项目说明。 |
| `editor.playmode.set` / `editor.playmode.set.immediate` | 延迟或立即切换 Play Mode。 |
| `editor.playmode.state` | 返回 Play Mode 状态。 |
| `editor.targets.list` / `runtime.targets.list` / `targets.list` | 列出本地和配置中的直连目标。 |
| `runtime.lua.eval` | 将 Lua 转发到本地 Play Mode 或显式指定的远程 Player。 |
| `runtime.tool.call` | 通过 Editor 调用 Runtime MCP 工具。 |
| `editor.compile` | 刷新资源并记录编译结果，供 reload 恢复使用。 |
| `op.status` | 读取持久化的 operation 状态或结果。 |

### Runtime / Player MCP

同一套 Runtime endpoint 用于 Editor Play Mode、Android、iOS 和 standalone Player。

| Tool | 用途 |
|---|---|
| `mcp.info` / `runtime.status` | 返回 Runtime 端点健康状态和运行状态。 |
| `runtime.targets.list` / `targets.list` | 列出当前 Player 端点。 |
| `runtime.lua.eval` | 在 Runtime ZLua 宿主中执行 Lua。 |
| `runtime.reflection.invoke` | 通过 reflection gateway 调用静态 C# 方法。 |
| `runtime.scriptTools.list` / `runtime.scriptTools.reload` | 列出或重新加载 Runtime Lua 工具。 |
| `runtime.skills.list` / `runtime.skill.load` | 列出或加载项目说明。 |
| `op.status` | 读取持久化的 operation 状态或结果。 |
| `runtime.logs` / `runtime.logs.clear` | 读取或清空 Runtime 日志缓冲区。 |
| `screen.screenshot` | 截取 Player 画面；手机默认使用内存 PNG。 |
| `runtime.ui.snapshot` | 返回可见 UGUI canvas 和控件快照。 |
| `runtime.ui.find` | 按 text、name、path 或 canvas 查找 UGUI 控件。 |
| `runtime.ui.raycast` | 对屏幕点或目标控件执行 UI raycast。 |
| `runtime.ui.click` / `input.tap` | 按坐标、path 或 instance ID 点击。 |

## Agent ZLua 速查

这一节供 Agent 生成 `editor.lua.eval`、`runtime.lua.eval` 或项目 Lua 工具代码时参考。

### 选择目标

| 任务 | Tool | 可用 API |
|---|---|---|
| Unity Editor 自动化 | `editor.lua.eval` | UnityEditor 和运行时 API |
| 本地 Play Mode 自动化 | `runtime.lua.eval` 指向本地目标 | Runtime API |
| Android、iOS、standalone 自动化 | `runtime.lua.eval` 指定 `targetId` 或 `httpUrl` | Player Runtime API |

Editor 和 Runtime 是两个不同的 MCP 宿主。在 Editor 进程中，ZLua 使用共享的 `LuaAppDomain`；项目如果已自行初始化它，需要与本扩展协调模块加载器。Player 代码应只使用运行时安全的 API。

### 基础 ZLua 写法

用 `CSharp['程序集名']['命名空间.类型名']` 访问公开 C# 类型。`Type.Method(...)` 调用静态方法，`Type()` 构造对象，`object:Method(...)` 调用实例方法。

```lua
local Application = CSharp['UnityEngine.CoreModule']['UnityEngine.Application']
return { productName = Application.productName, version = Application.version }
```

`mode: "expression"` 会自动返回表达式的值：

```lua
CSharp['UnityEngine.CoreModule']['UnityEngine.Application'].unityVersion
```

`mode: "script"` 需要显式 `return`。返回简单值，不要直接返回 Unity 对象：

```lua
local GameObject = CSharp['UnityEngine.CoreModule']['UnityEngine.GameObject']
local go = GameObject.Find('Canvas')
return { found = go ~= nil, name = go and go.name or '' }
```

在 Runtime 端点上，`runtime.reflection.invoke` 可通过 `typeName`、`methodName`、`argsJson` 调用静态 C# 方法。IL2CPP 下，通过反射或 Lua 访问的类型及成员需要避免被裁剪；请完成官方 ZLua 代码生成流程，并根据需要增加保留规则或项目包装类。

### 手机 UI 自动化模式

先用 `screen.screenshot` 观察画面，再通过 `runtime.ui.snapshot`、`runtime.ui.find`、`runtime.ui.raycast` 定位控件，最后用 `runtime.ui.click` 或 `input.tap` 点击。反复使用的游戏流程可写入 `unity-mcp-extension/lua/runtime`。

### Profiler 性能热点流程

先调用 `editor.profiler.targets.list`。分析手机或 Player 时，先在 Unity Profiler 中连接目标，或用 `editor.profiler.connect` 指定名称、ID、URL。随后调用 `editor.profiler.capture` 或 `performance.hotspot.report`，例如设置 `duration: "15s"`。`.unity-mcp/perf-reports` 下的报告包含帧摘要、marker、self time 和 GC.Alloc 证据。

### 返回值规则

动态执行返回含 `kind`、`value` 的 JSON 包装。字符串、数字、布尔值和 Lua 表可以编码，表会变成 JSON 对象或数组；循环引用和过深数据会表示为字符串。项目工具模块的 `execute(argsJson, contextJson)` 则直接返回 JSON 文本。

## 协议表面

| Endpoint | 用途 |
|---|---|
| `GET /health` | 端点元数据与健康状态 |
| `GET /api/ping` | 轻量健康检查别名 |
| `POST /mcp` | JSON-RPC MCP 调用 |

主要 MCP 方法是 `initialize`、`ping`、`tools/list`、`tools/call`。例如：

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "editor.lua.eval",
    "arguments": { "code": "1 + 2", "mode": "expression" }
  }
}
```

C# 序列化使用 Unity `JsonUtility`，MCP 包不依赖 Newtonsoft.Json。结果格式见[协议文档](docs/protocol.md)。

## 配置和状态目录

| 路径 | 用途 |
|---|---|
| `unity-mcp-extension/editor-mcp-config.json` | Editor 设置和显式目标选择 |
| `unity-mcp-extension/mobile-mcp-config.json` | 构建时复制的 Runtime / Player 设置 |
| `unity-mcp-extension/lua/editor` | 项目 Editor Lua 工具 |
| `unity-mcp-extension/lua/runtime` | 项目 Runtime Lua 工具 |
| `unity-mcp-extension/skills` | 项目 Agent 说明 |
| `.unity-mcp/editors/{editorId}/heartbeat.json` | Editor heartbeat |
| `.unity-mcp/players/{playerId}/heartbeat.json` | 可选的 Player heartbeat |
| `.unity-mcp/ops/{operationId}` | operation 状态和结果 |
| `.unity-mcp/temp/compile-results` | 编译结果提示 |
| `.unity-mcp/perf-reports` | Profiler 报告 |

Editor 默认端口是 `18990`，Player 默认端口是 `18991`。远程目标通过配置中的 URL、`--target-url` 或 `UNITY_MCP_TARGET_URL` 显式选择，不使用自动局域网发现。

## Unity 工程 `.gitignore`

在 Unity 工程中忽略运行状态：

```gitignore
.unity-mcp/
Assets/StreamingAssets/UnityMcp/mobile-mcp-config.json
```

当项目配置、Lua 工具和 skills 需要随项目发布时，应保留 `unity-mcp-extension`。三个 package 目录包含各自所需的 Unity `.meta` 文件和原生文件。

## 目录结构

```text
unity-mcp/
  Packages/
    unity-mcp/                 Editor / Runtime MCP、工具、测试、stdio 代理
    unity-mcp-zlua-extension/  ZLua 宿主和互操作测试
    com.code-philosophy.zlua/  官方 ZLua 包副本
  Tools/                      Node.js 测试脚本和 ZLua 更新脚本
  docs/                       协议和上游版本记录
```

## 测试与更新 ZLua

Unity 2022.3.60f1 EditMode 下，42 个协议、配置和存储测试以及 5 个 Lua 调用 C# 的测试全部通过。这组测试覆盖 Editor Mono；Player IL2CPP、重载、泛型、`ref/out` 和事件尚未验证。

```text
node Tools/run-editor-tests.mjs path/to/UnityProject
node Tools/run-editor-tests.mjs path/to/UnityProject --package=unity-mcp-zlua-extension --assembly=UnityMcp.ZLua.Editor.Tests
```

Node.js 脚本会在工程中启用 Unity Test Framework，在 `<UnityProject>/TestResults` 写入 NUnit XML。Unity 不在默认路径时，可传 `--unity=path/to/Unity` 或设置 `UNITY_EDITOR_PATH`。

从干净的官方 ZLua 检出更新独立包：

```text
node Tools/update-zlua.mjs --source path/to/zlua-checkout
```

加 `--check` 可只比较文件。更新脚本只修改 `Packages/com.code-philosophy.zlua` 和[上游版本记录](docs/zlua-upstream.md)。

## 开源协议

Unity MCP 使用 [MIT License](LICENSE)。[ZLua 官方包](https://github.com/focus-creative-games/zlua)保留其[上游 MIT 许可证](Packages/com.code-philosophy.zlua/LICENSE)。
