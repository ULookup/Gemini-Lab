#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.Apple;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>在 Boot/BootstrapRoot 上作者化苹果资源宿主。</summary>
    public static class BootAppleBootstrapAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/Boot.unity";

        public static void Author()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var root = GameObject.Find("BootstrapRoot");
            if (root == null)
            {
                Debug.LogError("[BootAppleBootstrap] 未找到 BootstrapRoot");
                return;
            }

            if (root.GetComponent<AppleRuntimeBootstrap>() == null)
            {
                root.AddComponent<AppleRuntimeBootstrap>();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log("[BootAppleBootstrap] AppleRuntimeBootstrap 已挂到 BootstrapRoot");
        }
    }
}
#endif
