# 塔罗历史记录解读文本持久化 — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将塔罗完整 session 数据（三牌 + 六段双宠解读 + 运势总结 + 幸运提示 + 建议）持久化入档，历史记录条目可点击查看详情弹窗。

**Architecture:** 新建 `TarotSessionRecord`（平铺数据模型）+ `TarotSessionRecordStore`（实现 `IPersistentService`），揭示完成时从 `TarotSession` 构建记录写入 store；历史页新增 `TarotHistoryDetailPopup` 弹窗展示完整解读。

**Tech Stack:** Unity C#, JsonUtility, TMPro, CanvasGroup fade animation

---

### Task 1: Create TarotSessionRecord data model

**Files:**
- Create: `Assets/_Project/Scripts/Modules/Tarot/TarotSessionRecord.cs`

- [ ] **Step 1: Write the data class**

```csharp
#nullable enable
using System;

namespace GeminiLab.Modules.Tarot
{
    [Serializable]
    public sealed class TarotSessionRecord
    {
        public string SessionId = string.Empty;
        public string Question = string.Empty;
        public string SessionDateIso = string.Empty;

        public string PastCardId = string.Empty;
        public string PastOrientation = string.Empty;
        public string PastAngelReading = string.Empty;
        public string PastDevilReading = string.Empty;

        public string PresentCardId = string.Empty;
        public string PresentOrientation = string.Empty;
        public string PresentAngelReading = string.Empty;
        public string PresentDevilReading = string.Empty;

        public string FutureCardId = string.Empty;
        public string FutureOrientation = string.Empty;
        public string FutureAngelReading = string.Empty;
        public string FutureDevilReading = string.Empty;

        public int FortuneLevel;
        public string LuckyColor = string.Empty;
        public string LuckyNumber = string.Empty;
        public string LuckyTime = string.Empty;
        public string LuckyAction = string.Empty;
        public string Advice = string.Empty;
    }
}
```

- [ ] **Step 2: Verify compilation — open Unity, wait for script compilation to complete, check Console for errors**

---

### Task 2: Create TarotSessionRecordStore

**Files:**
- Create: `Assets/_Project/Scripts/Modules/Tarot/TarotSessionRecordStore.cs`

- [ ] **Step 1: Write the interface and implementation**

```csharp
#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core.Persistence;
using UnityEngine;

namespace GeminiLab.Modules.Tarot
{
    public interface ITarotSessionRecordStore
    {
        void Add(TarotSessionRecord record);
        IReadOnlyList<TarotSessionRecord> GetAll();
        bool Remove(string sessionId);
    }

    public sealed class TarotSessionRecordStore : ITarotSessionRecordStore, IPersistentService
    {
        private readonly List<TarotSessionRecord> _records = new();

        public string Key => "tarot_history";

        public void Add(TarotSessionRecord record)
        {
            if (string.IsNullOrEmpty(record.SessionId)) return;
            int existing = _records.FindIndex(r => r.SessionId == record.SessionId);
            if (existing >= 0)
                _records[existing] = record;
            else
                _records.Add(record);
        }

        public IReadOnlyList<TarotSessionRecord> GetAll()
        {
            _records.Sort((a, b) => string.Compare(b.SessionDateIso, a.SessionDateIso, StringComparison.Ordinal));
            return _records;
        }

        public bool Remove(string sessionId)
        {
            int idx = _records.FindIndex(r => r.SessionId == sessionId);
            if (idx < 0) return false;
            _records.RemoveAt(idx);
            return true;
        }

        // ---- IPersistentService ----

        [Serializable]
        private struct SavePayload
        {
            public int version;
            public TarotSessionRecord[] records;
        }

        public string CaptureJson()
        {
            return JsonUtility.ToJson(new SavePayload { version = 1, records = _records.ToArray() });
        }

        public bool RestoreJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                var payload = JsonUtility.FromJson<SavePayload>(json);
                _records.Clear();
                if (payload.records != null) _records.AddRange(payload.records);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
```

- [ ] **Step 2: Verify compilation in Unity**

---

### Task 3: Register TarotSessionRecordStore in TarotRuntimeBootstrap

**Files:**
- Modify: `Assets/_Project/Scripts/Modules/Tarot/TarotRuntimeBootstrap.cs`

- [ ] **Step 1: Add store creation and registration in Awake()**

In `TarotRuntimeBootstrap.cs`, after line 43 (`ServiceLocator.Register<ITarotService>(service);`), add:

```csharp
            // Register session record store for persistence
            var recordStore = new TarotSessionRecordStore();
            ServiceLocator.Register<ITarotSessionRecordStore>(recordStore);
            if (ServiceLocator.TryResolve(out IPersistentServiceRegistry? registry) && registry is not null)
                registry.Register(recordStore);
```

Full `Awake()` after change:

```csharp
private void Awake()
{
    if (Application.isPlaying) DontDestroyOnLoad(gameObject);

    if (_deck == null)
    {
        Debug.LogError("[TarotBootstrap] 未绑定 TarotDeckSO，塔罗服务无法初始化");
        return;
    }

    ServiceLocator.TryResolve(out _eventBus);

    ITarotReadingBackend backend = ResolveBackend();
    var service = new TarotService(_deck, _eventBus, backend);
    ServiceLocator.Register<ITarotService>(service);

    // Register session record store for persistence
    var recordStore = new TarotSessionRecordStore();
    ServiceLocator.Register<ITarotSessionRecordStore>(recordStore);
    if (ServiceLocator.TryResolve(out IPersistentServiceRegistry? registry) && registry is not null)
        registry.Register(recordStore);

    Debug.Log($"[TarotBootstrap] TarotService registered. Backend: {backend.GetType().Name}");

    if (_eventBus is not null)
    {
        _drawnSub = _eventBus.Subscribe<TarotDrawnEvent>(OnTarotDrawn);
    }
}
```

- [ ] **Step 2: Add missing using directive at top**

Add after `using GeminiLab.Core.Events;`:
```csharp
using GeminiLab.Core.Persistence;
```

- [ ] **Step 3: Verify compilation in Unity**

---

### Task 4: Create TarotHistoryDetailPopup

**Files:**
- Create: `Assets/_Project/Scripts/Modules/HubUI/Panels/TarotHistoryDetailPopup.cs`

- [ ] **Step 1: Write the popup class**

```csharp
#nullable enable
using System;
using System.Collections;
using System.Linq;
using GeminiLab.Modules.Tarot;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    public sealed class TarotHistoryDetailPopup : MonoBehaviour
    {
        [Header("Top")]
        [SerializeField] private TMP_Text? _dateText;
        [SerializeField] private TMP_Text? _questionText;
        [SerializeField] private TMP_Text? _starsText;

        [Header("Cards")]
        [SerializeField] private Image? _pastCardImage;
        [SerializeField] private Image? _presentCardImage;
        [SerializeField] private Image? _futureCardImage;
        [SerializeField] private TMP_Text? _pastCardLabel;
        [SerializeField] private TMP_Text? _presentCardLabel;
        [SerializeField] private TMP_Text? _futureCardLabel;

        [Header("Angel Readings")]
        [SerializeField] private TMP_Text? _angelPastText;
        [SerializeField] private TMP_Text? _angelPresentText;
        [SerializeField] private TMP_Text? _angelFutureText;

        [Header("Devil Readings")]
        [SerializeField] private TMP_Text? _devilPastText;
        [SerializeField] private TMP_Text? _devilPresentText;
        [SerializeField] private TMP_Text? _devilFutureText;

        [Header("Lucky")]
        [SerializeField] private TMP_Text? _luckyColorText;
        [SerializeField] private TMP_Text? _luckyNumberText;
        [SerializeField] private TMP_Text? _luckyTimeText;
        [SerializeField] private TMP_Text? _luckyActionText;

        [Header("Advice")]
        [SerializeField] private TMP_Text? _adviceText;

        [Header("Controls")]
        [SerializeField] private Button? _closeButton;
        [SerializeField] private Button? _overlayButton;
        [SerializeField] private CanvasGroup? _canvasGroup;
        [SerializeField] private float _fadeDuration = 0.25f;

        private void Awake()
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (_closeButton != null) _closeButton.onClick.AddListener(Hide);
            if (_overlayButton != null) _overlayButton.onClick.AddListener(Hide);

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_closeButton != null) _closeButton.onClick.RemoveListener(Hide);
            if (_overlayButton != null) _overlayButton.onClick.RemoveListener(Hide);
        }

        public void Show(TarotSessionRecord record, TarotDeckSO deck)
        {
            // Top bar
            if (_dateText != null)
            {
                if (DateTime.TryParse(record.SessionDateIso, out var dt))
                    _dateText.text = dt.ToString("yyyy/MM/dd");
                else
                    _dateText.text = record.SessionDateIso;
            }

            if (_questionText != null)
                _questionText.text = !string.IsNullOrEmpty(record.Question)
                    ? record.Question : "今日整体运势";

            if (_starsText != null)
                _starsText.text = new string('★', record.FortuneLevel) + new string('☆', 5 - record.FortuneLevel);

            // Cards
            SetCardSlot(_pastCardImage, _pastCardLabel, deck, record.PastCardId, record.PastOrientation, "过去");
            SetCardSlot(_presentCardImage, _presentCardLabel, deck, record.PresentCardId, record.PresentOrientation, "当下");
            SetCardSlot(_futureCardImage, _futureCardLabel, deck, record.FutureCardId, record.FutureOrientation, "未来");

            // Readings
            if (_angelPastText != null)
            {
                _angelPastText.text = string.IsNullOrEmpty(record.PastAngelReading)
                    ? "" : $"【过去】\n{record.PastAngelReading}";
            }
            if (_angelPresentText != null)
            {
                _angelPresentText.text = string.IsNullOrEmpty(record.PresentAngelReading)
                    ? "" : $"【当下】\n{record.PresentAngelReading}";
            }
            if (_angelFutureText != null)
            {
                _angelFutureText.text = string.IsNullOrEmpty(record.FutureAngelReading)
                    ? "" : $"【未来】\n{record.FutureAngelReading}";
            }

            if (_devilPastText != null)
            {
                _devilPastText.text = string.IsNullOrEmpty(record.PastDevilReading)
                    ? "" : $"【过去】\n{record.PastDevilReading}";
            }
            if (_devilPresentText != null)
            {
                _devilPresentText.text = string.IsNullOrEmpty(record.PresentDevilReading)
                    ? "" : $"【当下】\n{record.PresentDevilReading}";
            }
            if (_devilFutureText != null)
            {
                _devilFutureText.text = string.IsNullOrEmpty(record.FutureDevilReading)
                    ? "" : $"【未来】\n{record.FutureDevilReading}";
            }

            // Lucky hints
            if (_luckyColorText != null) _luckyColorText.text = record.LuckyColor;
            if (_luckyNumberText != null) _luckyNumberText.text = record.LuckyNumber;
            if (_luckyTimeText != null) _luckyTimeText.text = record.LuckyTime;
            if (_luckyActionText != null) _luckyActionText.text = record.LuckyAction;

            // Advice
            if (_adviceText != null) _adviceText.text = record.Advice;

            gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(FadeIn());
        }

        public void Hide()
        {
            StopAllCoroutines();
            StartCoroutine(FadeOut());
        }

        private static void SetCardSlot(Image? img, TMP_Text? label,
            TarotDeckSO deck, string cardId, string orientation, string slotName)
        {
            var card = deck.Cards.FirstOrDefault(c => c.Id == cardId);

            if (img != null)
            {
                if (card != null && card.Artwork != null)
                {
                    img.sprite = card.Artwork;
                    img.color = Color.white;
                }
                else
                {
                    img.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                }
            }

            if (label != null)
            {
                string cardName = card != null ? card.DisplayNameZh : "???";
                string orientZh = orientation == "reversed" ? "逆位" : "正位";
                label.text = $"{cardName}\n{slotName} · {orientZh}";
            }
        }

        private IEnumerator FadeIn()
        {
            if (_canvasGroup == null) yield break;
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(0f, 1f, Mathf.Clamp01(elapsed / _fadeDuration));
                yield return null;
            }
            _canvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOut()
        {
            if (_canvasGroup == null) yield break;
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(elapsed / _fadeDuration));
                yield return null;
            }
            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
    }
}
```

**Note:** `TarotDeckSO.GetCardById(string id)` is used above. If that method doesn't exist on `TarotDeckSO`, we need to add it. Let me check.

- [ ] **Step 2: Check if TarotDeckSO.GetCardById exists; if not, add it**

Check `Assets/_Project/Scripts/Modules/Tarot/TarotDeckSO.cs` for a `GetCardById` method. If missing, add:

```csharp
public TarotCardSO? GetCardById(string id)
{
    foreach (var card in _cards)
        if (card.Id == id) return card;
    return null;
}
```

- [ ] **Step 3: Verify compilation in Unity**

---

### Task 5: Add click event to TarotHistoryEntry

**Files:**
- Modify: `Assets/_Project/Scripts/Modules/HubUI/Panels/TarotHistoryEntry.cs`

- [ ] **Step 1: Rewrite TarotHistoryEntry to support click and carry session data**

Replace the file content:

```csharp
#nullable enable
using System;
using GeminiLab.Modules.Tarot;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    public sealed class TarotHistoryEntry : MonoBehaviour
    {
        [Header("Left: Info")]
        [SerializeField] private TMP_Text? _dateText;
        [SerializeField] private TMP_Text? _typeText;

        [Header("Center: Cards")]
        [SerializeField] private Image? _cardImage1;
        [SerializeField] private Image? _cardImage2;
        [SerializeField] private Image? _cardImage3;

        [Header("Right: Stars")]
        [SerializeField] private TMP_Text? _starsText;

        [Header("Interaction")]
        [SerializeField] private Button? _button;

        public event Action<TarotSessionRecord>? OnClicked;

        private TarotSessionRecord? _record;

        private void Awake()
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_button != null) _button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(HandleClick);
            OnClicked = null;
        }

        public void SetData(string date, string type,
            Sprite? card1, Sprite? card2, Sprite? card3, int fortuneLevel,
            TarotSessionRecord record)
        {
            _record = record;

            if (_dateText != null) _dateText.text = date;
            if (_typeText != null) _typeText.text = type;
            SetCardSprite(_cardImage1, card1);
            SetCardSprite(_cardImage2, card2);
            SetCardSprite(_cardImage3, card3);
            if (_starsText != null)
                _starsText.text = new string('★', fortuneLevel) + new string('☆', 5 - fortuneLevel);
        }

        private void HandleClick()
        {
            if (_record != null)
                OnClicked?.Invoke(_record);
        }

        private static void SetCardSprite(Image? img, Sprite? sprite)
        {
            if (img == null) return;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.color = Color.white;
            }
            else
            {
                img.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            }
        }
    }
}
```

- [ ] **Step 2: Verify compilation in Unity**

---

### Task 6: Wire save logic in TarotPanelStub

**Files:**
- Modify: `Assets/_Project/Scripts/Modules/HubUI/Panels/TarotPanelStub.cs`

- [ ] **Step 1: Add _recordStore field and inject in EnsureServices**

In the `#region` around line 68-76, add:

```csharp
private ITarotSessionRecordStore? _recordStore;
```

In `EnsureServices()` (line 137-141), add after resolving `_collection`:

```csharp
if (_recordStore == null) ServiceLocator.TryResolve(out _recordStore);
```

- [ ] **Step 2: Add SaveSessionRecord method and call from SaveToCollection**

Add new method after `SaveToCollection` (after line 313):

```csharp
private void SaveSessionRecord(TarotSession session)
{
    if (_recordStore == null) return;

    var reading = new TarotSessionRecord
    {
        SessionId = $"tarot_session_{session.SessionDateIso}_{Guid.NewGuid():N}",
        Question = session.Question ?? string.Empty,
        SessionDateIso = session.SessionDateIso,
        FortuneLevel = session.SummaryResult?.fortuneLevel ?? 3,
        LuckyColor = session.SummaryResult?.luckyHint?.color ?? string.Empty,
        LuckyNumber = session.SummaryResult?.luckyHint?.number ?? string.Empty,
        LuckyTime = session.SummaryResult?.luckyHint?.time ?? string.Empty,
        LuckyAction = session.SummaryResult?.luckyHint?.action ?? string.Empty,
        Advice = session.SummaryResult?.advice ?? string.Empty
    };

    FillSlotData(reading, session, TarotSlotPosition.Past,
        ref reading.PastCardId, ref reading.PastOrientation,
        ref reading.PastAngelReading, ref reading.PastDevilReading);
    FillSlotData(reading, session, TarotSlotPosition.Present,
        ref reading.PresentCardId, ref reading.PresentOrientation,
        ref reading.PresentAngelReading, ref reading.PresentDevilReading);
    FillSlotData(reading, session, TarotSlotPosition.Future,
        ref reading.FutureCardId, ref reading.FutureOrientation,
        ref reading.FutureAngelReading, ref reading.FutureDevilReading);

    _recordStore.Add(reading);
}

private static void FillSlotData(TarotSessionRecord record, TarotSession session,
    TarotSlotPosition slot,
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
        angelReading = ar.Text;
    if (session.Readings.TryGetValue(devilKey, out var dr))
        devilReading = dr.Text;
}
```

- [ ] **Step 3: Call SaveSessionRecord in SaveToCollection**

In `SaveToCollection` (line 289), add at the beginning:

```csharp
SaveSessionRecord(session);
```

- [ ] **Step 4: Add missing using for PetId**

Add to the using block:
```csharp
using GeminiLab.Modules.Pet;
```

(It may already be imported — check existing usings.)

- [ ] **Step 5: Verify compilation in Unity**

---

### Task 7: Wire history detail popup in PopulateHistory

**Files:**
- Modify: `Assets/_Project/Scripts/Modules/HubUI/Panels/TarotPanelStub.cs`

- [ ] **Step 1: Add inspector field for the detail popup**

In the `[Header("历史记录")]` section (around line 56-58), add:

```csharp
[SerializeField] private TarotHistoryDetailPopup? _historyDetailPopup;
```

- [ ] **Step 2: Modify PopulateHistory to pass TarotSessionRecord and bind click**

Replace `PopulateHistory()` (lines 317-370) entirely:

```csharp
private void PopulateHistory()
{
    if (_historyContentRoot == null || _historyEntryPrefab == null) return;
    for (int i = _historyContentRoot.childCount - 1; i >= 0; i--)
    {
        var c = _historyContentRoot.GetChild(i).gameObject;
        if (Application.isPlaying) Destroy(c);
        else DestroyImmediate(c);
    }
    if (_collection == null && !ServiceLocator.TryResolve(out _collection)) return;
    if (_tarot == null && !ServiceLocator.TryResolve(out _tarot)) return;
    if (_recordStore == null) ServiceLocator.TryResolve(out _recordStore);

    var deck = _tarot?.Deck;
    var entries = _collection.GetByCategory(CollectionCategory.Tarot);

    // Group by session: same date + same question = one session row
    var groups = entries
        .GroupBy(e => (e.AcquiredDateIso, e.Description))
        .OrderByDescending(g => g.Key.AcquiredDateIso);

    // Build session record lookup: (dateIso, question) -> TarotSessionRecord
    var recordLookup = new Dictionary<(string, string), TarotSessionRecord>();
    if (_recordStore != null)
    {
        foreach (var r in _recordStore.GetAll())
        {
            var key = (r.SessionDateIso, r.Question ?? string.Empty);
            if (!recordLookup.ContainsKey(key))
                recordLookup[key] = r;
        }
    }

    foreach (var group in groups)
    {
        var list = group.ToList();
        if (list.Count == 0) continue;

        var first = list[0];
        var dateText = FormatHistoryDate(first.AcquiredDateIso);
        var typeText = !string.IsNullOrEmpty(first.Description)
            ? first.Description : "今日整体运势";
        var fortuneLevel = list.Max(e => e.FortuneLevel);

        // 3 cards: Past → Present → Future (ordered by slot in IconKey)
        var cardSprites = new Sprite?[3];
        for (int i = 0; i < list.Count && i < 3; i++)
        {
            var entry = list[i];
            if (deck != null && entry.IconKey.StartsWith(TarotIconPrefix))
            {
                string cardId = entry.IconKey.Substring(TarotIconPrefix.Length);
                cardSprites[i] = deck.Cards.FirstOrDefault(c => c.Id == cardId)?.Artwork;
            }
        }

        // Lookup session record
        var recordKey = (first.AcquiredDateIso, first.Description ?? string.Empty);
        recordLookup.TryGetValue(recordKey, out var matchedRecord);

        var go = Instantiate(_historyEntryPrefab, _historyContentRoot);
        var item = go.GetComponent<TarotHistoryEntry>();
        if (item == null) continue;

        item.SetData(dateText, typeText,
            cardSprites[0], cardSprites[1], cardSprites[2], fortuneLevel,
            matchedRecord ?? new TarotSessionRecord
            {
                SessionDateIso = first.AcquiredDateIso,
                Question = first.Description ?? string.Empty,
                FortuneLevel = fortuneLevel
            });

        if (matchedRecord != null && _historyDetailPopup != null && deck != null)
        {
            var capturedRecord = matchedRecord;
            var capturedDeck = deck;
            item.OnClicked += r => _historyDetailPopup.Show(capturedRecord, capturedDeck);
        }
    }

    if (_historyContentRoot is RectTransform rt)
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
}
```

- [ ] **Step 2: Verify compilation in Unity**

---

### Task 8: Unity Editor — Create TarotHistoryDetailPopup prefab

**Files:**
- Create: `Assets/_Project/Prefabs/UI/TarotHistoryDetailPopup.prefab` (via Unity Editor MCP)

- [ ] **Step 1: Build the prefab structure in Unity**

Use the ai-game-developer MCP tools to create the prefab GameObject hierarchy:

```
TarotHistoryDetailPopup (CanvasGroup + TarotHistoryDetailPopup script)
├── Overlay (Image, raycast target, semi-transparent black, covers full screen)
│   └── Button (overlay button to close)
├── ContentRoot (centered panel, background Image)
│   ├── TopBar
│   │   ├── DateText (TMP_Text)
│   │   ├── QuestionText (TMP_Text)
│   │   └── StarsText (TMP_Text)
│   ├── CardsRow (HorizontalLayoutGroup)
│   │   ├── PastCard
│   │   │   ├── CardImage (Image)
│   │   │   └── CardLabel (TMP_Text)
│   │   ├── PresentCard
│   │   │   ├── CardImage (Image)
│   │   │   └── CardLabel (TMP_Text)
│   │   └── FutureCard
│   │       ├── CardImage (Image)
│   │       └── CardLabel (TMP_Text)
│   ├── ReadingsScroll (ScrollRect + Scrollbar)
│   │   └── ReadingsContent (VerticalLayoutGroup)
│   │       ├── AngelHeader (TMP_Text "天使解读")
│   │       ├── AngelPastText (TMP_Text)
│   │       ├── AngelPresentText (TMP_Text)
│   │       ├── AngelFutureText (TMP_Text)
│   │       ├── DevilHeader (TMP_Text "恶魔解读")
│   │       ├── DevilPastText (TMP_Text)
│   │       ├── DevilPresentText (TMP_Text)
│   │       └── DevilFutureText (TMP_Text)
│   ├── LuckySection
│   │   ├── LuckyColorText (TMP_Text)
│   │   ├── LuckyNumberText (TMP_Text)
│   │   ├── LuckyTimeText (TMP_Text)
│   │   └── LuckyActionText (TMP_Text)
│   ├── AdviceText (TMP_Text)
│   └── CloseButton (Button + TMP_Text "关闭")
```

- [ ] **Step 2: Wire all serialized fields in the Inspector**

Bind all `[SerializeField]` fields on the `TarotHistoryDetailPopup` component to the corresponding GameObjects in the hierarchy.

- [ ] **Step 3: Add the popup reference in TarotPanelStub Inspector**

In the TarotPanelStub GameObject in the scene/prefab, drag the newly created `TarotHistoryDetailPopup` prefab into the `_historyDetailPopup` field.

Also ensure each `TarotHistoryEntry` prefab has a `Button` component added at the root and wired to the `_button` field.

- [ ] **Step 4: Playmode test**

1. Enter Play mode
2. Open Tarot panel → complete a draw session (3 cards → reveal → summary)
3. Switch to History tab — verify the new session row appears
4. Click the history entry — verify the detail popup opens with all readings, summary, and lucky hints
5. Close the popup — verify fade out works
6. Exit Play mode, re-enter Play mode — verify history entries persist (loaded from save)
