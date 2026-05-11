#nullable enable
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using GeminiLab.Modules.Inventory;
using UnityEditor;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 生成 10 个占位 ItemDefSO + 1 个 ItemCatalog.asset。
    /// 占位 icon 是 128x128 纯色块 PNG；真美术到位后替换 _icon 即可。
    /// </summary>
    public static class ItemCatalogAuthoring
    {
        private const string DefFolder = "Assets/_Project/ScriptableObjects/InventoryConfig/Items";
        private const string CatalogPath = "Assets/_Project/ScriptableObjects/InventoryConfig/ItemCatalog.asset";
        private const string IconFolder = "Assets/_Project/Art/Sprites/Items";

        private static readonly (string id, string zh, ItemCategory cat, Color color, string tooltip)[] Items = new[]
        {
            ("seed_carrot",   "胡萝卜种子", ItemCategory.Seed,          new Color(1.0f, 0.55f, 0.30f), "种下可长出胡萝卜。"),
            ("seed_tomato",   "番茄种子",   ItemCategory.Seed,          new Color(1.0f, 0.35f, 0.35f), "种下可长出番茄。"),
            ("seed_wheat",    "小麦种子",   ItemCategory.Seed,          new Color(0.95f, 0.85f, 0.45f),"种下可长出小麦。"),
            ("crop_carrot",   "胡萝卜",     ItemCategory.Crop,          new Color(0.95f, 0.5f,  0.20f),"可食用作物。"),
            ("crop_tomato",   "番茄",       ItemCategory.Crop,          new Color(0.85f, 0.25f, 0.25f),"可食用作物。"),
            ("crop_wheat",    "小麦",       ItemCategory.Crop,          new Color(0.90f, 0.80f, 0.45f),"可食用作物。"),
            ("tarot_ticket",  "塔罗券",     ItemCategory.Consumable,    new Color(0.55f, 0.40f, 0.95f),"可触发额外塔罗抽取（Phase 预留）。"),
            ("travel_supply", "旅行补给",   ItemCategory.Consumable,    new Color(0.55f, 0.85f, 0.95f),"发起旅行指令时会优先消耗。"),
            ("souvenir_sea",  "海边纪念物", ItemCategory.TravelSouvenir,new Color(0.45f, 0.65f, 0.85f),"旅行带回的纪念物。"),
            ("coin_gold",     "金币",       ItemCategory.Currency,      new Color(1.0f, 0.85f, 0.20f), "通用货币。")
        };

        [MenuItem("Tools/Gemini-Lab/Author Item Catalog (10 placeholders)")]
        public static void Author()
        {
            EnsureFolder(DefFolder);
            EnsureFolder(IconFolder);

            var items = new List<ItemDefSO>();
            foreach (var (id, zh, cat, color, tooltip) in Items)
            {
                string iconPath = Path.Combine(IconFolder, $"{id}.png").Replace('\\', '/');
                if (!File.Exists(iconPath))
                {
                    GeneratePlaceholderIcon(iconPath, color);
                }
            }
            AssetDatabase.Refresh();
            foreach (var (id, zh, cat, color, tooltip) in Items)
            {
                string iconPath = Path.Combine(IconFolder, $"{id}.png").Replace('\\', '/');
                ConfigureSpriteImport(iconPath);
            }

            foreach (var (id, zh, cat, color, tooltip) in Items)
            {
                string defPath = Path.Combine(DefFolder, $"{id}.asset").Replace('\\', '/');
                var def = AssetDatabase.LoadAssetAtPath<ItemDefSO>(defPath);
                if (def == null)
                {
                    def = ScriptableObject.CreateInstance<ItemDefSO>();
                    AssetDatabase.CreateAsset(def, defPath);
                }

                string iconPath = Path.Combine(IconFolder, $"{id}.png").Replace('\\', '/');
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

                var so = new SerializedObject(def);
                so.FindProperty("_id").stringValue = id;
                so.FindProperty("_displayNameZh").stringValue = zh;
                so.FindProperty("_category").enumValueIndex = (int)cat;
                so.FindProperty("_stackable").boolValue = cat != ItemCategory.TravelSouvenir;
                so.FindProperty("_maxPerStack").intValue = cat == ItemCategory.Currency ? 9999 : 99;
                so.FindProperty("_tooltip").stringValue = tooltip;
                so.FindProperty("_icon").objectReferenceValue = sprite;
                so.ApplyModifiedPropertiesWithoutUndo();

                items.Add(def);
            }

            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalogSO>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ItemCatalogSO>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.SetItemsEditorOnly(items);
            EditorUtility.SetDirty(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ItemCatalog] 已生成 {items.Count} 个 ItemDefSO + ItemCatalog.asset");
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

        private static void GeneratePlaceholderIcon(string assetPath, Color color)
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool border = x < 4 || x >= size - 4 || y < 4 || y >= size - 4;
                    tex.SetPixel(x, y, border ? new Color(0, 0, 0, 0.6f) : color);
                }
            }
            tex.Apply();
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void ConfigureSpriteImport(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = 100;
            importer.SaveAndReimport();
        }
    }
}
#endif
