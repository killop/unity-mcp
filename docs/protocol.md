# Unity MCP protocol

The Editor endpoint and Player endpoint expose MCP over HTTP at `/mcp`. The default Editor port is `18990`; the default Player port is `18991`. The stdio proxy forwards requests to the selected endpoint.

A tool call uses JSON-RPC 2.0:

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

`editor.lua.eval` and `runtime.lua.eval` return a JSON envelope with `kind` and `value`. Lua tables are encoded as JSON objects or arrays; cyclic or deeply nested values are rendered as strings. Lua module tools return JSON text directly from `execute(argsJson, contextJson)`.

Editor and Player tools also include state, target discovery, reflection, file backed operations, logs, UI interaction, screenshots, and profiler analysis. Use `tools/list` to discover the current endpoint's full tool set.

The Editor project config is `unity-mcp-extension/editor-mcp-config.json`, the Player config is `unity-mcp-extension/mobile-mcp-config.json`, and runtime state is under `.unity-mcp`. The Editor can route Player tools by `targetId` or `httpUrl`.
