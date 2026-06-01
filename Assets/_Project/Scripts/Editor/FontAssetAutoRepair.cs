#nullable enable
#if UNITY_EDITOR
using UnityEditor;

namespace GeminiLab.Editor
{
    /// <summary>
    /// 编辑器域重载后做一次字体健康检查，优先修复损坏的中文 TMP 字体，
    /// 让当前已打开的 Unity 工程也能自动恢复中文渲染链。
    /// </summary>
    [InitializeOnLoad]
    public static class FontAssetAutoRepair
    {
        private const string SessionKey = "GeminiLab.Editor.FontAssetAutoRepair.Ran";

        static FontAssetAutoRepair()
        {
            EditorApplication.delayCall += TryRunOnce;
        }

        private static void TryRunOnce()
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += TryRunOnce;
                return;
            }

            SessionState.SetBool(SessionKey, true);
            FontAssetRebake.RepairProjectFontsIfNeeded();
        }
    }
}
#endif
