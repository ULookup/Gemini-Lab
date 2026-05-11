#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.Tarot;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 在 Boot.unity 的 BootstrapRoot 下挂 TarotRuntimeBootstrap，并把 TarotDeck.asset 绑进去。
    /// 幂等。
    /// </summary>
    public static class BootTarotAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/Boot.unity";
        private const string DeckPath = "Assets/_Project/ScriptableObjects/TarotConfig/TarotDeck.asset";

        [MenuItem("Tools/Gemini-Lab/Author Boot TarotBootstrap")]
        public static void Author()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var bootstrapRoot = GameObject.Find("BootstrapRoot");
            if (bootstrapRoot == null)
            {
                Debug.LogError("[BootTarotAuthoring] 未找到 BootstrapRoot");
                return;
            }

            var host = bootstrapRoot.GetComponent<TarotRuntimeBootstrap>();
            if (host == null)
            {
                host = bootstrapRoot.AddComponent<TarotRuntimeBootstrap>();
            }

            var deck = AssetDatabase.LoadAssetAtPath<TarotDeckSO>(DeckPath);
            if (deck == null)
            {
                Debug.LogError($"[BootTarotAuthoring] Deck 未找到：{DeckPath}，请先跑 Author Tarot Deck");
                return;
            }

            var so = new SerializedObject(host);
            so.FindProperty("_deck").objectReferenceValue = deck;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[BootTarotAuthoring] TarotRuntimeBootstrap 已挂到 BootstrapRoot 并绑定 Deck");
        }
    }
}
#endif
