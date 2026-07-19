#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.EmotionGarden;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 在 Boot.unity 的 BootstrapRoot 下挂 <see cref="EmotionGardenRuntimeBootstrap"/>。幂等。
    /// </summary>
    public static class BootEmotionGardenBootstrapAuthoring
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
                Debug.LogError("[BootEmotionGardenBootstrap] 未找到 BootstrapRoot");
                return;
            }

            var boot = root.GetComponent<EmotionGardenRuntimeBootstrap>();
            if (boot == null)
            {
                boot = root.AddComponent<EmotionGardenRuntimeBootstrap>();
            }

            var so = new SerializedObject(boot);
            so.FindProperty("_refreshIntervalSeconds").floatValue = 1f;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[BootEmotionGardenBootstrap] EmotionGardenRuntimeBootstrap 已挂到 BootstrapRoot");
        }
    }
}
#endif
