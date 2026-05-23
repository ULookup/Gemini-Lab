#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.HubUI.Panels;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 把 Apartment 场景里的 Panel_Inventory / Panel_Collection 从 Stub 升级为真实 UI。
    /// 幂等（重跑清空 Content 再重建）。
    /// </summary>
    public static class InventoryCollectionPanelAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/Apartment/Apartment_Main.unity";

        [MenuItem("Tools/Gemini-Lab/Author Inventory + Collection Panels (Apartment)")]
        public static void Author()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            BuildInventoryPanel();
            BuildCollectionPanel();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[InventoryCollectionAuthoring] Apartment 的 Inventory / Collection 面板已升级");
        }

        private static void BuildInventoryPanel()
        {
            var panelRoot = GameObject.Find("Panel_Inventory");
            if (panelRoot == null)
            {
                Debug.LogError("[InventoryCollectionAuthoring] 未找到 Panel_Inventory");
                return;
            }

            var panel = panelRoot.GetComponent<InventoryPanelStub>();
            if (panel == null)
            {
                Debug.LogError("[InventoryCollectionAuthoring] Panel_Inventory 缺 InventoryPanelStub");
                return;
            }

            int layer = panelRoot.layer;
            var content = EnsureContent(panelRoot, layer);
            ClearChildren(content.transform);
            AttachContentBg(content);

            AddTitle(content, layer, "物品栏");

            // Grid
            var gridGo = CreateChild(content, layer, "Grid",
                new Vector2(0, 0), new Vector2(0.65f, 1), new Vector2(88, 16), new Vector2(-8, -76));
            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(96, 96);
            grid.spacing = new Vector2(8, 8);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.padding = new RectOffset(4, 4, 4, 4);

            // Tooltip panel
            var tip = CreateChild(content, layer, "Tooltip",
                new Vector2(0.66f, 0), new Vector2(1, 1), new Vector2(80, 16), new Vector2(-16, -76));
            var tipBg = tip.AddComponent<Image>();
            tipBg.color = new Color(0, 0, 0, 0.35f);

            var tipName = AddText(tip, layer, "Name", 22,
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(16, -40), new Vector2(-16, -8), TextAlignmentOptions.Left);
            var tipCat = AddText(tip, layer, "Category", 16,
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(16, -64), new Vector2(-16, -40), TextAlignmentOptions.Left);
            tipCat.color = new Color(0.85f, 0.9f, 1f, 0.8f);
            var tipText = AddText(tip, layer, "Text", 14,
                new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(16, 16), new Vector2(-16, -72), TextAlignmentOptions.TopLeft);
            tipText.enableWordWrapping = true;

            var so = new SerializedObject(panel);
            so.FindProperty("_content").objectReferenceValue = content;
            so.FindProperty("_gridRoot").objectReferenceValue = gridGo.transform;
            so.FindProperty("_tooltipRoot").objectReferenceValue = tip;
            so.FindProperty("_tooltipName").objectReferenceValue = tipName;
            so.FindProperty("_tooltipCategory").objectReferenceValue = tipCat;
            so.FindProperty("_tooltipText").objectReferenceValue = tipText;
            so.ApplyModifiedProperties();

            content.SetActive(false);
        }

        private static void BuildCollectionPanel()
        {
            var panelRoot = GameObject.Find("Panel_Collection");
            if (panelRoot == null)
            {
                Debug.LogError("[InventoryCollectionAuthoring] 未找到 Panel_Collection");
                return;
            }

            var panel = panelRoot.GetComponent<CollectionPanelStub>();
            if (panel == null)
            {
                Debug.LogError("[InventoryCollectionAuthoring] Panel_Collection 缺 CollectionPanelStub");
                return;
            }

            int layer = panelRoot.layer;
            var content = EnsureContent(panelRoot, layer);
            ClearChildren(content.transform);
            AttachContentBg(content);

            AddTitle(content, layer, "收藏");

            // Tabs
            var tabsGo = CreateChild(content, layer, "Tabs",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(104, -116), new Vector2(-32, -64));
            var hlg = tabsGo.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 12;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            var tabTravel = MakeTabButton(tabsGo, layer, "Tab_Travel", "旅行照片");
            var tabTarot = MakeTabButton(tabsGo, layer, "Tab_Tarot", "塔罗记录");
            var tabGarden = MakeTabButton(tabsGo, layer, "Tab_Garden", "花园收获");

            // Grid
            var gridHolder = CreateChild(content, layer, "GridHolder",
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(104, 32), new Vector2(-32, -128));
            var gridBg = gridHolder.AddComponent<Image>();
            gridBg.color = new Color(0, 0, 0, 0.25f);
            var gridRoot = CreateChild(gridHolder, layer, "Grid",
                Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -12));
            var grid = gridRoot.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(240, 96);
            grid.spacing = new Vector2(12, 12);
            grid.padding = new RectOffset(4, 4, 4, 4);

            // Empty text
            var emptyText = AddText(gridHolder, layer, "EmptyHint", 18,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            emptyText.text = "（此页签暂无收藏）";
            emptyText.color = new Color(1, 1, 1, 0.55f);

            var so = new SerializedObject(panel);
            so.FindProperty("_content").objectReferenceValue = content;
            so.FindProperty("_tabTravel").objectReferenceValue = tabTravel;
            so.FindProperty("_tabTarot").objectReferenceValue = tabTarot;
            so.FindProperty("_tabGarden").objectReferenceValue = tabGarden;
            so.FindProperty("_gridRoot").objectReferenceValue = gridRoot.transform;
            so.FindProperty("_emptyHint").objectReferenceValue = emptyText;
            so.ApplyModifiedProperties();

            content.SetActive(false);
        }

        // ---- helpers ----

        private static GameObject EnsureContent(GameObject panelRoot, int layer)
        {
            var tr = panelRoot.transform.Find("Content");
            if (tr != null) return tr.gameObject;

            var go = new GameObject("Content");
            go.transform.SetParent(panelRoot.transform, false);
            go.layer = layer;
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
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

        private static Button MakeTabButton(GameObject parent, int layer, string name, string text)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.layer = layer;
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.30f, 0.45f, 1f);
            var btn = go.AddComponent<Button>();

            var lbl = AddText(go, layer, "Label", 20, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            lbl.text = text;
            return btn;
        }
    }
}
#endif
