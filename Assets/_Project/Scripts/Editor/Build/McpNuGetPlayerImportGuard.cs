#nullable enable
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;

namespace GeminiLab.Editor.Build
{
    /// <summary>
    /// MCP 依赖解析器会把一批 NuGet DLL 落到 Assets/Plugins/NuGet。
    /// 这些 DLL 主要服务于编辑器工具链；若它们以 Player 插件身份进入构建，
    /// Burst 会在 AOT 扫描阶段因带版本号文件名而无法解析 SignalR / Microsoft.Extensions 依赖链。
    /// 本守卫统一把该目录下的 DLL 校正为 Editor-only。
    /// </summary>
    public static class McpNuGetPlayerImportGuard
    {
        private const string NuGetPluginsFolder = "Assets/Plugins/NuGet";

        [MenuItem("Tools/Gemini-Lab/Fix MCP NuGet Player Import Settings")]
        public static void ApplyNow()
        {
            bool defineChanged = StripStandaloneMcpReadyDefine();
            int importerChangedCount = ApplyEditorOnlyImportSettings();
            if (defineChanged || importerChangedCount > 0)
            {
                AssetDatabase.SaveAssets();
                UnityEngine.Debug.Log($"[MCP_BUILD_GUARD] 已修复 MCP 构建导入设置：Standalone define changed={defineChanged}, NuGet DLL changed={importerChangedCount}。如需让 Unity 立即重新导入，请手动 Refresh 或等待编辑器常规刷新。");
                return;
            }

            UnityEngine.Debug.Log("[MCP_BUILD_GUARD] Standalone define 与 NuGet DLL 导入设置都已符合构建要求，无需修改。");
        }

        private static int ApplyEditorOnlyImportSettings()
        {
            if (!AssetDatabase.IsValidFolder(NuGetPluginsFolder))
            {
                return 0;
            }

            string absoluteFolder = Path.GetFullPath(NuGetPluginsFolder);
            if (!Directory.Exists(absoluteFolder))
            {
                return 0;
            }

            string[] dllFiles = Directory.GetFiles(absoluteFolder, "*.dll", SearchOption.TopDirectoryOnly);
            var changedImporters = new List<PluginImporter>();

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < dllFiles.Length; i++)
                {
                    string assetPath = ToAssetPath(dllFiles[i]);
                    if (string.IsNullOrEmpty(assetPath))
                    {
                        continue;
                    }

                    if (AssetImporter.GetAtPath(assetPath) is not PluginImporter importer)
                    {
                        continue;
                    }

                    if (!NeedsEditorOnlyFix(importer))
                    {
                        continue;
                    }

                    importer.SetCompatibleWithAnyPlatform(false);
                    importer.SetCompatibleWithEditor(true);
                    importer.SaveAndReimport();
                    changedImporters.Add(importer);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            return changedImporters.Count;
        }

        private static bool NeedsEditorOnlyFix(PluginImporter importer)
        {
            return importer.GetCompatibleWithAnyPlatform() || !importer.GetCompatibleWithEditor();
        }

        private static bool StripStandaloneMcpReadyDefine()
        {
            const string ReadyDefine = "UNITY_MCP_READY";

            PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone, out string[] defines);
            int index = System.Array.IndexOf(defines, ReadyDefine);
            if (index < 0)
            {
                return false;
            }

            var newDefines = new List<string>(defines.Length - 1);
            for (int i = 0; i < defines.Length; i++)
            {
                if (!string.Equals(defines[i], ReadyDefine, System.StringComparison.Ordinal))
                {
                    newDefines.Add(defines[i]);
                }
            }

            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, newDefines.ToArray());
            return true;
        }

        private static string ToAssetPath(string absolutePath)
        {
            string normalized = absolutePath.Replace('\\', '/');
            string projectPath = Path.GetFullPath(".");
            string normalizedProjectPath = projectPath.Replace('\\', '/');
            if (!normalized.StartsWith(normalizedProjectPath))
            {
                return string.Empty;
            }

            string relative = normalized.Substring(normalizedProjectPath.Length).TrimStart('/');
            return relative.Replace('\\', '/');
        }
    }
}
#endif
