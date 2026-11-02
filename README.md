<h1 align="center">Unity MCP + ZLua</h1>
<p align="center">
  <strong>Control Unity Editors, Play Mode, and real mobile games through MCP with dynamic Lua.</strong>
  <br />
  <em>Android · iOS · Editor Lua · Runtime Lua · UI automation · Screenshots · Profiler reports · Domain reload recovery</em>
</p>

<p align="center">
  <a href="#quick-start"><img src="https://img.shields.io/badge/Quick_Start-4CAF50?style=for-the-badge" alt="Quick Start" /></a>
  <a href="#agent-zlua-guide"><img src="https://img.shields.io/badge/Agent_Lua_Guide-1976D2?style=for-the-badge" alt="Agent Lua Guide" /></a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Unity-2022.3_tested-black?style=flat&logo=unity&logoColor=white" alt="Unity 2022.3 tested" />
  <a href="https://github.com/focus-creative-games/zlua"><img src="https://img.shields.io/badge/ZLua-1.1.0-blue?style=flat" alt="ZLua 1.1.0" /></a>
  <img src="https://img.shields.io/badge/MCP-JSON--RPC-6A5ACD?style=flat" alt="MCP JSON-RPC" />
</p>

<p align="center">English · <a href="README-zh.md">中文</a></p>

---

Unity MCP uses [ZLua](https://github.com/focus-creative-games/zlua) for Lua execution in Unity Editor, Play Mode, and Players. The root `Packages` directory contains three independent Unity packages:

| Directory | Package name | Responsibility |
|---|---|---|
| `Packages/unity-mcp` | `unity-mcp` | MCP protocol, Editor and Player endpoints, built-in C# tools, and storage |
| `Packages/unity-mcp-zlua-extension` | `unity-mcp-zlua-extension` | ZLua host, Lua eval, and project Lua module execution |
| `Packages/com.code-philosophy.zlua` | `com.code-philosophy.zlua` | Unmodified official ZLua source and native plugins |

## ZLua upstream

[focus-creative-games/zlua](https://github.com/focus-creative-games/zlua) is the official ZLua project. It provides the Unity Lua runtime, native plugins, C# interop, and the platform workflow for Player builds. Its package is copied into `Packages/com.code-philosophy.zlua` without source changes at [revision `ad05c9af`](docs/zlua-upstream.md).

`unity-mcp-zlua-extension` connects the MCP scripting interface to ZLua's `LuaAppDomain`. `unity-mcp` calls that extension through `IUnityMcpScriptHost`. Lua accesses public C# types through `CSharp['AssemblyName']['Namespace.Type']`; the [official ZLua README](https://github.com/focus-creative-games/zlua/blob/main/README_EN.md) and [demo](https://github.com/focus-creative-games/zlua-demo) cover ZLua itself. This repository adds MCP endpoints and tools around it.

## Features

| Feature | Description |
|---|---|
| Direct phone debugging | Connect an agent to an Android, iOS, or standalone Unity Player over HTTP and run Lua in the live game. Player builds require the ZLua platform setup described below. |
| Editor Lua without routine domain reload | `editor.lua.eval` runs through ZLua without generating C# files or calling `AssetDatabase.Refresh`. |
| Runtime Lua | `runtime.lua.eval` targets local Play Mode or an explicitly selected remote Player. |
| Unity window focus | Bring the Unity Editor window to the foreground before visual automation or capture. |
| Editor and Player screenshots | Capture Game, Scene, Inspector, Console, or Hierarchy EditorWindow tabs and capture Player screens. |
| UI testing automation | Inspect visible UGUI controls with snapshot, find, and raycast tools; click by path, instance ID, or screen coordinates. |
| Profiler reports | Analyze Unity Editor Profiler frames for the Editor or an attached Player and write marker and GC.Alloc reports. |
| Project MCP tools | Add Lua tools from a project extension directory or C# tools through `IUnityMcpToolProvider`. |
| Domain reload recovery | Persist operations and compile results, then restore the Editor HTTP endpoint after a Unity domain reload. |

## What It Controls

Each controllable Unity process has an MCP endpoint. Play Mode uses the Runtime/Player endpoint inside the Unity Editor process; it is not a third endpoint kind.

```text
Agent / MCP client
  |
  | stdio JSON-RPC
  v
Node stdio proxy
  |
  | HTTP JSON-RPC POST /mcp
  v
+----------------------+      local C# route       +-----------------------+
| Unity Editor MCP     | ------------------------> | Play Mode Runtime MCP |
| endpointKind=editor  |                           | endpointKind=player   |
| C# + ZLua            |                           | C# + ZLua             |
+----------------------+                           +-----------------------+
        |
        | explicit target URL
        v
+----------------------+
| Phone / Player MCP   |
| Android, iOS, build  |
| C# + ZLua            |
+----------------------+
```

## Quick Start

### Install the three packages

Merge these entries into your Unity project's `Packages/manifest.json`. All three are needed because Unity resolves each package from its own subfolder:

```json
{
  "dependencies": {
    "unity-mcp": "https://github.com/killop/unity-mcp.git?path=/Packages/unity-mcp",
    "unity-mcp-zlua-extension": "https://github.com/killop/unity-mcp.git?path=/Packages/unity-mcp-zlua-extension",
    "com.code-philosophy.zlua": "https://github.com/killop/unity-mcp.git?path=/Packages/com.code-philosophy.zlua"
  }
}
```

For a local checkout, replace each URL with `file:<absolute-checkout-path>/Packages/<package-directory>`. Set the scripting define `ZLUA_LUA_5_5` for each build target you use. The packages declare Unity 2021.3 as their minimum; the tests reported below ran on Unity 2022.3.60f1.

### Start the Editor endpoint

Open **Unity MCP > Settings**, or choose **Unity MCP > Start Editor MCP**. The default Editor URL is `http://127.0.0.1:18990/mcp`; `GET http://127.0.0.1:18990/health` reports health. Settings are stored in `<UnityProject>/unity-mcp-extension/editor-mcp-config.json`.

### Configure your agent

Clone this repository if your MCP client needs the Node.js stdio proxy. Run:

```text
node path/to/unity-mcp/Packages/unity-mcp/Tools~/unity-mcp-stdio-proxy.js --config path/to/UnityProject/unity-mcp-extension/editor-mcp-config.json
```

Use `node` as the MCP client's command and the remaining tokens as its arguments. The proxy discovers project tools and skills and forwards JSON-RPC to the selected Unity endpoint. See the [agent setup guide](Packages/unity-mcp/setup-for-agent.md) for the script and extension format. A client that supports HTTP MCP can connect to `/mcp` directly.

## Phone And IL2CPP Builds

The Runtime MCP host starts in Play Mode or a Player when `runtimeAutoStart` is enabled. For a phone build, save `<UnityProject>/unity-mcp-extension/mobile-mcp-config.json`; the Unity build hook copies it to `Assets/StreamingAssets/UnityMcp/mobile-mcp-config.json` during the build and restores the project file afterward. A minimal config is:

```json
{
  "runtimeAutoStart": true,
  "runtimeBindAddress": "0.0.0.0",
  "runtimePort": 18991,
  "allowLuaEval": true
}
```

Follow the [official ZLua package](https://github.com/focus-creative-games/zlua) for native runtime and IL2CPP code generation on the chosen target. Android/iOS IL2CPP execution has not yet been verified by this repository's Editor test suite.

The Player endpoint exposes `GET /health` and `POST /mcp`. An agent can connect without opening the Unity Editor:

```text
node path/to/unity-mcp/Packages/unity-mcp/Tools~/unity-mcp-stdio-proxy.js --config path/to/UnityProject/unity-mcp-extension/editor-mcp-config.json --target-kind player --target-url http://PHONE_IP:18991
```

Remote targets require an explicit URL. You can also put `"selectedTargetKind":"player"` and `"selectedTargetUrl":"http://PHONE_IP:18991"` in `editor-mcp-config.json`. The phone's project Lua tools, if used, must be deployed under its `Application.persistentDataPath/unity-mcp-extension/lua/runtime`; the external Unity project extension directory is not automatically included in a Player build.

## Editor Lua Without Domain Reload

Use `editor.lua.eval` for normal Editor automation. It runs Lua in ZLua and does not create or compile C# scripts:

```json
{
  "name": "editor.lua.eval",
  "arguments": {
    "mode": "expression",
    "code": "CSharp['UnityEngine.CoreModule']['UnityEngine.Application'].isPlaying"
  }
}
```

For `mode: "expression"`, provide one Lua expression; `mode: "script"` executes a Lua chunk and requires `return` for a result. If project C# changes trigger a domain reload, the Editor MCP stores operation state under `.unity-mcp/ops`, records compile results, and restores its endpoint after assembly reload.

## MCP Tool Extension

Built-in tools are C#. Project Lua and C# tools appear in the same `tools/list`. A project extension cannot silently replace an existing tool name; use a prefix such as `game.*` or `project.*`.

```text
<UnityProject>/unity-mcp-extension/
  lua/editor/             Editor Lua tools
  lua/runtime/            Runtime and Player Lua tools
  Packages/<package>/     Optional project C# provider packages
  skills/                 Project guidance for agents
```

Each Lua tool has a manifest beside its module:

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

`editor.scriptTools.reload` and `runtime.scriptTools.reload` rescan the matching directories. **Unity MCP > Create Extension Demos** copies working Editor and Runtime Lua examples without overwriting existing files. Put project guidance in `unity-mcp-extension/skills/*.md`; agents can list and load it through `agent.extension.skills.list`, `editor.skills.list`, or `runtime.skills.list`.

Project C# tools belong in a separate package that references `UnityMcp.Runtime` and the required project assemblies. Implement `IUnityMcpToolProvider` to register them when an endpoint starts:

```csharp
using System.Threading.Tasks;
using UnityMcp;

public sealed class ProjectMcpTools : IUnityMcpToolProvider
{
    public string EndpointKind => "runtime"; // editor, runtime/player, or all

    public void RegisterTools(UnityMcpToolProviderContext context)
    {
        context.TryRegister(new DelegateUnityMcpTool(
            "game.status", "Return project runtime state.", JsonSchemas.Object(),
            (ctx, args) => Task.FromResult("{\"ok\":true}")));
    }
}
```

## Built-in MCP Tools

`tools/list` returns the tools available on the connected endpoint. These tables describe package-provided tools; project tools are added separately.

### Editor MCP

| Tool | Purpose |
|---|---|
| `mcp.info` | Return Editor endpoint metadata, health, and capabilities. |
| `editor.state` | Return Unity Editor state. |
| `editor.buildSettings.startupScene` | Return the first enabled scene in Build Settings. |
| `editor.lua.eval` | Execute Lua in the Editor ZLua host. |
| `editor.hierarchy.get` / `get-hierarchy` | Export scene or Play Mode hierarchy JSON under `.unity-mcp/hierarchy-results`. |
| `editor.window.focus` / `focus-window` | Bring the Unity Editor window to the foreground. |
| `editor.window.screenshot` / `screenshot` | Capture an EditorWindow PNG under `.unity-mcp/editor-window-screenshots`. |
| `editor.profiler.targets.list` | List Editor Profiler targets, including attached Players when available. |
| `editor.profiler.connect` | Select an Editor or attached Player Profiler target. |
| `editor.profiler.capture` | Record and analyze raw Profiler frames into JSON, CSV, and Markdown. |
| `editor.profiler.analyze` | Analyze Profiler frames already recorded. |
| `performance.hotspot.report` / `perf.hotspot.report` | Produce a Profiler hotspot report under `.unity-mcp/perf-reports`. |
| `editor.scriptTools.list` / `editor.scriptTools.reload` | List or reload Editor Lua tools. |
| `editor.skills.list` / `editor.skill.load` | List or load project guidance. |
| `editor.playmode.set` / `editor.playmode.set.immediate` | Change Play Mode, delayed or immediately. |
| `editor.playmode.state` | Return Play Mode state. |
| `editor.targets.list` / `runtime.targets.list` / `targets.list` | List local and configured direct targets. |
| `runtime.lua.eval` | Forward Lua to local Play Mode or an explicit remote Player. |
| `runtime.tool.call` | Call a Runtime MCP tool through the Editor. |
| `editor.compile` | Refresh assets and record compile results for reload recovery. |
| `op.status` | Read persisted operation state or result. |

### Runtime / Player MCP

The same Runtime endpoint runs in Editor Play Mode, Android, iOS, and standalone Players.

| Tool | Purpose |
|---|---|
| `mcp.info` / `runtime.status` | Return Runtime endpoint health and state. |
| `runtime.targets.list` / `targets.list` | List this Player endpoint. |
| `runtime.lua.eval` | Execute Lua in the Runtime ZLua host. |
| `runtime.reflection.invoke` | Invoke a static C# method through the reflection gateway. |
| `runtime.scriptTools.list` / `runtime.scriptTools.reload` | List or reload Runtime Lua tools. |
| `runtime.skills.list` / `runtime.skill.load` | List or load project guidance. |
| `op.status` | Read persisted operation state or result. |
| `runtime.logs` / `runtime.logs.clear` | Read or clear the Runtime log buffer. |
| `screen.screenshot` | Capture a Player screen; phone defaults use in-memory PNG. |
| `runtime.ui.snapshot` | Return visible UGUI canvases and controls. |
| `runtime.ui.find` | Find UGUI controls by text, name, path, or canvas. |
| `runtime.ui.raycast` | Raycast UI at a point or resolved target. |
| `runtime.ui.click` / `input.tap` | Click by coordinates, path, or instance ID. |

## Agent ZLua Guide

This section is for agents generating code for `editor.lua.eval`, `runtime.lua.eval`, or project Lua tools.

### Choose The Target

| Task | Tool | Available APIs |
|---|---|---|
| Unity Editor automation | `editor.lua.eval` | UnityEditor and runtime APIs |
| Local Play Mode automation | `runtime.lua.eval` with a local target | Runtime APIs |
| Android, iOS, or standalone automation | `runtime.lua.eval` with `targetId` or `httpUrl` | Player runtime APIs |

The Editor and Runtime have separate MCP hosts. ZLua uses a shared `LuaAppDomain` in the Editor process, so a project already initializing it needs to coordinate its module loader with this extension. Player code should use runtime-safe APIs.

### Basic ZLua Code

Use ZLua's `CSharp['AssemblyName']['Namespace.Type']` to access public C# types. `Type.Method(...)` calls a static method; `Type()` constructs an instance; `object:Method(...)` calls an instance method.

```lua
local Application = CSharp['UnityEngine.CoreModule']['UnityEngine.Application']
return { productName = Application.productName, version = Application.version }
```

An expression returns automatically with `mode: "expression"`:

```lua
CSharp['UnityEngine.CoreModule']['UnityEngine.Application'].unityVersion
```

In script mode, use `return` explicitly. Return simple values instead of raw Unity objects:

```lua
local GameObject = CSharp['UnityEngine.CoreModule']['UnityEngine.GameObject']
local go = GameObject.Find('Canvas')
return { found = go ~= nil, name = go and go.name or '' }
```

For a direct static C# call on a Runtime endpoint, `runtime.reflection.invoke` accepts `typeName`, `methodName`, and `argsJson`. On IL2CPP, types and members used through reflection or Lua must survive stripping; follow the official ZLua generation workflow and add preservation rules or project wrappers where needed.

### Runtime UI Automation Pattern

Observe a phone screen with `screen.screenshot`, then inspect controls with `runtime.ui.snapshot`, `runtime.ui.find`, and `runtime.ui.raycast`. Click with `runtime.ui.click` or `input.tap`. Use project Lua tools in `unity-mcp-extension/lua/runtime` for repeated game flows.

### Profiler Hotspot Workflow

Call `editor.profiler.targets.list` first. For a phone or Player, attach it in the Unity Profiler or use `editor.profiler.connect` with a target name, ID, or URL. Then call `editor.profiler.capture` or `performance.hotspot.report`, for example with `duration: "15s"`. Reports under `.unity-mcp/perf-reports` include frame summaries, markers, self time, and GC.Alloc evidence.

### Return Values

Eval returns a JSON envelope with `kind` and `value`. Strings, numbers, booleans, and Lua tables can be encoded; tables become JSON objects or arrays. Cycles and deeply nested values are represented as strings. Project tool modules return JSON text directly from `execute(argsJson, contextJson)`.

## Protocol Surface

| Endpoint | Purpose |
|---|---|
| `GET /health` | Endpoint metadata and health |
| `GET /api/ping` | Lightweight health alias |
| `POST /mcp` | JSON-RPC MCP calls |

The main MCP methods are `initialize`, `ping`, `tools/list`, and `tools/call`. For example:

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

C# serialization uses Unity `JsonUtility`; the MCP packages do not require Newtonsoft.Json. See the [protocol guide](docs/protocol.md) for result details.

## Configuration And State

| Path | Purpose |
|---|---|
| `unity-mcp-extension/editor-mcp-config.json` | Editor settings and explicit target selection |
| `unity-mcp-extension/mobile-mcp-config.json` | Runtime/Player settings copied into builds |
| `unity-mcp-extension/lua/editor` | Project Editor Lua tools |
| `unity-mcp-extension/lua/runtime` | Project Runtime Lua tools |
| `unity-mcp-extension/skills` | Project guidance for agents |
| `.unity-mcp/editors/{editorId}/heartbeat.json` | Editor heartbeat |
| `.unity-mcp/players/{playerId}/heartbeat.json` | Optional Player heartbeat |
| `.unity-mcp/ops/{operationId}` | Operation state and results |
| `.unity-mcp/temp/compile-results` | Compile result hints |
| `.unity-mcp/perf-reports` | Profiler reports |

The default Editor port is `18990`; the default Player port is `18991`. Remote selection uses an explicit URL in the config, `--target-url`, or `UNITY_MCP_TARGET_URL`. Automatic LAN discovery is not used.

## Unity Project `.gitignore`

Ignore runtime state in the Unity project:

```gitignore
.unity-mcp/
Assets/StreamingAssets/UnityMcp/mobile-mcp-config.json
```

Keep `unity-mcp-extension` when its configs, Lua tools, and skills should travel with the project. The three package directories include the Unity metadata and native files needed by their packages.

## Directory Structure

```text
unity-mcp/
  Packages/
    unity-mcp/                 Editor and Runtime MCP, tools, tests, stdio proxy
    unity-mcp-zlua-extension/  ZLua host and interop tests
    com.code-philosophy.zlua/  Official ZLua package copy
  Tools/                      Node.js test runner and ZLua updater
  docs/                       Protocol and upstream revision
```

## Tests And Updating ZLua

Unity 2022.3.60f1 EditMode tests passed: 42 protocol/configuration/storage tests and 5 Lua-to-C# interop tests. These tests cover Editor Mono; Player IL2CPP, overloads, generics, `ref/out`, and events remain unverified by this suite.

```text
node Tools/run-editor-tests.mjs path/to/UnityProject
node Tools/run-editor-tests.mjs path/to/UnityProject --package=unity-mcp-zlua-extension --assembly=UnityMcp.ZLua.Editor.Tests
```

The Node.js runner enables Unity Test Framework and writes NUnit XML under `<UnityProject>/TestResults`. Pass `--unity=path/to/Unity` or set `UNITY_EDITOR_PATH` for a different Editor installation.

To refresh the independent official ZLua package from a clean upstream checkout:

```text
node Tools/update-zlua.mjs --source path/to/zlua-checkout
```

`--check` compares without copying. The updater changes only `Packages/com.code-philosophy.zlua` and [the upstream revision record](docs/zlua-upstream.md).

## License

Unity MCP is released under the [MIT License](LICENSE). The official [ZLua](https://github.com/focus-creative-games/zlua) package retains its [upstream MIT license](Packages/com.code-philosophy.zlua/LICENSE).
