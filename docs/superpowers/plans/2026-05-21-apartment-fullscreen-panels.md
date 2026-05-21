# Apartment 全屏面板实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 Apartment 场景侧边栏面板从居中叠加层（900×620）改为全屏面板，侧边栏始终浮于最上层。

**Architecture:** 面板 RectTransform 改为 stretch 全屏；侧边栏移入子 Canvas（sortingOrder +1 确保层级）；SidebarController 新增 toggle 逻辑（点击已激活 Tab 关闭面板）；StubPanelBase 新增关闭按钮支持。

**Tech Stack:** Unity UI (uGUI), C#, TMPro, Editor scripting

---

### Task 1: SidebarController — toggle 逻辑 + 激活高亮 + EventBus 订阅

**Files:**
- Modify: `Assets/_Project/Scripts/Modules/HubUI/SidebarController.cs`

- [ ] **Step 1: 替换 SidebarController 完整代码**

用以下代码替换 `SidebarController.cs` 全部内容：

```csharp
#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI
{
    public sealed class SidebarController : MonoBehaviour
    {
        [Header("展开/收起")]
        [SerializeField] private RectTransform? _panelRoot;
        [SerializeField] private Button? _toggleButton;
        [SerializeField] private float _expandedX = 0f;
        [SerializeField] private float _collapsedX = -240f;

        [Header("Tab 按钮")]
        [SerializeField] private Button? _tabPetStatus;
        [SerializeField] private Button? _tabTarot;
        [SerializeField] private Button? _tabCollection;
        [SerializeField] private Button? _tabInventory;
        [SerializeField] private Button? _tabGarden;

        [Header("高亮色")]
        [SerializeField] private Color _activeTabColor = new Color(1f, 0.85f, 0.3f, 0.45f);
        [SerializeField] private Color _inactiveTabColor = new Color(0.18f, 0.22f, 0.3f, 1f);

        private IUIRouter? _router;
        private EventBus? _eventBus;
        private IDisposable? _panelClosedSub;
        private bool _expanded = true;

        private PanelId? _activePanelId;
        private Dictionary<PanelId, Button> _tabMap = new();
        private Dictionary<Button, Image> _tabBgMap = new();

        private void Awake()
        {
            ServiceLocator.TryResolve(out _router);
            ServiceLocator.TryResolve(out _eventBus);

            if (_eventBus is not null)
            {
                _panelClosedSub = _eventBus.Subscribe<UIPanelClosedEvent>(OnPanelClosed);
            }

            BuildTabMap();

            if (_toggleButton is not null)
            {
                _toggleButton.onClick.AddListener(Toggle);
            }

            if (_tabPetStatus is not null)
                _tabPetStatus.onClick.AddListener(() => OpenPanel(PanelId.PetStatus));
            if (_tabTarot is not null)
                _tabTarot.onClick.AddListener(() => OpenPanel(PanelId.Tarot));
            if (_tabCollection is not null)
                _tabCollection.onClick.AddListener(() => OpenPanel(PanelId.Collection));
            if (_tabInventory is not null)
                _tabInventory.onClick.AddListener(() => OpenPanel(PanelId.Inventory));
            if (_tabGarden is not null)
                _tabGarden.onClick.AddListener(() => OpenPanel(PanelId.Garden));

            ApplyState(instant: true);
        }

        private void OnDestroy()
        {
            _panelClosedSub?.Dispose();
        }

        public void Toggle()
        {
            _expanded = !_expanded;
            ApplyState(instant: false);
        }

        private void BuildTabMap()
        {
            AddTab(PanelId.PetStatus, _tabPetStatus);
            AddTab(PanelId.Tarot, _tabTarot);
            AddTab(PanelId.Collection, _tabCollection);
            AddTab(PanelId.Inventory, _tabInventory);
            AddTab(PanelId.Garden, _tabGarden);
        }

        private void AddTab(PanelId id, Button? btn)
        {
            if (btn == null) return;
            _tabMap[id] = btn;
            var img = btn.GetComponent<Image>();
            if (img != null) _tabBgMap[btn] = img;
        }

        private void OpenPanel(PanelId id)
        {
            if (_router is null && !ServiceLocator.TryResolve(out _router))
            {
                Debug.LogWarning($"[Sidebar] 未找到 IUIRouter，无法打开 {id}");
                return;
            }

            if (_activePanelId == id)
            {
                _router!.Close(id);
                return;
            }

            if (_activePanelId is not null)
            {
                _router!.Close(_activePanelId.Value);
            }

            _router!.Open(id);
            _activePanelId = id;
            RefreshTabHighlight();
        }

        private void OnPanelClosed(UIPanelClosedEvent e)
        {
            if (_activePanelId == e.Id)
            {
                _activePanelId = null;
                RefreshTabHighlight();
            }
        }

        private void RefreshTabHighlight()
        {
            foreach (var (id, btn) in _tabMap)
            {
                if (_tabBgMap.TryGetValue(btn, out var img))
                {
                    img.color = id == _activePanelId ? _activeTabColor : _inactiveTabColor;
                }
            }
        }

        private void ApplyState(bool instant)
        {
            if (_panelRoot is null) return;
            float targetX = _expanded ? _expandedX : _collapsedX;
            Vector2 pos = _panelRoot.anchoredPosition;
            pos.x = targetX;
            _panelRoot.anchoredPosition = pos;
            _ = instant;
        }
    }
}
```

- [ ] **Step 2: 验证编译**

在 Unity Editor 中打开项目，等待脚本编译完成，确认 Console 无编译错误。

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Modules/HubUI/SidebarController.cs
git commit -m "feat(sidebar): add toggle logic, tab highlight, and EventBus subscription"
```

---

### Task 2: StubPanelBase — 关闭按钮

**Files:**
- Modify: `Assets/_Project/Scripts/Modules/HubUI/Panels/StubPanelBase.cs`

- [ ] **Step 1: 替换 StubPanelBase 代码**

```csharp
#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    public abstract class StubPanelBase : MonoBehaviour, IUIPanel
    {
        [SerializeField] private GameObject? _content;
        [SerializeField] private Button? _closeButton;

        private IUIRouter? _router;
        public abstract PanelId Id { get; }

        protected virtual void Awake()
        {
            if (_content is not null)
            {
                _content.SetActive(false);
            }

            if (ServiceLocator.TryResolve(out IUIRouter? router))
            {
                _router = router;
                _router.Register(this);
            }

            if (_closeButton is not null)
            {
                _closeButton.onClick.AddListener(CloseSelf);
            }
        }

        protected virtual void OnDestroy()
        {
            if (ServiceLocator.TryResolve(out IUIRouter? router))
            {
                router.Unregister(Id);
            }
        }

        public virtual void OnOpen(object? payload)
        {
            if (_content is not null)
            {
                _content.SetActive(true);
            }
        }

        public virtual void OnClose()
        {
            if (_content is not null)
            {
                _content.SetActive(false);
            }
        }

        protected void CloseSelf()
        {
            _router?.Close(Id);
        }
    }
}
```

- [ ] **Step 2: 验证编译**

等待 Unity 编译完成，确认无编译错误。

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Modules/HubUI/Panels/StubPanelBase.cs
git commit -m "feat(panel): add close button support to StubPanelBase"
```

---

### Task 3: ApartmentSidebarAuthoring — 面板 stretch + 子 Canvas 分层

**Files:**
- Modify: `Assets/_Project/Scripts/Editor/SceneBootstrap/ApartmentSidebarAuthoring.cs`

- [ ] **Step 1: 替换 ApartmentSidebarAuthoring 完整代码**

```csharp
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
```

- [ ] **Step 2: 验证编译**

等待 Unity 编译完成，确认无编译错误。

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Editor/SceneBootstrap/ApartmentSidebarAuthoring.cs
git commit -m "feat(authoring): stretch panels fullscreen, move sidebar into sub-Canvas, add close button"
```

---

### Task 4: ApartmentGardenSidebarPatch — 面板 anchor 对齐新规范

**Files:**
- Modify: `Assets/_Project/Scripts/Editor/SceneBootstrap/ApartmentGardenSidebarPatch.cs`

- [ ] **Step 1: 替换 CreateStubPanel 方法**

将 `CreateStubPanel<T>` 方法（约 136-180 行）整体替换为：

```csharp
private static GameObject CreateStubPanel<T>(GameObject parent, int uiLayer, string name, string labelText) where T : MonoBehaviour
{
    var go = new GameObject(name);
    go.transform.SetParent(parent.transform, false);
    go.layer = uiLayer;
    var rt = go.AddComponent<RectTransform>();
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

    // Close button (top-right)
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
    return go;
}
```

- [ ] **Step 2: 验证编译**

等待 Unity 编译完成。

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Editor/SceneBootstrap/ApartmentGardenSidebarPatch.cs
git commit -m "feat(patch): align Garden panel anchor to stretch fullscreen with close button"
```

---

### Task 5: GardenPanelAuthoring — 内容区左侧 padding 72px

**Files:**
- Modify: `Assets/_Project/Scripts/Editor/SceneBootstrap/GardenPanelAuthoring.cs`

- [ ] **Step 1: 修改所有子容器 offsetMin.x**

`GardenPanelAuthoring.cs` 中所有 `CreateChild` 调用的 `offsetMin.x` 从 `0`/`12`/`16`/`32` 改为 `72`/`84`/`88`/`104`（即原值 +72）：

| 节点 | 原 offsetMin.x | 新 offsetMin.x |
|---|---|---|
| GridHolder | 32 | 104 |
| Grid (内层) | 16 | 88 |
| SeedHolder | 12 | 84 |
| SeedBar | 12 | 84 |
| Hint | 12 | 84 |

定位到 `Author()` 方法中的对应行并修改。

- [ ] **Step 2: Title 保持不变**

标题 "花园" 居中显示，不受左 padding 影响，无需修改。

- [ ] **Step 3: 验证编译**

等待 Unity 编译完成。

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Scripts/Editor/SceneBootstrap/GardenPanelAuthoring.cs
git commit -m "feat(garden): add left padding 72px to avoid sidebar overlap"
```

---

### Task 6: InventoryCollectionPanelAuthoring — 内容区左侧 padding 72px

**Files:**
- Modify: `Assets/_Project/Scripts/Editor/SceneBootstrap/InventoryCollectionPanelAuthoring.cs`

- [ ] **Step 1: 修改 BuildInventoryPanel 子容器 offsetMin.x**

| 节点 | 原 offsetMin.x | 新 offsetMin.x |
|---|---|---|
| Grid | 16 | 88 |
| Tooltip | 8 | 80 |

- [ ] **Step 2: 修改 BuildCollectionPanel 子容器 offsetMin.x**

| 节点 | 原 offsetMin.x | 新 offsetMin.x |
|---|---|---|
| Tabs | 32 | 104 |
| GridHolder | 32 | 104 |

- [ ] **Step 3: 验证编译并 Commit**

```bash
git add Assets/_Project/Scripts/Editor/SceneBootstrap/InventoryCollectionPanelAuthoring.cs
git commit -m "feat(inventory+collection): add left padding 72px to avoid sidebar overlap"
```

---

### Task 7: PetStatusPanelAuthoring — 内容区左侧 padding 72px

**Files:**
- Modify: `Assets/_Project/Scripts/Editor/SceneBootstrap/PetStatusPanelAuthoring.cs`

- [ ] **Step 1: 修改子容器 offsetMin.x**

| 节点 | 原 offsetMin.x | 新 offsetMin.x |
|---|---|---|
| StatBars | 32 | 104 |
| CurrentState | 32 | 104 |

- [ ] **Step 2: 验证编译并 Commit**

```bash
git add Assets/_Project/Scripts/Editor/SceneBootstrap/PetStatusPanelAuthoring.cs
git commit -m "feat(pet-status): add left padding 72px to avoid sidebar overlap"
```

---

### Task 8: TarotPanelAuthoring — 内容区左侧 padding 72px

**Files:**
- Modify: `Assets/_Project/Scripts/Editor/SceneBootstrap/TarotPanelAuthoring.cs`

- [ ] **Step 1: 修改卡面和按钮的水平位置**

卡面 `CardRoot` 和抽卡按钮 `DrawButton` 使用 `anchoredPosition.x` + anchor 定位，需要将其 x 偏移 +72：

| 节点 | 原 anchoredPosition.x | 新 anchoredPosition.x |
|---|---|---|
| CardRoot | 24 | 96 |
| DrawButton | 24 | 96 |

定位到 `Author()` 方法中 `cardRt.anchoredPosition` 和 `dbRt.anchoredPosition` 两处。

- [ ] **Step 2: 验证编译并 Commit**

```bash
git add Assets/_Project/Scripts/Editor/SceneBootstrap/TarotPanelAuthoring.cs
git commit -m "feat(tarot): add left padding 72px to avoid sidebar overlap"
```

---

### Task 9: 运行 Authoring 工具，场景内验证

- [ ] **Step 1: 在 Unity Editor 中依次执行菜单**

```
Tools/Gemini-Lab/Author Apartment Sidebar
Tools/Gemini-Lab/Author Inventory + Collection Panels (Apartment)
Tools/Gemini-Lab/Author Garden Panel (Apartment)
Tools/Gemini-Lab/Author Pet Status Panel UI
Tools/Gemini-Lab/Author Tarot Panel UI
```

- [ ] **Step 2: 进入 Play Mode 验证**

1. 侧边栏在左侧可见
2. 点击 Tab（如 Garden）→ 面板全屏显示，侧边栏仍在最上层
3. 再次点击同一 Tab → 面板关闭（toggle）
4. 点击不同 Tab → 前一个面板关闭，新面板打开
5. 点击面板右上角 ✕ → 面板关闭
6. 面板内容区左侧不被侧边栏遮挡（留出 72px padding）
7. 侧边栏折叠/展开不影响面板

- [ ] **Step 3: 提交场景文件**

```bash
git add Assets/_Project/Scenes/Apartment/Apartment_Main.unity
git commit -m "chore(scene): apply fullscreen panel layout to Apartment_Main"
```
