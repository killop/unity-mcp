using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;
using UnityEditor.UnityLinker;
using UnityEngine;

namespace UnityMcp.Editor
{
    public sealed class UnityMcpLinkXmlProcessor : IUnityLinkerProcessor
    {
        private const string PackageAssetPath = "Packages/unity-mcp/package.json";
        private const string LinkXmlRelativePath = "Runtime/UnityMcp.link.xml";

        public int callbackOrder => -900;

        public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            var path = ResolveLinkXmlPath();
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                Debug.Log("[UnityMCP] Providing package linker config: " + path);
                return path;
            }

            Debug.LogWarning("[UnityMCP] Package linker config was not found: " + path);
            return null;
        }

        private static string ResolveLinkXmlPath()
        {
            var packageInfo = PackageInfo.FindForAssetPath(PackageAssetPath);
            if (packageInfo != null && !string.IsNullOrEmpty(packageInfo.resolvedPath))
            {
                return Path.Combine(packageInfo.resolvedPath, LinkXmlRelativePath);
            }

            var projectRoot = UnityMcpPaths.ProjectRoot;
            return string.IsNullOrEmpty(projectRoot)
                ? null
                : Path.Combine(projectRoot, "unity-mcp", "Packages", "unity-mcp", LinkXmlRelativePath);
        }
    }
}
