#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.HubUI.Panels;
using GeminiLab.Modules.Inventory;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 把 Apartment 场景里的 Panel_Garden 从 Stub 升级为真实 UI。
    /// - 标题 "花园"
    /// - 左侧 3×3 地块 Grid
    /// - 右侧种子选择条（横向）+ 提示文案
    /// 幂等：重跑清空 Content 再重建。
    /// </summary>
    public static class GardenPanelAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/Apartment/Apartment_Main.unity";
        private const string ItemCatalogPath = "Assets/_Project/ScriptableObjects/InventoryConfig/ItemCatalog.asset";

        [MenuItem("Tools/Gemini-Lab/Author Garden Panel (Apartment)")]
        public static void Author()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var panelRoot = GameObject.Find("Panel_Garden");
            if (panelRoot == null)
            {
                Debug.LogError("[GardenPanelAuthoring] 未找到 Panel_Garden，请先跑 Author Apartment Sidebar");
                return;
            }

            var panel = panelRoot.GetComponent<GardenPanelStub>();
            if (panel == null)
            {
                Debug.LogError("[GardenPanelAuthoring] Panel_Garden 缺 GardenPanelStub");
                return;
            }

            int layer = panelRoot.layer;
            var content = EnsureContent(panelRoot, layer);
            ClearChildren(content.transform);
            AttachContentBg(content);

            AddTitle(content, layer, "花园");

            // 左侧 3×3 地块
            var gridHolder = CreateChild(content, layer, "GridHolder",
                new Vector2(0, 0), new Vector2(0.6f, 1), new Vector2(104, 32), new Vector2(-12, -76));
            var gridBg = gridHolder.AddComponent<Image>();
            gridBg.color = new Color(0f, 0f, 0f, 0.25f);

            var gridRoot = CreateChild(gridHolder, layer, "Grid",
                Vector2.zero, Vector2.one, new Vector2(88, 16), new Vector2(-16, -16));
            var grid = gridRoot.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(128, 128);
            grid.spacing = new Vector2(10, 10);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.padding = new RectOffset(4, 4, 4, 4);

            // 右侧：种子条 + 提示
            var rightHolder = CreateChild(content, layer, "SeedHolder",
                new Vector2(0.6f, 0), new Vector2(1, 1), new Vector2(84, 32), new Vector2(-32, -76));
            var rightBg = rightHolder.AddComponent<Image>();
            rightBg.color = new Color(0f, 0f, 0f, 0.25f);

            var seedTitle = AddText(rightHolder, layer, "SeedTitle", 22,
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(88, -48), new Vector2(-16, -8), TextAlignmentOptions.Center);
            seedTitle.text = "种子";
            seedTitle.color = new Color(0.95f, 0.9f, 0.7f, 1f);

            var seedBar = CreateChild(rightHolder, layer, "SeedBar",
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(84, -180), new Vector2(-12, -52));
            var hlg = seedBar.AddComponent<GridLayoutGroup>();
            hlg.cellSize = new Vector2(72, 72);
            hlg.spacing = new Vector2(8, 8);
            hlg.startCorner = GridLayoutGroup.Corner.UpperLeft;
            hlg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            hlg.constraintCount = 3;

            var hint = AddText(rightHolder, layer, "Hint", 14,
                new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(84, 16), new Vector2(-12, -192), TextAlignmentOptions.TopLeft);
            hint.enableWordWrapping = true;
            hint.color = new Color(1f, 1f, 1f, 0.75f);
            hint.text = "点地块种下；成熟后点收获";

            // Bind
            var itemCatalog = AssetDatabase.LoadAssetAtPath<ItemCatalogSO>(ItemCatalogPath);
            var so = new SerializedObject(panel);
            so.FindProperty("_content").objectReferenceValue = content;
            so.FindProperty("_gridRoot").objectReferenceValue = gridRoot.transform;
            so.FindProperty("_seedBarRoot").objectReferenceValue = seedBar.transform;
            so.FindProperty("_seedHintText").objectReferenceValue = hint;
            so.FindProperty("_itemCatalog").objectReferenceValue = itemCatalog;
            so.ApplyModifiedProperties();

            content.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[GardenPanelAuthoring] Apartment Panel_Garden 已升级");
        }

        // ---- helpers（与 InventoryCollectionPanelAuthoring 保持一致风格）----
        private static GameObject EnsureContent(GameObject panelRoot, int layer)
        {
            var tr = panelRoot.transform.Find("Content");
            if (tr != null) return tr.gameObject;

            var go = new GameObject("Content");
            go.transform.SetParent(panelRoot.transform, false);
            go.layer = layer;
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return go;
        }

        private static void AttachContentBg(GameObject content)
        {
            var img = content.GetComponent<Image>();
            if (img == null) img = content.AddComponent<Image>();
            img.color = new Color(0.12f, 0.14f, 0.2f, 0.97f);
        }

        private static void ClearChildren(Transform tr)
        {
            for (int i = tr.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(tr.GetChild(i).gameObject);
            }
        }

        private static void AddTitle(GameObject content, int layer, string text)
        {
            var title = AddText(content, layer, "Title", 32,
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -56), new Vector2(0, -8), TextAlignmentOptions.Center);
            title.text = text;
            title.color = new Color(0.95f, 0.9f, 0.7f, 1f);
        }

        private static GameObject CreateChild(GameObject parent, int layer, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.layer = layer;
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return go;
        }

        private static TMP_Text AddText(GameObject parent, int layer, string name, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, TextAlignmentOptions align)
        {
            var go = CreateChild(parent, layer, name, anchorMin, anchorMax, offsetMin, offsetMax);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = align;
            go.AddComponent<GeminiLab.Modules.UI.Catalogs.TMPFontBinder>();
            return tmp;
        }
    }
}
#endif
