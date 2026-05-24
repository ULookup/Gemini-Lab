#nullable enable
using System;
using System.Collections;
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
    /// 塔罗面板：3 阶段状态机 —— Idle（3按钮）/ Select（选牌）/ Reveal（解读揭晓）。
    /// </summary>
    public sealed class TarotPanelStub : StubPanelBase
    {
        public override PanelId Id => PanelId.Tarot;

        private enum Stage { Idle, Select, Reveal }
        private enum SubView { Draw, History, Guide }

        // ---- Inspector 绑定 ----
        [Header("Idle 阶段")]
        [SerializeField] private Button? _drawButton;       // "开始抽牌"
        [SerializeField] private Button? _historyButton;     // "历史记录"
        [SerializeField] private Button? _guideButton;       // "塔罗图鉴"
        [SerializeField] private TMP_InputField? _questionInput;
        [SerializeField] private GameObject? _idleRoot;

        [Header("Select 阶段")]
        [SerializeField] private GameObject? _selectRoot;
        [SerializeField] private Transform? _cardSpreadContainer; // TarotArcLayout 挂这里
        [SerializeField] private GameObject? _cardSelectablePrefab;
        [SerializeField] private TarotSlot? _pastSlot;
        [SerializeField] private TarotSlot? _presentSlot;
        [SerializeField] private TarotSlot? _futureSlot;
        [SerializeField] private Button? _shuffleButton;
        [SerializeField] private Button? _confirmButton;

        [Header("Reveal 阶段")]
        [SerializeField] private GameObject? _revealRoot;
        [SerializeField] private Image? _revealPastImage;
        [SerializeField] private Image? _revealPresentImage;
        [SerializeField] private Image? _revealFutureImage;
        [SerializeField] private ReadingBubble? _pastAngelBubble;
        [SerializeField] private ReadingBubble? _pastDevilBubble;
        [SerializeField] private ReadingBubble? _presentAngelBubble;
        [SerializeField] private ReadingBubble? _presentDevilBubble;
        [SerializeField] private ReadingBubble? _futureAngelBubble;
        [SerializeField] private ReadingBubble? _futureDevilBubble;
        [SerializeField] private Button? _continueButton;
        [SerializeField] private Button? _finishButton;

        [Header("子视图根节点")]
        [SerializeField] private GameObject? _drawView;
        [SerializeField] private GameObject? _historyView;
        [SerializeField] private GameObject? _guideView;

        [Header("历史记录")]
        [SerializeField] private Transform? _historyContentRoot;
        [SerializeField] private GameObject? _historyEntryPrefab;

        [Header("图鉴")]
        [SerializeField] private Transform? _guideGridRoot;
        [SerializeField] private GameObject? _guideCardPrefab;

        [Header("Tab 按钮 Image（高亮用）")]
        [SerializeField] private Image? _drawTabImage;
        [SerializeField] private Image? _historyTabImage;
        [SerializeField] private Image? _guideTabImage;

        [Header("Tab 高亮色")]
        [SerializeField] private Color _activeTabColor = new Color(1f, 0.85f, 0.3f, 1f);
        [SerializeField] private Color _inactiveTabColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);

        [Header("Layout")]
        [SerializeField] private TarotLayoutSO? _layoutConfig;

        // ---- 运行态 ----
        private ITarotService? _tarot;
        private ICollectionService? _collection;
        private CancellationTokenSource? _cts;
        private TarotSession? _session;
        private Stage _currentStage;
        private SubView _currentTab;
        private TarotArcLayout? _arcLayout;

        // SubView tab Image lookup
        private Dictionary<SubView, Image?> _tabImages = new();

        protected override void Awake()
        {
            base.Awake();
            EnsureServices();
            CacheTabImages();

            if (_historyButton != null) _historyButton.onClick.AddListener(() => SwitchTab(SubView.History));
            if (_guideButton != null) _guideButton.onClick.AddListener(() => SwitchTab(SubView.Guide));

            // Idle → Select (from draw button inside drawView)
            if (_drawButton != null) _drawButton.onClick.AddListener(OnStartDrawClicked);
            if (_shuffleButton != null) _shuffleButton.onClick.AddListener(OnShuffleClicked);
            if (_confirmButton != null) _confirmButton.onClick.AddListener(OnConfirmSelection);
            if (_continueButton != null) _continueButton.onClick.AddListener(OnContinueReveal);
            if (_finishButton != null) _finishButton.onClick.AddListener(OnFinish);

            _arcLayout = _cardSpreadContainer?.GetComponent<TarotArcLayout>();
        }

        protected override void OnDestroy()
        {
            if (_drawButton != null) _drawButton.onClick.RemoveAllListeners();
            if (_historyButton != null) _historyButton.onClick.RemoveAllListeners();
            if (_guideButton != null) _guideButton.onClick.RemoveAllListeners();
            if (_shuffleButton != null) _shuffleButton.onClick.RemoveAllListeners();
            if (_confirmButton != null) _confirmButton.onClick.RemoveAllListeners();
            if (_continueButton != null) _continueButton.onClick.RemoveAllListeners();
            if (_finishButton != null) _finishButton.onClick.RemoveAllListeners();
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

        // ======================== Init ========================

        private void EnsureServices()
        {
            if (_tarot == null) ServiceLocator.TryResolve(out _tarot);
            if (_collection == null) ServiceLocator.TryResolve(out _collection);
        }

        private void CacheTabImages()
        {
            _tabImages[SubView.Draw] = _drawTabImage;
            _tabImages[SubView.History] = _historyTabImage;
            _tabImages[SubView.Guide] = _guideTabImage;
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
                case SubView.Draw: EnterStage(Stage.Idle); break;
                case SubView.History: PopulateHistory(); break;
                case SubView.Guide: PopulateGuide(); break;
            }
        }

        private void RefreshTabHighlight()
        {
            foreach (var (tab, img) in _tabImages)
                if (img != null) img.color = tab == _currentTab ? _activeTabColor : _inactiveTabColor;
        }

        // ======================== Stage 切换 ========================

        private void EnterStage(Stage stage)
        {
            _currentStage = stage;
            if (_idleRoot != null) _idleRoot.SetActive(stage == Stage.Idle);
            if (_selectRoot != null) _selectRoot.SetActive(stage == Stage.Select);
            if (_revealRoot != null) _revealRoot.SetActive(stage == Stage.Reveal);

            switch (stage)
            {
                case Stage.Idle: break;
                case Stage.Select: SetupSelectStage(); break;
                case Stage.Reveal: SetupRevealStage(); break;
            }
        }

        // ======================== Stage.Idle ========================

        private void OnStartDrawClicked()
        {
            if (_tarot == null) return;
            string? question = _questionInput?.text;
            _session = _tarot.CreateSession(question);
            EnterStage(Stage.Select);
        }

        // ======================== Stage.Select ========================

        private void SetupSelectStage()
        {
            if (_session == null) return;
            if (_pastSlot != null) _pastSlot.Initialize(TarotSlotPosition.Past, "过去");
            if (_presentSlot != null) _presentSlot.Initialize(TarotSlotPosition.Present, "当下");
            if (_futureSlot != null) _futureSlot.Initialize(TarotSlotPosition.Future, "未来");

            BuildCardSpread(_session.CandidateCards);
        }

        private void BuildCardSpread(List<TarotCardSO> cards)
        {
            if (_cardSpreadContainer == null || _cardSelectablePrefab == null) return;

            for (int i = _cardSpreadContainer.childCount - 1; i >= 0; i--)
                Destroy(_cardSpreadContainer.GetChild(i).gameObject);

            Sprite? cardBack = _tarot?.Deck?.CardBack;

            foreach (var card in cards)
            {
                var go = Instantiate(_cardSelectablePrefab, _cardSpreadContainer);
                var selectable = go.GetComponent<TarotCardSelectable>();
                if (selectable != null)
                {
                    selectable.SetCard(card, cardBack);
                    selectable.OnClicked += OnCardClicked;
                }
            }

            if (_arcLayout != null)
                StartCoroutine(_arcLayout.ArrangeWithAppear(0.05f));
        }

        private void OnCardClicked(TarotCardSO card)
        {
            if (_session == null || _tarot == null) return;
            if (_session.PickedCount >= 3) return;

            _session = _tarot.PickCard(_session, card);

            // Place into next slot
            var slotPos = (TarotSlotPosition)(_session.PickedCount - 1);
            var slot = GetSlot(slotPos);
            if (slot != null) slot.PlaceCard(card);

            // Flip card to face
            var selectables = _cardSpreadContainer?.GetComponentsInChildren<TarotCardSelectable>();
            if (selectables != null)
            {
                foreach (var s in selectables)
                    if (s.CardData == card) s.FlipToFace(true);
            }
        }

        private TarotSlot? GetSlot(TarotSlotPosition pos) => pos switch
        {
            TarotSlotPosition.Past => _pastSlot,
            TarotSlotPosition.Present => _presentSlot,
            TarotSlotPosition.Future => _futureSlot,
            _ => null
        };

        private void OnShuffleClicked()
        {
            if (_session == null || _tarot == null) return;
            _session = _tarot.ShuffleCards(_session);
            SetupSelectStage();
        }

        private void OnConfirmSelection()
        {
            if (_session == null || _session.PickedCount < 3) return;
            if (_tarot == null) return;
            _session = _tarot.ConfirmSelection(_session);
            EnterStage(Stage.Reveal);
        }

        // ======================== Stage.Reveal ========================

        private void SetupRevealStage()
        {
            if (_session == null) return;

            // Show selected cards in reveal area
            ShowCardInRevealSlot(_revealPastImage, _session.PastCard);
            ShowCardInRevealSlot(_revealPresentImage, _session.PresentCard);
            ShowCardInRevealSlot(_revealFutureImage, _session.FutureCard);

            // Hide all bubbles
            HideAllBubbles();

            // Show loading on past slot
            ShowSlotLoading(TarotSlotPosition.Past);

            // Fire all 6 requests in parallel
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _ = FireAllReadingsAsync(_cts.Token);

            _session.RevealedSlotIndex = 0;
            if (_continueButton != null) _continueButton.gameObject.SetActive(true);
            if (_finishButton != null) _finishButton.gameObject.SetActive(false);
        }

        private async Task FireAllReadingsAsync(CancellationToken token)
        {
            if (_session == null || _tarot == null) return;

            var tasks = new List<Task>();
            var slots = new[] { TarotSlotPosition.Past, TarotSlotPosition.Present, TarotSlotPosition.Future };
            var personas = new[] { (PetId.Angel, TarotOrientation.Upright), (PetId.Devil, TarotOrientation.Reversed) };

            foreach (var slot in slots)
            {
                var draw = _session.GetCardAtSlot(slot);
                if (draw == null) continue;
                foreach (var (petId, orient) in personas)
                {
                    tasks.Add(GetReadingForSlot(slot, draw.Value, petId, orient, token));
                }
            }

            await Task.WhenAll(tasks).ConfigureAwait(true);
        }

        private async Task GetReadingForSlot(TarotSlotPosition slot, TarotDrawResult draw,
            PetId petId, TarotOrientation orientation, CancellationToken token)
        {
            if (_tarot == null) return;
            try
            {
                var reading = await _tarot.RequestReadingAsync(draw, petId, orientation, token)
                    .ConfigureAwait(true);
                if (token.IsCancellationRequested) return;

                string key = TarotSession.ReadingKey(slot, petId);
                if (_session != null) _session.Readings[key] = reading;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TarotPanel] Reading failed for {slot} {petId}: {ex.Message}");
                string key = TarotSession.ReadingKey(slot, petId);
                var fallback = LocalFallback.Build(draw, petId, orientation);
                if (_session != null) _session.Readings[key] = fallback;
            }
        }

        private void OnContinueReveal()
        {
            if (_session == null) return;
            int idx = _session.RevealedSlotIndex;
            if (idx >= 3) return;

            var currentSlot = (TarotSlotPosition)idx;
            RevealSlotReadings(currentSlot);
            _session.RevealedSlotIndex = idx + 1;

            if (_session.RevealedSlotIndex >= 3)
            {
                if (_continueButton != null) _continueButton.gameObject.SetActive(false);
                if (_finishButton != null) _finishButton.gameObject.SetActive(true);
            }
            else
            {
                // Show loading for next slot
                ShowSlotLoading((TarotSlotPosition)_session.RevealedSlotIndex);
            }
        }

        private void RevealSlotReadings(TarotSlotPosition slot)
        {
            if (_session == null) return;

            string angelKey = TarotSession.ReadingKey(slot, PetId.Angel);
            string devilKey = TarotSession.ReadingKey(slot, PetId.Devil);

            string slotName = slot switch
            {
                TarotSlotPosition.Past => "过去",
                TarotSlotPosition.Present => "当下",
                TarotSlotPosition.Future => "未来",
                _ => ""
            };

            var (tarotSlot, angelBubble, devilBubble) = GetSlotBubbles(slot);
            tarotSlot?.ClearLoading();

            if (_session.Readings.TryGetValue(angelKey, out var angelReading))
                angelBubble?.Show($"天使 · {slotName}", angelReading.Text, isAngel: true);
            if (_session.Readings.TryGetValue(devilKey, out var devilReading))
                devilBubble?.Show($"恶魔 · {slotName}", devilReading.Text, isAngel: false);
        }

        private void ShowSlotLoading(TarotSlotPosition slot)
        {
            var (tarotSlot, _, _) = GetSlotBubbles(slot);
            if (_layoutConfig != null && tarotSlot != null)
            {
                string loadingText = _layoutConfig.GetRandomLoadingText(slot, isAngel: true);
                tarotSlot.ShowLoading(loadingText);
            }
        }

        private (TarotSlot?, ReadingBubble?, ReadingBubble?) GetSlotBubbles(TarotSlotPosition slot) => slot switch
        {
            TarotSlotPosition.Past => (_pastSlot, _pastAngelBubble, _pastDevilBubble),
            TarotSlotPosition.Present => (_presentSlot, _presentAngelBubble, _presentDevilBubble),
            TarotSlotPosition.Future => (_futureSlot, _futureAngelBubble, _futureDevilBubble),
            _ => (null, null, null)
        };

        private void ShowCardInRevealSlot(Image? image, TarotDrawResult? draw)
        {
            if (image == null || draw == null) return;
            if (draw.Value.Card.Artwork != null)
            {
                image.sprite = draw.Value.Card.Artwork;
                image.color = Color.white;
            }
        }

        private void HideAllBubbles()
        {
            _pastAngelBubble?.Hide();
            _pastDevilBubble?.Hide();
            _presentAngelBubble?.Hide();
            _presentDevilBubble?.Hide();
            _futureAngelBubble?.Hide();
            _futureDevilBubble?.Hide();
        }

        private void OnFinish()
        {
            if (_session == null) return;
            SaveToCollection(_session);
            EnterStage(Stage.Idle);
        }

        private void SaveToCollection(TarotSession session)
        {
            if (_collection == null) return;

            var slots = new[] { (TarotSlotPosition.Past, session.PastCard),
                                (TarotSlotPosition.Present, session.PresentCard),
                                (TarotSlotPosition.Future, session.FutureCard) };

            foreach (var (slot, draw) in slots)
            {
                if (draw == null) continue;
                var card = draw.Value.Card;
                var entry = new CollectionEntry
                {
                    Id = $"tarot_{card.Id}_{session.SessionDateIso}_{slot}",
                    Category = CollectionCategory.Tarot,
                    Title = $"{card.DisplayNameZh} · {slot switch { TarotSlotPosition.Past => "过去", TarotSlotPosition.Present => "当下", _ => "未来" }}",
                    Description = session.Question ?? string.Empty,
                    AcquiredDateIso = session.SessionDateIso,
                    IconKey = $"tarot_{card.Id}"
                };
                _collection.Add(entry);
            }
        }

        // ======================== Tab 视图 ========================

        private void PopulateHistory()
        {
            if (_historyContentRoot == null || _historyEntryPrefab == null) return;
            for (int i = _historyContentRoot.childCount - 1; i >= 0; i--)
                Destroy(_historyContentRoot.GetChild(i).gameObject);
            if (_collection == null && !ServiceLocator.TryResolve(out _collection)) return;
            if (_tarot == null && !ServiceLocator.TryResolve(out _tarot)) return;

            var deck = _tarot?.Deck;
            var entries = _collection.GetByCategory(CollectionCategory.Tarot)
                .OrderByDescending(e => e.AcquiredDateIso);

            foreach (var entry in entries)
            {
                var go = Instantiate(_historyEntryPrefab, _historyContentRoot);
                var item = go.GetComponent<TarotHistoryEntry>();
                if (item == null) continue;

                Sprite? sprite = null;
                if (deck != null && entry.IconKey.StartsWith("tarot_"))
                {
                    string cardId = entry.IconKey.Substring("tarot_".Length);
                    sprite = deck.Cards.FirstOrDefault(c => c.Id == cardId)?.Artwork;
                }
                item.SetData(entry, sprite);
            }
        }

        private void PopulateGuide()
        {
            if (_guideGridRoot == null || _guideCardPrefab == null) return;
            for (int i = _guideGridRoot.childCount - 1; i >= 0; i--)
                Destroy(_guideGridRoot.GetChild(i).gameObject);
            if (_tarot == null && !ServiceLocator.TryResolve(out _tarot)) return;

            var deck = _tarot?.Deck;
            if (deck == null) return;

            // Collection-based: show all 22, mark locked/unlocked
            var collectedIds = new HashSet<string>();
            if (_collection == null) ServiceLocator.TryResolve(out _collection);
            if (_collection != null)
            {
                foreach (var e in _collection.GetByCategory(CollectionCategory.Tarot))
                {
                    if (e.IconKey.StartsWith("tarot_"))
                        collectedIds.Add(e.IconKey.Substring("tarot_".Length));
                }
            }

            foreach (var card in deck.Cards)
            {
                var go = Instantiate(_guideCardPrefab, _guideGridRoot);
                var item = go.GetComponent<TarotGuideCard>();
                if (item == null) continue;
                bool unlocked = collectedIds.Contains(card.Id);
                item.SetData(card, unlocked);
            }
        }
    }
}
