#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.Garden;
using GeminiLab.Modules.Inventory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 在 Boot.unity 的 BootstrapRoot 下挂 <see cref="GardenRuntimeBootstrap"/> 并绑定 SeedCatalog.asset；
    /// 顺手把 InventoryRuntimeBootstrap 的 _starterItems 填上 3 种子 × 5。幂等。
    /// </summary>
    public static class BootGardenBootstrapAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/Boot.unity";
        private const string SeedCatalogPath = "Assets/_Project/ScriptableObjects/GardenConfig/SeedCatalog.asset";

        private static readonly (string id, int count)[] StarterSeeds = new[]
        {
            ("seed_carrot", 5),
            ("seed_tomato", 5),
            ("seed_wheat",  5),
        };

        [MenuItem("Tools/Gemini-Lab/Author Boot Garden Bootstrap")]
        public static void Author()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var root = GameObject.Find("BootstrapRoot");
            if (root == null)
            {
                Debug.LogError("[BootGardenBootstrap] 未找到 BootstrapRoot");
                return;
            }

            // 1) Garden bootstrap
            var boot = root.GetComponent<GardenRuntimeBootstrap>();
            if (boot == null)
            {
                boot = root.AddComponent<GardenRuntimeBootstrap>();
            }
            var seedCatalog = AssetDatabase.LoadAssetAtPath<SeedCatalogSO>(SeedCatalogPath);
            if (seedCatalog == null)
            {
                Debug.LogError($"[BootGardenBootstrap] SeedCatalog 未找到：{SeedCatalogPath}，请先跑 Author Seed Catalog");
            }
            else
            {
                var so = new SerializedObject(boot);
                so.FindProperty("_seedCatalog").objectReferenceValue = seedCatalog;
                so.ApplyModifiedProperties();
            }

            // 2) 顺手把 Inventory 的 _starterItems 填满
            var inv = root.GetComponent<InventoryRuntimeBootstrap>();
            if (inv != null)
            {
                var invSo = new SerializedObject(inv);
                var arr = invSo.FindProperty("_starterItems");
                arr.arraySize = StarterSeeds.Length;
                for (int i = 0; i < StarterSeeds.Length; i++)
                {
                    var el = arr.GetArrayElementAtIndex(i);
                    el.FindPropertyRelative("ItemId").stringValue = StarterSeeds[i].id;
                    el.FindPropertyRelative("Count").intValue = StarterSeeds[i].count;
                }
                invSo.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning("[BootGardenBootstrap] 没找到 InventoryRuntimeBootstrap，跳过 starter items 注入");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[BootGardenBootstrap] Garden bootstrap 已挂到 BootstrapRoot；Inventory starter items 已填入");
        }
    }
}
#endif
