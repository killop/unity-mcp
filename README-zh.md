# Unity MCP + ZLua

[English](README.md)

本仓库通过 MCP 提供 Unity Editor、Play Mode 和 Player 自动化，并使用 [ZLua](https://github.com/focus-creative-games/zlua) 执行 Lua。根目录 `Packages` 下有三个独立的 Unity package：

| 目录 | 包名 | 用途 |
| --- | --- | --- |
| `Packages/unity-mcp` | `unity-mcp` | MCP 协议、端点、内置工具和存储 |
| `Packages/unity-mcp-zlua-extension` | `unity-mcp-zlua-extension` | ZLua 脚本宿主和 Lua 工具集成 |
| `Packages/com.code-philosophy.zlua` | `com.code-philosophy.zlua` | 原样复制的官方 ZLua 包 |

核心包通过 `IUnityMcpScriptHost` 接口调用脚本扩展。官方 ZLua 副本对应提交 `ad05c9af02e065a6cd157f4ad6e9b90decdac481`，详见[上游版本记录](docs/zlua-upstream.md)。更新 ZLua 时无需混入另外两个包的代码。

## 安装

将以下三个 Git 依赖合并到 Unity 工程的 `Packages/manifest.json`，保留工程原有的依赖项：

```json
{
  "dependencies": {
    "unity-mcp": "https://github.com/killop/unity-mcp.git?path=/Packages/unity-mcp",
    "unity-mcp-zlua-extension": "https://github.com/killop/unity-mcp.git?path=/Packages/unity-mcp-zlua-extension",
    "com.code-philosophy.zlua": "https://github.com/killop/unity-mcp.git?path=/Packages/com.code-philosophy.zlua"
  }
}
```

三个包位于同一仓库的不同目录，工程需要分别声明。使用本地检出时，可把依赖值改为 `file:<仓库绝对路径>/Packages/<包目录>`。为使用的构建目标设置脚本宏 `ZLUA_LUA_5_5`。构建 Player 还需按官方 ZLua 包的流程准备目标平台的原生运行时和代码生成。

在 Unity 菜单打开 **Unity MCP > Settings**，或点击 **Unity MCP > Start Editor MCP**。Editor 默认地址是 `http://127.0.0.1:18990/mcp`。通过 stdio 连接 MCP 客户端时，按[代理配置指南](Packages/unity-mcp/setup-for-agent.md)运行 Node.js 代理。

## Lua 与项目工具

- `editor.lua.eval`：在 Editor 执行 Lua。
- `runtime.lua.eval`：在 Play Mode 或 Player 执行 Lua；远程 Player 可用 `targetId` 或 `httpUrl` 指定。
- `editor.scriptTools.list` 和 `runtime.scriptTools.list`：列出项目 Lua 工具；对应的 `*.reload` 工具重新扫描清单。
- 核心包还提供场景、窗口、UI、日志、Profiler 和反射工具。完整工具列表请调用 `tools/list`，协议说明见[文档](docs/protocol.md)。

向 `editor.lua.eval` 传入 `{"code":"1 + 2","mode":"expression"}` 可计算表达式。Lua 通过 ZLua 的 `CSharp` 表访问 C#：

```lua
return CSharp['UnityEngine.CoreModule']['UnityEngine.Application'].unityVersion
```

其他公开类型使用 `CSharp['程序集名']['命名空间.类型名']`。动态执行会把可转为 JSON 的返回值包装为含 `kind` 和 `value` 的结果。

项目 Lua 工具放在 `<UnityProject>/unity-mcp-extension/lua/editor` 或 `lua/runtime`。每个工具把 `*.tool.json` 清单与 `.lua` 模块放在一起；模块返回 `{ execute = function(argsJson, contextJson) ... end }`，`execute` 返回 JSON 文本。Unity 菜单 **Unity MCP > Create Extension Demos** 可生成示例。stdio 代理位于 [unity-mcp-stdio-proxy.js](Packages/unity-mcp/Tools~/unity-mcp-stdio-proxy.js)。

## 测试

在 Unity 2022.3.60f1 的 EditMode 中，迁移后的 42 个协议、配置和存储测试，以及 5 个 Lua 调用 C# 的测试均通过。互操作测试覆盖静态与实例调用、构造函数、字段、属性、Unity `GameObject` 和缺失成员报错。原测试中另有 6 个仅针对已移除的 PuerTS 原生库、生成文件或安装脚本，已经退役。这些结果针对 Editor 的 Mono 运行时；Player IL2CPP、重载、泛型、`ref/out` 和事件尚未由这组测试验证。

在本地检出和 Unity 工程中运行两个测试程序集：

```sh
node Tools/run-editor-tests.mjs path/to/UnityProject
node Tools/run-editor-tests.mjs path/to/UnityProject --package=unity-mcp-zlua-extension --assembly=UnityMcp.ZLua.Editor.Tests
```

Node.js 脚本会在工程清单中启用 Unity Test Framework 和指定包的测试，并在 `<UnityProject>/TestResults` 写入 NUnit XML。
若 Unity 不在脚本的默认路径，可传入 `--unity=path/to/Unity` 或设置 `UNITY_EDITOR_PATH`。

## 更新官方 ZLua 包

先更新一个干净的 [focus-creative-games/zlua](https://github.com/focus-creative-games/zlua) 检出，再运行：

```sh
node Tools/update-zlua.mjs --source path/to/zlua-checkout
```

可加 `--check` 只比较文件。脚本仅更新 `Packages/com.code-philosophy.zlua` 和[上游版本记录](docs/zlua-upstream.md)。

## 来源与许可证

MCP 实现改造自 [killop/puerts-unity-mcp](https://github.com/killop/puerts-unity-mcp) 提交 `6372926e61d7b6db2eae1c0a92d92acd15b860a0`。原 PuerTS 源码快照保留在本地供迁移对照。两个上游项目均使用 MIT 许可证；本仓库和官方 ZLua 包保留各自的许可证文件。
