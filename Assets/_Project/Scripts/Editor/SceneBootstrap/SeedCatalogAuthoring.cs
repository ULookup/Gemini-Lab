#nullable enable
#if UNITY_EDITOR
using System.Collections.Generic;
using GeminiLab.Modules.Garden;
using UnityEditor;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 生成 SeedDef_*.asset + SeedCatalog.asset，复用 ItemCatalog 已有的 3 对种子 / 作物。
    /// </summary>
    public static class SeedCatalogAuthoring
    {
        private const string Folder = "Assets/_Project/ScriptableObjects/GardenConfig";
        private const string CatalogPath = Folder + "/SeedCatalog.asset";

        private static readonly (string seedId, string cropId, int harvestCount, int growSeconds, int growingStart)[] Seeds = new[]
        {
            // 默认 2 小时 = 7200 秒；Growing 从 1/3 开始
            ("seed_carrot", "crop_carrot", 2, 7200, 2400),
            ("seed_tomato", "crop_tomato", 3, 7200, 2400),
            ("seed_wheat",  "crop_wheat",  4, 7200, 2400),
        };

        [MenuItem("Tools/Gemini-Lab/Author Seed Catalog")]
        public static void Author()
        {
            EnsureFolder(Folder);

            var defs = new List<SeedDefinitionSO>();
            foreach (var (seedId, cropId, harvest, grow, growing) in Seeds)
            {
                string defPath = $"{Folder}/SeedDef_{seedId}.asset";
                var def = AssetDatabase.LoadAssetAtPath<SeedDefinitionSO>(defPath);
                if (def == null)
                {
                    def = ScriptableObject.CreateInstance<SeedDefinitionSO>();
                    AssetDatabase.CreateAsset(def, defPath);
                }

                var so = new SerializedObject(def);
                so.FindProperty("SeedItemId").stringValue = seedId;
                so.FindProperty("CropItemId").stringValue = cropId;
                so.FindProperty("HarvestCount").intValue = harvest;
                so.FindProperty("TotalGrowSeconds").intValue = grow;
                so.FindProperty("GrowingStartSeconds").intValue = growing;
                so.ApplyModifiedPropertiesWithoutUndo();

                defs.Add(def);
            }

            var catalog = AssetDatabase.LoadAssetAtPath<SeedCatalogSO>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SeedCatalogSO>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var catSo = new SerializedObject(catalog);
            var list = catSo.FindProperty("Seeds");
            list.arraySize = defs.Count;
            for (int i = 0; i < defs.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = defs[i];
            }
            catSo.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SeedCatalog] 已生成 / 刷新 {CatalogPath}（seeds={defs.Count}）");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(cur, parts[i]);
                }
                cur = next;
            }
        }
    }
}
#endif
