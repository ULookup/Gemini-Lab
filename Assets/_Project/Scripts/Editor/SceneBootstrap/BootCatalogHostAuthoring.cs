#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.UI.Catalogs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 在 Boot.unity 的 BootstrapRoot 下挂 UICatalogHost，并把 Font/Art Catalog 资产绑进去。
    /// 幂等。
    /// </summary>
    public static class BootCatalogHostAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/Boot.unity";
        private const string FontCatalogPath = "Assets/_Project/ScriptableObjects/UIArt/UIFontCatalog.asset";
        private const string ArtCatalogPath = "Assets/_Project/ScriptableObjects/UIArt/UIArtCatalog.asset";

        [MenuItem("Tools/Gemini-Lab/Author Boot UICatalogHost")]
        public static void Author()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var bootstrapRoot = GameObject.Find("BootstrapRoot");
            if (bootstrapRoot == null)
            {
                Debug.LogError("[BootCatalogHostAuthoring] 未找到 BootstrapRoot");
                return;
            }

            var host = bootstrapRoot.GetComponent<UICatalogHost>();
            if (host == null)
            {
                host = bootstrapRoot.AddComponent<UICatalogHost>();
            }

            var fontCatalog = AssetDatabase.LoadAssetAtPath<UIFontCatalogSO>(FontCatalogPath);
            var artCatalog = AssetDatabase.LoadAssetAtPath<UIArtCatalogSO>(ArtCatalogPath);

            var so = new SerializedObject(host);
            so.FindProperty("_fontCatalog").objectReferenceValue = fontCatalog;
            so.FindProperty("_artCatalog").objectReferenceValue = artCatalog;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[BootCatalogHostAuthoring] UICatalogHost 已挂到 BootstrapRoot 并绑定 Catalog");
        }
    }
}
#endif
