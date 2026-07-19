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
    /// 塔罗面板：3 阶段状态机 —— Idle / Select / Reveal。
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
        [SerializeField] private GameObject? _revealRoot;
        [SerializeField] private TarotRevealController? _revealController;

        [Header("子视图根节点")]
        [SerializeField] private GameObject? _drawView;
        [SerializeField] private GameObject? _historyView;
        [SerializeField] private GameObject? _guideView;
        [SerializeField] private Button? _guideBackButton;

        [Header("历史记录")]
        [SerializeField] private Transform? _historyContentRoot;
        [SerializeField] private Button? _historyBackButton;
        [SerializeField] private GameObject? _historyEntryPrefab;
        [SerializeField] private TarotHistoryDetailPopup? _historyDetailPopup;

        [Header("图鉴")]
        [SerializeField] private Transform? _guideGridRoot;
        [SerializeField] private GameObject? _guideCardPrefab;
        [SerializeField] private TarotCardDetailPopup? _detailPopup;

        [Header("Layout")]
        [SerializeField] private TarotLayoutSO? _layoutConfig;

        // ---- 运行态 ----
        private const string TarotIconPrefix = "tarot_";
        private ITarotService? _tarot;
        private ICollectionService? _collection;
        private ITarotSessionRecordStore? _recordStore;
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
            if (_historyBackButton != null) _historyBackButton.onClick.AddListener(() => SwitchTab(SubView.Draw));

            if (_drawButton != null)
            {
                _drawButton.onClick.AddListener(OnStartDrawClicked);
                Debug.Log("[TarotPanel] DrawButton 监听已挂载");
            }
            else Debug.LogWarning("[TarotPanel] _drawButton 为 null，Inspector 引用可能丢失！");
            if (_shuffleButton != null) _shuffleButton.onClick.AddListener(OnShuffleClicked);
            if (_confirmButton != null) _confirmButton.onClick.AddListener(OnConfirmSelection);

            _arcLayout = _cardSpreadContainer?.GetComponent<TarotArcLayout>();

            if (_revealController != null)
            {
                _revealController.OnRevealComplete += () =>
                {
                    SaveToCollection(_session!);
                    _session = _tarot!.CreateSession(null);
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
            if (_historyBackButton != null) _historyBackButton.onClick.RemoveAllListeners();
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
            if (_recordStore == null) ServiceLocator.TryResolve(out _recordStore);
            Debug.Log($"[TarotPanel] EnsureServices — _tarot={(_tarot != null ? "OK" : "NULL")}, _collection={(_collection != null ? "OK" : "NULL")}, _recordStore={(_recordStore != null ? "OK" : "NULL")}");
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
            if (_revealRoot != null) _revealRoot.SetActive(stage == Stage.Reveal);

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
            Debug.Log($"[TarotPanel] OnStartDrawClicked 触发 — _tarot={(_tarot != null ? "OK" : "NULL")}");
            if (_tarot == null)
            {
                Debug.LogError("[TarotPanel] _tarot 为 null，无法跳转！检查 ITarotService 是否已注册到 ServiceLocator。");
                return;
            }
            _session = _tarot.CreateSession(null);
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
                child.SetParent(null);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
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
            SaveSessionRecord(session);
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
                    Id = $"{TarotIconPrefix}{card.Id}_{session.SessionDateIso}_{slot}",
                    Category = CollectionCategory.Tarot,
                    Title = $"{card.DisplayNameZh} · {slot switch { TarotSlotPosition.Past => "过去", TarotSlotPosition.Present => "当下", _ => "未来" }}",
                    Description = session.Question ?? string.Empty,
                    AcquiredDateIso = session.SessionDateIso,
                    IconKey = $"{TarotIconPrefix}{card.Id}",
                    FortuneLevel = session.SummaryResult?.fortuneLevel ?? 0
                };
                _collection.Add(entry);
            }
        }

        private void SaveSessionRecord(TarotSession session)
        {
            if (_recordStore == null) return;

            var reading = new TarotSessionRecord
            {
                SessionId = $"tarot_session_{session.SessionDateIso}_{session.Question ?? ""}",
                Question = session.Question ?? string.Empty,
                SessionDateIso = session.SessionDateIso,
                FortuneLevel = session.SummaryResult?.fortuneLevel ?? 3,
                LuckyColor = session.SummaryResult?.luckyHint?.color ?? string.Empty,
                LuckyNumber = session.SummaryResult?.luckyHint?.number ?? string.Empty,
                LuckyTime = session.SummaryResult?.luckyHint?.time ?? string.Empty,
                LuckyAction = session.SummaryResult?.luckyHint?.action ?? string.Empty,
                Advice = session.SummaryResult?.advice ?? string.Empty
            };

            FillSlotData(session, TarotSlotPosition.Past,
                ref reading.PastCardId, ref reading.PastOrientation,
                ref reading.PastAngelReading, ref reading.PastDevilReading);
            FillSlotData(session, TarotSlotPosition.Present,
                ref reading.PresentCardId, ref reading.PresentOrientation,
                ref reading.PresentAngelReading, ref reading.PresentDevilReading);
            FillSlotData(session, TarotSlotPosition.Future,
                ref reading.FutureCardId, ref reading.FutureOrientation,
                ref reading.FutureAngelReading, ref reading.FutureDevilReading);

            reading.Advice = TruncateText(reading.Advice);
            _recordStore.Add(reading);
        }

        private static void FillSlotData(TarotSession session, TarotSlotPosition slot,
            ref string cardId, ref string orientation,
            ref string angelReading, ref string devilReading)
        {
            var draw = session.GetCardAtSlot(slot);
            if (draw == null) return;

            cardId = draw.Value.Card.Id;
            orientation = draw.Value.Orientation == TarotOrientation.Upright ? "upright" : "reversed";

            string angelKey = TarotSession.ReadingKey(slot, PetId.Angel);
            string devilKey = TarotSession.ReadingKey(slot, PetId.Devil);

            if (session.Readings.TryGetValue(angelKey, out var ar))
                angelReading = TruncateText(ar.Text);
            if (session.Readings.TryGetValue(devilKey, out var dr))
                devilReading = TruncateText(dr.Text);
        }

        private static string TruncateText(string text, int maxLength = 200)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;
            return text.Substring(0, maxLength) + "...";
        }

        // ======================== Tab 视图 ========================

        private void PopulateHistory()
        {
            if (_historyContentRoot == null || _historyEntryPrefab == null) return;
            for (int i = _historyContentRoot.childCount - 1; i >= 0; i--)
            {
                var c = _historyContentRoot.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(c);
                else DestroyImmediate(c);
            }
            if (_tarot == null && !ServiceLocator.TryResolve(out _tarot)) return;
            if (_recordStore == null) ServiceLocator.TryResolve(out _recordStore);
            if (_recordStore == null) return;

            var deck = _tarot?.Deck;
            var records = _recordStore.GetAll();

            foreach (var record in records)
            {
                var dateText = FormatHistoryDate(record.SessionDateIso);
                var typeText = !string.IsNullOrEmpty(record.Question)
                    ? record.Question : "今日整体运势";

                Sprite? GetCardSprite(string cardId)
                {
                    if (deck == null || string.IsNullOrEmpty(cardId)) return null;
                    return deck.Cards.FirstOrDefault(c => c.Id == cardId)?.Artwork;
                }

                var go = Instantiate(_historyEntryPrefab, _historyContentRoot);
                var item = go.GetComponent<TarotHistoryEntry>();
                if (item == null) continue;

                var displayFortuneLevel = Mathf.Clamp(record.FortuneLevel, 0, 5);

                item.SetData(dateText, typeText,
                    GetCardSprite(record.PastCardId),
                    GetCardSprite(record.PresentCardId),
                    GetCardSprite(record.FutureCardId),
                    displayFortuneLevel,
                    record);

                if (_historyDetailPopup != null && deck != null)
                {
                    var capturedRecord = record;
                    var capturedDeck = deck;
                    item.OnClicked += r => _historyDetailPopup.Show(capturedRecord, capturedDeck);
                }
            }

            if (_historyContentRoot is RectTransform rt)
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }

        private static string FormatHistoryDate(string isoDate)
        {
            if (System.DateTime.TryParse(isoDate, out var dt))
                return dt.ToString("yyyy/MM/dd");
            return isoDate;
        }

        private void PopulateGuide()
        {
            if (_guideGridRoot == null || _guideCardPrefab == null) return;
            for (int i = _guideGridRoot.childCount - 1; i >= 0; i--)
            {
                var c = _guideGridRoot.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(c);
                else DestroyImmediate(c);
            }
            if (_tarot == null && !ServiceLocator.TryResolve(out _tarot))
                return;

            var deck = _tarot?.Deck;
            if (deck == null) return;

            // Collection-based: show all 22, mark locked/unlocked
            var collectedIds = new HashSet<string>();
            if (_collection == null) ServiceLocator.TryResolve(out _collection);
            if (_collection != null)
            {
                foreach (var e in _collection.GetByCategory(CollectionCategory.Tarot))
                {
                    if (e.IconKey.StartsWith(TarotIconPrefix))
                        collectedIds.Add(e.IconKey.Substring(TarotIconPrefix.Length));
                }
            }

            foreach (var card in deck.Cards)
            {
                var go = Instantiate(_guideCardPrefab, _guideGridRoot);
                var item = go.GetComponent<TarotGuideCard>();
                if (item == null) continue;
                bool unlocked = collectedIds.Contains(card.Id);
                item.SetData(card, unlocked);
                item.OnClicked += c => _detailPopup?.Show(c);
            }

            if (_guideGridRoot is RectTransform rt)
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }
}
