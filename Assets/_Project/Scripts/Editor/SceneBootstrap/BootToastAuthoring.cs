#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.HubUI.Toast;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 在 Boot.unity 的 BootstrapRoot 下挂一个 ToastOverlayController。幂等。
    /// </summary>
    public static class BootToastAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/Boot.unity";

        [MenuItem("Tools/Gemini-Lab/Author Boot ToastOverlay")]
        public static void Author()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var bootstrapRoot = GameObject.Find("BootstrapRoot");
            if (bootstrapRoot == null)
            {
                Debug.LogError("[BootToastAuthoring] 未找到 BootstrapRoot");
                return;
            }

            var existing = bootstrapRoot.GetComponent<ToastOverlayController>();
            if (existing == null)
            {
                bootstrapRoot.AddComponent<ToastOverlayController>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[BootToastAuthoring] ToastOverlayController 已挂到 BootstrapRoot");
        }
    }
}
#endif
