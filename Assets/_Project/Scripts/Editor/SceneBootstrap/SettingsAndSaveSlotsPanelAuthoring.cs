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
    /// 在 MainMenu.unity 的 Canvas 里追加 Panel_Settings + Panel_SaveSlots 两个面板，
    /// 分别挂 SettingsPanel / SaveSlotsPanel。幂等（已存在会清空重建内部节点）。
    /// </summary>
    public static class SettingsAndSaveSlotsPanelAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/MainMenu/MainMenu.unity";

        [MenuItem("Tools/Gemini-Lab/Author Settings + SaveSlots Panels (MainMenu)")]
        public static void Author()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[SettingsAndSaveSlots] 未找到 MainMenu 场景的 Canvas");
                return;
            }

            BuildSettingsPanel(canvas);
            BuildSaveSlotsPanel(canvas);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SettingsAndSaveSlots] MainMenu 的 Settings / SaveSlots 面板已注入");
        }

        private static void BuildSettingsPanel(GameObject canvas)
        {
            var existing = canvas.transform.Find("Panel_Settings");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            int layer = canvas.layer;
            var panel = MakeCenteredPanel(canvas, layer, "Panel_Settings", new Vector2(720, 540));
            var content = BuildContent(panel, layer);

            var panelComp = panel.AddComponent<SettingsPanel>();

            // 标题
            var title = AddText(content, layer, "标题：设置", 32, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -60), new Vector2(0, -10), TextAlignmentOptions.Center);
            title.text = "设置";

            // 音量 Sliders
            var masterSlider = AddSlider(content, layer, "Master", "主音量", 60);
            var bgmSlider = AddSlider(content, layer, "Bgm", "背景音乐", 110);
            var sfxSlider = AddSlider(content, layer, "Sfx", "音效", 160);

            // 开关
            var fullscreenToggle = AddToggle(content, layer, "Fullscreen", "全屏", 220);
            var overlayToggle = AddToggle(content, layer, "Overlay", "启用桌面 Overlay", 260);

            // 操作按钮（底部）
            var resetBtn = AddButton(content, layer, "ResetBtn", "恢复默认", 0.12f, 0.38f, 24);
            var closeBtn = AddButton(content, layer, "CloseBtn", "关闭", 0.62f, 0.88f, 24);

            // Bind via SerializedObject
            var so = new SerializedObject(panelComp);
            so.FindProperty("_content").objectReferenceValue = content;
            so.FindProperty("_master").objectReferenceValue = masterSlider.slider;
            so.FindProperty("_masterValue").objectReferenceValue = masterSlider.value;
            so.FindProperty("_bgm").objectReferenceValue = bgmSlider.slider;
            so.FindProperty("_bgmValue").objectReferenceValue = bgmSlider.value;
            so.FindProperty("_sfx").objectReferenceValue = sfxSlider.slider;
            so.FindProperty("_sfxValue").objectReferenceValue = sfxSlider.value;
            so.FindProperty("_fullscreen").objectReferenceValue = fullscreenToggle;
            so.FindProperty("_desktopOverlay").objectReferenceValue = overlayToggle;
            so.FindProperty("_resetButton").objectReferenceValue = resetBtn;
            so.FindProperty("_closeButton").objectReferenceValue = closeBtn;
            so.ApplyModifiedProperties();

            content.SetActive(false);
        }

        private static void BuildSaveSlotsPanel(GameObject canvas)
        {
            var existing = canvas.transform.Find("Panel_SaveSlots");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            int layer = canvas.layer;
            var panel = MakeCenteredPanel(canvas, layer, "Panel_SaveSlots", new Vector2(820, 580));
            var content = BuildContent(panel, layer);

            var panelComp = panel.AddComponent<SaveSlotsPanel>();

            AddText(content, layer, "Title", 32, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -60), new Vector2(0, -10), TextAlignmentOptions.Center).text = "存档";

            // Slot container (VerticalLayoutGroup)
            var slotsGo = new GameObject("Slots");
            slotsGo.transform.SetParent(content.transform, false);
            slotsGo.layer = layer;
            var srt = slotsGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.05f, 0.2f);
            srt.anchorMax = new Vector2(0.95f, 0.88f);
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;
            var vlg = slotsGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            // Status text (bottom)
            var status = AddText(content, layer, "Status", 16, new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(32, 10), new Vector2(-32, 54), TextAlignmentOptions.MidlineLeft);
            status.text = "";
            status.color = new Color(1f, 1f, 1f, 0.7f);

            // Close button
            var closeBtn = AddButton(content, layer, "CloseBtn", "关闭", 0.40f, 0.60f, 10);

            // Slot template (hidden, cloned at runtime)
            var template = BuildSlotTemplate(panel, layer);

            var so = new SerializedObject(panelComp);
            so.FindProperty("_content").objectReferenceValue = content;
            so.FindProperty("_slotTemplate").objectReferenceValue = template;
            so.FindProperty("_slotContainer").objectReferenceValue = slotsGo.transform;
            so.FindProperty("_closeButton").objectReferenceValue = closeBtn;
            so.FindProperty("_statusText").objectReferenceValue = status;
            so.ApplyModifiedProperties();

            content.SetActive(false);
        }

        private static GameObject BuildSlotTemplate(GameObject panel, int layer)
        {
            var root = new GameObject("SlotTemplate");
            root.transform.SetParent(panel.transform, false);
            root.layer = layer;
            var rt = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 110);
            var bg = root.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.06f);

            // Summary
            var summaryGo = new GameObject("Summary");
            summaryGo.transform.SetParent(root.transform, false);
            summaryGo.layer = layer;
            var srt = summaryGo.AddComponent<RectTransform>();
            srt.anchorMin = Vector2.zero;
            srt.anchorMax = new Vector2(0.55f, 1);
            srt.offsetMin = new Vector2(16, 8);
            srt.offsetMax = new Vector2(-8, -8);
            var summary = summaryGo.AddComponent<TextMeshProUGUI>();
            summary.fontSize = 18;
            summary.color = Color.white;
            summary.alignment = TextAlignmentOptions.MidlineLeft;
            summary.text = "Slot_1：预览槽位";
            summaryGo.AddComponent<GeminiLab.Modules.UI.Catalogs.TMPFontBinder>();

            // Buttons
            BuildTemplateButton(root, layer, "Btn_读取", "读取", new Vector2(0.55f, 0), new Vector2(0.70f, 1));
            BuildTemplateButton(root, layer, "Btn_保存", "新建 / 覆盖", new Vector2(0.70f, 0), new Vector2(0.88f, 1));
            BuildTemplateButton(root, layer, "Btn_删除", "删除", new Vector2(0.88f, 0), new Vector2(1.0f, 1));

            root.SetActive(false);
            return root;
        }

        private static void BuildTemplateButton(GameObject parent, int layer, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.layer = layer;
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(6, 20);
            rt.offsetMax = new Vector2(-6, -20);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.3f, 0.45f, 1f);
            go.AddComponent<Button>();

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.layer = layer;
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            labelGo.AddComponent<GeminiLab.Modules.UI.Catalogs.TMPFontBinder>();
        }

        // ---- helpers ----

        private static GameObject MakeCenteredPanel(GameObject canvas, int layer, string name, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(canvas.transform, false);
            go.layer = layer;
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
            return go;
        }

        private static GameObject BuildContent(GameObject panel, int layer)
        {
            var go = new GameObject("Content");
            go.transform.SetParent(panel.transform, false);
            go.layer = layer;
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.14f, 0.2f, 0.97f);
            return go;
        }

        private static TMP_Text AddText(GameObject parent, int layer, string name, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, TextAlignmentOptions align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.layer = layer;
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = align;
            go.AddComponent<GeminiLab.Modules.UI.Catalogs.TMPFontBinder>();
            return tmp;
        }

        private static (Slider slider, TMP_Text value, TMP_Text label) AddSlider(GameObject parent, int layer, string name, string labelText, float topOffset)
        {
            var row = new GameObject($"Slider_{name}");
            row.transform.SetParent(parent.transform, false);
            row.layer = layer;
            var rrt = row.AddComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0, 1);
            rrt.anchorMax = new Vector2(1, 1);
            rrt.pivot = new Vector2(0.5f, 1);
            rrt.anchoredPosition = new Vector2(0, -topOffset);
            rrt.sizeDelta = new Vector2(-80, 40);

            var label = AddText(row, layer, "Label", 20, new Vector2(0, 0), new Vector2(0.25f, 1),
                Vector2.zero, Vector2.zero, TextAlignmentOptions.MidlineLeft);
            label.text = labelText;

            // Slider
            var sliderGo = new GameObject("Slider");
            sliderGo.transform.SetParent(row.transform, false);
            sliderGo.layer = layer;
            var srt = sliderGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.26f, 0.2f);
            srt.anchorMax = new Vector2(0.85f, 0.8f);
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;

            // Background
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(sliderGo.transform, false);
            bgGo.layer = layer;
            var brt = bgGo.AddComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(1, 1, 1, 0.12f);

            // Fill area + Fill
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGo.transform, false);
            fillArea.layer = layer;
            var fart = fillArea.AddComponent<RectTransform>();
            fart.anchorMin = new Vector2(0, 0.25f);
            fart.anchorMax = new Vector2(1, 0.75f);
            fart.offsetMin = new Vector2(5, 0);
            fart.offsetMax = new Vector2(-15, 0);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillArea.transform, false);
            fillGo.layer = layer;
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color(0.4f, 0.6f, 1f, 1f);

            // Handle area + Handle
            var handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderGo.transform, false);
            handleArea.layer = layer;
            var hart = handleArea.AddComponent<RectTransform>();
            hart.anchorMin = Vector2.zero; hart.anchorMax = Vector2.one;
            hart.offsetMin = new Vector2(10, 0); hart.offsetMax = new Vector2(-10, 0);

            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(handleArea.transform, false);
            handleGo.layer = layer;
            var handleRt = handleGo.AddComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(20, 0);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = Color.white;

            var slider = sliderGo.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;

            // Value label
            var value = AddText(row, layer, "Value", 18, new Vector2(0.86f, 0), new Vector2(1, 1),
                Vector2.zero, Vector2.zero, TextAlignmentOptions.MidlineRight);
            value.text = "80%";

            return (slider, value, label);
        }

        private static Toggle AddToggle(GameObject parent, int layer, string name, string label, float topOffset)
        {
            var row = new GameObject($"Toggle_{name}");
            row.transform.SetParent(parent.transform, false);
            row.layer = layer;
            var rrt = row.AddComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0, 1);
            rrt.anchorMax = new Vector2(1, 1);
            rrt.pivot = new Vector2(0.5f, 1);
            rrt.anchoredPosition = new Vector2(0, -topOffset);
            rrt.sizeDelta = new Vector2(-80, 32);

            var toggle = row.AddComponent<Toggle>();
            toggle.isOn = true;

            // Background
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(row.transform, false);
            bgGo.layer = layer;
            var brt = bgGo.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0);
            brt.anchorMax = new Vector2(0, 1);
            brt.pivot = new Vector2(0, 0.5f);
            brt.anchoredPosition = new Vector2(0, 0);
            brt.sizeDelta = new Vector2(28, 28);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(1, 1, 1, 0.2f);
            toggle.targetGraphic = bgImg;

            var checkGo = new GameObject("Checkmark");
            checkGo.transform.SetParent(bgGo.transform, false);
            checkGo.layer = layer;
            var crt = checkGo.AddComponent<RectTransform>();
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = new Vector2(4, 4); crt.offsetMax = new Vector2(-4, -4);
            var checkImg = checkGo.AddComponent<Image>();
            checkImg.color = new Color(0.4f, 0.8f, 1f, 1f);
            toggle.graphic = checkImg;

            var lbl = AddText(row, layer, "Label", 20, new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(40, 0), Vector2.zero, TextAlignmentOptions.MidlineLeft);
            lbl.text = label;

            return toggle;
        }

        private static Button AddButton(GameObject parent, int layer, string name, string label, float anchorMinX, float anchorMaxX, float bottomOffset)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.layer = layer;
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorMinX, 0);
            rt.anchorMax = new Vector2(anchorMaxX, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, bottomOffset);
            rt.sizeDelta = new Vector2(0, 48);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.3f, 0.35f, 0.55f, 1f);
            var btn = go.AddComponent<Button>();

            var lbl = AddText(go, layer, "Label", 20, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            lbl.text = label;
            return btn;
        }
    }
}
#endif
