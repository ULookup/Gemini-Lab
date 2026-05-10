#nullable enable
#if UNITY_EDITOR
using System.Linq;
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
    /// 把 Apartment 场景里占位的 Panel_Tarot 升级为真实塔罗面板：
    /// 左侧卡面 + 正逆位标识，底部"抽卡"按钮，右侧上下两个气泡（天使正位 / 恶魔逆位）。
    /// 幂等：重复运行会先清空 Content 再重建。
    /// </summary>
    public static class TarotPanelAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/Apartment/Apartment_Main.unity";

        [MenuItem("Tools/Gemini-Lab/Author Tarot Panel UI")]
        public static void Author()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var panelRoot = GameObject.Find("Panel_Tarot");
            if (panelRoot == null)
            {
                Debug.LogError("[TarotPanelAuthoring] 未找到 Panel_Tarot，请先跑 Author Apartment Sidebar");
                return;
            }

            var panel = panelRoot.GetComponent<TarotPanelStub>();
            if (panel == null)
            {
                Debug.LogError("[TarotPanelAuthoring] Panel_Tarot 上没有 TarotPanelStub 组件");
                return;
            }

            int uiLayer = panelRoot.layer;
            Transform contentTrans = panelRoot.transform.Find("Content");
            GameObject content = contentTrans != null ? contentTrans.gameObject : CreateUIChild(panelRoot, uiLayer, "Content", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // 清掉 Content 旧的子节点
            for (int i = content.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(content.transform.GetChild(i).gameObject);
            }
            var contentImg = content.GetComponent<Image>();
            if (contentImg == null) contentImg = content.AddComponent<Image>();
            contentImg.color = new Color(0.12f, 0.14f, 0.2f, 0.97f);

            // 标题
            var titleGo = CreateUIChild(content, uiLayer, "Title",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0.92f), new Vector2(0, 1));
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1); titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1);
            titleRt.anchoredPosition = new Vector2(0, -16);
            titleRt.sizeDelta = new Vector2(0, 56);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "每日塔罗";
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.fontSize = 36;
            titleTmp.color = new Color(0.95f, 0.9f, 0.7f, 1f);
            EnsureFontBinder(titleGo);

            // 左侧卡面
            var cardRoot = CreateUIChild(content, uiLayer, "CardRoot",
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            var cardRt = cardRoot.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.05f, 0.2f);
            cardRt.anchorMax = new Vector2(0.05f, 0.2f);
            cardRt.pivot = new Vector2(0, 0);
            cardRt.anchoredPosition = new Vector2(24, 40);
            cardRt.sizeDelta = new Vector2(256, 384);
            var cardImg = cardRoot.AddComponent<Image>();
            cardImg.color = new Color(0.25f, 0.15f, 0.35f, 1f);
            cardImg.preserveAspect = true;

            // 卡名 & 正逆位
            var cardTitleGo = CreateUIChildUnder(cardRoot, uiLayer, "CardTitle");
            var ctRt = cardTitleGo.GetComponent<RectTransform>();
            ctRt.anchorMin = new Vector2(0, 1); ctRt.anchorMax = new Vector2(1, 1);
            ctRt.pivot = new Vector2(0.5f, 0);
            ctRt.anchoredPosition = new Vector2(0, 4);
            ctRt.sizeDelta = new Vector2(0, 28);
            var ctTmp = cardTitleGo.AddComponent<TextMeshProUGUI>();
            ctTmp.text = "—";
            ctTmp.alignment = TextAlignmentOptions.Center;
            ctTmp.fontSize = 22;
            ctTmp.color = Color.white;
            EnsureFontBinder(cardTitleGo);

            var cardOrientGo = CreateUIChildUnder(cardRoot, uiLayer, "CardOrientation");
            var coRt = cardOrientGo.GetComponent<RectTransform>();
            coRt.anchorMin = new Vector2(0, 1); coRt.anchorMax = new Vector2(1, 1);
            coRt.pivot = new Vector2(0.5f, 0);
            coRt.anchoredPosition = new Vector2(0, 32);
            coRt.sizeDelta = new Vector2(0, 24);
            var coTmp = cardOrientGo.AddComponent<TextMeshProUGUI>();
            coTmp.text = "";
            coTmp.alignment = TextAlignmentOptions.Center;
            coTmp.fontSize = 18;
            coTmp.color = new Color(0.9f, 0.85f, 0.5f, 1f);
            EnsureFontBinder(cardOrientGo);

            // 抽卡按钮
            var drawBtnGo = CreateUIChild(content, uiLayer, "DrawButton",
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            var dbRt = drawBtnGo.GetComponent<RectTransform>();
            dbRt.anchorMin = new Vector2(0.05f, 0.1f);
            dbRt.anchorMax = new Vector2(0.05f, 0.1f);
            dbRt.pivot = new Vector2(0, 1);
            dbRt.anchoredPosition = new Vector2(24, 0);
            dbRt.sizeDelta = new Vector2(256, 56);
            var dbImg = drawBtnGo.AddComponent<Image>();
            dbImg.color = new Color(0.3f, 0.35f, 0.55f, 1f);
            var drawBtn = drawBtnGo.AddComponent<Button>();
            var dbLabelGo = CreateUIChildUnder(drawBtnGo, uiLayer, "Label");
            var dblRt = dbLabelGo.GetComponent<RectTransform>();
            dblRt.anchorMin = Vector2.zero; dblRt.anchorMax = Vector2.one;
            dblRt.offsetMin = Vector2.zero; dblRt.offsetMax = Vector2.zero;
            var dblTmp = dbLabelGo.AddComponent<TextMeshProUGUI>();
            dblTmp.text = "抽今日塔罗";
            dblTmp.alignment = TextAlignmentOptions.Center;
            dblTmp.fontSize = 22;
            dblTmp.color = Color.white;
            EnsureFontBinder(dbLabelGo);

            // 右侧气泡：天使（上）/ 恶魔（下）
            var angelBubble = BuildBubble(content, uiLayer, "AngelBubble", "天使 · 正位",
                new Vector2(0.42f, 0.55f), new Vector2(0.95f, 0.90f),
                new Color(0.18f, 0.32f, 0.5f, 0.9f));
            var devilBubble = BuildBubble(content, uiLayer, "DevilBubble", "恶魔 · 逆位",
                new Vector2(0.42f, 0.15f), new Vector2(0.95f, 0.50f),
                new Color(0.45f, 0.18f, 0.28f, 0.9f));

            // 绑定 controller 字段
            var so = new SerializedObject(panel);
            so.FindProperty("_content").objectReferenceValue = content;
            so.FindProperty("_cardImage").objectReferenceValue = cardImg;
            so.FindProperty("_cardTitleText").objectReferenceValue = ctTmp;
            so.FindProperty("_cardOrientationText").objectReferenceValue = coTmp;
            so.FindProperty("_drawButton").objectReferenceValue = drawBtn;
            so.FindProperty("_drawButtonLabel").objectReferenceValue = dblTmp;
            so.FindProperty("_angelReadingText").objectReferenceValue = angelBubble;
            so.FindProperty("_devilReadingText").objectReferenceValue = devilBubble;
            so.ApplyModifiedProperties();

            content.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TarotPanelAuthoring] Panel_Tarot 升级完成");
        }

        private static TMP_Text BuildBubble(GameObject parent, int uiLayer, string name, string header,
            Vector2 anchorMin, Vector2 anchorMax, Color bg)
        {
            var root = CreateUIChild(parent, uiLayer, name, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var img = root.AddComponent<Image>();
            img.color = bg;

            var headerGo = CreateUIChildUnder(root, uiLayer, "Header");
            var hrt = headerGo.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0, 1); hrt.anchorMax = new Vector2(1, 1);
            hrt.pivot = new Vector2(0.5f, 1);
            hrt.anchoredPosition = new Vector2(0, -4);
            hrt.sizeDelta = new Vector2(0, 32);
            var ht = headerGo.AddComponent<TextMeshProUGUI>();
            ht.text = header;
            ht.alignment = TextAlignmentOptions.Center;
            ht.fontSize = 20;
            ht.color = new Color(1f, 1f, 1f, 0.9f);
            EnsureFontBinder(headerGo);

            var textGo = CreateUIChildUnder(root, uiLayer, "Text");
            var trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(18, 12); trt.offsetMax = new Vector2(-18, -38);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "";
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.enableWordWrapping = true;
            tmp.color = Color.white;
            EnsureFontBinder(textGo);
            return tmp;
        }

        private static GameObject CreateUIChild(GameObject parent, int uiLayer, string name,
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

        private static GameObject CreateUIChildUnder(GameObject parent, int uiLayer, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.layer = uiLayer;
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void EnsureFontBinder(GameObject go)
        {
            if (go.GetComponent<TMPFontBinder>() == null)
            {
                go.AddComponent<TMPFontBinder>();
            }
        }
    }
}
#endif
