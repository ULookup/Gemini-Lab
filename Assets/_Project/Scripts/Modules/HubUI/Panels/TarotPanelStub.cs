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

        [Header("Reveal")]
        [SerializeField] private TarotRevealController? _revealController;

        [Header("子视图根节点")]
        [SerializeField] private GameObject? _drawView;
        [SerializeField] private GameObject? _historyView;
        [SerializeField] private GameObject? _guideView;
        [SerializeField] private Button? _guideBackButton;

        [Header("历史记录")]
        [SerializeField] private Transform? _historyContentRoot;
        [SerializeField] private GameObject? _historyEntryPrefab;

        [Header("图鉴")]
        [SerializeField] private Transform? _guideGridRoot;
        [SerializeField] private GameObject? _guideCardPrefab;

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

        protected override void Awake()
        {
            base.Awake();
            EnsureServices();

            if (_historyButton != null) _historyButton.onClick.AddListener(() => SwitchTab(SubView.History));
            if (_guideButton != null) _guideButton.onClick.AddListener(() => SwitchTab(SubView.Guide));
            if (_guideBackButton != null) _guideBackButton.onClick.AddListener(() => SwitchTab(SubView.Draw));

            // Idle → Select (from draw button inside drawView)
            if (_drawButton != null) _drawButton.onClick.AddListener(OnStartDrawClicked);
            if (_shuffleButton != null) _shuffleButton.onClick.AddListener(OnShuffleClicked);
            if (_confirmButton != null) _confirmButton.onClick.AddListener(OnConfirmSelection);

            _arcLayout = _cardSpreadContainer?.GetComponent<TarotArcLayout>();

            if (_revealController != null)
            {
                _revealController.OnRevealComplete += () =>
                {
                    SaveToCollection(_session!);
                    _session = _tarot!.CreateSession(_questionInput?.text);
                    EnterStage(Stage.Select);
                };
                _revealController.OnOpenGuide += () =>
                {
                    SaveToCollection(_session!);
                    SwitchTab(SubView.Guide);
                };
            }
        }

        protected override void OnDestroy()
        {
            if (_drawButton != null) _drawButton.onClick.RemoveAllListeners();
            if (_historyButton != null) _historyButton.onClick.RemoveAllListeners();
            if (_guideButton != null) _guideButton.onClick.RemoveAllListeners();
            if (_guideBackButton != null) _guideBackButton.onClick.RemoveAllListeners();
            if (_shuffleButton != null) _shuffleButton.onClick.RemoveAllListeners();
            if (_confirmButton != null) _confirmButton.onClick.RemoveAllListeners();
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

        // ======================== Tab 切换 ========================

        private void SwitchTab(SubView tab)
        {
            _currentTab = tab;
            if (_drawView != null) _drawView.SetActive(tab == SubView.Draw);
            if (_historyView != null) _historyView.SetActive(tab == SubView.History);
            if (_guideView != null) _guideView.SetActive(tab == SubView.Guide);

            switch (tab)
            {
                case SubView.Draw: EnterStage(Stage.Idle); break;
                case SubView.History: PopulateHistory(); break;
                case SubView.Guide: PopulateGuide(); break;
            }
        }

        // ======================== Stage 切换 ========================

        private void EnterStage(Stage stage)
        {
            _currentStage = stage;
            if (_idleRoot != null) _idleRoot.SetActive(stage == Stage.Idle);
            if (_selectRoot != null) _selectRoot.SetActive(stage == Stage.Select);

            switch (stage)
            {
                case Stage.Idle: break;
                case Stage.Select: SetupSelectStage(); break;
                case Stage.Reveal:
                    if (_revealController != null && _session != null && _tarot != null)
                        _revealController.BeginReveal(_session, _tarot);
                    break;
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
            {
                var child = _cardSpreadContainer.GetChild(i);
                child.SetParent(null);       // 立即脱离容器层级，避免后续排列搜到残留
                Destroy(child.gameObject);
            }

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

            // Find the clicked card's selectable
            var selectables = _cardSpreadContainer?.GetComponentsInChildren<TarotCardSelectable>();
            TarotCardSelectable? clicked = null;
            if (selectables != null)
            {
                foreach (var s in selectables)
                    if (s.CardData == card) { clicked = s; break; }
            }

            _session = _tarot.PickCard(_session, card);

            var slotPos = (TarotSlotPosition)(_session.PickedCount - 1);
            var slot = GetSlot(slotPos);

            if (clicked != null && slot != null)
            {
                // Flip to face + fly to slot
                clicked.FlipToFace(true);
                var slotRt = slot.GetComponent<RectTransform>();
                if (slotRt != null)
                    StartCoroutine(clicked.FlyToSlot(slotRt, _layoutConfig?.CardFlyDuration ?? 0.5f, () =>
                    {
                        slot.PlaceCard(card);
                    }));
            }
            else if (slot != null)
            {
                slot.PlaceCard(card);
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
