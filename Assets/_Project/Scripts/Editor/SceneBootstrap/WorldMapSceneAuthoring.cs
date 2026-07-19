#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.WorldMap;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// WorldMap_Main.unity 一站式搭建工具。
    /// 包含：相机、双层地面、双宠+RandomWander、场景物（小木屋/祈愿树/邮箱）、
    /// 九宫格花圃（GardenPlotView）、返回按钮、移动边界。
    /// </summary>
    public static class WorldMapSceneAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";
        private const string AngelPrefabPath = "Assets/_Project/Prefabs/Pet/Pet_Angel.prefab";
        private const string DevilPrefabPath = "Assets/_Project/Prefabs/Pet/Pet_Devil.prefab";
        private const float GroundWidth = 40f;
        private const float GroundY = -3f;

        [MenuItem("Tools/Gemini-Lab/Author WorldMap Scene")]
        public static void Author()
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                var created = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(created, ScenePath);
            }

            Scene scene;
            if (EditorSceneManager.GetActiveScene().path == ScenePath)
            {
                scene = EditorSceneManager.GetActiveScene();
            }
            else
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            GameObject? sceneRoot = GameObject.Find("_SceneRoot");
            if (sceneRoot == null)
            {
                sceneRoot = new GameObject("_SceneRoot");
                SceneManager.MoveGameObjectToScene(sceneRoot, scene);
            }

            for (int i = sceneRoot.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(sceneRoot.transform.GetChild(i).gameObject);
            }

            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer < 0) uiLayer = 5;

            CreateCamera(sceneRoot.transform);
            CreateEventSystem(sceneRoot.transform);
            CreateGround(sceneRoot.transform);
            CreateMovementBounds(sceneRoot.transform);
            CreatePets(sceneRoot.transform);
            CreateSceneObjects(sceneRoot.transform);
            CreateGardenZone(sceneRoot.transform);
            CreateReturnButton(sceneRoot.transform, uiLayer);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[WorldMapSceneAuthoring] 场景搭建完成：相机/地面/双宠/场景物(木屋/树/邮箱)/九宫格花圃/返回按钮。");
        }

        private static void CreateCamera(Transform parent)
        {
            var camGo = new GameObject("Main Camera");
            camGo.transform.SetParent(parent, false);
            camGo.transform.localPosition = new Vector3(0, 2.4f, -10);
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.78f, 0.90f, 1f);
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<WorldMapCameraController>();
        }

        private static void CreateEventSystem(Transform parent)
        {
            var esGo = new GameObject("EventSystem");
            esGo.transform.SetParent(parent, false);
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
        }

        private static void CreateGround(Transform parent)
        {
            // 深层泥土
            var dirtGo = new GameObject("Ground_Dirt");
            dirtGo.transform.SetParent(parent, false);
            dirtGo.transform.localPosition = new Vector3(0, GroundY - 0.6f, 0);
            var dirtSr = dirtGo.AddComponent<SpriteRenderer>();
            dirtSr.color = new Color(0.3f, 0.2f, 0.12f, 1f);
            dirtSr.size = new Vector2(GroundWidth, 1.2f);
            dirtSr.drawMode = SpriteDrawMode.Sliced;
            dirtSr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            dirtSr.sortingLayerName = "Floor";

            // 草地表层
            var grassGo = new GameObject("Ground_Grass");
            grassGo.transform.SetParent(parent, false);
            grassGo.transform.localPosition = new Vector3(0, GroundY, 0);
            var grassSr = grassGo.AddComponent<SpriteRenderer>();
            grassSr.color = new Color(0.4f, 0.6f, 0.3f, 1f);
            grassSr.size = new Vector2(GroundWidth, 1.8f);
            grassSr.drawMode = SpriteDrawMode.Sliced;
            grassSr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            grassSr.sortingLayerName = "Floor";
            grassSr.sortingOrder = 1;
        }

        private static void CreateMovementBounds(Transform parent)
        {
            // 共享移动边界：覆盖地面可行走区域
            // 窄 Y 轴范围（0.3），桌宠在地面水平线上左右移动，几乎不上下偏移
            var boundsGo = new GameObject("PetMovementBounds");
            boundsGo.transform.SetParent(parent, false);
            boundsGo.transform.localPosition = new Vector3(0, GroundY + 1.2f, 0);
            var col = boundsGo.AddComponent<BoxCollider2D>();
            col.size = new Vector2(GroundWidth - 4f, 0.3f);
            col.isTrigger = true;
            col.enabled = false; // 仅作为数据标记，不参与物理

            var so = new SerializedObject(col);
            so.Update();
            so.ApplyModifiedProperties();
        }

        private static void CreatePets(Transform parent)
        {
            CreatePetFromPrefab(AngelPrefabPath, "Pet_Angel", PetId.Angel,
                new Vector3(-3f, GroundY + 1.25f, 0f), parent);
            CreatePetFromPrefab(DevilPrefabPath, "Pet_Devil", PetId.Devil,
                new Vector3(3f, GroundY + 1.25f, 0f), parent);
        }

        private static void CreatePetFromPrefab(string prefabPath, string name, PetId petId, Vector3 pos, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[WorldMapSceneAuthoring] 未找到 prefab: {prefabPath}");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = pos;

            // 修正 PetId
            var ctrl = instance.GetComponent<PetController>();
            if (ctrl != null)
            {
                var so = new SerializedObject(ctrl);
                var prop = so.FindProperty("_petId");
                if (prop != null)
                {
                    prop.intValue = (int)petId;
                    so.ApplyModifiedProperties();
                }
            }

            // PetPlayerInputController：横板模式只允许水平移动
            var input = instance.GetComponent<PetPlayerInputController>();
            if (input != null)
            {
                var inputSo = new SerializedObject(input);
                inputSo.FindProperty("_horizontalOnly").boolValue = true;
                inputSo.ApplyModifiedProperties();
            }

            // RandomWander：横板模式，从 PetMovementBounds 读取范围
            var wander = instance.GetComponent<RandomWander>();
            if (wander == null) wander = instance.AddComponent<RandomWander>();
            var boundsGo = GameObject.Find("PetMovementBounds");
            if (boundsGo != null)
            {
                var col = boundsGo.GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    Vector2 center = (Vector2)boundsGo.transform.position + col.offset;
                    Vector2 halfSize = col.size * 0.5f;
                    var wanderSo = new SerializedObject(wander);
                    wanderSo.FindProperty("_boundsMin").vector2Value = center - halfSize;
                    wanderSo.FindProperty("_boundsMax").vector2Value = center + halfSize;
                    wanderSo.FindProperty("_moveSpeed").floatValue = 1.2f;
                    wanderSo.FindProperty("_horizontalOnly").boolValue = true;
                    wanderSo.ApplyModifiedProperties();
                }
            }
        }

        private static void CreateSceneObjects(Transform parent)
        {
            // 小木屋 — 场景左侧
            CreatePlaceholderObject(parent, "Cabin", new Vector3(-12f, GroundY - 0.2f, 0),
                new Vector2(2.5f, 3f), new Color(0.6f, 0.45f, 0.25f, 1f),
                "小木屋", "小木屋 · 功能待接入");

            // 祈愿树 — 场景左侧
            CreatePlaceholderObject(parent, "WishingTree", new Vector3(-6f, GroundY + 0.2f, 0),
                new Vector2(1.8f, 3.5f), new Color(0.35f, 0.6f, 0.25f, 1f),
                "祈愿树", "祈愿树 · 功能待接入");

            // 邮箱 — 场景右侧
            CreatePlaceholderObject(parent, "Mailbox", new Vector3(6f, GroundY + 0.5f, 0),
                new Vector2(1f, 1.5f), new Color(0.7f, 0.3f, 0.25f, 1f),
                "邮箱", "邮箱 · 功能待接入");
        }

        private static void CreatePlaceholderObject(Transform parent, string name, Vector3 pos,
            Vector2 size, Color color, string label, string clickMessage)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.color = color;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.sortingLayerName = "Furniture";
            sr.sortingOrder = 1;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = true;

            var clickable = go.AddComponent<ClickableSceneObject>();
            var clkSo = new SerializedObject(clickable);
            clkSo.FindProperty("_displayName").stringValue = label;
            clkSo.FindProperty("_clickMessage").stringValue = clickMessage;
            clkSo.ApplyModifiedProperties();

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.transform.localPosition = new Vector3(0, size.y * 0.5f + 0.5f, 0);
            var tmp = labelGo.AddComponent<TextMeshPro>();
            tmp.text = label;
            tmp.fontSize = 3;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.sortingOrder = 11;
        }

        private static void CreateGardenZone(Transform parent)
        {
            // 天使入口
            CreateEmotionEntry(parent, "EmotionEntry_Angel", "angel", "天使花园",
                new Vector3(-4f, GroundY + 0.8f, 0),
                new Color(0.95f, 0.85f, 0.45f, 0.95f));
            // 恶魔入口
            CreateEmotionEntry(parent, "EmotionEntry_Demon", "demon", "恶魔花园",
                new Vector3(4f, GroundY + 0.8f, 0),
                new Color(0.7f, 0.18f, 0.25f, 0.95f));

            // 共享九宫格花圃可视化（纯视觉，不挂 WorldMapGardenZone）
            var plotsGo = new GameObject("GardenPlots");
            plotsGo.transform.SetParent(parent, false);
            plotsGo.transform.localPosition = new Vector3(0, GroundY + 1.2f, 0);

            var bgSr = plotsGo.AddComponent<SpriteRenderer>();
            bgSr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            bgSr.color = new Color(0.35f, 0.28f, 0.18f, 0.7f);
            bgSr.drawMode = SpriteDrawMode.Sliced;
            bgSr.size = new Vector2(5.5f, 5.5f);
            bgSr.sortingLayerName = "Furniture";
            bgSr.sortingOrder = 4;

            var col = plotsGo.AddComponent<BoxCollider2D>();
            col.size = new Vector2(5.5f, 5.5f);
            col.isTrigger = true;

            var plotView = plotsGo.AddComponent<GardenPlotView>();
            var pvSo = new SerializedObject(plotView);
            pvSo.FindProperty("_cellSize").floatValue = 1.2f;
            pvSo.FindProperty("_cellGap").floatValue = 0.18f;
            pvSo.ApplyModifiedProperties();

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(plotsGo.transform, false);
            labelGo.transform.localPosition = new Vector3(0, 3.2f, 0);
            var tmp = labelGo.AddComponent<TextMeshPro>();
            tmp.text = "九宫格花圃";
            tmp.fontSize = 3;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.sortingOrder = 11;
        }

        private static void CreateEmotionEntry(Transform parent, string name, string owner, string label,
            Vector3 pos, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(3f, 3f);
            sr.sortingLayerName = "Furniture";
            sr.sortingOrder = 4;
            sr.color = color;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3f, 3f);
            col.isTrigger = true;

            var zone = go.AddComponent<WorldMapGardenZone>();
            var zoneSo = new SerializedObject(zone);
            zoneSo.FindProperty("_owner").stringValue = owner;
            zoneSo.ApplyModifiedProperties();

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.transform.localPosition = new Vector3(0, 1.7f, 0);
            var tmp = labelGo.AddComponent<TextMeshPro>();
            tmp.text = label;
            tmp.fontSize = 3;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.sortingOrder = 11;
        }

        private static void CreateReturnButton(Transform parent, int uiLayer)
        {
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(parent, false);
            canvasGo.layer = uiLayer;
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var returnGo = new GameObject("Btn_ReturnApartment");
            returnGo.transform.SetParent(canvasGo.transform, false);
            returnGo.layer = uiLayer;
            var rt = returnGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(32, -32);
            rt.sizeDelta = new Vector2(200, 72);
            var img = returnGo.AddComponent<Image>();
            img.color = new Color(0.2f, 0.25f, 0.35f, 1f);
            var btn = returnGo.AddComponent<Button>();

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(returnGo.transform, false);
            labelGo.layer = uiLayer;
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "返回公寓";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 28;
            tmp.color = Color.white;

            var exitGo = new GameObject("WorldMapExit");
            exitGo.transform.SetParent(parent, false);
            var exit = exitGo.AddComponent<WorldMapExit>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, exit.ReturnToApartment);
        }
    }
}
#endif
