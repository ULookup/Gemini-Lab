#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Core;
using GeminiLab.Core.UI;
using GeminiLab.Modules.Collection;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.Tarot;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 塔罗面板：三个子页签 —— 抽塔罗 / 历史记录 / 塔罗图鉴。
    /// </summary>
    public sealed class TarotPanelStub : StubPanelBase
    {
        public override PanelId Id => PanelId.Tarot;

        private enum SubView { Draw, History, Guide }

        [Header("Tab 按钮")]
        [SerializeField] private Button? _chouTarotButton;
        [SerializeField] private Button? _historyButton;
        [SerializeField] private Button? _guideButton;

        [Header("子视图根节点")]
        [SerializeField] private GameObject? _drawView;
        [SerializeField] private GameObject? _historyView;
        [SerializeField] private GameObject? _guideView;

        [Header("Tab 高亮色")]
        [SerializeField] private Color _activeTabColor = new Color(1f, 0.85f, 0.3f, 1f);
        [SerializeField] private Color _inactiveTabColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);

        // ---- 抽卡视图 ----
        [Header("卡面")]
        [SerializeField] private Image? _cardImage;
        [SerializeField] private TMP_Text? _cardTitleText;
        [SerializeField] private TMP_Text? _cardOrientationText;

        [Header("操作")]
        [SerializeField] private Button? _drawButton;
        [SerializeField] private TMP_Text? _drawButtonLabel;

        [Header("解读气泡")]
        [SerializeField] private TMP_Text? _angelReadingText;
        [SerializeField] private TMP_Text? _devilReadingText;

        // ---- 历史记录视图 ----
        [Header("历史记录")]
        [SerializeField] private Transform? _historyContentRoot;
        [SerializeField] private GameObject? _historyEntryPrefab;

        // ---- 图鉴视图 ----
        [Header("图鉴")]
        [SerializeField] private Transform? _guideGridRoot;
        [SerializeField] private GameObject? _guideCardPrefab;

        private ITarotService? _tarot;
        private ICollectionService? _collection;
        private CancellationTokenSource? _cts;
        private TarotDrawResult? _currentDraw;
        private SubView _currentTab;
        private Dictionary<SubView, Image?> _tabImages = new();

        protected override void Awake()
        {
            base.Awake();

            EnsureServices();
            CacheTabImages();

            if (_chouTarotButton != null) _chouTarotButton.onClick.AddListener(() => SwitchTab(SubView.Draw));
            if (_historyButton != null) _historyButton.onClick.AddListener(() => SwitchTab(SubView.History));
            if (_guideButton != null) _guideButton.onClick.AddListener(() => SwitchTab(SubView.Guide));

            if (_drawButton != null) _drawButton.onClick.AddListener(OnDrawClicked);
        }

        protected override void OnDestroy()
        {
            if (_drawButton != null) _drawButton.onClick.RemoveListener(OnDrawClicked);
            if (_chouTarotButton != null) _chouTarotButton.onClick.RemoveAllListeners();
            if (_historyButton != null) _historyButton.onClick.RemoveAllListeners();
            if (_guideButton != null) _guideButton.onClick.RemoveAllListeners();
            _cts?.Cancel();
            _cts?.Dispose();
            base.OnDestroy();
        }

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);
            EnsureServices();
            SwitchTab(SubView.Draw);
        }

        public override void OnClose()
        {
            base.OnClose();
            _cts?.Cancel();
        }

        private void EnsureServices()
        {
            if (_tarot == null) ServiceLocator.TryResolve(out _tarot);
            if (_collection == null) ServiceLocator.TryResolve(out _collection);
        }

        private void CacheTabImages()
        {
            _tabImages[SubView.Draw] = _chouTarotButton?.GetComponent<Image>();
            _tabImages[SubView.History] = _historyButton?.GetComponent<Image>();
            _tabImages[SubView.Guide] = _guideButton?.GetComponent<Image>();
        }

        // ======================== Tab 切换 ========================

        private void SwitchTab(SubView tab)
        {
            _currentTab = tab;

            if (_drawView != null) _drawView.SetActive(tab == SubView.Draw);
            if (_historyView != null) _historyView.SetActive(tab == SubView.History);
            if (_guideView != null) _guideView.SetActive(tab == SubView.Guide);

            RefreshTabHighlight();

            switch (tab)
            {
                case SubView.Draw:
                    RefreshDrawView();
                    break;
                case SubView.History:
                    PopulateHistory();
                    break;
                case SubView.Guide:
                    PopulateGuide();
                    break;
            }
        }

        private void RefreshTabHighlight()
        {
            foreach (var (tab, img) in _tabImages)
            {
                if (img != null) img.color = tab == _currentTab ? _activeTabColor : _inactiveTabColor;
            }
        }

        // ======================== 抽卡视图 ========================

        private void RefreshDrawView()
        {
            if (_tarot == null)
            {
                SetDrawButton("塔罗未就绪", interactable: false);
                return;
            }

            if (_tarot.CanDrawToday())
            {
                SetDrawButton("抽今日塔罗", interactable: true);
                ClearCardDisplay();
                ClearReadings();
            }
            else
            {
                SetDrawButton("今天已抽过，明天再来", interactable: false);
            }
        }

        private void OnDrawClicked()
        {
            if (_tarot == null) return;
            if (!_tarot.CanDrawToday()) return;

            var draw = _tarot.DrawDaily();
            if (draw == null)
            {
                SetDrawButton("抽卡失败", interactable: false);
                return;
            }

            _currentDraw = draw.Value;
            ShowCard(_currentDraw.Value);
            SetDrawButton("今天已抽过，明天再来", interactable: false);

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _ = RequestDualReadingsAsync(_currentDraw.Value, _cts.Token);
        }

        private async Task RequestDualReadingsAsync(TarotDrawResult draw, CancellationToken token)
        {
            if (_tarot == null) return;

            var angelTask = _tarot.RequestReadingAsync(draw, PetId.Angel, TarotOrientation.Upright, token);
            var devilTask = _tarot.RequestReadingAsync(draw, PetId.Devil, TarotOrientation.Reversed, token);

            SetReadingText(_angelReadingText, "（天使正在看牌…）");
            SetReadingText(_devilReadingText, "（恶魔正在看牌…）");

            try
            {
                var angel = await angelTask.ConfigureAwait(true);
                if (token.IsCancellationRequested) return;
                SetReadingText(_angelReadingText, angel.Text);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TarotPanel] angel reading failed: {ex.Message}");
                SetReadingText(_angelReadingText, "（天使暂时无法开口。）");
            }

            try
            {
                var devil = await devilTask.ConfigureAwait(true);
                if (token.IsCancellationRequested) return;
                SetReadingText(_devilReadingText, devil.Text);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TarotPanel] devil reading failed: {ex.Message}");
                SetReadingText(_devilReadingText, "（恶魔扭过脸不说话。）");
            }
        }

        private void ShowCard(TarotDrawResult draw)
        {
            if (_cardImage != null && draw.Card.Artwork != null)
            {
                _cardImage.sprite = draw.Card.Artwork;
                _cardImage.color = Color.white;
            }
            if (_cardTitleText != null)
            {
                _cardTitleText.text = $"{draw.Card.DisplayNameZh} · {draw.Card.DisplayNameEn}";
            }
            if (_cardOrientationText != null)
            {
                _cardOrientationText.text = draw.Orientation == TarotOrientation.Upright ? "正位" : "逆位";
            }
        }

        private void ClearCardDisplay()
        {
            if (_cardTitleText != null) _cardTitleText.text = "—";
            if (_cardOrientationText != null) _cardOrientationText.text = "";
        }

        private void ClearReadings()
        {
            SetReadingText(_angelReadingText, "");
            SetReadingText(_devilReadingText, "");
        }

        private void SetDrawButton(string label, bool interactable)
        {
            if (_drawButtonLabel != null) _drawButtonLabel.text = label;
            if (_drawButton != null) _drawButton.interactable = interactable;
        }

        private static void SetReadingText(TMP_Text? tmp, string text)
        {
            if (tmp != null) tmp.text = text;
        }

        // ======================== 历史记录视图 ========================

        private void PopulateHistory()
        {
            if (_historyContentRoot == null || _historyEntryPrefab == null) return;

            for (int i = _historyContentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_historyContentRoot.GetChild(i).gameObject);
            }

            if (_collection == null && !ServiceLocator.TryResolve(out _collection)) return;
            if (_tarot == null && !ServiceLocator.TryResolve(out _tarot)) return;

            var deck = _tarot?.Deck;
            var entries = _collection.GetByCategory(CollectionCategory.Tarot)
                .OrderByDescending(e => e.AcquiredDateIso);

            foreach (var entry in entries)
            {
                var go = Instantiate(_historyEntryPrefab, _historyContentRoot);
                var item = go.GetComponent<TarotHistoryEntry>();
                if (item == null)
                {
                    Debug.LogWarning("[TarotPanel] HistoryEntry prefab 缺少 TarotHistoryEntry 组件");
                    continue;
                }

                Sprite? sprite = null;
                if (deck != null && entry.IconKey.StartsWith("tarot_"))
                {
                    string cardId = entry.IconKey.Substring("tarot_".Length);
                    sprite = deck.Cards.FirstOrDefault(c => c.Id == cardId)?.Artwork;
                }

                item.SetData(entry, sprite);
            }
        }

        // ======================== 图鉴视图 ========================

        private void PopulateGuide()
        {
            if (_guideGridRoot == null || _guideCardPrefab == null) return;

            for (int i = _guideGridRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_guideGridRoot.GetChild(i).gameObject);
            }

            if (_tarot == null && !ServiceLocator.TryResolve(out _tarot)) return;

            var deck = _tarot?.Deck;
            if (deck == null) return;

            foreach (var card in deck.Cards)
            {
                var go = Instantiate(_guideCardPrefab, _guideGridRoot);
                var item = go.GetComponent<TarotGuideCard>();
                if (item == null)
                {
                    Debug.LogWarning("[TarotPanel] GuideCard prefab 缺少 TarotGuideCard 组件");
                    continue;
                }
                item.SetData(card);
            }
        }
    }
}
