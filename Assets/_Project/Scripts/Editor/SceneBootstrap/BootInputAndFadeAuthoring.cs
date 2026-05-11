#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.HubUI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 在 Boot.unity 的 BootstrapRoot 下挂 UIInputRouter + SceneFadeOverlay。幂等。
    /// </summary>
    public static class BootInputAndFadeAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/Boot.unity";

        [MenuItem("Tools/Gemini-Lab/Author Boot InputRouter + Fade")]
        public static void Author()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var bootstrapRoot = GameObject.Find("BootstrapRoot");
            if (bootstrapRoot == null)
            {
                Debug.LogError("[BootInputAndFadeAuthoring] 未找到 BootstrapRoot");
                return;
            }

            if (bootstrapRoot.GetComponent<UIInputRouter>() == null)
            {
                bootstrapRoot.AddComponent<UIInputRouter>();
            }

            if (bootstrapRoot.GetComponent<SceneFadeOverlay>() == null)
            {
                bootstrapRoot.AddComponent<SceneFadeOverlay>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[BootInputAndFadeAuthoring] UIInputRouter + SceneFadeOverlay 已挂到 BootstrapRoot");
        }
    }
}
#endif
