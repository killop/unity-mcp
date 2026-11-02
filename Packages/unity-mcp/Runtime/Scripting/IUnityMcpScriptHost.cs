using System;

namespace UnityMcp
{
    public interface IUnityMcpScriptHost : IDisposable
    {
        string Eval(string code, string mode = "script", string chunkName = null);
        string ExecuteModuleFunctionJson(string modulePath, string functionName, string argsJson, string contextJson);
        void Tick();
    }

    public static class UnityMcpScriptHostFactory
    {
        private static Func<string, IUnityMcpScriptHost> create;

        public static void Register(Func<string, IUnityMcpScriptHost> factory)
        {
            create = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public static IUnityMcpScriptHost Create(string hostName)
        {
            if (create == null)
            {
                throw new InvalidOperationException("No scripting extension is registered. Install unity-mcp-zlua-extension.");
            }

            return create(hostName);
        }
    }
}
