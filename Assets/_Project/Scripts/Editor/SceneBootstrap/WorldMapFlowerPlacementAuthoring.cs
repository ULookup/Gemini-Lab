#nullable enable
#if UNITY_EDITOR
using System.Collections.Generic;
using GeminiLab.Modules.WorldMap;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 为 WorldMap 创建花朵自由摆放的 Scene / Inspector 结构。
    /// 幂等执行，不覆盖已存在的用户布局。
    /// </summary>
    public static class WorldMapFlowerPlacementAuthoring
    {
        public static void Patch(GameObject canvasGo, int uiLayer)
        {
            var controller = canvasGo.GetComponent<WorldMapFlowerPlacementController>();
            if (controller == null) controller = canvasGo.AddComponent<WorldMapFlowerPlacementController>();

            var openButton = EnsureButton(canvasGo.transform, uiLayer, "Btn_FlowerPlacement", "布置",
                new Vector2(24, 24), new Vector2(160, 52), new Color(0.16f, 0.36f, 0.24f, 0.96f),
                new Vector2(0f, 0f));

            var panel = EnsurePanel(canvasGo.transform, uiLayer);
            var flowerButtons = EnsureChild(panel.transform, "FlowerButtons", uiLayer);
            ConfigureRect(flowerButtons, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(24, 52), new Vector2(-24, -64), Vector2.zero);
            var grid = GetOrAdd<GridLayoutGroup>(flowerButtons);
            grid.cellSize = new Vector2(132, 76);
            grid.spacing = new Vector2(8, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 8;
            grid.childAlignment = TextAnchor.UpperCenter;

            var singleButton = EnsureButton(panel.transform, uiLayer, "SingleButton", "单花",
                new Vector2(-170, 24), new Vector2(120, 42), new Color(0.22f, 0.32f, 0.48f, 1f),
                new Vector2(1f, 0f));
            var clusterButton = EnsureButton(panel.transform, uiLayer, "ClusterButton", "花丛",
                new Vector2(-302, 24), new Vector2(120, 42), new Color(0.34f, 0.26f, 0.48f, 1f),
                new Vector2(1f, 0f));
            var cancelButton = EnsureButton(panel.transform, uiLayer, "CancelButton", "取消",
                new Vector2(-434, 24), new Vector2(120, 42), new Color(0.42f, 0.24f, 0.24f, 1f),
                new Vector2(1f, 0f));

            var statusBar = EnsureStatusBar(canvasGo.transform, uiLayer);

            var root = GameObject.Find("WorldMapPlacedFlowers");
            if (root == null)
            {
                root = new GameObject("WorldMapPlacedFlowers");
                root.transform.position = Vector3.zero;
            }

            var surface = EnsurePlacementBounds();
            var so = new SerializedObject(controller);
            SetObject(so, "_openButton", openButton);
            SetObject(so, "_inventoryPanel", panel);
            SetObject(so, "_flowerButtonRoot", flowerButtons.transform);
            SetObject(so, "_singleButton", singleButton);
            SetObject(so, "_clusterButton", clusterButton);
            SetObject(so, "_cancelButton", cancelButton);
            SetObject(so, "_placementStatusBar", statusBar);
            SetObject(so, "_placementSurface", surface);
            SetObject(so, "_placementRoot", root.transform);
            var gridReference = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/_Project/Art/WorldMap/garden/中景/花丛.png");
            SetObject(so, "_gridReferenceSprite", gridReference);
            ConfigureOptions(so, flowerButtons.transform, uiLayer);
            so.FindProperty("_cellSize")!.vector2Value = gridReference != null
                ? gridReference.bounds.size
                : new Vector2(4.01f, 2.24f);
            so.FindProperty("_useSurfaceBoundsAsGridOrigin")!.boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
            EditorUtility.SetDirty(controller);
            Debug.Log("[WorldMapFlowerPlacement] 已创建二维花朵摆放结构，正式花朵资源等待 Inspector 接入");
        }

        private static BoxCollider2D EnsurePlacementBounds()
        {
            var boundsGo = GameObject.Find("FlowerPlacementBounds");
            if (boundsGo == null)
            {
                boundsGo = new GameObject("FlowerPlacementBounds");
                boundsGo.transform.position = new Vector3(0f, -3f, 0f);
                var box = boundsGo.AddComponent<BoxCollider2D>();
                // 4 个花丛.png 高度的作者化草地范围；可在 Inspector 中直接调整。
                box.size = new Vector2(36f, 8.96f);
                box.isTrigger = true;
                box.enabled = false;
                return box;
            }

            var existing = boundsGo.GetComponent<BoxCollider2D>();
            if (existing != null) return existing;

            var created = boundsGo.AddComponent<BoxCollider2D>();
            created.size = new Vector2(36f, 8.96f);
            created.isTrigger = true;
            created.enabled = false;
            return created;
        }

        private static void ConfigureOptions(SerializedObject so, Transform buttonRoot, int uiLayer)
        {
            var ids = new List<string> { "Flower_01", "Flower_02", "Flower_03", "Flower_04", "Flower_05", "Flower_06" };
            var options = so.FindProperty("_options");
            options.arraySize = ids.Count;

            while (buttonRoot.childCount > ids.Count)
                UnityEngine.Object.DestroyImmediate(buttonRoot.GetChild(buttonRoot.childCount - 1).gameObject);

            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                var element = options.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_id").stringValue = id;
                element.FindPropertyRelative("_displayName").stringValue = $"花朵 {i + 1}";
                element.FindPropertyRelative("_singleFootprint").vector2IntValue = Vector2Int.one;
                element.FindPropertyRelative("_clusterFootprint").vector2IntValue = new Vector2Int(2, 2);

                GameObject buttonGo;
                if (i < buttonRoot.childCount) buttonGo = buttonRoot.GetChild(i).gameObject;
                else buttonGo = CreateButton(buttonRoot, uiLayer, $"FlowerOption_{i}");

                var image = GetOrAdd<Image>(buttonGo);
                image.sprite = null;
                image.color = new Color(0.24f, 0.3f, 0.34f, 1f);
                var label = EnsureChild(buttonGo.transform, "Label", uiLayer);
                ConfigureRect(label, new Vector2(0f, 0f), new Vector2(1f, 0f),
                    Vector2.zero, new Vector2(0f, 22f), Vector2.zero);
                var tmp = GetOrAdd<TextMeshProUGUI>(label);
                tmp.text = $"花朵 {i + 1}\n待接入资源";
                tmp.fontSize = 12;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
            }
        }

        private static GameObject EnsureStatusBar(Transform canvas, int layer)
        {
            var bar = EnsureChild(canvas, "FlowerPlacementStatusBar", layer);
            ConfigureRect(bar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -72f), new Vector2(560, 48), new Vector2(0.5f, 1f));
            var image = GetOrAdd<Image>(bar);
            image.color = new Color(0.08f, 0.12f, 0.14f, 0.94f);
            var textGo = EnsureChild(bar.transform, "StatusText", layer);
            ConfigureRect(textGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var statusText = GetOrAdd<TextMeshProUGUI>(textGo);
            statusText.fontSize = 18;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.color = Color.white;
            return bar;
        }

        private static GameObject EnsurePanel(Transform canvas, int layer)
        {
            var panel = EnsureChild(canvas, "FlowerPlacementPanel", layer);
            ConfigureRect(panel, new Vector2(0f, 0f), new Vector2(1f, 0f),
                Vector2.zero, new Vector2(0f, 300f), Vector2.zero);
            var image = GetOrAdd<Image>(panel);
            image.color = new Color(0.06f, 0.1f, 0.12f, 0.96f);
            return panel;
        }

        private static Button EnsureButton(Transform parent, int layer, string name, string label,
            Vector2 anchoredPosition, Vector2 size, Color color, Vector2 anchor)
        {
            var go = EnsureChild(parent, name, layer);
            ConfigureRect(go, anchor, anchor, anchoredPosition, size, anchor);
            var image = GetOrAdd<Image>(go);
            image.color = color;
            var button = GetOrAdd<Button>(go);
            var labelGo = EnsureChild(go.transform, "Label", layer);
            ConfigureRect(labelGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var tmp = GetOrAdd<TextMeshProUGUI>(labelGo);
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return button;
        }

        private static GameObject CreateButton(Transform parent, int layer, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = layer;
            GetOrAdd<RectTransform>(go);
            GetOrAdd<Image>(go);
            GetOrAdd<Button>(go);
            return go;
        }

        private static GameObject EnsureChild(Transform parent, string name, int layer)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = layer;
            return go;
        }

        private static void ConfigureRect(GameObject go, Vector2 min, Vector2 max,
            Vector2 position, Vector2 size, Vector2 pivot)
        {
            var rt = GetOrAdd<RectTransform>(go);
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }

        private static void SetObject(SerializedObject so, string propertyName, UnityEngine.Object? value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null) prop.objectReferenceValue = value;
        }
    }
}
#endif
