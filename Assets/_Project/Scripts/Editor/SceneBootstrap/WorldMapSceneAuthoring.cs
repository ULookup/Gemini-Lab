#nullable enable
#if UNITY_EDITOR
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
    /// 在 WorldMap_Main.unity 中搭建横板摄像头 + 返回按钮骨架。
    /// </summary>
    public static class WorldMapSceneAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";

        [MenuItem("Tools/Gemini-Lab/Author WorldMap Scene")]
        public static void Author()
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                var created = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(created, ScenePath);
            }

            UnityEngine.SceneManagement.Scene scene;
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

            // Camera with WorldMapCameraController
            var camGo = new GameObject("Main Camera");
            camGo.transform.SetParent(sceneRoot.transform, false);
            camGo.transform.localPosition = new Vector3(0, 0, -10);
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.78f, 0.90f, 1f);
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<WorldMapCameraController>();

            // EventSystem
            var esGo = new GameObject("EventSystem");
            esGo.transform.SetParent(sceneRoot.transform, false);
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();

            // Placeholder ground sprite (visual cue only; real art later)
            var groundGo = new GameObject("GroundPlaceholder");
            groundGo.transform.SetParent(sceneRoot.transform, false);
            groundGo.transform.localPosition = new Vector3(0, -3, 0);
            var sr = groundGo.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.4f, 0.6f, 0.3f, 1f);
            sr.size = new Vector2(40, 2);
            sr.drawMode = SpriteDrawMode.Sliced;
            // Use Unity built-in UI sprite as placeholder fill
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            // Zone marker: Garden
            var gardenGo = new GameObject("GardenZone");
            gardenGo.transform.SetParent(sceneRoot.transform, false);
            gardenGo.transform.localPosition = new Vector3(10, 0, 0);
            var gardenLabelGo = new GameObject("Label");
            gardenLabelGo.transform.SetParent(gardenGo.transform, false);
            var gardenTmp = gardenLabelGo.AddComponent<TextMeshPro>();
            gardenTmp.text = "Garden";
            gardenTmp.fontSize = 4;
            gardenTmp.alignment = TextAlignmentOptions.Center;
            gardenTmp.color = Color.white;

            // Return button UI
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(sceneRoot.transform, false);
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
            tmp.text = "Return";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 28;
            tmp.color = Color.white;

            // Exit component hosts the callback target
            var exitGo = new GameObject("WorldMapExit");
            exitGo.transform.SetParent(sceneRoot.transform, false);
            var exit = exitGo.AddComponent<WorldMapExit>();

            UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, exit.ReturnToApartment);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[WorldMapSceneAuthoring] Scene authoring complete");
        }
    }
}
#endif
