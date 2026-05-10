#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.MainMenu;
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
    /// 一次性脚本：在 MainMenu.unity 中构建场景根节点、Camera、EventSystem、Canvas 与三个入口按钮。
    /// 通过 Tools 菜单触发；作为框架搭建的 authoring 工具，不参与运行时。
    /// </summary>
    public static class MainMenuSceneAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/MainMenu/MainMenu.unity";

        [MenuItem("Tools/Gemini-Lab/Author MainMenu Scene")]
        public static void Author()
        {
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

            // Camera
            var camGo = new GameObject("Main Camera");
            camGo.transform.SetParent(sceneRoot.transform, false);
            camGo.transform.localPosition = new Vector3(0, 0, -10);
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
            camGo.AddComponent<AudioListener>();

            // EventSystem
            var esGo = new GameObject("EventSystem");
            esGo.transform.SetParent(sceneRoot.transform, false);
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();

            // Canvas
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

            // Buttons root
            var btnRoot = new GameObject("MenuButtons");
            btnRoot.transform.SetParent(canvasGo.transform, false);
            btnRoot.layer = uiLayer;
            var btnRootRt = btnRoot.AddComponent<RectTransform>();
            btnRootRt.anchorMin = new Vector2(0.5f, 0.5f);
            btnRootRt.anchorMax = new Vector2(0.5f, 0.5f);
            btnRootRt.pivot = new Vector2(0.5f, 0.5f);
            btnRootRt.anchoredPosition = Vector2.zero;
            btnRootRt.sizeDelta = new Vector2(400, 500);
            var vlg = btnRoot.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 32;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            var startBtn = MakeButton(btnRoot, uiLayer, "Btn_Start", "Start");
            var savesBtn = MakeButton(btnRoot, uiLayer, "Btn_Saves", "Save Slots");
            var settingsBtn = MakeButton(btnRoot, uiLayer, "Btn_Settings", "Settings");

            // Controller
            var ctrlGo = new GameObject("MainMenuController");
            ctrlGo.transform.SetParent(sceneRoot.transform, false);
            var ctrl = ctrlGo.AddComponent<MainMenuController>();

            var so = new SerializedObject(ctrl);
            so.FindProperty("_startButton").objectReferenceValue = startBtn;
            so.FindProperty("_saveSlotsButton").objectReferenceValue = savesBtn;
            so.FindProperty("_settingsButton").objectReferenceValue = settingsBtn;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[MainMenuSceneAuthoring] Scene authoring complete");
        }

        private static Button MakeButton(GameObject parent, int uiLayer, string name, string labelText)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.layer = uiLayer;
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(320, 96);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.25f, 0.35f, 1f);
            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.4f, 0.55f, 1f);
            btn.colors = colors;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.layer = uiLayer;
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = labelText;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 36;
            tmp.color = Color.white;
            return btn;
        }
    }
}
#endif
