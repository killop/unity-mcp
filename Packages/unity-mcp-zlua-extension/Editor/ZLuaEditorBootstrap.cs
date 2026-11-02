using UnityEditor;

namespace UnityMcp.ZLua.Editor
{
    [InitializeOnLoad]
    internal static class ZLuaEditorBootstrap
    {
        static ZLuaEditorBootstrap()
        {
            ZLuaScriptHost.RegisterForEditor();
        }
    }
}
