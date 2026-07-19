#nullable enable
#if UNITY_EDITOR
using GeminiLab.Core;
using GeminiLab.Core.UI;
using GeminiLab.Modules.Collection;
using GeminiLab.Modules.DevTools;
using GeminiLab.Modules.HubUI;
using GeminiLab.Modules.HubUI.Panels;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 在 WorldMap 场景的 Canvas 下创建情绪花园三个面板 + 触发按钮。
    /// 幂等：已存在的对象只补缺失组件和连线，不覆盖用户手动调整的属性。
    /// </summary>
    public static class WorldMapEmotionGardenUIPatch
    {
        private const string ScenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";

        public static void Patch()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null)
            {
                Debug.LogError("[WorldMapEmotionGardenUI] 未找到 Canvas，请先跑 Author WorldMap Scene");
                return;
            }

            int uiLayer = canvasGo.layer;

            var panelInput = EnsurePanel<EmotionInputPanelStub>(canvasGo, uiLayer, "Panel_EmotionInput");
            var panelWeekly = EnsurePanel<WeeklyGardenPanelStub>(canvasGo, uiLayer, "Panel_WeeklyGarden");
            var panelCollection = EnsurePanel<FlowerCollectionPanelStub>(canvasGo, uiLayer, "Panel_EmotionCollection");

            SetupEmotionInputContent(panelInput, uiLayer);
            SetupWeeklyGardenContent(panelWeekly, uiLayer);
            SetupFlowerCollectionContent(panelCollection, uiLayer);

            EnsureDevTools(canvasGo, uiLayer);

            var inputBtn = EnsureButton(canvasGo, uiLayer, "Btn_EmotionInput", "情绪输入", 0);
            var weeklyBtn = EnsureButton(canvasGo, uiLayer, "Btn_WeeklyGarden", "每周培育", 1);
            var collectionBtn = EnsureButton(canvasGo, uiLayer, "Btn_EmotionCollection", "情绪图鉴", 2);

            WireButtonToPanel(inputBtn, PanelId.EmotionInput);
            WireButtonToPanel(weeklyBtn, PanelId.WeeklyGardenView);
            WireButtonToPanel(collectionBtn, PanelId.EmotionCollection);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[WorldMapEmotionGardenUI] 情绪花园面板 + 触发按钮已添加到 WorldMap Canvas");
        }

        // ── DevTools 调试工具父节点 ──────────────────────────

        private static void EnsureDevTools(GameObject canvasGo, int uiLayer)
        {
            var devTools = canvasGo.transform.Find("DevTools");
            if (devTools == null)
            {
                var go = new GameObject("DevTools");
                go.transform.SetParent(canvasGo.transform, false);
                go.layer = uiLayer;
                devTools = go.transform;
            }

            devTools.gameObject.SetActive(DevMode.Active);

            var actions = devTools.GetComponent<DevToolsActions>();
            if (actions == null) actions = devTools.gameObject.AddComponent<DevToolsActions>();

            // 清理旧版散落在面板里的调试按钮
            foreach (var panelName in new[] { "Panel_EmotionInput", "Panel_EmotionCollection" })
            {
                var panel = canvasGo.transform.Find(panelName);
                if (panel == null) continue;
                foreach (var oldName in new[] { "DebugResetBtn", "DebugNextDayBtn", "DebugBloomBtn" })
                {
                    var old = panel.Find("Content")?.Find(oldName);
                    if (old != null) Object.DestroyImmediate(old.gameObject);
                }
            }

            EnsureDevButton(devTools, uiLayer, "DebugNextDayBtn", "进入下一天(调试)",
                new Vector2(-32, -180), actions.AdvanceDay);
            EnsureDevButton(devTools, uiLayer, "ResetClockBtn", "重置时钟(调试)",
                new Vector2(-32, -224), actions.ResetClock);
            EnsureDevButton(devTools, uiLayer, "ClearGardenBtn", "清空花园数据(调试)",
                new Vector2(-32, -268), actions.ClearGardenData);
        }

        private static GameObject EnsureDevButton(Transform parent, int uiLayer, string name, string label,
            Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
        {
            var btnGo = EnsureChild(parent, name, uiLayer);
            if (btnGo.transform.childCount != 0) return btnGo;

            var drt = GetOrAdd<RectTransform>(btnGo);
            drt.anchorMin = new Vector2(1f, 1f);
            drt.anchorMax = new Vector2(1f, 1f);
            drt.pivot = new Vector2(1f, 1f);
            drt.anchoredPosition = anchoredPosition;
            drt.sizeDelta = new Vector2(180, 36);
            var dImg = GetOrAdd<Image>(btnGo);
            dImg.color = new Color(0.6f, 0.25f, 0.1f, 0.9f);
            var btn = GetOrAdd<Button>(btnGo);

            var labelGo = EnsureChild(btnGo.transform, "Label", uiLayer);
            var lrt = GetOrAdd<RectTransform>(labelGo);
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tmp = GetOrAdd<TextMeshProUGUI>(labelGo);
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 14;
            tmp.color = Color.white;

            UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, onClick);
            return btnGo;
        }

        // ── 通用 panel / button ──────────────────────────────

        private static GameObject EnsurePanel<T>(GameObject canvas, int uiLayer, string name) where T : MonoBehaviour
        {
            var existing = canvas.transform.Find(name);
            if (existing != null)
            {
                if (existing.GetComponent<T>() == null)
                    existing.gameObject.AddComponent<T>();
                return existing.gameObject;
            }
            return CreateStubPanel<T>(canvas, uiLayer, name);
        }

        private static Button EnsureButton(GameObject canvas, int uiLayer, string name, string label, int index)
        {
            var existing = canvas.transform.Find(name);
            if (existing != null)
            {
                var existingBtn = existing.GetComponent<Button>();
                return existingBtn != null ? existingBtn : existing.gameObject.AddComponent<Button>();
            }
            return CreatePanelButton(canvas, uiLayer, name, label, index);
        }

        private static void WireButtonToPanel(Button btn, PanelId panelId)
        {
            var panelBtn = btn.GetComponent<PanelOpenButton>();
            if (panelBtn == null) panelBtn = btn.gameObject.AddComponent<PanelOpenButton>();

            var so = new SerializedObject(panelBtn);
            so.FindProperty("_panelId").intValue = (int)panelId;
            so.ApplyModifiedProperties();
        }

        // ── 基础面板骨架（Content + Close + Balance）─────────

        private static GameObject CreateStubPanel<T>(GameObject canvas, int uiLayer, string name) where T : MonoBehaviour
        {
            var go = new GameObject(name);
            go.transform.SetParent(canvas.transform, false);
            go.layer = uiLayer;
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(go.transform, false);
            contentGo.layer = uiLayer;
            var crt = contentGo.AddComponent<RectTransform>();
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
            var img = contentGo.AddComponent<Image>();
            img.color = new Color(0.12f, 0.14f, 0.2f, 0.95f);

            // Close button
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

            var xGo = new GameObject("X");
            xGo.transform.SetParent(closeBtnGo.transform, false);
            xGo.layer = uiLayer;
            var xrt = xGo.AddComponent<RectTransform>();
            xrt.anchorMin = Vector2.zero; xrt.anchorMax = Vector2.one;
            xrt.offsetMin = Vector2.zero; xrt.offsetMax = Vector2.zero;
            var xtmp = xGo.AddComponent<TextMeshProUGUI>();
            xtmp.text = "✕";
            xtmp.alignment = TextAlignmentOptions.Center;
            xtmp.fontSize = 22;
            xtmp.color = Color.white;

            // Coin balance
            var topResGo = new GameObject("TopResource");
            topResGo.transform.SetParent(contentGo.transform, false);
            topResGo.layer = uiLayer;
            var trRt = topResGo.AddComponent<RectTransform>();
            trRt.anchorMin = new Vector2(0.5f, 0.5f);
            trRt.anchorMax = new Vector2(0.5f, 0.5f);
            trRt.pivot = new Vector2(0.5f, 0.5f);
            trRt.anchoredPosition = new Vector2(582, 442);
            trRt.sizeDelta = new Vector2(295, 91);
            var trImg = topResGo.AddComponent<Image>();
            trImg.color = Color.white;

            var balanceGo = new GameObject("BalanceLabel");
            balanceGo.transform.SetParent(topResGo.transform, false);
            balanceGo.layer = uiLayer;
            var bRt = balanceGo.AddComponent<RectTransform>();
            bRt.anchorMin = Vector2.zero;
            bRt.anchorMax = Vector2.one;
            bRt.anchoredPosition = Vector2.zero;
            bRt.sizeDelta = new Vector2(-16, -4);
            var bTmp = balanceGo.AddComponent<TextMeshProUGUI>();
            bTmp.text = "0";
            bTmp.alignment = TextAlignmentOptions.Center;
            bTmp.fontSize = 20;
            bTmp.color = new Color(1f, 0.84f, 0f, 1f);
            balanceGo.AddComponent<GeminiLab.Modules.UI.Catalogs.TMPFontBinder>();
            balanceGo.AddComponent<CoinBalanceDisplay>();

            var stub = go.AddComponent<T>();
            var so = new SerializedObject(stub);
            so.FindProperty("_content").objectReferenceValue = contentGo;
            so.FindProperty("_closeButton").objectReferenceValue = closeBtn;
            so.FindProperty("_balanceText").objectReferenceValue = bTmp;
            so.ApplyModifiedProperties();

            // Content 初始保持 active，方便在 Scene 视图中手动编辑 UI。
            // 运行时由 StubPanelBase.Awake() 负责关闭，Open 时再打开。
            return go;
        }

        // ── 情绪输入面板内容 ──────────────────────────────────

        private static void SetupEmotionInputContent(GameObject panel, int uiLayer)
        {
            var stub = panel.GetComponent<EmotionInputPanelStub>();
            if (stub == null) return;

            var so = new SerializedObject(stub);
            var content = so.FindProperty("_content").objectReferenceValue as GameObject;
            if (content == null) return;

            var contentT = content.transform;

            var titleGo = EnsureChildText(contentT, uiLayer, "Title_Input", "情绪花园 — 每日心情输入", 28,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(600, 46));
            var ownerGo = EnsureChildText(contentT, uiLayer, "OwnerText", "培育者: —", 20,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -100), new Vector2(300, 36));

            var inputField = EnsureInputField(contentT, uiLayer);
            var submitBtn = EnsureButtonWithLabel(contentT, uiLayer, "SubmitBtn", "提交心情",
                new Vector2(0, -50), new Vector2(200, 48), new Color(0.25f, 0.45f, 0.7f, 1f));
            var statusGo = EnsureChildText(contentT, uiLayer, "StatusText", "", 16,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -100), new Vector2(500, 30));
            var statusTmp = statusGo.GetComponent<TextMeshProUGUI>();
            statusTmp.color = new Color(1f, 0.85f, 0.4f, 1f);

            // 调试按钮已统一迁移到 Canvas/DevTools 节点
            var oldResetBtn = contentT.Find("DebugResetBtn");
            if (oldResetBtn != null) Object.DestroyImmediate(oldResetBtn.gameObject);
            var oldNextDayBtn = contentT.Find("DebugNextDayBtn");
            if (oldNextDayBtn != null) Object.DestroyImmediate(oldNextDayBtn.gameObject);

            so.FindProperty("_inputField").objectReferenceValue = inputField;
            so.FindProperty("_submitButton").objectReferenceValue = submitBtn;
            so.FindProperty("_statusText").objectReferenceValue = statusTmp;
            so.FindProperty("_ownerText").objectReferenceValue = ownerGo.GetComponent<TextMeshProUGUI>();
            so.ApplyModifiedProperties();
        }

        // ── 每周培育面板内容 ──────────────────────────────────

        private static void SetupWeeklyGardenContent(GameObject panel, int uiLayer)
        {
            var stub = panel.GetComponent<WeeklyGardenPanelStub>();
            if (stub == null) return;

            var so = new SerializedObject(stub);
            var content = so.FindProperty("_content").objectReferenceValue as GameObject;
            if (content == null) return;

            var contentT = content.transform;

            var titleGo = EnsureChildText(contentT, uiLayer, "WeekTitle", "每周培育", 28,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(400, 46));

            var prevBtn = EnsureWeekNavButton(contentT, uiLayer, "PrevWeekBtn", "◀ 上一周", new Vector2(-260, -50));
            var nextBtn = EnsureWeekNavButton(contentT, uiLayer, "NextWeekBtn", "下一周 ▶", new Vector2(260, -50));

            if (prevBtn.onClick.GetPersistentEventCount() == 0)
                UnityEditor.Events.UnityEventTools.AddPersistentListener(prevBtn.onClick, stub.ShowPrevWeek);
            if (nextBtn.onClick.GetPersistentEventCount() == 0)
                UnityEditor.Events.UnityEventTools.AddPersistentListener(nextBtn.onClick, stub.ShowNextWeek);

            var gridGo = EnsureChild(contentT, "Grid", uiLayer);
            if (gridGo.transform.childCount == 0)
            {
                // 首次创建才设置布局属性
                var grt = GetOrAdd<RectTransform>(gridGo);
                grt.anchorMin = new Vector2(0.5f, 0.5f);
                grt.anchorMax = new Vector2(0.5f, 0.5f);
                grt.pivot = new Vector2(0.5f, 0.5f);
                grt.anchoredPosition = new Vector2(0, 30);
                grt.sizeDelta = new Vector2(1600, 380);
                var hlg = GetOrAdd<HorizontalLayoutGroup>(gridGo);
                hlg.spacing = 14;
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlWidth = true;
                hlg.childControlHeight = false;
                hlg.childForceExpandWidth = true;
                hlg.childForceExpandHeight = false;
            }
            else
            {
                // 已存在：只确保组件不丢
                if (gridGo.GetComponent<RectTransform>() == null) gridGo.AddComponent<RectTransform>();
                if (gridGo.GetComponent<HorizontalLayoutGroup>() == null) gridGo.AddComponent<HorizontalLayoutGroup>();
            }

            so.FindProperty("_weekTitleText").objectReferenceValue = titleGo.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_gridRoot").objectReferenceValue = gridGo.transform;
            so.FindProperty("_nextWeekButton").objectReferenceValue = nextBtn;
            so.ApplyModifiedProperties();
        }

        /// <summary>顶部锚定的翻周导航按钮（EnsureButtonWithLabel 是中心锚定，不适用标题栏）。</summary>
        private static Button EnsureWeekNavButton(Transform parent, int uiLayer, string name, string label, Vector2 anchoredPosition)
        {
            var go = EnsureChild(parent, name, uiLayer);
            bool isNew = go.transform.childCount == 0;
            var btn = GetOrAdd<Button>(go);

            if (isNew)
            {
                var rt = GetOrAdd<RectTransform>(go);
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = anchoredPosition;
                rt.sizeDelta = new Vector2(120, 40);
                var img = GetOrAdd<Image>(go);
                img.color = new Color(0.22f, 0.3f, 0.42f, 1f);

                var labelGo = EnsureChild(go.transform, "Label", uiLayer);
                var lrt = GetOrAdd<RectTransform>(labelGo);
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
                var tmp = GetOrAdd<TextMeshProUGUI>(labelGo);
                tmp.text = label;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 16;
                tmp.color = Color.white;
            }

            return btn;
        }

        // ── 情绪图鉴面板内容 ──────────────────────────────────

        private static void SetupFlowerCollectionContent(GameObject panel, int uiLayer)
        {
            var stub = panel.GetComponent<FlowerCollectionPanelStub>();
            if (stub == null) return;

            var so = new SerializedObject(stub);
            var content = so.FindProperty("_content").objectReferenceValue as GameObject;
            if (content == null) return;

            var contentT = content.transform;

            EnsureChildText(contentT, uiLayer, "CollectionTitle", "情绪花图鉴", 28,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(400, 46));

            // ScrollView
            var scrollGo = EnsureChild(contentT, "ScrollView", uiLayer);
            ScrollRect scrollRect;
            RectTransform vpRt;
            RectTransform lRt;
            bool isNew = scrollGo.transform.childCount == 0;

            if (isNew)
            {
                var svRt = scrollGo.AddComponent<RectTransform>();
                svRt.anchorMin = new Vector2(0.5f, 0.5f);
                svRt.anchorMax = new Vector2(0.5f, 0.5f);
                svRt.pivot = new Vector2(0.5f, 0.5f);
                svRt.anchoredPosition = new Vector2(0, -40);
                svRt.sizeDelta = new Vector2(600, 520);
                var svImg = scrollGo.AddComponent<Image>();
                svImg.color = new Color(1f, 1f, 1f, 0.05f);
            }
            else
            {
                if (scrollGo.GetComponent<RectTransform>() == null) scrollGo.AddComponent<RectTransform>();
                if (scrollGo.GetComponent<Image>() == null) scrollGo.AddComponent<Image>();
            }
            scrollRect = GetOrAdd<ScrollRect>(scrollGo);

            // Viewport
            var viewportGo = EnsureChild(scrollGo.transform, "Viewport", uiLayer);
            if (viewportGo.transform.childCount == 0)
            {
                vpRt = viewportGo.AddComponent<RectTransform>();
                vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
                vpRt.sizeDelta = Vector2.zero;
                vpRt.pivot = new Vector2(0, 0.5f);
                var vpImg = viewportGo.AddComponent<Image>();
                vpImg.color = Color.clear;
                var vpMask = viewportGo.AddComponent<Mask>();
                vpMask.showMaskGraphic = false;
            }
            else
            {
                vpRt = GetOrAdd<RectTransform>(viewportGo);
                if (viewportGo.GetComponent<Image>() == null) viewportGo.AddComponent<Image>();
                if (viewportGo.GetComponent<Mask>() == null) viewportGo.AddComponent<Mask>();
            }

            // 遮罩图形必须不透明：全透明 Image 会被 CanvasRenderer 剔除，
            // Mask 写不进 stencil，列表整体不可见（showMaskGraphic=false 已保证它不显示）
            var vpImgFix = GetOrAdd<Image>(viewportGo);
            vpImgFix.color = Color.white;
            var vpMaskFix = GetOrAdd<Mask>(viewportGo);
            vpMaskFix.showMaskGraphic = false;

            // Content (_listRoot)
            var listGo = EnsureChild(viewportGo.transform, "ListRoot", uiLayer);
            if (listGo == null) return;
            lRt = GetOrAdd<RectTransform>(listGo);
            if (lRt == null) return;
            // ListRoot 的子物体是运行时生成的，Editor 下 childCount 恒为 0，
            // 因此这里必须全程 get-or-add，属性重复设置是幂等的
            lRt.anchorMin = new Vector2(0, 1);
            lRt.anchorMax = new Vector2(1, 1);
            lRt.pivot = new Vector2(0.5f, 1);
            lRt.sizeDelta = new Vector2(0, 0);
            var vl = GetOrAdd<VerticalLayoutGroup>(listGo);
            vl.spacing = 8;
            vl.padding = new RectOffset(12, 12, 12, 12);
            vl.childAlignment = TextAnchor.UpperCenter;
            vl.childControlWidth = true;
            vl.childControlHeight = false;
            var fitter = GetOrAdd<ContentSizeFitter>(listGo);
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vpRt;
            scrollRect.content = lRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // 旧的"立即开花"调试按钮已被输入面板的"进入下一天"取代
            var oldBloomBtn = contentT.Find("DebugBloomBtn");
            if (oldBloomBtn != null) Object.DestroyImmediate(oldBloomBtn.gameObject);

            so.FindProperty("_listRoot").objectReferenceValue = listGo.transform;
            so.ApplyModifiedProperties();
        }

        // ── UI 元素工厂（仅首次创建设置属性）──────────────

        private static TMP_InputField EnsureInputField(Transform parent, int uiLayer)
        {
            var existing = parent.Find("InputField");
            GameObject go;

            if (existing != null)
            {
                go = existing.gameObject;
                // 已存在：只确保组件齐全 + 连线
                if (go.GetComponent<RectTransform>() == null) go.AddComponent<RectTransform>();
                if (go.GetComponent<Image>() == null) go.AddComponent<Image>();
                var field = GetOrAdd<TMP_InputField>(go);
                field.lineType = TMP_InputField.LineType.MultiLineNewline;
                field.interactable = true;
                field.targetGraphic = go.GetComponent<Image>();

                var textArea = EnsureChild(go.transform, "Text Area", uiLayer);
                if (textArea != null)
                {
                    if (textArea.GetComponent<RectTransform>() == null) textArea.AddComponent<RectTransform>();
                    var placeholder = EnsureChild(textArea.transform, "Placeholder", uiLayer);
                    if (placeholder != null && placeholder.GetComponent<TextMeshProUGUI>() == null)
                        placeholder.AddComponent<TextMeshProUGUI>();
                    var text = EnsureChild(textArea.transform, "Text", uiLayer);
                    if (text != null && text.GetComponent<TextMeshProUGUI>() == null)
                        text.AddComponent<TextMeshProUGUI>();
                    field.textViewport = textArea.GetComponent<RectTransform>();
                    field.placeholder = placeholder?.GetComponent<TextMeshProUGUI>();
                    field.textComponent = text?.GetComponent<TextMeshProUGUI>();
                }

                return field;
            }

            // 首次创建
            go = new GameObject("InputField");
            go.transform.SetParent(parent, false);
            go.layer = uiLayer;
            var irt = go.AddComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.5f, 0.5f);
            irt.anchorMax = new Vector2(0.5f, 0.5f);
            irt.pivot = new Vector2(0.5f, 0.5f);
            irt.anchoredPosition = new Vector2(0, 40);
            irt.sizeDelta = new Vector2(500, 140);
            var inputImg = go.AddComponent<Image>();
            inputImg.color = new Color(1f, 1f, 1f, 0.08f);
            var inputField = go.AddComponent<TMP_InputField>();
            inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
            inputField.interactable = true;
            inputField.targetGraphic = inputImg;

            var textAreaNew = EnsureChild(go.transform, "Text Area", uiLayer);
            textAreaNew.AddComponent<RectTransform>();
            var placeholderNew = EnsureChild(textAreaNew.transform, "Placeholder", uiLayer);
            var phTmp = placeholderNew.AddComponent<TextMeshProUGUI>();
            phTmp.text = "输入你今天的心情…";
            phTmp.fontSize = 18;
            phTmp.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            inputField.placeholder = phTmp;

            var textNew = EnsureChild(textAreaNew.transform, "Text", uiLayer);
            var tTmp = textNew.AddComponent<TextMeshProUGUI>();
            tTmp.fontSize = 18;
            tTmp.color = Color.white;
            inputField.textComponent = tTmp;
            inputField.textViewport = textAreaNew.GetComponent<RectTransform>();

            return inputField;
        }

        private static Button EnsureButtonWithLabel(Transform parent, int uiLayer, string name, string label,
            Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
        {
            var existing = parent.Find(name);
            GameObject go;

            if (existing != null)
            {
                go = existing.gameObject;
                if (go.GetComponent<RectTransform>() == null) go.AddComponent<RectTransform>();
                if (go.GetComponent<Image>() == null) go.AddComponent<Image>();
                var btn = GetOrAdd<Button>(go);

                var labelChild = EnsureChild(go.transform, "Label", uiLayer);
                if (labelChild.GetComponent<RectTransform>() == null) labelChild.AddComponent<RectTransform>();
                if (labelChild.GetComponent<TextMeshProUGUI>() == null) labelChild.AddComponent<TextMeshProUGUI>();
                return btn;
            }

            go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = uiLayer;
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
            var img = go.AddComponent<Image>();
            img.color = color;
            var button = go.AddComponent<Button>();

            var labelGo = EnsureChild(go.transform, "Label", uiLayer);
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 20;
            tmp.color = Color.white;

            return button;
        }

        // ── 按钮 ──────────────────────────────────────────────

        private static Button CreatePanelButton(GameObject canvas, int uiLayer, string name, string label, int index)
        {
            var go = new GameObject(name);
            go.transform.SetParent(canvas.transform, false);
            go.layer = uiLayer;
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-32 - index * 170, -120);
            rt.sizeDelta = new Vector2(160, 52);
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
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 20;
            tmp.color = Color.white;

            return btn;
        }

        // ── helpers ────────────────────────────────────────────

        private static GameObject EnsureChild(Transform parent, string name, int uiLayer)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing.gameObject;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = uiLayer;
            return go;
        }

        /// <summary>不能写 GetComponent ?? AddComponent：Editor 下 GetComponent 对缺失组件返回假 null，?? 不会触发 AddComponent。</summary>
        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        private static GameObject EnsureChildText(Transform parent, int uiLayer, string name, string text, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                if (existing.GetComponent<TextMeshProUGUI>() == null)
                    existing.gameObject.AddComponent<TextMeshProUGUI>();
                return existing.gameObject;
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = uiLayer;
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return go;
        }
    }
}
#endif
