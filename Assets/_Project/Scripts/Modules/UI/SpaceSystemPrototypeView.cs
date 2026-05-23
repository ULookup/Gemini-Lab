#nullable enable
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GeminiLab.Modules.UI
{
    /// <summary>
    /// 在 Apartment_Main 场景内生成“空间系统”主体 UI 原型。
    /// 布局尽量贴近参考图：上方双角色纸卡，中间共享空间主视觉，底部统一信息条。
    /// </summary>
    [ExecuteAlways]
    public sealed class SpaceSystemPrototypeView : MonoBehaviour
    {
        [SerializeField] private Sprite? _angelSpaceSprite;
        [SerializeField] private Sprite? _demonSpaceSprite;
        [SerializeField] private Sprite? _angelPortraitSprite;
        [SerializeField] private Sprite? _demonPortraitSprite;

        private const string RootName = "SpaceSystemPrototypeRoot";

        private void Awake()
        {
            EnsurePrototype();
        }

        private void OnEnable()
        {
            EnsurePrototype();
        }

        private void OnValidate()
        {
            EnsurePrototype();
        }

        private void EnsurePrototype()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (Application.isPlaying)
            {
                BuildPrototype();
                return;
            }

            Transform existing = transform.Find(RootName);
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }

            BuildPrototype();
        }

        private void BuildPrototype()
        {
            Transform existing = transform.Find(RootName);
            if (existing != null)
            {
                return;
            }

            GameObject root = CreateUIObject(RootName, transform);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.18f, 0.08f);
            rootRect.anchorMax = new Vector2(0.96f, 0.95f);
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            BuildCharacterCards(root.transform);
            BuildSharedSceneBoard(root.transform);
            BuildBottomStrip(root.transform);
        }

        private void BuildCharacterCards(Transform parent)
        {
            GameObject top = CreateUIObject("CharacterCards", parent);
            RectTransform rect = top.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.12f, 0.72f);
            rect.anchorMax = new Vector2(0.88f, 0.98f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            BuildPaperCard(
                top.transform,
                "DemonCard",
                new Vector2(0f, 0.02f),
                new Vector2(0.46f, 1f),
                new Color(0.94f, 0.87f, 0.75f, 0.98f),
                new Color(0.55f, 0.20f, 0.22f, 1f),
                "恶魔·咪咪",
                _demonPortraitSprite,
                moodText: "心情",
                energyText: "能量 85/100");

            BuildPaperCard(
                top.transform,
                "AngelCard",
                new Vector2(0.54f, 0.02f),
                new Vector2(1f, 1f),
                new Color(0.95f, 0.89f, 0.78f, 0.98f),
                new Color(0.22f, 0.40f, 0.58f, 1f),
                "天使·露露",
                _angelPortraitSprite,
                moodText: "心情",
                energyText: "能量 95/100");

            GameObject bond = CreateUIObject("BondHeart", top.transform);
            RectTransform bondRect = bond.GetComponent<RectTransform>();
            bondRect.anchorMin = new Vector2(0.47f, 0.3f);
            bondRect.anchorMax = new Vector2(0.53f, 0.7f);
            bondRect.offsetMin = Vector2.zero;
            bondRect.offsetMax = Vector2.zero;
            Image heartBg = bond.AddComponent<Image>();
            heartBg.color = new Color(0.78f, 0.29f, 0.32f, 0.95f);
            BuildTextFull(bond.transform, "HeartText", "♥", 42, TextAlignmentOptions.Center, new Color(1f, 0.96f, 0.92f, 1f));
        }

        private void BuildPaperCard(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color paperColor,
            Color accentColor,
            string title,
            Sprite? portrait,
            string moodText,
            string energyText)
        {
            GameObject card = CreateUIObject(name, parent);
            RectTransform rect = card.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.Euler(0f, 0f, name.Contains("Demon") ? -2f : 2f);

            Image paper = card.AddComponent<Image>();
            paper.color = paperColor;

            GameObject titleBand = CreateUIObject("TitleBand", card.transform);
            RectTransform titleRect = titleBand.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.04f, 0.70f);
            titleRect.anchorMax = new Vector2(0.96f, 0.96f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            Image titleBg = titleBand.AddComponent<Image>();
            titleBg.color = accentColor;
            BuildTextFull(titleBand.transform, "Title", title, 28, TextAlignmentOptions.Center, new Color(1f, 0.96f, 0.88f, 1f));

            GameObject portraitNode = CreateUIObject("Portrait", card.transform);
            RectTransform portraitRect = portraitNode.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0.04f, 0.12f);
            portraitRect.anchorMax = new Vector2(0.34f, 0.7f);
            portraitRect.offsetMin = Vector2.zero;
            portraitRect.offsetMax = Vector2.zero;
            Image portraitImage = portraitNode.AddComponent<Image>();
            portraitImage.color = portrait != null ? Color.white : new Color(1f, 0.9f, 0.82f, 0.9f);
            portraitImage.sprite = portrait;
            portraitImage.preserveAspect = true;

            GameObject info = CreateUIObject("Info", card.transform);
            RectTransform infoRect = info.GetComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.36f, 0.12f);
            infoRect.anchorMax = new Vector2(0.96f, 0.66f);
            infoRect.offsetMin = Vector2.zero;
            infoRect.offsetMax = Vector2.zero;

            BuildText(info.transform, "MoodLabel", moodText, 22, TextAlignmentOptions.TopLeft, new Color(0.26f, 0.16f, 0.1f, 1f), new Vector2(0f, -8f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, -34f));
            CreateBar(info.transform, "MoodBar", new Vector2(0f, -42f), new Vector2(0.82f, 18f), new Color(0.24f, 0.72f, 0.42f, 1f), 0.82f);
            BuildText(info.transform, "EnergyLabel", energyText, 22, TextAlignmentOptions.TopLeft, new Color(0.26f, 0.16f, 0.1f, 1f), new Vector2(0f, -78f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, -34f));
            CreateBar(info.transform, "EnergyBar", new Vector2(0f, -112f), new Vector2(0.88f, 18f), new Color(0.88f, 0.58f, 0.18f, 1f), 0.74f);
        }

        private void BuildSharedSceneBoard(Transform parent)
        {
            GameObject board = CreateUIObject("SharedSceneBoard", parent);
            RectTransform rect = board.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.06f, 0.23f);
            rect.anchorMax = new Vector2(0.94f, 0.71f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image boardBg = board.AddComponent<Image>();
            boardBg.color = new Color(0.15f, 0.11f, 0.08f, 0.93f);

            GameObject sceneFrame = CreateUIObject("SceneFrame", board.transform);
            RectTransform frameRect = sceneFrame.GetComponent<RectTransform>();
            frameRect.anchorMin = new Vector2(0.04f, 0.08f);
            frameRect.anchorMax = new Vector2(0.96f, 0.92f);
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;
            Image frameBg = sceneFrame.AddComponent<Image>();
            frameBg.color = new Color(0.90f, 0.81f, 0.67f, 0.98f);

            GameObject joinedScene = CreateUIObject("JoinedScene", sceneFrame.transform);
            RectTransform joinedRect = joinedScene.GetComponent<RectTransform>();
            joinedRect.anchorMin = new Vector2(0.02f, 0.02f);
            joinedRect.anchorMax = new Vector2(0.98f, 0.98f);
            joinedRect.offsetMin = Vector2.zero;
            joinedRect.offsetMax = Vector2.zero;

            GameObject demonZone = CreateUIObject("DemonZone", joinedScene.transform);
            RectTransform demonRect = demonZone.GetComponent<RectTransform>();
            demonRect.anchorMin = new Vector2(0f, 0f);
            demonRect.anchorMax = new Vector2(0.52f, 1f);
            demonRect.offsetMin = Vector2.zero;
            demonRect.offsetMax = Vector2.zero;
            Image demonBg = demonZone.AddComponent<Image>();
            demonBg.color = new Color(0.32f, 0.14f, 0.15f, 0.82f);
            if (_demonSpaceSprite != null)
            {
                demonBg.sprite = _demonSpaceSprite;
                demonBg.type = Image.Type.Simple;
                demonBg.preserveAspect = false;
            }

            GameObject angelZone = CreateUIObject("AngelZone", joinedScene.transform);
            RectTransform angelRect = angelZone.GetComponent<RectTransform>();
            angelRect.anchorMin = new Vector2(0.48f, 0f);
            angelRect.anchorMax = new Vector2(1f, 1f);
            angelRect.offsetMin = Vector2.zero;
            angelRect.offsetMax = Vector2.zero;
            Image angelBg = angelZone.AddComponent<Image>();
            angelBg.color = new Color(0.58f, 0.73f, 0.86f, 0.88f);
            if (_angelSpaceSprite != null)
            {
                angelBg.sprite = _angelSpaceSprite;
                angelBg.type = Image.Type.Simple;
                angelBg.preserveAspect = false;
            }

            CreatePaperTag(joinedScene.transform, "DemonTag", "恶魔区", new Vector2(0.02f, 0.84f), new Vector2(0.28f, 0.98f), new Color(0.52f, 0.18f, 0.20f, 0.96f));
            CreatePaperTag(joinedScene.transform, "AngelTag", "天使区", new Vector2(0.72f, 0.84f), new Vector2(0.98f, 0.98f), new Color(0.21f, 0.43f, 0.64f, 0.96f));
            CreateAlertBadge(joinedScene.transform, "DemonAlert", new Vector2(0.31f, 0.88f));
            CreateAlertBadge(joinedScene.transform, "AngelAlert", new Vector2(0.69f, 0.88f));
        }

        private void BuildBottomStrip(Transform parent)
        {
            GameObject bottom = CreateUIObject("BottomInfoStrip", parent);
            RectTransform rect = bottom.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.12f, 0.05f);
            rect.anchorMax = new Vector2(0.88f, 0.18f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = bottom.AddComponent<Image>();
            bg.color = new Color(0.93f, 0.86f, 0.72f, 0.97f);

            BuildText(bottom.transform, "ComfortLabel", "舒适度", 26, TextAlignmentOptions.TopLeft, new Color(0.29f, 0.18f, 0.10f, 1f), new Vector2(18f, -10f), new Vector2(0.3f, 1f), new Vector2(0f, 1f), new Vector2(0f, -30f));
            BuildText(bottom.transform, "ComfortHearts", "♥ ♥ ♥ ♥ ♥", 28, TextAlignmentOptions.TopLeft, new Color(0.74f, 0.29f, 0.34f, 1f), new Vector2(18f, -42f), new Vector2(0.3f, 1f), new Vector2(0f, 1f), new Vector2(0f, -34f));
            BuildText(bottom.transform, "Desc", "双宠共居空间：温暖、陪伴、风格共生", 22, TextAlignmentOptions.MidlineLeft, new Color(0.32f, 0.20f, 0.12f, 1f), new Vector2(0f, 0f), new Vector2(0.76f, 1f), new Vector2(0.28f, 0f), new Vector2(0f, 0f));

            GameObject preview = CreateUIObject("DecorPreviewButton", bottom.transform);
            RectTransform previewRect = preview.GetComponent<RectTransform>();
            previewRect.anchorMin = new Vector2(0.78f, 0.18f);
            previewRect.anchorMax = new Vector2(0.97f, 0.82f);
            previewRect.offsetMin = Vector2.zero;
            previewRect.offsetMax = Vector2.zero;
            Image previewBg = preview.AddComponent<Image>();
            previewBg.color = new Color(0.78f, 0.64f, 0.36f, 1f);
            BuildTextFull(preview.transform, "Label", "装饰预览", 22, TextAlignmentOptions.Center, new Color(0.24f, 0.14f, 0.08f, 1f));
        }

        private void CreatePaperTag(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject tag = CreateUIObject(name, parent);
            RectTransform rect = tag.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image bg = tag.AddComponent<Image>();
            bg.color = color;
            BuildTextFull(tag.transform, "Label", text, 24, TextAlignmentOptions.Center, new Color(1f, 0.95f, 0.88f, 1f));
        }

        private void CreateAlertBadge(Transform parent, string name, Vector2 anchor)
        {
            GameObject badge = CreateUIObject(name, parent);
            RectTransform rect = badge.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(36f, 36f);
            rect.anchoredPosition = Vector2.zero;
            Image bg = badge.AddComponent<Image>();
            bg.color = new Color(0.86f, 0.36f, 0.14f, 0.98f);
            BuildTextFull(badge.transform, "Mark", "!", 26, TextAlignmentOptions.Center, Color.white);
        }

        private static void CreateBar(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color fillColor, float fill)
        {
            GameObject bar = CreateUIObject(name, parent);
            RectTransform rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            Image bg = bar.AddComponent<Image>();
            bg.color = new Color(0.22f, 0.18f, 0.14f, 0.9f);

            GameObject fillNode = CreateUIObject(name + "Fill", bar.transform);
            RectTransform fillRect = fillNode.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(fill, 1f);
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            Image fillBg = fillNode.AddComponent<Image>();
            fillBg.color = fillColor;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new(name);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            return go;
        }

        private static void BuildTextFull(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
        }

        private static void BuildText(
            Transform parent,
            string name,
            string text,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color,
            Vector2 anchoredPosition,
            Vector2 anchorMax,
            Vector2 anchorMin,
            Vector2 sizeDelta)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            if (sizeDelta != Vector2.zero)
            {
                rect.sizeDelta = sizeDelta;
            }

            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
        }
    }
}
