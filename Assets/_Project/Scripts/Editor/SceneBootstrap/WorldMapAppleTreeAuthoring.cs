#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.Apple;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 将 WorldMap 现有的 5 棵大树绑定到苹果领取服务。
    /// 只补脚本和树 ID，不创建或替换树的任何视觉资源。
    /// </summary>
    public static class WorldMapAppleTreeAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";
        private static readonly string[] TreeNames = { "大树 1", "大树 2", "大树 3", "大树 4", "大树 5" };

        public static void Patch()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            bool changed = false;
            for (int i = 0; i < TreeNames.Length; i++)
            {
                var tree = GameObject.Find(TreeNames[i]);
                if (tree == null)
                {
                    Debug.LogWarning($"[WorldMapAppleTreeAuthoring] 未找到「{TreeNames[i]}」，跳过");
                    continue;
                }

                var interactable = tree.GetComponent<AppleTreeInteractable>();
                if (interactable == null)
                {
                    interactable = Undo.AddComponent<AppleTreeInteractable>(tree);
                    changed = true;
                }

                var so = new SerializedObject(interactable);
                var treeId = so.FindProperty("_treeId");
                string expectedId = $"world_tree_{i + 1}";
                if (treeId != null && treeId.stringValue != expectedId)
                {
                    treeId.stringValue = expectedId;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log("[WorldMapAppleTreeAuthoring] 5 棵大树的苹果领取入口已作者化");
        }
    }
}
#endif
