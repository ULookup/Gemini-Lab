#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.HubUI;
using GeminiLab.Modules.HubUI.Panels;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GeminiLab.Editor.SceneBootstrap
{
    public static class ApartmentSidebarAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/Apartment/Apartment_Main.unity";
        private const string SidebarRootName = "UI_Sidebar";

        [MenuItem("Tools/Gemini-Lab/Author Apartment Sidebar")]
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

            var existing = GameObject.Find(SidebarRootName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer < 0) uiLayer = 5;

            // Ensure EventSystem exists
            var es = Object.FindObjectOfType<EventSystem>();
            if (es == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }

            // Build Canvas host (or reuse existing)
            GameObject canvasGo = BuildOrGetSidebarCanvas(uiLayer);

            // --- SidebarOverlay sub-Canvas (higher sortingOrder, stays on top of panels) ---
            var overlayGo = new GameObject("SidebarOverlay");
            overlayGo.transform.SetParent(canvasGo.transform, false);
            overlayGo.layer = uiLayer;
            var overlayCanvas = overlayGo.AddComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 101;
            overlayGo.AddComponent<GraphicRaycaster>();

            // Sidebar panel + tabs (inside overlay)
            var sidebarGo = new GameObject("Sidebar");
            sidebarGo.transform.SetParent(overlayGo.transform, false);
            sidebarGo.layer = uiLayer;
            var sidebarRt = sidebarGo.AddComponent<RectTransform>();
            sidebarRt.anchorMin = new Vector2(0f, 0f);
            sidebarRt.anchorMax = new Vector2(0f, 1f);
            sidebarRt.pivot = new Vector2(0f, 0.5f);
            sidebarRt.anchoredPosition = new Vector2(0, 0);
            sidebarRt.sizeDelta = new Vector2(240, 0);
            var sidebarBg = sidebarGo.AddComponent<Image>();
            sidebarBg.color = new Color(0.1f, 0.12f, 0.18f, 0.85f);
            var vlg = sidebarGo.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 32, 16);
            vlg.spacing = 16;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            var toggleBtn = MakeTab(sidebarGo, uiLayer, "Btn_Toggle", "<<");
            var petStatusBtn = MakeTab(sidebarGo, uiLayer, "Btn_PetStatus", "Status");
            var tarotBtn = MakeTab(sidebarGo, uiLayer, "Btn_Tarot", "Tarot");
            var collectionBtn = MakeTab(sidebarGo, uiLayer, "Btn_Collection", "Collection");
            var inventoryBtn = MakeTab(sidebarGo, uiLayer, "Btn_Inventory", "Inventory");
            var gardenBtn = MakeTab(sidebarGo, uiLayer, "Btn_Garden", "Garden");

            var sidebar = sidebarGo.AddComponent<SidebarController>();
            var so = new SerializedObject(sidebar);
            so.FindProperty("_panelRoot").objectReferenceValue = sidebarRt;
            so.FindProperty("_toggleButton").objectReferenceValue = toggleBtn;
            so.FindProperty("_expandedX").floatValue = 0f;
            so.FindProperty("_collapsedX").floatValue = -200f;
            so.FindProperty("_tabPetStatus").objectReferenceValue = petStatusBtn;
            so.FindProperty("_tabTarot").objectReferenceValue = tarotBtn;
            so.FindProperty("_tabCollection").objectReferenceValue = collectionBtn;
            so.FindProperty("_tabInventory").objectReferenceValue = inventoryBtn;
            so.FindProperty("_tabGarden").objectReferenceValue = gardenBtn;
            so.ApplyModifiedProperties();

            // 5 panels — stretch fullscreen (anchor 0,0→1,1)
            CreateStubPanel<PetStatusPanelStub>(canvasGo, uiLayer, "Panel_PetStatus", "Pet Status (WIP)");
            CreateStubPanel<TarotPanelStub>(canvasGo, uiLayer, "Panel_Tarot", "Tarot (WIP)");
            CreateStubPanel<CollectionPanelStub>(canvasGo, uiLayer, "Panel_Collection", "Collection (WIP)");
            CreateStubPanel<InventoryPanelStub>(canvasGo, uiLayer, "Panel_Inventory", "Inventory (WIP)");
            CreateStubPanel<GardenPanelStub>(canvasGo, uiLayer, "Panel_Garden", "Garden (WIP)");

            // Portal to WorldMap (inside overlay, stays on top)
            var portalGo = GameObject.Find("UI_WorldMapPortal");
            if (portalGo != null) Object.DestroyImmediate(portalGo);
            portalGo = new GameObject("UI_WorldMapPortal");
            portalGo.transform.SetParent(overlayGo.transform, false);
            portalGo.layer = uiLayer;
            var portalRt = portalGo.AddComponent<RectTransform>();
            portalRt.anchorMin = new Vector2(1f, 1f);
            portalRt.anchorMax = new Vector2(1f, 1f);
            portalRt.pivot = new Vector2(1f, 1f);
            portalRt.anchoredPosition = new Vector2(-32, -32);
            portalRt.sizeDelta = new Vector2(220, 72);
            var portalImg = portalGo.AddComponent<Image>();
            portalImg.color = new Color(0.25f, 0.3f, 0.45f, 1f);
            var portalBtn = portalGo.AddComponent<Button>();

            var portalLabelGo = new GameObject("Label");
            portalLabelGo.transform.SetParent(portalGo.transform, false);
            portalLabelGo.layer = uiLayer;
            var plrt = portalLabelGo.AddComponent<RectTransform>();
            plrt.anchorMin = Vector2.zero; plrt.anchorMax = Vector2.one;
            plrt.offsetMin = Vector2.zero; plrt.offsetMax = Vector2.zero;
            var ptmp = portalLabelGo.AddComponent<TextMeshProUGUI>();
            ptmp.text = "→ World Map";
            ptmp.alignment = TextAlignmentOptions.Center;
            ptmp.fontSize = 24;
            ptmp.color = Color.white;

            var portalComp = portalGo.AddComponent<ApartmentToWorldMapPortal>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(portalBtn.onClick, portalComp.GoToWorldMap);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.LogWarning("[ApartmentSidebarAuthoring] Sidebar + 5 panels + portal 已重建（全屏面板 + 子 Canvas 分层）。" +
                             "各 Panel 现为占位 stub，如需恢复真实 UI 请依次重跑 " +
                             "Author Inventory + Collection Panels / Author Garden Panel。");
        }

        private static GameObject BuildOrGetSidebarCanvas(int uiLayer)
        {
            var canvasGo = GameObject.Find(SidebarRootName);
            if (canvasGo != null) return canvasGo;

            canvasGo = new GameObject(SidebarRootName);
            canvasGo.layer = uiLayer;
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            return canvasGo;
        }

        private static void CreateStubPanel<T>(GameObject parentCanvas, int uiLayer, string name, string labelText) where T : MonoBehaviour
        {
            var go = new GameObject(name);
            go.transform.SetParent(parentCanvas.transform, false);
            go.layer = uiLayer;
            var rt = go.AddComponent<RectTransform>();
            // stretch fullscreen
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(go.transform, false);
            contentGo.layer = uiLayer;
            var crt = contentGo.AddComponent<RectTransform>();
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
            var img = contentGo.AddComponent<Image>();
            img.color = new Color(0.12f, 0.14f, 0.2f, 0.95f);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(contentGo.transform, false);
            labelGo.layer = uiLayer;
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = labelText;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 48;
            tmp.color = Color.white;

            // Close button (top-right, avoid sidebar area)
            var closeBtnGo = new GameObject("Btn_Close");
            closeBtnGo.transform.SetParent(contentGo.transform, false);
            closeBtnGo.layer = uiLayer;
            var cbrt = closeBtnGo.AddComponent<RectTransform>();
            cbrt.anchorMin = new Vector2(1f, 1f);
            cbrt.anchorMax = new Vector2(1f, 1f);
            cbrt.pivot = new Vector2(1f, 1f);
            cbrt.anchoredPosition = new Vector2(-24, -24);
            cbrt.sizeDelta = new Vector2(40, 40);
            var cbImg = closeBtnGo.AddComponent<Image>();
            cbImg.color = new Color(1f, 1f, 1f, 0.15f);
            var closeBtn = closeBtnGo.AddComponent<Button>();

            var xLabelGo = new GameObject("X");
            xLabelGo.transform.SetParent(closeBtnGo.transform, false);
            xLabelGo.layer = uiLayer;
            var xrt = xLabelGo.AddComponent<RectTransform>();
            xrt.anchorMin = Vector2.zero; xrt.anchorMax = Vector2.one;
            xrt.offsetMin = Vector2.zero; xrt.offsetMax = Vector2.zero;
            var xtmp = xLabelGo.AddComponent<TextMeshProUGUI>();
            xtmp.text = "✕";
            xtmp.alignment = TextAlignmentOptions.Center;
            xtmp.fontSize = 22;
            xtmp.color = Color.white;

            var stub = go.AddComponent<T>();
            var so = new SerializedObject(stub);
            so.FindProperty("_content").objectReferenceValue = contentGo;
            so.FindProperty("_closeButton").objectReferenceValue = closeBtn;
            so.ApplyModifiedProperties();

            contentGo.SetActive(false);
        }

        private static Button MakeTab(GameObject parent, int uiLayer, string name, string labelText)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.layer = uiLayer;
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 56);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.18f, 0.22f, 0.3f, 1f);
            var btn = go.AddComponent<Button>();

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.layer = uiLayer;
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = labelText;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 22;
            tmp.color = Color.white;
            return btn;
        }
    }
}
#endif
