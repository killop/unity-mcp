# Unity MCP + ZLua

[简体中文](README-zh.md)

Unity MCP exposes Unity Editor, Play Mode, and Player automation through MCP. Lua execution uses [ZLua](https://github.com/focus-creative-games/zlua). The repository contains three independent Unity packages:

| Package folder | Package name | Purpose |
| --- | --- | --- |
| `Packages/unity-mcp` | `unity-mcp` | MCP protocol, endpoints, built-in tools, and storage |
| `Packages/unity-mcp-zlua-extension` | `unity-mcp-zlua-extension` | ZLua script host and Lua tool integration |
| `Packages/com.code-philosophy.zlua` | `com.code-philosophy.zlua` | Unmodified copy of the official ZLua package |

The core package calls the scripting extension through `IUnityMcpScriptHost`. The official ZLua copy is tracked at [revision `ad05c9a`](docs/zlua-upstream.md); it can be updated without mixing changes into the MCP packages.

## Install

Add all three Git dependencies to your Unity project's `Packages/manifest.json` (merge these entries with its existing dependencies):

```json
{
  "dependencies": {
    "unity-mcp": "https://github.com/killop/unity-mcp.git?path=/Packages/unity-mcp",
    "unity-mcp-zlua-extension": "https://github.com/killop/unity-mcp.git?path=/Packages/unity-mcp-zlua-extension",
    "com.code-philosophy.zlua": "https://github.com/killop/unity-mcp.git?path=/Packages/com.code-philosophy.zlua"
  }
}
```

Unity resolves each package from its own subfolder; all three entries are required. For a local checkout, use `file:<absolute-path-to-checkout>/Packages/<package-folder>` instead. Set the scripting define `ZLUA_LUA_5_5` for each build target you use. Player builds also need the official ZLua package's native runtime and code generation setup for that platform.

In Unity, open **Unity MCP > Settings** or **Unity MCP > Start Editor MCP**. The default Editor endpoint is `http://127.0.0.1:18990/mcp`. For a stdio MCP client, run the Node.js proxy described in the [agent setup guide](Packages/unity-mcp/setup-for-agent.md).

## Lua and project tools

- `editor.lua.eval` executes Lua in the Editor.
- `runtime.lua.eval` executes Lua in Play Mode or a Player. Use `targetId` or `httpUrl` to select a remote Player.
- `editor.scriptTools.list` and `runtime.scriptTools.list` show project Lua tools; their `*.reload` counterparts rescan manifests.
- Scene, window, UI, logging, profiler, and reflection tools are available from the core package. See the [protocol guide](docs/protocol.md) or call `tools/list` for the current endpoint.

For `editor.lua.eval`, an expression can be sent as `{"code":"1 + 2","mode":"expression"}`. Lua accesses C# through ZLua's `CSharp` table:

```lua
return CSharp['UnityEngine.CoreModule']['UnityEngine.Application'].unityVersion
```

Use `CSharp['AssemblyName']['Namespace.Type']` for other public types. Eval returns a JSON envelope with `kind` and `value` for JSON-compatible Lua values.

Project Lua tools live in `<UnityProject>/unity-mcp-extension/lua/editor` or `lua/runtime`. Place a `*.tool.json` manifest beside a `.lua` module that returns `{ execute = function(argsJson, contextJson) ... end }`; `execute` returns JSON text. **Unity MCP > Create Extension Demos** creates working examples. The stdio proxy is [unity-mcp-stdio-proxy.js](Packages/unity-mcp/Tools~/unity-mcp-stdio-proxy.js).

## Tests

On Unity 2022.3.60f1 EditMode, 42 migrated protocol/configuration/storage tests and 5 ZLua-to-C# interop tests passed. The interop tests cover static and instance calls, construction, fields, properties, a Unity `GameObject`, and missing-member errors. Six original tests tied to removed PuerTS binaries, generated files, or installation scripts were retired. These results cover the Editor's Mono runtime; Player IL2CPP, overloads, generics, `ref/out`, and events have not been verified by this suite.

To run either test assembly from a local checkout and Unity project:

```sh
node Tools/run-editor-tests.mjs path/to/UnityProject
node Tools/run-editor-tests.mjs path/to/UnityProject --package=unity-mcp-zlua-extension --assembly=UnityMcp.ZLua.Editor.Tests
```

The Node.js runner enables Unity Test Framework and the selected package's tests in the project manifest, then writes NUnit XML under `<UnityProject>/TestResults`.
Pass `--unity=path/to/Unity` or set `UNITY_EDITOR_PATH` if Unity is installed elsewhere.

## Update the official ZLua copy

Update a clean checkout of [focus-creative-games/zlua](https://github.com/focus-creative-games/zlua), then run:

```sh
node Tools/update-zlua.mjs --source path/to/zlua-checkout
```

Use `--check` to compare without copying. The script changes only `Packages/com.code-philosophy.zlua` and [the upstream revision record](docs/zlua-upstream.md).

## Source and licenses

The MCP implementation was adapted from [killop/puerts-unity-mcp](https://github.com/killop/puerts-unity-mcp) revision `6372926e61d7b6db2eae1c0a92d92acd15b860a0`. The original PuerTS source snapshot is retained locally for migration reference. Both upstream projects use MIT licenses; the repository and official ZLua package retain their license files.
