# Unity MCP agent setup

Install the three packages in the Unity project as described in the repository README. Start the Unity MCP endpoint from the Unity Editor settings window. The default Editor URL is `http://127.0.0.1:18990/mcp`.

To connect an MCP client through stdio, run Node with the proxy script:

```text
node <repo>/Packages/unity-mcp/Tools~/unity-mcp-stdio-proxy.js --config <UnityProject>/unity-mcp-extension/editor-mcp-config.json
```

The proxy discovers project extension tools and skills from `<UnityProject>/unity-mcp-extension`. The state directory is `<UnityProject>/.unity-mcp`.

## Script tools

Use `editor.lua.eval` for Editor automation and `runtime.lua.eval` for Play Mode or Player automation. Lua can access C# through `CSharp['UnityEngine.CoreModule']['UnityEngine.Application']`. On the Runtime endpoint, `runtime.reflection.invoke` also supports direct static C# calls. Return JSON-compatible Lua values from eval.

Project Lua tools are manifest and module pairs in `unity-mcp-extension/lua/editor` or `lua/runtime`:

```json
{
  "name": "project.echo",
  "description": "Return the request arguments.",
  "inputSchemaJson": "{\"type\":\"object\",\"additionalProperties\":true}",
  "modulePath": "echo.lua",
  "functionName": "execute"
}
```

```lua
return {
    execute = function(argsJson, contextJson)
        return argsJson or "{}"
    end
}
```

Use the Unity menu **Unity MCP > Create Extension Demos** to copy examples without overwriting project files. Project authoring notes can be put in `unity-mcp-extension/skills/*.md`.

The ZLua package owns one Lua app domain shared by MCP Editor and Play Mode hosts. The extension initializes it on first Lua call. A project that already initializes `LuaAppDomain` needs to coordinate its module loader with this extension.
