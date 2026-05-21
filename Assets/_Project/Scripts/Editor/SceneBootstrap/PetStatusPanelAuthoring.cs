#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.HubUI.Panels;
using GeminiLab.Modules.UI.Catalogs;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 把 Apartment 场景里占位的 Panel_PetStatus 升级为真实面板：
    /// 顶部"天使 / 恶魔"页签、左侧 3 条状态值、右侧 7 维性格雷达图。
    /// 幂等：重复运行会先清空 Content 再重建。
    /// </summary>
    public static class PetStatusPanelAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/Apartment/Apartment_Main.unity";

        [MenuItem("Tools/Gemini-Lab/Author Pet Status Panel UI")]
        public static void Author()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var panelRoot = GameObject.Find("Panel_PetStatus");
            if (panelRoot == null)
            {
                Debug.LogError("[PetStatusPanelAuthoring] 未找到 Panel_PetStatus，请先跑 Author Apartment Sidebar");
                return;
            }

            var panel = panelRoot.GetComponent<PetStatusPanelStub>();
            if (panel == null)
            {
                Debug.LogError("[PetStatusPanelAuthoring] Panel_PetStatus 上没有 PetStatusPanelStub 组件");
                return;
            }

            int uiLayer = panelRoot.layer;

            // Content 节点
            var contentTrans = panelRoot.transform.Find("Content");
            GameObject content = contentTrans != null ? contentTrans.gameObject
                : CreateChild(panelRoot, uiLayer, "Content", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            for (int i = content.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(content.transform.GetChild(i).gameObject);
            }
            var contentBg = content.GetComponent<Image>();
            if (contentBg == null) contentBg = content.AddComponent<Image>();
            contentBg.color = new Color(0.12f, 0.14f, 0.2f, 0.97f);

            // 标题
            var titleGo = CreateChild(content, uiLayer, "Title", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -56), new Vector2(0, 0));
            var titleRt = (RectTransform)titleGo.transform;
            titleRt.pivot = new Vector2(0.5f, 1f);
            var titleTmp = AddTmp(titleGo, "宠物状态", 32, TextAlignmentOptions.Center, new Color(0.95f, 0.9f, 0.7f, 1f));

            // 页签容器
            var tabsGo = CreateChild(content, uiLayer, "Tabs", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -112), new Vector2(0, -56));
            var tabsHlg = tabsGo.AddComponent<HorizontalLayoutGroup>();
            tabsHlg.childAlignment = TextAnchor.MiddleCenter;
            tabsHlg.spacing = 16;
            tabsHlg.padding = new RectOffset(32, 32, 6, 6);
            tabsHlg.childForceExpandWidth = false;
            tabsHlg.childForceExpandHeight = true;

            var tabAngel = MakeTabButton(tabsGo, uiLayer, "Tab_Angel", "天使", new Color(0.30f, 0.50f, 0.7f, 1f));
            var tabDevil = MakeTabButton(tabsGo, uiLayer, "Tab_Devil", "恶魔", new Color(0.65f, 0.28f, 0.38f, 1f));

            // 宠物名
            var nameGo = CreateChild(content, uiLayer, "PetName", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -160), new Vector2(0, -120));
            var nameTmp = AddTmp(nameGo, "天使", 26, TextAlignmentOptions.Center, Color.white);

            // 左侧 3 条状态值
            var barsGo = CreateChild(content, uiLayer, "StatBars", new Vector2(0, 0), new Vector2(0.5f, 1), new Vector2(104, 60), new Vector2(-16, -180));
            var barsVlg = barsGo.AddComponent<VerticalLayoutGroup>();
            barsVlg.spacing = 14;
            barsVlg.childAlignment = TextAnchor.UpperLeft;
            barsVlg.childForceExpandWidth = true;
            barsVlg.childForceExpandHeight = false;
            barsVlg.childControlWidth = true;
            barsVlg.childControlHeight = true;
            barsVlg.padding = new RectOffset(0, 0, 16, 0);

            var (moodTmp, moodFill) = MakeStatBar(barsGo, uiLayer, "Mood", "心情", new Color(0.95f, 0.55f, 0.4f, 1f));
            var (energyTmp, energyFill) = MakeStatBar(barsGo, uiLayer, "Energy", "精力", new Color(0.5f, 0.85f, 0.55f, 1f));
            var (satietyTmp, satietyFill) = MakeStatBar(barsGo, uiLayer, "Satiety", "饱食", new Color(0.95f, 0.8f, 0.35f, 1f));

            // 当前状态
            var stateGo = CreateChild(content, uiLayer, "CurrentState", new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(104, 24), new Vector2(-16, 60));
            var stateTmp = AddTmp(stateGo, "状态：Idle", 20, TextAlignmentOptions.MidlineLeft, new Color(0.85f, 0.9f, 1f, 0.85f));

            // 右侧雷达图
            var radarHolder = CreateChild(content, uiLayer, "RadarHolder", new Vector2(0.5f, 0), new Vector2(1f, 1), new Vector2(16, 60), new Vector2(-32, -180));
            var radarHolderBg = radarHolder.AddComponent<Image>();
            radarHolderBg.color = new Color(0f, 0f, 0f, 0.25f);

            var radarGo = CreateChild(radarHolder, uiLayer, "Radar", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var radarRt = (RectTransform)radarGo.transform;
            radarRt.anchorMin = new Vector2(0.5f, 0.5f);
            radarRt.anchorMax = new Vector2(0.5f, 0.5f);
            radarRt.pivot = new Vector2(0.5f, 0.5f);
            radarRt.anchoredPosition = Vector2.zero;
            radarRt.sizeDelta = new Vector2(320, 320);
            var radar = radarGo.AddComponent<PersonalityRadarGraphic>();

            // 轴标签（绕半径外一点布）
            var axisLabels = new TMP_Text[7];
            string[] labels = { "善良", "邪恶", "冷静", "勇敢", "害羞", "正直", "好奇" };
            float labelRadius = 190f;
            for (int i = 0; i < 7; i++)
            {
                float angle = Mathf.PI * 0.5f - i * Mathf.PI * 2f / 7f;
                var lg = CreateChild(radarHolder, uiLayer, $"AxisLabel_{labels[i]}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                var lrt = (RectTransform)lg.transform;
                lrt.anchorMin = new Vector2(0.5f, 0.5f);
                lrt.anchorMax = new Vector2(0.5f, 0.5f);
                lrt.pivot = new Vector2(0.5f, 0.5f);
                lrt.sizeDelta = new Vector2(120, 30);
                lrt.anchoredPosition = new Vector2(Mathf.Cos(angle) * labelRadius, Mathf.Sin(angle) * labelRadius);
                axisLabels[i] = AddTmp(lg, labels[i], 16, TextAlignmentOptions.Center, new Color(0.85f, 0.9f, 1f, 0.9f));
            }

            // Wire panel refs
            var so = new SerializedObject(panel);
            so.FindProperty("_content").objectReferenceValue = content;
            so.FindProperty("_tabAngel").objectReferenceValue = tabAngel;
            so.FindProperty("_tabDevil").objectReferenceValue = tabDevil;
            so.FindProperty("_petNameText").objectReferenceValue = nameTmp;
            so.FindProperty("_moodText").objectReferenceValue = moodTmp;
            so.FindProperty("_moodFill").objectReferenceValue = moodFill;
            so.FindProperty("_energyText").objectReferenceValue = energyTmp;
            so.FindProperty("_energyFill").objectReferenceValue = energyFill;
            so.FindProperty("_satietyText").objectReferenceValue = satietyTmp;
            so.FindProperty("_satietyFill").objectReferenceValue = satietyFill;
            so.FindProperty("_stateText").objectReferenceValue = stateTmp;
            so.FindProperty("_radar").objectReferenceValue = radar;

            var labelsProp = so.FindProperty("_radarAxisLabels");
            labelsProp.arraySize = axisLabels.Length;
            for (int i = 0; i < axisLabels.Length; i++)
            {
                labelsProp.GetArrayElementAtIndex(i).objectReferenceValue = axisLabels[i];
            }

            so.ApplyModifiedProperties();

            content.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PetStatusPanelAuthoring] Panel_PetStatus 升级完成");
        }

        private static GameObject CreateChild(GameObject parent, int uiLayer, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.layer = uiLayer;
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return go;
        }

        private static TextMeshProUGUI AddTmp(GameObject host, string text, int fontSize, TextAlignmentOptions align, Color color)
        {
            var tmp = host.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            tmp.enableWordWrapping = false;
            host.AddComponent<TMPFontBinder>();
            return tmp;
        }

        private static Button MakeTabButton(GameObject parent, int uiLayer, string name, string label, Color tint)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.layer = uiLayer;
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 48);
            var img = go.AddComponent<Image>();
            img.color = tint;
            var btn = go.AddComponent<Button>();
            var lg = CreateChild(go, uiLayer, "Label", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddTmp(lg, label, 22, TextAlignmentOptions.Center, Color.white);
            return btn;
        }

        private static (TMP_Text text, Image fill) MakeStatBar(GameObject parent, int uiLayer, string name, string label, Color fillColor)
        {
            var row = new GameObject(name);
            row.transform.SetParent(parent.transform, false);
            row.layer = uiLayer;
            var rowRt = row.AddComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(0, 36);

            // Label 文本（悬浮在条上方左侧）
            var labelGo = CreateChild(row, uiLayer, "Label", new Vector2(0, 1), new Vector2(1, 1), new Vector2(4, -20), new Vector2(-4, 0));
            var labelTmp = AddTmp(labelGo, $"{label} 60", 18, TextAlignmentOptions.MidlineLeft, Color.white);

            // 条底
            var barBgGo = CreateChild(row, uiLayer, "Track", new Vector2(0, 0), new Vector2(1, 0), new Vector2(4, 4), new Vector2(-4, 16));
            var barBg = barBgGo.AddComponent<Image>();
            barBg.color = new Color(1f, 1f, 1f, 0.12f);

            // 填充
            var fillGo = CreateChild(barBgGo, uiLayer, "Fill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fill = fillGo.AddComponent<Image>();
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0.6f;

            return (labelTmp, fill);
        }
    }
}
#endif
