using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

namespace UnityMcp.ZLua
{
    public sealed class ZLuaScriptHost : IUnityMcpScriptHost
    {
        private const string JsonModuleName = "unity_mcp_json";
        private const string EvalModuleName = "unity_mcp_eval";
        private static readonly ConcurrentDictionary<string, string> PendingModules = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, ModuleVersion> FileModules = new ConcurrentDictionary<string, ModuleVersion>(StringComparer.OrdinalIgnoreCase);
        private static readonly object InitializationLock = new object();
        private static int nextModuleId;
        private static bool initialized;
        private readonly string hostName;

        public ZLuaScriptHost(string hostName)
        {
            this.hostName = string.IsNullOrEmpty(hostName) ? "mcp" : hostName;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterForPlayers()
        {
            UnityMcpScriptHostFactory.Register(name => new ZLuaScriptHost(name));
        }

        public static void RegisterForEditor()
        {
            UnityMcpScriptHostFactory.Register(name => new ZLuaScriptHost(name));
        }

        public string Eval(string code, string mode = "script", string chunkName = null)
        {
            EnsureInitialized();
            var body = string.Equals(mode, "expression", StringComparison.OrdinalIgnoreCase)
                ? "return (" + (code ?? string.Empty) + ")"
                : code ?? string.Empty;
            var execute = global::ZLua.LuaAppDomain.GetFunction<Func<string, string, string>>(EvalModuleName, "execute");
            return execute(body, chunkName ?? hostName);
        }

        public string ExecuteModuleFunctionJson(string modulePath, string functionName, string argsJson, string contextJson)
        {
            if (string.IsNullOrEmpty(modulePath) || !File.Exists(modulePath))
            {
                throw new FileNotFoundException("Lua tool module was not found.", modulePath);
            }

            EnsureInitialized();
            var fullPath = Path.GetFullPath(modulePath);
            var version = File.GetLastWriteTimeUtc(fullPath).Ticks;
            var module = FileModules.TryGetValue(fullPath, out var cached) && cached.version == version
                ? cached.module
                : AddFileModule(fullPath, version);
            try
            {
                var execute = global::ZLua.LuaAppDomain.GetFunction<Func<string, string, string>>(
                    module, string.IsNullOrEmpty(functionName) ? "execute" : functionName);
                var result = execute(argsJson ?? "{}", contextJson ?? "{}");
                return string.IsNullOrEmpty(result) ? "{}" : result;
            }
            catch
            {
                FileModules.TryRemove(fullPath, out _);
                throw;
            }
            finally
            {
                PendingModules.TryRemove(module, out _);
            }
        }

        public void Tick() { }

        public void Dispose() { }

        private static void EnsureInitialized()
        {
            if (initialized) return;
            lock (InitializationLock)
            {
                if (initialized) return;
                global::ZLua.LuaAppDomain.Initialize(LoadModule);
                initialized = true;
            }
        }

        private static string AddModule(string source)
        {
            var name = "unity_mcp_dynamic_" + Interlocked.Increment(ref nextModuleId);
            PendingModules[name] = source;
            return name;
        }

        private static string AddFileModule(string path, long version)
        {
            var module = AddModule(File.ReadAllText(path, Encoding.UTF8));
            FileModules[path] = new ModuleVersion { version = version, module = module };
            return module;
        }

        private static object LoadModule(string name)
        {
            if (name == JsonModuleName) return JsonModule;
            if (name == EvalModuleName) return EvalModule;
            if (PendingModules.TryGetValue(name, out var source)) return source;

            // Project modules may use require('editor.my_module') or require('runtime.my_module').
            if (string.IsNullOrEmpty(name) || name.Contains("..") || name.IndexOfAny(new[] { '/', '\\', ':' }) >= 0)
                return null;
            var relative = name.Replace('.', Path.DirectorySeparatorChar) + ".lua";
            var root = Path.Combine(UnityMcpPaths.ProjectExtensionRoot, "lua");
            var path = Path.Combine(root, relative);
            return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : null;
        }

        private sealed class ModuleVersion
        {
            public long version;
            public string module;
        }

        private const string EvalModule = "local json = require('unity_mcp_json')\n"
            + "return { execute = function(source, chunk)\n"
            + "local compile = loadstring or load\n"
            + "local fn, err = compile(source, chunk)\n"
            + "if not fn then error(err) end\n"
            + "return json.envelope(fn())\n"
            + "end }";

        private const string JsonModule = "local function quote(s)\n    local escapes = { ['\"']='\\\\\"', ['\\\\']='\\\\\\\\', ['\\n']='\\\\n', ['\\r']='\\\\r', ['\\t']='\\\\t' }\n    return '\"' .. tostring(s):gsub('[%z\\1-\\31\\\\\"]', function(c)\n        return escapes[c] or string.format('\\\\u%04x', string.byte(c))\n    end) .. '\"'\nend\n\nlocal function encode(v, seen, depth)\n    local t = type(v)\n    if t == 'nil' then return 'null' end\n    if t == 'string' then return quote(v) end\n    if t == 'boolean' then return tostring(v) end\n    if t == 'number' then\n        if v ~= v or v == math.huge or v == -math.huge then return 'null' end\n        return tostring(v)\n    end\n    if t ~= 'table' then return quote(tostring(v)) end\n    if seen[v] or depth > 16 then return quote('[circular or too deep]') end\n    seen[v] = true\n    local n, array = #v, #v > 0\n    if array then\n        for k in pairs(v) do\n            if type(k) ~= 'number' or k < 1 or k > n or k % 1 ~= 0 then array = false; break end\n        end\n    end\n    local parts = {}\n    if array then\n        for i=1,n do parts[#parts+1] = encode(v[i], seen, depth+1) end\n        seen[v] = nil\n        return '[' .. table.concat(parts, ',') .. ']'\n    end\n    for k,value in pairs(v) do\n        parts[#parts+1] = quote(tostring(k)) .. ':' .. encode(value, seen, depth+1)\n    end\n    seen[v] = nil\n    return '{' .. table.concat(parts, ',') .. '}'\nend\n\nreturn { envelope = function(v)\n    local kind = type(v)\n    if kind == 'nil' then kind = 'null' end\n    if kind == 'table' then kind = 'object' end\n    return '{\"kind\":' .. quote(kind) .. ',\"value\":' .. encode(v, {}, 0) .. '}'\nend }\n";
    }
}
