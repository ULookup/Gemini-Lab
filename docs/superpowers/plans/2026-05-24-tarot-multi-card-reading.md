# Tarot Multi-Card Reading — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace "daily random 1-card draw" with an immersive 3-card manual-selection flow (Past/Present/Future) backed by Direct LLM dual-persona readings, with collection-based guide and session-based history.

**Architecture:** Extend existing `ITarotService` / `TarotService` with session management methods, add `DirectLLMBackend` alongside the existing `ITarotReadingBackend` interface, rewrite `TarotPanelStub` as a 3-stage state machine (Idle → Select → Reveal), introduce 4 new Prefab scripts and 2 new ScriptableObjects for data-driven UI configuration.

**Tech Stack:** C# / Unity, TMP, UnityWebRequest, EventBus, ServiceLocator, CollectionService, IPersonalityEvolutionService

---

### File Structure

| File | Action | Purpose |
|------|--------|---------|
| `TarotModels.cs` | Modify | Add `TarotSession`, `TarotSlotPosition` enum |
| `LLMConfigSO.cs` | Create | LLM connection + prompt template configuration |
| `TarotLayoutSO.cs` | Create | Arc layout + animation + loading text parameters |
| `DirectLLMBackend.cs` | Create | UnityWebRequest-based LLM reading backend |
| `ITarotService.cs` | Modify | Add session management methods, remove daily limit |
| `TarotService.cs` | Modify | Implement session management, remove PlayerPrefs date logic |
| `TarotRuntimeBootstrap.cs` | Modify | Inject `LLMConfigSO`, select backend dynamically |
| `TarotCardSelectable.cs` | Create | Prefab script: interactable card with hover/select/fly |
| `TarotSlot.cs` | Create | Prefab script: position slot with label + loading state |
| `ReadingBubble.cs` | Create | Prefab script: reading text bubble with appear animation |
| `TarotArcLayout.cs` | Create | Layout script: position children in arc using TarotLayoutSO |
| `TarotPanelStub.cs` | Modify | Full rewrite: 3-stage state machine |
| `TarotGuideCard.cs` | Modify | Support locked/unlocked state |
| `TarotHistoryEntry.cs` | Modify | Session-based display (3 cards + question + readings) |

---

### Task 1: Add TarotSlotPosition enum and TarotSession class

**Files:**
- Modify: `Assets/_Project/Scripts/Modules/Tarot/TarotModels.cs`

- [ ] **Step 1: Append new types to TarotModels.cs**

Add after the existing `TarotReadingReceivedEvent` struct:

```csharp
/// <summary>三张牌的槽位位置。</summary>
public enum TarotSlotPosition
{
    Past = 0,
    Present = 1,
    Future = 2
}

/// <summary>
/// 一次完整的抽牌会话。由 TarotPanelStub 持有和驱动。
/// </summary>
public sealed class TarotSession
{
    public string Question;
    public string SessionDateIso;
    public List<TarotCardSO> CandidateCards = new();
    public TarotDrawResult? PastCard;
    public TarotDrawResult? PresentCard;
    public TarotDrawResult? FutureCard;
    public int PickedCount;
    /// <summary>key = "past_angel" / "past_devil" / "present_angel" 等</summary>
    public Dictionary<string, TarotReading> Readings = new();
    public int RevealedSlotIndex;

    public TarotDrawResult? GetCardAtSlot(TarotSlotPosition slot)
    {
        return slot switch
        {
            TarotSlotPosition.Past => PastCard,
            TarotSlotPosition.Present => PresentCard,
            TarotSlotPosition.Future => FutureCard,
            _ => null
        };
    }

    public void SetCardAtSlot(TarotSlotPosition slot, TarotDrawResult draw)
    {
        switch (slot)
        {
            case TarotSlotPosition.Past: PastCard = draw; break;
            case TarotSlotPosition.Present: PresentCard = draw; break;
            case TarotSlotPosition.Future: FutureCard = draw; break;
        }
    }

    public static string ReadingKey(TarotSlotPosition slot, PetId petId)
    {
        string slotName = slot switch
        {
            TarotSlotPosition.Past => "past",
            TarotSlotPosition.Present => "present",
            TarotSlotPosition.Future => "future",
            _ => "unknown"
        };
        string petName = petId == PetId.Angel ? "angel" : "devil";
        return $"{slotName}_{petName}";
    }
}
```

Add `using System.Collections.Generic;` to top of file imports (if not already present).

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Scripts/Modules/Tarot/TarotModels.cs
git commit -m "feat(tarot): add TarotSession model and TarotSlotPosition enum"
```

---

### Task 2: Create LLMConfigSO ScriptableObject

**Files:**
- Create: `Assets/_Project/Scripts/Modules/Tarot/LLMConfigSO.cs`

- [ ] **Step 1: Write LLMConfigSO**

```csharp
#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Tarot
{
    /// <summary>
    /// LLM 直连配置：endpoint / key / model / prompt 模板。
    /// 在 Project 窗口右键 Create → GeminiLab → Tarot → LLM Config 创建。
    /// </summary>
    [CreateAssetMenu(menuName = "GeminiLab/Tarot/LLM Config", fileName = "LLMConfig")]
    public sealed class LLMConfigSO : ScriptableObject
    {
        [Tooltip("OpenAI 兼容 API 地址，如 https://api.openai.com/v1/chat/completions")]
        [SerializeField] private string _endpoint = "https://api.openai.com/v1/chat/completions";

        [Tooltip("API Key")]
        [SerializeField] private string _apiKey = string.Empty;

        [Tooltip("模型名，如 gpt-4o / claude-sonnet-4-6")]
        [SerializeField] private string _model = "gpt-4o";

        [Tooltip("天使 System prompt。占位符: {personality}")]
        [TextArea(5, 20)]
        [SerializeField] private string _angelSystemTemplate =
            "你是一位天使 (Angel) —— 温柔、包容、愿意指出希望。\n当前的你：{personality}\n用 2-3 句中文给出塔罗解读，不超过 80 个汉字。";

        [Tooltip("恶魔 System prompt。占位符: {personality}")]
        [TextArea(5, 20)]
        [SerializeField] private string _devilSystemTemplate =
            "你是一位恶魔 (Devil) —— 尖锐、坦白、敢把阴影讲透。\n当前的你：{personality}\n用 2-3 句中文给出塔罗解读，不超过 80 个汉字。回答要带戏剧性但不恶毒。";

        [Tooltip("User 消息模板。占位符: {cardName}, {slotName}, {question}, {keywords}")]
        [TextArea(3, 10)]
        [SerializeField] private string _userMessageTemplate =
            "玩家抽到了：{cardName}。这是代表「{slotName}」的牌。\n" +
            "玩家想问：{question}\n" +
            "关键词：{keywords}\n" +
            "请从你的人格视角给出「{slotName}」的解读。";

        [Tooltip("单次请求超时秒数")]
        [SerializeField] private float _timeoutSeconds = 30f;

        public string Endpoint => _endpoint;
        public string ApiKey => _apiKey;
        public string Model => _model;
        public string AngelSystemTemplate => _angelSystemTemplate;
        public string DevilSystemTemplate => _devilSystemTemplate;
        public string UserMessageTemplate => _userMessageTemplate;
        public float TimeoutSeconds => _timeoutSeconds;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_endpoint) && !string.IsNullOrWhiteSpace(_apiKey);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Scripts/Modules/Tarot/LLMConfigSO.cs
git commit -m "feat(tarot): add LLMConfigSO for direct LLM backend configuration"
```

---

### Task 3: Create TarotLayoutSO ScriptableObject

**Files:**
- Create: `Assets/_Project/Scripts/Modules/Tarot/TarotLayoutSO.cs`

- [ ] **Step 1: Write TarotLayoutSO**

```csharp
#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Tarot
{
    /// <summary>
    /// 塔罗选牌 UI 布局参数。美术/策划在 Inspector 调整，脚本只读。
    /// </summary>
    [CreateAssetMenu(menuName = "GeminiLab/Tarot/Tarot Layout", fileName = "TarotLayout")]
    public sealed class TarotLayoutSO : ScriptableObject
    {
        [Header("弧形排列")]
        [Tooltip("弧形半径（像素）")]
        [SerializeField] private float _arcRadius = 400f;

        [Tooltip("弧形展开角度（度）")]
        [SerializeField] private float _arcSpanAngle = 160f;

        [Tooltip("展示牌数量")]
        [SerializeField] private int _cardSpreadCount = 11;

        [Header("Hover 效果")]
        [Tooltip("hover 放大倍率")]
        [SerializeField] private float _hoverScale = 1.15f;

        [Tooltip("hover 上浮距离（像素）")]
        [SerializeField] private float _hoverLift = 30f;

        [Header("动画")]
        [Tooltip("牌浮现动画时长（秒）")]
        [SerializeField] private float _cardAppearDuration = 0.4f;

        [Tooltip("牌飞入槽位动画时长（秒）")]
        [SerializeField] private float _cardFlyDuration = 0.5f;

        [Tooltip("每幕解读揭晓之间最小间隔（秒）")]
        [SerializeField] private float _revealIntervalSeconds = 1.5f;

        [Header("等待文案")]
        [Tooltip("等待解读时的情境文案池，按位置分组")]
        [SerializeField] private string[] _pastLoadingTexts = new string[]
        {
            "天使正在回望你的过去…",
            "恶魔翻开了昨日的账本…",
        };

        [SerializeField] private string[] _presentLoadingTexts = new string[]
        {
            "天使在凝视此刻的因果…",
            "恶魔端详着你现在的选择…",
        };

        [SerializeField] private string[] _futureLoadingTexts = new string[]
        {
            "天使在为你铺展前路…",
            "恶魔看到了你想要又不敢要的东西…",
        };

        public float ArcRadius => _arcRadius;
        public float ArcSpanAngle => _arcSpanAngle;
        public int CardSpreadCount => _cardSpreadCount;
        public float HoverScale => _hoverScale;
        public float HoverLift => _hoverLift;
        public float CardAppearDuration => _cardAppearDuration;
        public float CardFlyDuration => _cardFlyDuration;
        public float RevealIntervalSeconds => _revealIntervalSeconds;

        public string GetRandomLoadingText(TarotSlotPosition slot, bool isAngel)
        {
            var pool = slot switch
            {
                TarotSlotPosition.Past => _pastLoadingTexts,
                TarotSlotPosition.Present => _presentLoadingTexts,
                TarotSlotPosition.Future => _futureLoadingTexts,
                _ => _pastLoadingTexts
            };
            if (pool.Length == 0) return "…";
            return pool[UnityEngine.Random.Range(0, pool.Length)];
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Scripts/Modules/Tarot/TarotLayoutSO.cs
git commit -m "feat(tarot): add TarotLayoutSO for data-driven UI arc layout params"
```

---

### Task 4: Create DirectLLMBackend

**Files:**
- Create: `Assets/_Project/Scripts/Modules/Tarot/DirectLLMBackend.cs`

- [ ] **Step 1: Write DirectLLMBackend**

```csharp
#nullable enable
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Core;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.Pet.Personality;
using UnityEngine;
using UnityEngine.Networking;

namespace GeminiLab.Modules.Tarot
{
    /// <summary>
    /// UnityWebRequest 直连 OpenAI 兼容 LLM API 的塔罗解读后端。
    /// 需要 LLMConfigSO 配置 endpoint + key；未配置时回退到 LocalFallback。
    /// </summary>
    public sealed class DirectLLMBackend : ITarotReadingBackend
    {
        private readonly LLMConfigSO _config;

        public DirectLLMBackend(LLMConfigSO config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task<TarotReading> RequestAsync(
            TarotDrawResult draw,
            PetId petId,
            TarotOrientation orientation,
            CancellationToken cancellationToken)
        {
            if (!_config.IsConfigured)
            {
                return LocalFallback.Build(draw, petId, orientation);
            }

            string systemPrompt = BuildSystemPrompt(petId);
            string userPrompt = BuildUserPrompt(draw, petId);

            string responseText;
            try
            {
                responseText = await SendRequestAsync(systemPrompt, userPrompt, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DirectLLM] Request failed: {ex.Message}");
                return LocalFallback.Build(draw, petId, orientation);
            }

            return new TarotReading(petId, orientation, responseText, isFromGateway: true);
        }

        private string BuildSystemPrompt(PetId petId)
        {
            string template = petId == PetId.Angel
                ? _config.AngelSystemTemplate
                : _config.DevilSystemTemplate;

            string personalityText = ResolvePersonality(petId);

            return template.Replace("{personality}", personalityText);
        }

        private string BuildUserPrompt(TarotDrawResult draw, PetId petId)
        {
            string template = _config.UserMessageTemplate;
            // slotName 和 question 是额外上下文，由调用方通过 TarotSession 提供。
            // 这里仅用基础字段填充；额外字段调用方可直接传 cardName/keywords。
            return template
                .Replace("{cardName}", $"{draw.Card.DisplayNameZh} ({draw.Card.DisplayNameEn})")
                .Replace("{slotName}", "")
                .Replace("{question}", "")
                .Replace("{keywords}", string.Join("、", draw.Card.GetKeywords(draw.Orientation)));
        }

        private async Task<string> SendRequestAsync(string systemPrompt, string userPrompt,
            CancellationToken cancellationToken)
        {
            var body = new LLMRequest
            {
                model = _config.Model,
                messages = new[]
                {
                    new LLMMessage { role = "system", content = systemPrompt },
                    new LLMMessage { role = "user", content = userPrompt }
                },
                max_tokens = 200
            };

            string json = JsonUtility.ToJson(body);
            using var req = new UnityWebRequest(_config.Endpoint, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {_config.ApiKey}");

            var operation = req.SendWebRequest();
            while (!operation.isDone)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    req.Abort();
                    cancellationToken.ThrowIfCancellationRequested();
                }
                await Task.Yield();
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                throw new Exception($"LLM request failed: {req.error} — {req.downloadHandler?.text}");
            }

            string responseJson = req.downloadHandler?.text ?? string.Empty;
            var response = JsonUtility.FromJson<LLMResponse>(responseJson);
            if (response.choices == null || response.choices.Length == 0)
            {
                throw new Exception("LLM response has no choices");
            }

            return response.choices[0].message?.content ?? string.Empty;
        }

        private static string ResolvePersonality(PetId petId)
        {
            if (ServiceLocator.TryResolve(out IPersonalityEvolutionService? evo) && evo is not null)
            {
                var pv = evo.GetMatrix(petId);
                return $"善良:{pv.Kindness:F1} 邪恶:{pv.Evilness:F1} 沉着:{pv.Calmness:F1} " +
                       $"勇敢:{pv.Bravery:F1} 害羞:{pv.Shyness:F1} 正直:{pv.Integrity:F1} 好奇:{pv.Curiosity:F1}";
            }
            return "性格数据未加载";
        }

        [Serializable]
        private sealed class LLMRequest
        {
            public string model = string.Empty;
            public LLMMessage[] messages = Array.Empty<LLMMessage>();
            public int max_tokens;
        }

        [Serializable]
        private sealed class LLMMessage
        {
            public string role = string.Empty;
            public string content = string.Empty;
        }

        [Serializable]
        private sealed class LLMResponse
        {
            public LLMChoice[] choices = Array.Empty<LLMChoice>();
        }

        [Serializable]
        private sealed class LLMChoice
        {
            public LLMMessage? message;
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Scripts/Modules/Tarot/DirectLLMBackend.cs
git commit -m "feat(tarot): add DirectLLMBackend using UnityWebRequest to OpenAI-compatible API"
```

---

### Task 5: Update ITarotService interface

**Files:**
- Modify: `Assets/_Project/Scripts/Modules/Tarot/ITarotService.cs`

- [ ] **Step 1: Replace ITarotService.cs**

```csharp
#nullable enable
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Modules.Pet;

namespace GeminiLab.Modules.Tarot
{
    /// <summary>
    /// 塔罗对外服务。支持创建抽牌会话（从 22 张随机选 11 张备选），
    /// 玩家手动选 3 张后并发请求解读。
    /// </summary>
    public interface ITarotService
    {
        /// <summary>塔罗牌堆，供图鉴等 UI 遍历展示。</summary>
        TarotDeckSO Deck { get; }

        /// <summary>创建一次新的抽牌会话。从 Deck 随机取 11 张备选。</summary>
        TarotSession CreateSession(string? question);

        /// <summary>洗牌：重新随机 11 张备选，清空已选。</summary>
        TarotSession ShuffleCards(TarotSession session);

        /// <summary>将指定牌填入下一个可用槽位（Past→Present→Future）。</summary>
        TarotSession PickCard(TarotSession session, TarotCardSO card);

        /// <summary>确认选牌完成。</summary>
        TarotSession ConfirmSelection(TarotSession session);

        /// <summary>
        /// 请求一次解读：以指定 PetId 的人格 + 指定正/逆位对牌面进行解读。
        /// 完成后会通过 EventBus 广播 <see cref="TarotReadingReceivedEvent"/>。
        /// </summary>
        Task<TarotReading> RequestReadingAsync(TarotDrawResult draw, PetId petId,
            TarotOrientation orientation, CancellationToken cancellationToken = default);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Scripts/Modules/Tarot/ITarotService.cs
git commit -m "feat(tarot): update ITarotService with session-based multi-card flow"
```

---

### Task 6: Update TarotService implementation

**Files:**
- Modify: `Assets/_Project/Scripts/Modules/Tarot/TarotService.cs`

- [ ] **Step 1: Rewrite TarotService.cs**

```csharp
#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Modules.Pet;
using UnityEngine;

namespace GeminiLab.Modules.Tarot
{
    /// <summary>
    /// <see cref="ITarotService"/> 默认实现。移除每日限制，支持 session 驱动的选牌流程。
    /// </summary>
    public sealed class TarotService : ITarotService
    {
        private readonly TarotDeckSO _deck;
        private readonly EventBus? _eventBus;
        private readonly ITarotReadingBackend _readingBackend;

        public TarotService(
            TarotDeckSO deck,
            EventBus? eventBus,
            ITarotReadingBackend readingBackend)
        {
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
            _eventBus = eventBus;
            _readingBackend = readingBackend ?? throw new ArgumentNullException(nameof(readingBackend));
        }

        public TarotDeckSO Deck => _deck;

        public TarotSession CreateSession(string? question)
        {
            var session = new TarotSession
            {
                Question = question ?? string.Empty,
                SessionDateIso = System.DateTime.Now.ToString("yyyy-MM-dd"),
                CandidateCards = PickRandomCards(11)
            };
            return session;
        }

        public TarotSession ShuffleCards(TarotSession session)
        {
            session.CandidateCards = PickRandomCards(11);
            session.PastCard = null;
            session.PresentCard = null;
            session.FutureCard = null;
            session.PickedCount = 0;
            session.Readings.Clear();
            session.RevealedSlotIndex = 0;
            return session;
        }

        public TarotSession PickCard(TarotSession session, TarotCardSO card)
        {
            if (session.PickedCount >= 3) return session;

            var slot = (TarotSlotPosition)session.PickedCount;
            var draw = new TarotDrawResult(card, TarotOrientation.Upright, session.SessionDateIso);
            session.SetCardAtSlot(slot, draw);
            session.PickedCount++;

            _eventBus?.Publish(new TarotDrawnEvent(draw));
            return session;
        }

        public TarotSession ConfirmSelection(TarotSession session)
        {
            return session; // placeholder; UI transitions to Reveal stage
        }

        public async Task<TarotReading> RequestReadingAsync(TarotDrawResult draw, PetId petId,
            TarotOrientation orientation, CancellationToken cancellationToken = default)
        {
            TarotReading reading;
            try
            {
                reading = await _readingBackend.RequestAsync(draw, petId, orientation, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Tarot] Reading backend failed, falling back. {ex.Message}");
                reading = LocalFallback.Build(draw, petId, orientation);
            }

            _eventBus?.Publish(new TarotReadingReceivedEvent(draw, reading));
            return reading;
        }

        private List<TarotCardSO> PickRandomCards(int count)
        {
            var deckCards = new List<TarotCardSO>(_deck.Cards);
            int n = deckCards.Count;
            // Fisher-Yates shuffle, take first count
            for (int i = n - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (deckCards[i], deckCards[j]) = (deckCards[j], deckCards[i]);
            }
            int take = Mathf.Min(count, n);
            return deckCards.GetRange(0, take);
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Scripts/Modules/Tarot/TarotService.cs
git commit -m "feat(tarot): rewrite TarotService with session-based flow, remove daily limit"
```

---

### Task 7: Update TarotRuntimeBootstrap

**Files:**
- Modify: `Assets/_Project/Scripts/Modules/Tarot/TarotRuntimeBootstrap.cs`

- [ ] **Step 1: Rewrite TarotRuntimeBootstrap.cs**

```csharp
#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Modules.Gateway;
using UnityEngine;

namespace GeminiLab.Modules.Tarot
{
    /// <summary>
    /// 塔罗系统运行态宿主。挂在 Boot.unity 的 BootstrapRoot 上；DontDestroyOnLoad。
    /// 在 Inspector 拖入 `TarotDeckSO` 和可选的 `LLMConfigSO`。
    /// LLMConfigSO 已配置 API Key → 使用 DirectLLMBackend；
    /// 否则 Gateway 可用 → GatewayTarotBackend；
    /// 否则 → FallbackOnlyBackend（全本地解读）。
    /// </summary>
    public sealed class TarotRuntimeBootstrap : MonoBehaviour
    {
        [SerializeField] private TarotDeckSO? _deck;
        [SerializeField] private LLMConfigSO? _llmConfig;

        private IDisposable? _drawnSub;
        private EventBus? _eventBus;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            if (_deck == null)
            {
                Debug.LogError("[TarotBootstrap] 未绑定 TarotDeckSO，塔罗服务无法初始化");
                return;
            }

            ServiceLocator.TryResolve(out _eventBus);

            ITarotReadingBackend backend = ResolveBackend();
            var service = new TarotService(_deck, _eventBus, backend);
            ServiceLocator.Register<ITarotService>(service);

            Debug.Log($"[TarotBootstrap] TarotService registered. Backend: {backend.GetType().Name}");

            if (_eventBus is not null)
            {
                _drawnSub = _eventBus.Subscribe<TarotDrawnEvent>(OnTarotDrawn);
            }
        }

        private ITarotReadingBackend ResolveBackend()
        {
            if (_llmConfig != null && _llmConfig.IsConfigured)
            {
                Debug.Log("[TarotBootstrap] 使用 DirectLLMBackend");
                return new DirectLLMBackend(_llmConfig);
            }

            if (ServiceLocator.TryResolve(out IGatewayClient? client) && client is not null)
            {
                Debug.Log("[TarotBootstrap] 使用 GatewayTarotBackend");
                return new GatewayTarotBackend(client);
            }

            Debug.Log("[TarotBootstrap] 使用 FallbackOnlyBackend（本地解读）");
            return new FallbackOnlyBackend();
        }

        private void OnDestroy()
        {
            _drawnSub?.Dispose();
        }

        private void OnTarotDrawn(TarotDrawnEvent evt)
        {
            if (_eventBus is null) return;

            string orientZh = evt.Result.Orientation == TarotOrientation.Upright ? "正位" : "逆位";
            string msg = $"已选牌：{evt.Result.Card.DisplayNameZh} · {orientZh}";
            _eventBus.Publish(new ToastRequestedEvent(msg, ToastKind.Success, 0f));
        }

        private sealed class FallbackOnlyBackend : ITarotReadingBackend
        {
            public System.Threading.Tasks.Task<TarotReading> RequestAsync(
                TarotDrawResult draw,
                GeminiLab.Modules.Pet.PetId petId,
                TarotOrientation orientation,
                System.Threading.CancellationToken cancellationToken)
            {
                return System.Threading.Tasks.Task.FromResult(LocalFallback.Build(draw, petId, orientation));
            }
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Scripts/Modules/Tarot/TarotRuntimeBootstrap.cs
git commit -m "feat(tarot): update bootstrap to prefer DirectLLMBackend when LLMConfigSO configured"
```

---

### Task 8: Create TarotCardSelectable prefab script

**Files:**
- Create: `Assets/_Project/Scripts/Modules/HubUI/Panels/TarotCardSelectable.cs`

- [ ] **Step 1: Write TarotCardSelectable**

```csharp
#nullable enable
using System;
using GeminiLab.Modules.Tarot;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 可选取的塔罗牌 Prefab 脚本。
    /// 挂到卡牌 Prefab 上，绑定 Image + TMP_Text（牌名）+ 可选 Animator。
    /// 支持 hover 放大/上浮、点击选中、飞行动画槽位。
    /// </summary>
    public sealed class TarotCardSelectable : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private Image? _cardImage;
        [SerializeField] private Image? _cardBackImage;
        [SerializeField] private TMP_Text? _nameText;

        public TarotCardSO? CardData { get; private set; }
        public event Action<TarotCardSO>? OnClicked;

        private Vector3 _originalScale;
        private Vector3 _originalPosition;
        private bool _isSelected;

        private void Awake()
        {
            _originalScale = transform.localScale;
            _originalPosition = transform.localPosition;
        }

        public void SetCard(TarotCardSO card, Sprite? cardBack)
        {
            CardData = card;
            if (_cardBackImage != null && cardBack != null)
            {
                _cardBackImage.sprite = cardBack;
                _cardBackImage.gameObject.SetActive(true);
            }
            if (_cardImage != null && card.Artwork != null)
            {
                _cardImage.sprite = card.Artwork;
                _cardImage.gameObject.SetActive(false);
            }
            if (_nameText != null)
            {
                _nameText.text = card.DisplayNameZh;
            }
            _isSelected = false;
            transform.localScale = _originalScale;
            transform.localPosition = _originalPosition;
        }

        public void FlipToFace(bool showFace)
        {
            if (_cardBackImage != null) _cardBackImage.gameObject.SetActive(!showFace);
            if (_cardImage != null) _cardImage.gameObject.SetActive(showFace);
        }

        public void MarkSelected(bool selected)
        {
            _isSelected = selected;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isSelected) return;
            var layout = GetComponentInParent<TarotArcLayout>();
            float scale = layout?.LayoutConfig?.HoverScale ?? 1.15f;
            float lift = layout?.LayoutConfig?.HoverLift ?? 30f;
            transform.localScale = _originalScale * scale;
            transform.localPosition = _originalPosition + new Vector3(0, lift, 0);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isSelected) return;
            transform.localScale = _originalScale;
            transform.localPosition = _originalPosition;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (CardData == null || _isSelected) return;
            _isSelected = true;
            OnClicked?.Invoke(CardData);
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Scripts/Modules/HubUI/Panels/TarotCardSelectable.cs
git commit -m "feat(tarot): add TarotCardSelectable prefab script"
```

---

### Task 9: Create TarotSlot prefab script

**Files:**
- Create: `Assets/_Project/Scripts/Modules/HubUI/Panels/TarotSlot.cs`

- [ ] **Step 1: Write TarotSlot**

```csharp
#nullable enable
using GeminiLab.Modules.Tarot;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 牌位槽 Prefab 脚本。显示槽位标签（过去/当下/未来）和已放入的卡牌。
    /// </summary>
    public sealed class TarotSlot : MonoBehaviour
    {
        [SerializeField] private Image? _cardImage;
        [SerializeField] private Image? _slotBg;
        [SerializeField] private TMP_Text? _slotLabel;
        [SerializeField] private TMP_Text? _loadingText;
        [SerializeField] private GameObject? _emptyState;
        [SerializeField] private GameObject? _filledState;
        [SerializeField] private GameObject? _loadingState;

        public TarotSlotPosition SlotPosition { get; private set; }

        public void Initialize(TarotSlotPosition position, string labelText)
        {
            SlotPosition = position;
            if (_slotLabel != null) _slotLabel.text = labelText;
            SetState(0); // empty
        }

        public void PlaceCard(TarotCardSO card)
        {
            if (_cardImage != null && card.Artwork != null)
            {
                _cardImage.sprite = card.Artwork;
                _cardImage.color = Color.white;
            }
            SetState(1); // filled
        }

        public void ShowLoading(string text)
        {
            if (_loadingText != null) _loadingText.text = text;
            SetState(2); // loading
        }

        public void ClearLoading()
        {
            SetState(1); // back to filled
        }

        private void SetState(int state)
        {
            if (_emptyState != null) _emptyState.SetActive(state == 0);
            if (_filledState != null) _filledState.SetActive(state == 1);
            if (_loadingState != null) _loadingState.SetActive(state == 2);
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Scripts/Modules/HubUI/Panels/TarotSlot.cs
git commit -m "feat(tarot): add TarotSlot prefab script"
```

---

### Task 10: Create ReadingBubble prefab script

**Files:**
- Create: `Assets/_Project/Scripts/Modules/HubUI/Panels/ReadingBubble.cs`

- [ ] **Step 1: Write ReadingBubble**

```csharp
#nullable enable
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 解读气泡 Prefab 脚本。支持弹出动画和天使/恶魔不同配色。
    /// </summary>
    public sealed class ReadingBubble : MonoBehaviour
    {
        [SerializeField] private Image? _bubbleBg;
        [SerializeField] private TMP_Text? _readingText;
        [SerializeField] private TMP_Text? _personaLabel;
        [SerializeField] private Animator? _animator;

        [Header("配色")]
        [SerializeField] private Color _angelColor = new Color(1f, 0.95f, 0.7f, 1f);
        [SerializeField] private Color _devilColor = new Color(0.7f, 0.3f, 0.3f, 1f);

        private static readonly int AppearTrigger = Animator.StringToHash("Appear");

        public void Show(string personaName, string text, bool isAngel, Action? onComplete = null)
        {
            if (_personaLabel != null) _personaLabel.text = personaName;
            if (_readingText != null) _readingText.text = text;
            if (_bubbleBg != null) _bubbleBg.color = isAngel ? _angelColor : _devilColor;

            gameObject.SetActive(true);

            if (_animator != null)
            {
                _animator.SetTrigger(AppearTrigger);
                // Rely on animation event or just show immediately
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Scripts/Modules/HubUI/Panels/ReadingBubble.cs
git commit -m "feat(tarot): add ReadingBubble prefab script"
```

---

### Task 11: Create TarotArcLayout layout script

**Files:**
- Create: `Assets/_Project/Scripts/Modules/HubUI/Panels/TarotArcLayout.cs`

- [ ] **Step 1: Write TarotArcLayout**

```csharp
#nullable enable
using System;
using System.Collections;
using GeminiLab.Modules.Tarot;
using UnityEngine;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 弧形排列子对象。挂到选牌区域容器上，读取 TarotLayoutSO 参数，
    /// 将子对象按弧形排列。调用 Arrange() 触发重新排列。
    /// </summary>
    public sealed class TarotArcLayout : MonoBehaviour
    {
        [SerializeField] private TarotLayoutSO? _layoutConfig;
        [SerializeField] private float _arcBottomOffset = -150f; // Y offset for arc center

        public TarotLayoutSO? LayoutConfig => _layoutConfig;

        /// <summary>立即排列所有子对象。</summary>
        public void ArrangeImmediate()
        {
            if (_layoutConfig == null) return;

            int childCount = transform.childCount;
            if (childCount == 0) return;

            float spanAngle = _layoutConfig.ArcSpanAngle;
            float radius = _layoutConfig.ArcRadius;
            float startAngle = -spanAngle / 2f;

            for (int i = 0; i < childCount; i++)
            {
                float t = childCount > 1 ? (float)i / (childCount - 1) : 0.5f;
                float angle = (startAngle + spanAngle * t) * Mathf.Deg2Rad;
                float x = Mathf.Sin(angle) * radius;
                float y = Mathf.Cos(angle) * radius * 0.5f + _arcBottomOffset;

                var child = transform.GetChild(i);
                var rt = child as RectTransform;
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(x, y);
                    // Tilt cards toward center
                    rt.localRotation = Quaternion.Euler(0, 0, -angle * Mathf.Rad2Deg * 0.3f);
                }
            }
        }

        /// <summary>带浮现动画依次排列。</summary>
        public IEnumerator ArrangeWithAppear(float delayBetween = 0.05f)
        {
            ArrangeImmediate();

            if (_layoutConfig == null) yield break;
            float duration = _layoutConfig.CardAppearDuration;

            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                child.gameObject.SetActive(true);
                // Scale-from-zero appear
                StartCoroutine(ScaleIn(child, duration));
                yield return new WaitForSeconds(delayBetween);
            }
        }

        private static IEnumerator ScaleIn(Transform target, float duration)
        {
            target.localScale = Vector3.zero;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                target.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
                yield return null;
            }
            target.localScale = Vector3.one;
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Scripts/Modules/HubUI/Panels/TarotArcLayout.cs
git commit -m "feat(tarot): add TarotArcLayout for arc card spread positioning"
```

---

### Task 12: Rewrite TarotPanelStub — 3-stage state machine

**Files:**
- Modify: `Assets/_Project/Scripts/Modules/HubUI/Panels/TarotPanelStub.cs`

- [ ] **Step 1: Rewrite TarotPanelStub.cs**

```csharp
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

            if (_drawButton != null) _drawButton.onClick.AddListener(() => SwitchTab(SubView.Draw));
            if (_historyButton != null) _historyButton.onClick.AddListener(() => SwitchTab(SubView.History));
            if (_guideButton != null) _guideButton.onClick.AddListener(() => SwitchTab(SubView.Guide));

            // Idle → Select (from sketch button inside drawView)
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

        // ======================== Tab 视图（保留） ========================

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
```

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Scripts/Modules/HubUI/Panels/TarotPanelStub.cs
git commit -m "feat(tarot): rewrite TarotPanelStub with 3-stage state machine (Idle/Select/Reveal)"
```

---

### Task 13: Update TarotGuideCard to support locked/unlocked state

**Files:**
- Modify: `Assets/_Project/Scripts/Modules/HubUI/Panels/TarotGuideCard.cs`

- [ ] **Step 1: Rewrite TarotGuideCard.cs**

```csharp
#nullable enable
using GeminiLab.Modules.Tarot;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 塔罗图鉴中单张卡牌组件。支持已解锁/未解锁双状态。
    /// </summary>
    public sealed class TarotGuideCard : MonoBehaviour
    {
        [SerializeField] private Image? _cardImage;
        [SerializeField] private TMP_Text? _nameText;
        [SerializeField] private GameObject? _unlockedState;
        [SerializeField] private GameObject? _lockedState;
        [SerializeField] private Sprite? _lockedPlaceholder;

        public void SetData(TarotCardSO card, bool unlocked)
        {
            if (unlocked)
            {
                if (_unlockedState != null) _unlockedState.SetActive(true);
                if (_lockedState != null) _lockedState.SetActive(false);
                if (_cardImage != null && card.Artwork != null)
                {
                    _cardImage.sprite = card.Artwork;
                    _cardImage.color = Color.white;
                }
                if (_nameText != null) _nameText.text = $"{card.MajorIndex}: {card.DisplayNameZh}";
            }
            else
            {
                if (_unlockedState != null) _unlockedState.SetActive(false);
                if (_lockedState != null) _lockedState.SetActive(true);
                if (_cardImage != null && _lockedPlaceholder != null)
                {
                    _cardImage.sprite = _lockedPlaceholder;
                    _cardImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                }
                if (_nameText != null) _nameText.text = "???";
            }
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Scripts/Modules/HubUI/Panels/TarotGuideCard.cs
git commit -m "feat(tarot): update TarotGuideCard to support locked/unlocked collection state"
```

---

### Task 14: Verify build compiles and run self-review

**Files:** (none — verification only)

- [ ] **Step 1: Refresh Unity assets**

Use MCP tool: `assets-refresh`

- [ ] **Step 2: Check Unity console for errors**

Use MCP tool: `console-get-logs` — verify no compile errors.

- [ ] **Step 3: Commit any remaining changes**

```bash
git add .
git commit -m "feat(tarot): finalize multi-card reading implementation"
```
