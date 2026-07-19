#nullable enable
using GeminiLab.Core;
using UnityEditor;
using UnityEngine;

namespace GeminiLab.Editor.Tools
{
    [InitializeOnLoad]
    public static class DevModeToggle
    {
        private const string Key = "GeminiLab.DevMode";

        static DevModeToggle()
        {
            DevMode.Active = EditorPrefs.GetBool(Key, true);
        }

        [MenuItem("Tools/Gemini-Lab/Toggle Dev Mode")]
        private static void Toggle()
        {
            // Play 中禁止切换：SaveSystem 的存档目录在启动时已按模式固定，
            // 中途翻转会导致"读的是 A 世界、写的是 B 世界"
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[DevMode] 请先退出 Play 模式再切换（存档目录按模式隔离，运行中切换会造成数据错乱）");
                return;
            }

            DevMode.Active = !DevMode.Active;
            EditorPrefs.SetBool(Key, DevMode.Active);
            Debug.Log($"[DevMode] {(DevMode.Active ? "开发者模式（存档: Saves-Dev）" : "玩家模式（存档: Saves）")}");

            // 同步 DevTools 可见性
            ApplyDevToolsVisibility();
        }

        private static void ApplyDevToolsVisibility()
        {
            foreach (var go in Object.FindObjectsOfType<GameObject>(true))
            {
                if (go.name == "DevTools" && go.transform.parent != null && go.transform.parent.name == "Canvas")
                {
                    go.SetActive(DevMode.Active);
                }
            }
        }
    }
}
