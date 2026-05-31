#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.UI;
using GeminiLab.Modules.Collection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 抽卡收藏面板。优先复用场景已有子对象（通过名称查找），
    /// 缺失的元素（金币栏、抽卡按钮、奖励弹窗）在运行时自动创建。
    /// </summary>
    public sealed class GachaPanelController : StubPanelBase
    {
        public override PanelId Id => PanelId.Collection;

        // ---- 从场景查找 / Inspector 绑定 ----
        private Button? _tabPartner;
        private Button? _tabAngel;
        private Button? _tabEvil;
        private Transform? _gridRoot;
        private TMP_Text? _emptyHint;

        // ---- 运行时创建 ----
        private TMP_Text? _balanceText;
        private Button? _singleDrawButton;
        private Button? _multiDrawButton;
        private GameObject? _rewardWindow;
        private TMP_Text? _rewardText;
        private Transform? _rewardGridRoot;
        private Button? _rewardCloseButton;

        private IGachaService? _gacha;
        private ICoinService? _coin;
        private ICollectionService? _collection;
        private EventBus? _eventBus;
        private IDisposable? _coinChangedSub;
        private IDisposable? _collectionChangedSub;

        private string _currentTag = "partner_tag";
        private readonly List<GameObject> _spawned = new();
        private readonly List<GameObject> _rewardSpawned = new();

        private const int SingleCost = 100;
        private const int MultiCost = 500;

        protected override void Awake()
        {
            base.Awake();

            // 找到 Content（StubPanelBase._content 或按名称查找）
            Transform contentRoot = GetContentRoot();

            // 从场景查找已有元素
            FindSceneElements(contentRoot);

            // 为缺失元素创建 UI
            int layer = contentRoot.gameObject.layer;
            EnsureTopBar(contentRoot, layer);
            EnsureDrawSection(contentRoot, layer);
            EnsureRewardWindow(transform, layer);
        }

        private Transform GetContentRoot()
        {
            // 优先用 StubPanelBase._content
            var f = typeof(StubPanelBase).GetField("_content",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f?.GetValue(this) is GameObject go && go != null)
                return go.transform;

            // fallback: 按名称找
            var c = transform.Find("Content");
            if (c != null) return c;

            return transform;
        }

        private void FindSceneElements(Transform contentRoot)
        {
            // Tab 按钮
            var tabs = contentRoot.Find("Tabs");
            if (tabs != null)
            {
                _tabPartner = GetButton(tabs, "Tab_Travel");
                _tabAngel = GetButton(tabs, "Tab_Tarot");
                _tabEvil = GetButton(tabs, "Tab_Garden");

                // 重命名标签文字
                SetLabelText(tabs, "Tab_Travel", "Partner");
                SetLabelText(tabs, "Tab_Tarot", "Angel");
                SetLabelText(tabs, "Tab_Garden", "Evil");
            }

            // Grid 与空态提示
            var gridHolder = contentRoot.Find("GridHolder");
            if (gridHolder != null)
            {
                var grid = gridHolder.Find("Grid");
                if (grid != null) _gridRoot = grid;

                var hint = gridHolder.Find("EmptyHint");
                if (hint != null) _emptyHint = hint.GetComponent<TMP_Text>();
            }

            // 绑定 Tab 点击
            if (_tabPartner != null) _tabPartner.onClick.AddListener(() => SwitchTag("partner_tag"));
            if (_tabAngel != null) _tabAngel.onClick.AddListener(() => SwitchTag("angel_tag"));
            if (_tabEvil != null) _tabEvil.onClick.AddListener(() => SwitchTag("evil_tag"));
        }

        private static Button? GetButton(Transform parent, string childName)
        {
            var t = parent.Find(childName);
            return t?.GetComponent<Button>();
        }

        private static void SetLabelText(Transform parent, string childName, string text)
        {
            var tab = parent.Find(childName);
            var label = tab?.Find("Label");
            var tmp = label?.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = text;
        }

        // ---- 金币栏 ----
        private void EnsureTopBar(Transform contentRoot, int layer)
        {
            // 在 Content 顶部找或创建 CoinBar
            var existing = contentRoot.Find("CoinBar");
            if (existing != null)
            {
                _balanceText = existing.GetComponentInChildren<TMP_Text>();
                return;
            }

            var barGo = new GameObject("CoinBar");
            barGo.transform.SetParent(contentRoot, false);
            barGo.layer = layer;
            var barRt = barGo.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 1);
            barRt.anchorMax = new Vector2(1, 1);
            barRt.pivot = new Vector2(0.5f, 1);
            barRt.anchoredPosition = new Vector2(0, -40);
            barRt.sizeDelta = new Vector2(-24, 32);

            var iconGo = new GameObject("CoinIcon");
            iconGo.transform.SetParent(barGo.transform, false);
            iconGo.layer = layer;
            var ciRt = iconGo.AddComponent<RectTransform>();
            ciRt.anchorMin = new Vector2(0, 0.5f);
            ciRt.anchorMax = new Vector2(0, 0.5f);
            ciRt.pivot = new Vector2(0, 0.5f);
            ciRt.sizeDelta = new Vector2(24, 24);
            var ciImg = iconGo.AddComponent<Image>();
            var coinSprite = Resources.Load<Sprite>("Sprites/Collection/collection_system/coin_button");
            if (coinSprite != null) ciImg.sprite = coinSprite;

            var labelGo = new GameObject("BalanceLabel");
            labelGo.transform.SetParent(barGo.transform, false);
            labelGo.layer = layer;
            var lRt = labelGo.AddComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0, 0.5f);
            lRt.anchorMax = new Vector2(0, 0.5f);
            lRt.pivot = new Vector2(0, 0.5f);
            lRt.anchoredPosition = new Vector2(28, 0);
            lRt.sizeDelta = new Vector2(120, 24);
            _balanceText = labelGo.AddComponent<TextMeshProUGUI>();
            _balanceText.fontSize = 18;
            _balanceText.color = new Color(1f, 0.84f, 0f);
            _balanceText.alignment = TextAlignmentOptions.Left;
            _balanceText.text = "0";
            labelGo.AddComponent<GeminiLab.Modules.UI.Catalogs.TMPFontBinder>();
        }

        // ---- 抽卡按钮 ----
        private void EnsureDrawSection(Transform contentRoot, int layer)
        {
            var existing = contentRoot.Find("DrawBar");
            if (existing != null)
            {
                _singleDrawButton = existing.Find("SingleDraw")?.GetComponent<Button>();
                _multiDrawButton = existing.Find("MultiDraw")?.GetComponent<Button>();
                if (_singleDrawButton != null) _singleDrawButton.onClick.AddListener(OnSingleDraw);
                if (_multiDrawButton != null) _multiDrawButton.onClick.AddListener(OnMultiDraw);
                return;
            }

            var drawBar = new GameObject("DrawBar");
            drawBar.transform.SetParent(contentRoot, false);
            drawBar.layer = layer;
            var dbRt = drawBar.AddComponent<RectTransform>();
            dbRt.anchorMin = new Vector2(0, 0);
            dbRt.anchorMax = new Vector2(1, 0);
            dbRt.pivot = new Vector2(0.5f, 0);
            dbRt.anchoredPosition = new Vector2(0, 12);
            dbRt.sizeDelta = new Vector2(-24, 64);

            var hlg = drawBar.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            _singleDrawButton = CreateDrawButton(drawBar.transform, $"单抽 ({SingleCost})", "SingleDraw", layer, OnSingleDraw);
            _multiDrawButton = CreateDrawButton(drawBar.transform, $"五连抽 ({MultiCost})", "MultiDraw", layer, OnMultiDraw);
        }

        private Button CreateDrawButton(Transform parent, string label, string name, int layer, Action onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = layer;
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 52);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.45f, 0.75f, 1f);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.layer = layer;
            var lRt = labelGo.AddComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.sizeDelta = Vector2.zero;
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 16;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            labelGo.AddComponent<GeminiLab.Modules.UI.Catalogs.TMPFontBinder>();

            return btn;
        }

        // ---- 奖励弹窗 ----
        private void EnsureRewardWindow(Transform root, int layer)
        {
            var existing = root.Find("RewardWindow");
            if (existing != null)
            {
                _rewardWindow = existing.gameObject;
                _rewardCloseButton = existing.Find("CloseButton")?.GetComponent<Button>();
                _rewardText = existing.Find("RewardText")?.GetComponent<TMP_Text>();
                _rewardGridRoot = existing.Find("IconGrid");
                _rewardCloseButton?.onClick.AddListener(CloseReward);
                _rewardWindow.SetActive(false);
                return;
            }

            var rwGo = new GameObject("RewardWindow");
            rwGo.transform.SetParent(root, false);
            rwGo.layer = layer;
            var rwRt = rwGo.AddComponent<RectTransform>();
            rwRt.anchorMin = Vector2.zero;
            rwRt.anchorMax = Vector2.one;
            rwRt.sizeDelta = Vector2.zero;
            var bg = rwGo.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.85f);
            _rewardWindow = rwGo;

            var panel = new GameObject("Panel");
            panel.transform.SetParent(rwGo.transform, false);
            panel.layer = layer;
            var pRt = panel.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.5f, 0.5f);
            pRt.anchorMax = new Vector2(0.5f, 0.5f);
            pRt.sizeDelta = new Vector2(360, 400);
            var panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.12f, 0.12f, 0.18f, 1f);

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panel.transform, false);
            titleGo.layer = layer;
            var tRt = titleGo.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 1);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1);
            tRt.anchoredPosition = new Vector2(0, -12);
            tRt.sizeDelta = new Vector2(-24, 32);
            var tTmp = titleGo.AddComponent<TextMeshProUGUI>();
            tTmp.text = "抽卡结果";
            tTmp.fontSize = 20;
            tTmp.color = Color.white;
            tTmp.alignment = TextAlignmentOptions.Center;
            titleGo.AddComponent<GeminiLab.Modules.UI.Catalogs.TMPFontBinder>();

            var iconGrid = new GameObject("IconGrid");
            iconGrid.transform.SetParent(panel.transform, false);
            iconGrid.layer = layer;
            var igRt = iconGrid.AddComponent<RectTransform>();
            igRt.anchorMin = new Vector2(0, 1);
            igRt.anchorMax = new Vector2(1, 1);
            igRt.pivot = new Vector2(0.5f, 1);
            igRt.anchoredPosition = new Vector2(0, -52);
            igRt.sizeDelta = new Vector2(-24, 80);
            iconGrid.AddComponent<HorizontalLayoutGroup>().spacing = 8;
            _rewardGridRoot = iconGrid.transform;

            var textGo = new GameObject("RewardText");
            textGo.transform.SetParent(panel.transform, false);
            textGo.layer = layer;
            var txtRt = textGo.AddComponent<RectTransform>();
            txtRt.anchorMin = new Vector2(0, 0);
            txtRt.anchorMax = new Vector2(1, 0);
            txtRt.pivot = new Vector2(0.5f, 0);
            txtRt.anchoredPosition = new Vector2(0, 64);
            txtRt.sizeDelta = new Vector2(-24, 140);
            _rewardText = textGo.AddComponent<TextMeshProUGUI>();
            _rewardText.fontSize = 15;
            _rewardText.color = Color.white;
            _rewardText.alignment = TextAlignmentOptions.Center;
            textGo.AddComponent<GeminiLab.Modules.UI.Catalogs.TMPFontBinder>();

            var closeBtnGo = new GameObject("CloseButton");
            closeBtnGo.transform.SetParent(panel.transform, false);
            closeBtnGo.layer = layer;
            var cbRt = closeBtnGo.AddComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0.5f, 0);
            cbRt.anchorMax = new Vector2(0.5f, 0);
            cbRt.pivot = new Vector2(0.5f, 0);
            cbRt.anchoredPosition = new Vector2(0, 12);
            cbRt.sizeDelta = new Vector2(120, 40);
            _rewardCloseButton = closeBtnGo.AddComponent<Button>();
            _rewardCloseButton.onClick.AddListener(CloseReward);
            var cbImg = closeBtnGo.AddComponent<Image>();
            cbImg.color = new Color(0.3f, 0.3f, 0.4f, 1f);

            var cbLabel = new GameObject("Label");
            cbLabel.transform.SetParent(closeBtnGo.transform, false);
            cbLabel.layer = layer;
            var cblRt = cbLabel.AddComponent<RectTransform>();
            cblRt.anchorMin = Vector2.zero;
            cblRt.anchorMax = Vector2.one;
            cblRt.sizeDelta = Vector2.zero;
            var cblTmp = cbLabel.AddComponent<TextMeshProUGUI>();
            cblTmp.text = "关闭";
            cblTmp.fontSize = 16;
            cblTmp.color = Color.white;
            cblTmp.alignment = TextAlignmentOptions.Center;
            cbLabel.AddComponent<GeminiLab.Modules.UI.Catalogs.TMPFontBinder>();

            _rewardWindow.SetActive(false);
        }

        protected override void OnDestroy()
        {
            _coinChangedSub?.Dispose();
            _collectionChangedSub?.Dispose();
            base.OnDestroy();
        }

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);
            EnsureServices();

            if (_eventBus != null)
            {
                _coinChangedSub ??= _eventBus.Subscribe<CoinChangedEvent>(_ => RefreshBalance());
                _collectionChangedSub ??= _eventBus.Subscribe<CollectionChangedEvent>(_ => RefreshGrid());
            }

            RefreshBalance();
            RefreshGrid();
        }

        public override void OnClose()
        {
            base.OnClose();
            _coinChangedSub?.Dispose();
            _coinChangedSub = null;
            _collectionChangedSub?.Dispose();
            _collectionChangedSub = null;
        }

        private void EnsureServices()
        {
            _gacha ??= ServiceLocator.TryResolve(out IGachaService? g) ? g : null;
            _coin ??= ServiceLocator.TryResolve(out ICoinService? c) ? c : null;
            _collection ??= ServiceLocator.TryResolve(out ICollectionService? col) ? col : null;
            _eventBus ??= ServiceLocator.TryResolve(out EventBus? eb) ? eb : null;
        }

        private void SwitchTag(string tag)
        {
            _currentTag = tag;
            RefreshGrid();
        }

        private void RefreshBalance()
        {
            if (_balanceText != null && _coin != null)
                _balanceText.text = $"{_coin.Balance}";

            if (_singleDrawButton != null)
                _singleDrawButton.interactable = _coin != null && _coin.Balance >= SingleCost;
            if (_multiDrawButton != null)
                _multiDrawButton.interactable = _coin != null && _coin.Balance >= MultiCost;
        }

        private void RefreshGrid()
        {
            foreach (var go in _spawned)
            {
                if (go != null) Destroy(go);
            }
            _spawned.Clear();

            if (_gridRoot == null) return;

            int count = 0;
            foreach (var id in GachaService.AllCollectibleIds)
            {
                if (!GachaService.CollectibleTags.TryGetValue(id, out string? tag) || tag != _currentTag)
                    continue;

                SpawnGridItem(id);
                count++;
            }

            if (_emptyHint != null)
            {
                _emptyHint.gameObject.SetActive(count == 0);
                _emptyHint.text = count == 0 ? "（此页签暂无收藏品）" : string.Empty;
            }
        }

        private void SpawnGridItem(string id)
        {
            if (_gridRoot == null) return;

            bool isUnlocked = _gacha != null && _gacha.IsUnlocked(id);
            GachaService.CollectibleNames.TryGetValue(id, out string? displayName);

            var go = new GameObject($"Item_{id}");
            go.transform.SetParent(_gridRoot, false);
            int layer = _gridRoot.gameObject.layer;
            go.layer = layer;

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100, 120);

            var img = go.AddComponent<Image>();
            img.color = isUnlocked ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.6f);

            if (isUnlocked)
            {
                var sprite = Resources.Load<Sprite>($"Sprites/Collection/collection_system/{displayName}");
                if (sprite != null) img.sprite = sprite;
            }
            else
            {
                var locked = Resources.Load<Sprite>("Sprites/Collection/collection_system/unlocked");
                if (locked != null) img.sprite = locked;
            }

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.layer = layer;
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 0);
            lrt.anchorMax = new Vector2(1, 0);
            lrt.pivot = new Vector2(0.5f, 0);
            lrt.anchoredPosition = new Vector2(0, 4);
            lrt.sizeDelta = new Vector2(0, 24);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = isUnlocked ? displayName ?? id : "???";
            tmp.fontSize = 12;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            labelGo.AddComponent<GeminiLab.Modules.UI.Catalogs.TMPFontBinder>();

            _spawned.Add(go);
        }

        // ---- 抽卡 ----
        private void OnSingleDraw()
        {
            if (_gacha == null) return;
            var result = _gacha.PullSingle();
            if (result.Items.Length == 0) return;
            ShowReward(result);
        }

        private void OnMultiDraw()
        {
            if (_gacha == null) return;
            var result = _gacha.PullMulti(5);
            if (result.Items.Length == 0) return;
            ShowReward(result);
        }

        private void ShowReward(GachaResult result)
        {
            if (_rewardWindow == null) return;

            foreach (var go in _rewardSpawned)
            {
                if (go != null) Destroy(go);
            }
            _rewardSpawned.Clear();

            var parts = new List<string>();
            foreach (var item in result.Items)
            {
                GachaService.CollectibleNames.TryGetValue(item.Id, out string? name);
                parts.Add(item.IsNew
                    ? $"<color=#FFD700>{name ?? item.Id}</color> NEW!"
                    : $"<color=#888>{name ?? item.Id}</color> (重复 +30金币)");

                if (_rewardGridRoot != null)
                {
                    var iconGo = new GameObject($"Reward_{item.Id}");
                    iconGo.transform.SetParent(_rewardGridRoot, false);
                    int layer = _rewardGridRoot.gameObject.layer;
                    iconGo.layer = layer;
                    var irt = iconGo.AddComponent<RectTransform>();
                    irt.sizeDelta = new Vector2(64, 64);
                    var iimg = iconGo.AddComponent<Image>();
                    var sprite = Resources.Load<Sprite>($"Sprites/Collection/collection_system/{name}");
                    if (sprite != null) iimg.sprite = sprite;
                    _rewardSpawned.Add(iconGo);
                }
            }

            if (result.CoinRefund > 0)
                parts.Add($"返还 {result.CoinRefund} 金币");

            if (_rewardText != null)
                _rewardText.text = string.Join("\n", parts);

            _rewardWindow.SetActive(true);
            RefreshBalance();
            RefreshGrid();
        }

        private void CloseReward()
        {
            if (_rewardWindow != null) _rewardWindow.SetActive(false);
        }
    }
}
