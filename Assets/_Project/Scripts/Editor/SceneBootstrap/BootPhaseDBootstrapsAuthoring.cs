#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.Collection;
using GeminiLab.Modules.Inventory;
using GeminiLab.Modules.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 在 Boot.unity 的 BootstrapRoot 下挂 Phase D 的三个运行时 Bootstrap，
    /// 并把 ItemCatalog.asset 绑到 InventoryRuntimeBootstrap。幂等。
    /// </summary>
    public static class BootPhaseDBootstrapsAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/Boot.unity";
        private const string ItemCatalogPath = "Assets/_Project/ScriptableObjects/InventoryConfig/ItemCatalog.asset";

        [MenuItem("Tools/Gemini-Lab/Author Boot Phase D Bootstraps")]
        public static void Author()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var root = GameObject.Find("BootstrapRoot");
            if (root == null)
            {
                Debug.LogError("[BootPhaseDBootstraps] 未找到 BootstrapRoot");
                return;
            }

            if (root.GetComponent<SettingsRuntimeBootstrap>() == null)
            {
                root.AddComponent<SettingsRuntimeBootstrap>();
            }

            var inv = root.GetComponent<InventoryRuntimeBootstrap>();
            if (inv == null)
            {
                inv = root.AddComponent<InventoryRuntimeBootstrap>();
            }
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalogSO>(ItemCatalogPath);
            if (catalog == null)
            {
                Debug.LogError($"[BootPhaseDBootstraps] ItemCatalog 未找到：{ItemCatalogPath}，请先跑 Author Item Catalog");
            }
            else
            {
                var so = new SerializedObject(inv);
                so.FindProperty("_catalog").objectReferenceValue = catalog;
                so.ApplyModifiedProperties();
            }

            if (root.GetComponent<CollectionRuntimeBootstrap>() == null)
            {
                root.AddComponent<CollectionRuntimeBootstrap>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[BootPhaseDBootstraps] Phase D 三件 Bootstrap 已挂到 BootstrapRoot");
        }
    }
}
#endif
