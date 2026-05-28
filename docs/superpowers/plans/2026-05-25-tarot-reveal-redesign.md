# Tarot Reveal 阶段重设计 — 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 提取 TarotRevealController 编排 4 轮揭示流程（过去→当下→未来→总结），支持占位图渐隐/气泡渐显动画，新增总结轮 LLM 结构化返回。

**Architecture:** 新增 TarotRevealController 接管 Reveal 阶段全部逻辑，TarotPanelStub 仅持有引用并委托。数据模型层新增 TarotSummaryResult + LuckyHintData。Backend 扩展 RequestSummaryAsync 方法。LLMConfigSO 新增总结轮 prompt 模板。

**Tech Stack:** Unity C#, NUnit (EditMode tests), Unity Test Framework, UnityEngine.UI, TMPro

---

## 文件结构

| 文件 | 操作 | 职责 |
|------|------|------|
| `TarotModels.cs` | 修改 | 新增 TarotSummaryResult + LuckyHintData |
| `LLMConfigSO.cs` | 修改 | 新增 SummarySystemTemplate 字段 |
| `ITarotReadingBackend.cs` | 修改 | 新增 RequestSummaryAsync 方法签名 |
| `DirectLLMBackend.cs` | 修改 | 实现 RequestSummaryAsync |
| `TarotRevealController.cs` | 新建 | Reveal 阶段编排器 |
| `TarotPanelStub.cs` | 修改 | 移除 Reveal 逻辑，委托给 Controller |
| `GeminiLab.Tests.EditMode.asmdef` | 修改 | 添加 Tarot 模块引用 |

---

### Task 1: TarotSummaryResult + LuckyHintData 数据模型

**Files:**
- Modify: `Assets/_Project/Scripts/Modules/Tarot/TarotModels.cs`

- [ ] **Step 1: 添加 LuckyHintData 和 TarotSummaryResult 类**

在 TarotModels.cs 末尾（`TarotSession` 类之后，namespace 闭合之前）添加：

```csharp
/// <summary>总结轮幸运提示（LLM 结构化返回的子对象）。</summary>
[Serializable]
public sealed class LuckyHintData
{
    public string color;
    public string number;
    public string time;
    public string action;
}

/// <summary>总结轮 LLM 返回的结构化数据。</summary>
[Serializable]
public sealed class TarotSummaryResult
{
    public int fortuneLevel;
    public LuckyHintData luckyHint;
    public string advice;

    public static TarotSummaryResult FromJson(string json)
    {
        try
        {
            var result = JsonUtility.FromJson<TarotSummaryResult>(json);
            if (result == null) return Default();
            // 校验范围
            result.fortuneLevel = Mathf.Clamp(result.fortuneLevel, 1, 5);
            result.luckyHint ??= new LuckyHintData();
            result.advice ??= string.Empty;
            return result;
        }
        catch (Exception)
        {
            return Default();
        }
    }

    public static TarotSummaryResult Default()
    {
        return new TarotSummaryResult
        {
            fortuneLevel = 3,
            luckyHint = new LuckyHintData
            {
                color = "蓝色",
                number = "7",
                time = "午后",
                action = "保持平常心"
            },
            advice = "今日运势平稳，保持平常心，关注身边的小确幸。"
        };
    }
}
```

- [ ] **Step 2: 在 TarotSession 中新增 SummaryResult 字段**

在 `TarotSession` 类中，`RevealedSlotIndex` 字段之后添加：

```csharp
/// <summary>总结轮结构化结果（第 7 次 LLM 调用返回）。</summary>
public TarotSummaryResult? SummaryResult;
```

- [ ] **Step 3: 编译验证**

在 Unity Editor 中执行 `Assets > Refresh`，确认无编译错误。

- [ ] **Step 4: 写 TarotSummaryResult 单元测试**

创建 `Assets/_Project/Tests/EditMode/TarotSummaryResultTests.cs`：

```csharp
#nullable enable
using GeminiLab.Modules.Tarot;
using NUnit.Framework;

namespace GeminiLab.Tests.EditMode
{
    public sealed class TarotSummaryResultTests
    {
        [Test]
        public void FromJson_ValidJson_ParsesCorrectly()
        {
            string json = @"{
                ""fortuneLevel"": 5,
                ""luckyHint"": {
                    ""color"": ""金色"",
                    ""number"": ""8"",
                    ""time"": ""黄昏"",
                    ""action"": ""主动出击""
                },
                ""advice"": ""今日宜大胆行动。""
            }";

            var result = TarotSummaryResult.FromJson(json);

            Assert.AreEqual(5, result.fortuneLevel);
            Assert.AreEqual("金色", result.luckyHint.color);
            Assert.AreEqual("8", result.luckyHint.number);
            Assert.AreEqual("黄昏", result.luckyHint.time);
            Assert.AreEqual("主动出击", result.luckyHint.action);
            Assert.AreEqual("今日宜大胆行动。", result.advice);
        }

        [Test]
        public void FromJson_ClampsFortuneLevel()
        {
            string json = @"{""fortuneLevel"": 99, ""luckyHint"": {}, ""advice"": ""x""}";
            var result = TarotSummaryResult.FromJson(json);
            Assert.AreEqual(5, result.fortuneLevel);
        }

        [Test]
        public void FromJson_InvalidJson_ReturnsDefault()
        {
            var result = TarotSummaryResult.FromJson("not json");
            Assert.AreEqual(3, result.fortuneLevel);
            Assert.IsNotNull(result.luckyHint);
            Assert.IsNotNull(result.advice);
        }

        [Test]
        public void Default_ReturnsSaneValues()
        {
            var result = TarotSummaryResult.Default();
            Assert.AreEqual(3, result.fortuneLevel);
            Assert.IsNotNull(result.luckyHint.color);
            Assert.IsNotNull(result.advice);
        }
    }
}
```

- [ ] **Step 5: 添加测试程序集引用**

修改 `Assets/_Project/Tests/EditMode/GeminiLab.Tests.EditMode.asmdef`，在 `"references"` 数组中追加：

```json
"GeminiLab.Modules.Tarot"
```

- [ ] **Step 6: 运行测试验证通过**

Unity Editor 中 `Window > General > Test Runner`，运行 `TarotSummaryResultTests`，预期全部 PASS。

- [ ] **Step 7: Commit**

```bash
git add Assets/_Project/Scripts/Modules/Tarot/TarotModels.cs \
        Assets/_Project/Tests/EditMode/TarotSummaryResultTests.cs \
        Assets/_Project/Tests/EditMode/GeminiLab.Tests.EditMode.asmdef
git commit -m "feat(tarot): add TarotSummaryResult and LuckyHintData models with tests"
```

---

### Task 2: LLMConfigSO 新增 SummarySystemTemplate

**Files:**
- Modify: `Assets/_Project/Scripts/Modules/Tarot/LLMConfigSO.cs`

- [ ] **Step 1: 添加 SummarySystemTemplate 字段**

在 `_userMessageTemplate` 字段之后、`_timeoutSeconds` 字段之前插入：

```csharp
[Tooltip("总结轮 System prompt。占位符: {pastCard}, {presentCard}, {futureCard}, {question}")]
[TextArea(5, 20)]
[SerializeField] private string _summarySystemTemplate =
    "你是一位塔罗占卜师，已为玩家解读了过去、现在、未来三张牌。\n" +
    "请基于这三张牌给出综合运势总结，严格返回以下 JSON 格式（不要包含任何其他文字）：\n" +
    "{\n" +
    "  \"fortuneLevel\": <1-5的整数，代表运势星级>,\n" +
    "  \"luckyHint\": {\n" +
    "    \"color\": \"<幸运颜色>\",\n" +
    "    \"number\": \"<幸运数字>\",\n" +
    "    \"time\": \"<幸运时间段>\",\n" +
    "    \"action\": \"<幸运行动建议>\"\n" +
    "  },\n" +
    "  \"advice\": \"<今日综合建议，2-3句话>\"\n" +
    "}\n" +
    "过去牌：{pastCard}\n" +
    "现在牌：{presentCard}\n" +
    "未来牌：{futureCard}\n" +
    "玩家问题：{question}";
```

- [ ] **Step 2: 添加公共属性**

在 `UserMessageTemplate` 属性之后、`TimeoutSeconds` 属性之前插入：

```csharp
public string SummarySystemTemplate => _summarySystemTemplate;
```

- [ ] **Step 3: 编译验证**

Unity Editor 中 Refresh，确认无编译错误。

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Scripts/Modules/Tarot/LLMConfigSO.cs
git commit -m "feat(tarot): add SummarySystemTemplate field to LLMConfigSO"
```

---

### Task 3: ITarotReadingBackend + DirectLLMBackend 扩展总结方法

**Files:**
- Modify: `Assets/_Project/Scripts/Modules/Tarot/ITarotReadingBackend.cs`
- Modify: `Assets/_Project/Scripts/Modules/Tarot/DirectLLMBackend.cs`

- [ ] **Step 1: ITarotReadingBackend 新增方法签名**

在 `ITarotReadingBackend.cs` 中，`RequestAsync` 方法签名之后（接口闭合之前）添加：

```csharp
/// <summary>
/// 请求总结轮结构化解读。传入三张已选牌 + 用户问题，返回运势总结。
/// </summary>
Task<TarotSummaryResult> RequestSummaryAsync(
    TarotDrawResult past, TarotDrawResult present, TarotDrawResult future,
    string? question, CancellationToken cancellationToken);
```

- [ ] **Step 2: DirectLLMBackend 实现 RequestSummaryAsync**

在 `DirectLLMBackend.cs` 中，`RequestAsync` 方法之后（类闭合之前）添加：

```csharp
public async Task<TarotSummaryResult> RequestSummaryAsync(
    TarotDrawResult past, TarotDrawResult present, TarotDrawResult future,
    string? question, CancellationToken cancellationToken)
{
    if (!_config.IsConfigured)
    {
        return TarotSummaryResult.Default();
    }

    string systemPrompt = _config.SummarySystemTemplate
        .Replace("{pastCard}", $"{past.Card.DisplayNameZh} ({past.Card.DisplayNameEn})")
        .Replace("{presentCard}", $"{present.Card.DisplayNameZh} ({present.Card.DisplayNameEn})")
        .Replace("{futureCard}", $"{future.Card.DisplayNameZh} ({future.Card.DisplayNameEn})")
        .Replace("{question}", question ?? "未指定");

    // 对总结轮使用空 user prompt，所有指令在 system prompt 中
    string responseText;
    try
    {
        responseText = await SendRequestAsync(systemPrompt, "请返回 JSON。", cancellationToken)
            .ConfigureAwait(false);
    }
    catch (OperationCanceledException)
    {
        throw;
    }
    catch (Exception ex)
    {
        Debug.LogWarning($"[DirectLLM] Summary request failed: {ex.Message}");
        return TarotSummaryResult.Default();
    }

    return TarotSummaryResult.FromJson(responseText);
}
```

需要给 `DirectLLMBackend.cs` 添加 `System` 命名空间引用（`using System;`），检查文件顶部是否已有（当前 `DirectLLMBackend.cs` 已有 `using System;`）。

- [ ] **Step 3: 编译验证**

Unity Editor 中 Refresh，确认无编译错误。

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Scripts/Modules/Tarot/ITarotReadingBackend.cs \
        Assets/_Project/Scripts/Modules/Tarot/DirectLLMBackend.cs
git commit -m "feat(tarot): add RequestSummaryAsync to reading backend"
```

---

### Task 4: 创建 TarotRevealController

**Files:**
- Create: `Assets/_Project/Scripts/Modules/HubUI/Panels/TarotRevealController.cs`

- [ ] **Step 1: 创建 TarotRevealController.cs**

```csharp
#nullable enable
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.Tarot;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// Reveal 阶段编排器。4 子阶段：Past → Present → Future → Summary。
    /// 进入 Reveal 时 6 个 Angel/Devil 请求并行，结果缓存；每槽位动画揭示。
    /// 总结轮单次 LLM 请求，返回结构化运势数据。
    /// </summary>
    public sealed class TarotRevealController : MonoBehaviour
    {
        private enum RevealPhase { Past, Present, Future, Summary }

        // ---- Inspector ----

        [Header("底图（美术资源）")]
        [SerializeField] private Image? _pastBaseImage;
        [SerializeField] private Image? _presentBaseImage;
        [SerializeField] private Image? _futureBaseImage;

        [Header("占位图（美术资源，每阶段不同）")]
        [SerializeField] private Image? _pastPlaceholder;
        [SerializeField] private Image? _presentPlaceholder;
        [SerializeField] private Image? _futurePlaceholder;
        [SerializeField] private Image? _summaryPlaceholder;

        [Header("卡面")]
        [SerializeField] private Image? _pastCardImage;
        [SerializeField] private Image? _presentCardImage;
        [SerializeField] private Image? _futureCardImage;

        [Header("解读气泡")]
        [SerializeField] private ReadingBubble? _pastAngelBubble;
        [SerializeField] private ReadingBubble? _pastDevilBubble;
        [SerializeField] private ReadingBubble? _presentAngelBubble;
        [SerializeField] private ReadingBubble? _presentDevilBubble;
        [SerializeField] private ReadingBubble? _futureAngelBubble;
        [SerializeField] private ReadingBubble? _futureDevilBubble;

        [Header("总结 UI")]
        [SerializeField] private GameObject? _summaryContentRoot;
        [SerializeField] private TMP_Text? _fortuneStarsText;
        [SerializeField] private TMP_Text? _luckyColorText;
        [SerializeField] private TMP_Text? _luckyNumberText;
        [SerializeField] private TMP_Text? _luckyTimeText;
        [SerializeField] private TMP_Text? _luckyActionText;
        [SerializeField] private TMP_Text? _adviceText;

        [Header("按钮")]
        [SerializeField] private Button? _continueButton;
        [SerializeField] private Button? _redrawButton;
        [SerializeField] private Button? _openGuideButton;

        [Header("动画参数")]
        [SerializeField] private float _fadeDuration = 0.5f;
        [SerializeField] private float _buttonFadeDuration = 0.3f;

        // ---- 公开事件 ----
        public event Action? OnRevealComplete;
        public event Action? OnOpenGuide;

        // ---- 运行态 ----
        private TarotSession? _session;
        private ITarotService? _tarot;
        private CancellationTokenSource? _cts;
        private RevealPhase _phase;

        private void Awake()
        {
            if (_continueButton != null) _continueButton.onClick.AddListener(OnContinueClicked);
            if (_redrawButton != null) _redrawButton.onClick.AddListener(OnRedrawClicked);
            if (_openGuideButton != null) _openGuideButton.onClick.AddListener(OnOpenGuideClicked);

            // 初始全部隐藏
            if (_continueButton != null) SetAlpha(_continueButton, 0f);
            if (_redrawButton != null) SetAlpha(_redrawButton, 0f);
            if (_openGuideButton != null) SetAlpha(_openGuideButton, 0f);
            if (_summaryContentRoot != null) _summaryContentRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_continueButton != null) _continueButton.onClick.RemoveAllListeners();
            if (_redrawButton != null) _redrawButton.onClick.RemoveAllListeners();
            if (_openGuideButton != null) _openGuideButton.onClick.RemoveAllListeners();
            _cts?.Cancel();
            _cts?.Dispose();
        }

        // ======================== 入口 ========================

        public void BeginReveal(TarotSession session, ITarotService tarot)
        {
            _session = session;
            _tarot = tarot;

            // 显示卡面
            ShowCardFace(_pastCardImage, session.PastCard);
            ShowCardFace(_presentCardImage, session.PresentCard);
            ShowCardFace(_futureCardImage, session.FutureCard);

            // 隐藏所有气泡
            HideAllBubbles();

            // 初始化总结 UI 为隐藏
            if (_summaryContentRoot != null) _summaryContentRoot.SetActive(false);
            if (_summaryPlaceholder != null) _summaryPlaceholder.gameObject.SetActive(false);

            // 发起 6 个并行 LLM 请求
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _ = FireAllReadingsAsync(_cts.Token);

            // 进入第一阶段
            EnterPhase(RevealPhase.Past);
        }

        // ======================== 阶段管理 ========================

        private void EnterPhase(RevealPhase phase)
        {
            _phase = phase;

            // 隐藏继续按钮
            SetAlpha(_continueButton, 0f);
            if (_continueButton != null) _continueButton.interactable = false;

            switch (phase)
            {
                case RevealPhase.Past:
                    ShowPlaceholder(TarotSlotPosition.Past);
                    StartCoroutine(WaitThenRevealSlot(TarotSlotPosition.Past, RevealPhase.Present));
                    break;
                case RevealPhase.Present:
                    ShowPlaceholder(TarotSlotPosition.Present);
                    StartCoroutine(WaitThenRevealSlot(TarotSlotPosition.Present, RevealPhase.Future));
                    break;
                case RevealPhase.Future:
                    ShowPlaceholder(TarotSlotPosition.Future);
                    StartCoroutine(WaitThenRevealSlot(TarotSlotPosition.Future, RevealPhase.Summary));
                    break;
                case RevealPhase.Summary:
                    EnterSummaryPhase();
                    break;
            }
        }

        // ======================== 槽位占卜 → 揭示 ========================

        private void ShowPlaceholder(TarotSlotPosition slot)
        {
            var placeholder = GetPlaceholder(slot);
            if (placeholder != null) placeholder.gameObject.SetActive(true);
            SetAlpha(placeholder, 1f);
            // 确保对应气泡隐藏
            HideSlotBubbles(slot);
        }

        private IEnumerator WaitThenRevealSlot(TarotSlotPosition slot, RevealPhase nextPhase)
        {
            // 轮询等待该槽位的两个解读就绪
            string angelKey = TarotSession.ReadingKey(slot, PetId.Angel);
            string devilKey = TarotSession.ReadingKey(slot, PetId.Devil);

            while (_session != null &&
                   (!_session.Readings.ContainsKey(angelKey) || !_session.Readings.ContainsKey(devilKey)))
            {
                yield return new WaitForSeconds(0.1f);
            }

            // LLM 就绪 → 占位图渐隐 + 气泡渐显
            var placeholder = GetPlaceholder(slot);
            yield return StartCoroutine(Crossfade(placeholder, toAlpha: 0f, _fadeDuration));

            if (placeholder != null) placeholder.gameObject.SetActive(false);

            if (_session != null)
            {
                string slotName = slot switch
                {
                    TarotSlotPosition.Past => "过去",
                    TarotSlotPosition.Present => "当下",
                    TarotSlotPosition.Future => "未来",
                    _ => ""
                };

                var (angelBubble, devilBubble) = GetBubbles(slot);
                if (_session.Readings.TryGetValue(angelKey, out var angelReading))
                {
                    angelBubble?.Show($"天使 · {slotName}", angelReading.Text, isAngel: true);
                    StartCoroutine(FadeToAlpha(angelBubble?.GetComponent<CanvasGroup>(), 1f, _fadeDuration));
                }
                if (_session.Readings.TryGetValue(devilKey, out var devilReading))
                {
                    devilBubble?.Show($"恶魔 · {slotName}", devilReading.Text, isAngel: false);
                    StartCoroutine(FadeToAlpha(devilBubble?.GetComponent<CanvasGroup>(), 1f, _fadeDuration));
                }
            }

            // 显示继续按钮（除 Summary 外）
            if (_phase != RevealPhase.Summary)
            {
                yield return StartCoroutine(FadeButton(_continueButton, toAlpha: 1f));
                if (_continueButton != null) _continueButton.interactable = true;
            }
        }

        private void OnContinueClicked()
        {
            if (_continueButton != null) _continueButton.interactable = false;
            StartCoroutine(FadeButton(_continueButton, toAlpha: 0f));

            var nextPhase = _phase switch
            {
                RevealPhase.Past => RevealPhase.Present,
                RevealPhase.Present => RevealPhase.Future,
                RevealPhase.Future => RevealPhase.Summary,
                _ => RevealPhase.Summary
            };
            EnterPhase(nextPhase);
        }

        // ======================== 总结阶段 ========================

        private void EnterSummaryPhase()
        {
            if (_summaryPlaceholder != null)
            {
                _summaryPlaceholder.gameObject.SetActive(true);
                SetAlpha(_summaryPlaceholder, 1f);
            }
            if (_summaryContentRoot != null) _summaryContentRoot.SetActive(false);

            _ = RequestSummaryAsync();
        }

        private async Task RequestSummaryAsync()
        {
            if (_session == null || _tarot == null) return;

            var backend = GetReadingBackend();
            if (backend == null)
            {
                PopulateSummaryUI(TarotSummaryResult.Default());
                return;
            }

            var past = _session.PastCard ?? default;
            var present = _session.PresentCard ?? default;
            var future = _session.FutureCard ?? default;

            TarotSummaryResult result;
            try
            {
                result = await backend.RequestSummaryAsync(past, present, future,
                    _session.Question, _cts?.Token ?? CancellationToken.None)
                    .ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TarotReveal] Summary failed: {ex.Message}");
                result = TarotSummaryResult.Default();
            }

            PopulateSummaryUI(result);
        }

        private void PopulateSummaryUI(TarotSummaryResult result)
        {
            // 占位图渐隐
            StartCoroutine(Crossfade(_summaryPlaceholder, toAlpha: 0f, _fadeDuration));
            if (_summaryPlaceholder != null) _summaryPlaceholder.gameObject.SetActive(false);

            if (_summaryContentRoot != null) _summaryContentRoot.SetActive(true);
            SetAlpha(_summaryContentRoot, 0f);

            if (_fortuneStarsText != null)
            {
                _fortuneStarsText.text = new string('★', result.fortuneLevel)
                    + new string('☆', 5 - result.fortuneLevel);
            }
            if (_luckyColorText != null) _luckyColorText.text = result.luckyHint.color;
            if (_luckyNumberText != null) _luckyNumberText.text = result.luckyHint.number;
            if (_luckyTimeText != null) _luckyTimeText.text = result.luckyHint.time;
            if (_luckyActionText != null) _luckyActionText.text = result.luckyHint.action;
            if (_adviceText != null) _adviceText.text = result.advice;

            // 运势内容 + 按钮渐显
            StartCoroutine(FadeToAlpha(_summaryContentRoot?.GetComponent<CanvasGroup>(), 1f, _fadeDuration));
            StartCoroutine(FadeButton(_redrawButton, toAlpha: 1f));
            StartCoroutine(FadeButton(_openGuideButton, toAlpha: 1f));
            if (_redrawButton != null) _redrawButton.interactable = true;
            if (_openGuideButton != null) _openGuideButton.interactable = true;
        }

        private void OnRedrawClicked()
        {
            _cts?.Cancel();
            OnRevealComplete?.Invoke();
        }

        private void OnOpenGuideClicked()
        {
            _cts?.Cancel();
            OnOpenGuide?.Invoke();
        }

        // ======================== LLM 并行请求 ========================

        private async Task FireAllReadingsAsync(CancellationToken token)
        {
            if (_session == null || _tarot == null) return;

            var slots = new[] { TarotSlotPosition.Past, TarotSlotPosition.Present, TarotSlotPosition.Future };
            var personas = new[] { (PetId.Angel, TarotOrientation.Upright), (PetId.Devil, TarotOrientation.Reversed) };

            var tasks = new System.Collections.Generic.List<Task>();
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
                Debug.LogWarning($"[TarotReveal] Reading failed for {slot} {petId}: {ex.Message}");
                string key = TarotSession.ReadingKey(slot, petId);
                var fallback = LocalFallback.Build(draw, petId, orientation);
                if (_session != null) _session.Readings[key] = fallback;
            }
        }

        private ITarotReadingBackend? GetReadingBackend()
        {
            // 通过 ServiceLocator 或直接反射获取 backend
            // TarotService 持有 _readingBackend，但我们无法直接访问
            // 方式：通过 ITarotService 的 RequestReadingAsync 间接调用
            // 对于总结，需要新接口。如果 ITarotService 不暴露 RequestSummaryAsync，
            // 这里暂时直接 new DirectLLMBackend 或通过 ServiceLocator 获取。
            // —— 见 Task 3.5 在 ITarotService 中添加 RequestSummaryAsync 透传 ——
            return null; // 将通过 _tarot.RequestSummaryAsync 替代（见后续步骤）
        }

        // ======================== 动画辅助 ========================

        private IEnumerator Crossfade(Image? img, float toAlpha, float duration)
        {
            if (img == null) yield break;
            var cg = img.GetComponent<CanvasGroup>();
            if (cg == null) cg = img.gameObject.AddComponent<CanvasGroup>();
            yield return StartCoroutine(FadeToAlpha(cg, toAlpha, duration));
        }

        private IEnumerator FadeToAlpha(CanvasGroup? cg, float toAlpha, float duration)
        {
            if (cg == null) yield break;
            float startAlpha = cg.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(startAlpha, toAlpha, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            cg.alpha = toAlpha;
        }

        private IEnumerator FadeButton(Button? btn, float toAlpha)
        {
            if (btn == null) yield break;
            var cg = btn.GetComponent<CanvasGroup>();
            if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
            yield return StartCoroutine(FadeToAlpha(cg, toAlpha, _buttonFadeDuration));
        }

        private static void SetAlpha(Graphic? graphic, float alpha)
        {
            if (graphic == null) return;
            var c = graphic.color;
            c.a = alpha;
            graphic.color = c;
        }

        private static void SetAlpha(GameObject? go, float alpha)
        {
            if (go == null) return;
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            cg.alpha = alpha;
        }

        private static void SetAlpha(Button? btn, float alpha)
        {
            if (btn == null) return;
            var cg = btn.GetComponent<CanvasGroup>();
            if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = alpha;
        }

        // ======================== 工具方法 ========================

        private Image? GetPlaceholder(TarotSlotPosition slot) => slot switch
        {
            TarotSlotPosition.Past => _pastPlaceholder,
            TarotSlotPosition.Present => _presentPlaceholder,
            TarotSlotPosition.Future => _futurePlaceholder,
            _ => null
        };

        private (ReadingBubble?, ReadingBubble?) GetBubbles(TarotSlotPosition slot) => slot switch
        {
            TarotSlotPosition.Past => (_pastAngelBubble, _pastDevilBubble),
            TarotSlotPosition.Present => (_presentAngelBubble, _presentDevilBubble),
            TarotSlotPosition.Future => (_futureAngelBubble, _futureDevilBubble),
            _ => (null, null)
        };

        private void HideSlotBubbles(TarotSlotPosition slot)
        {
            var (a, d) = GetBubbles(slot);
            a?.Hide();
            d?.Hide();
        }

        private void HideAllBubbles()
        {
            HideSlotBubbles(TarotSlotPosition.Past);
            HideSlotBubbles(TarotSlotPosition.Present);
            HideSlotBubbles(TarotSlotPosition.Future);
        }

        private static void ShowCardFace(Image? image, TarotDrawResult? draw)
        {
            if (image == null || draw == null) return;
            if (draw.Value.Card.Artwork != null)
            {
                image.sprite = draw.Value.Card.Artwork;
                image.color = Color.white;
            }
        }
    }
}
```

- [ ] **Step 2: 编译器验证**

Unity Editor 中 Refresh，确认无编译错误。

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Modules/HubUI/Panels/TarotRevealController.cs
git commit -m "feat(tarot): add TarotRevealController for 4-stage reveal orchestration"
```

---

### Task 5: ITarotService 添加 RequestSummaryAsync 透传 + 修复 Controller 的 Backend 访问

**Files:**
- Modify: `Assets/_Project/Scripts/Modules/Tarot/ITarotService.cs`
- Modify: `Assets/_Project/Scripts/Modules/Tarot/TarotService.cs`
- Modify: `Assets/_Project/Scripts/Modules/HubUI/Panels/TarotRevealController.cs`

- [ ] **Step 1: ITarotService 新增方法签名**

在 `ITarotService.cs` 接口中，`RequestReadingAsync` 之后添加：

```csharp
/// <summary>
/// 请求总结轮结构化运势解读。
/// </summary>
Task<TarotSummaryResult> RequestSummaryAsync(
    TarotDrawResult past, TarotDrawResult present, TarotDrawResult future,
    string? question, CancellationToken cancellationToken = default);
```

- [ ] **Step 2: TarotService 实现透传**

在 `TarotService.cs` 中，`RequestReadingAsync` 方法之后添加：

```csharp
public async Task<TarotSummaryResult> RequestSummaryAsync(
    TarotDrawResult past, TarotDrawResult present, TarotDrawResult future,
    string? question, CancellationToken cancellationToken = default)
{
    try
    {
        return await _readingBackend.RequestSummaryAsync(past, present, future, question, cancellationToken)
            .ConfigureAwait(false);
    }
    catch (OperationCanceledException)
    {
        throw;
    }
    catch (Exception ex)
    {
        Debug.LogWarning($"[Tarot] Summary backend failed, falling back. {ex.Message}");
        return TarotSummaryResult.Default();
    }
}
```

- [ ] **Step 3: 替换 TarotRevealController 中的 GetReadingBackend()**

在 `TarotRevealController.cs` 中，将 `RequestSummaryAsync` 方法和 `GetReadingBackend` 方法替换为直接使用 `_tarot`：

删除 `GetReadingBackend()` 方法。

修改 `RequestSummaryAsync()` 中调用 backend 的部分——将：

```csharp
var backend = GetReadingBackend();
if (backend == null)
{
    PopulateSummaryUI(TarotSummaryResult.Default());
    return;
}
// ... backend.RequestSummaryAsync(...)
```

替换为：

```csharp
TarotSummaryResult result;
try
{
    result = await _tarot.RequestSummaryAsync(past, present, future,
        _session.Question, _cts?.Token ?? CancellationToken.None)
        .ConfigureAwait(true);
}
catch (OperationCanceledException)
{
    return;
}
catch (Exception ex)
{
    Debug.LogWarning($"[TarotReveal] Summary failed: {ex.Message}");
    result = TarotSummaryResult.Default();
}
```

- [ ] **Step 4: 编译验证 + Commit**

```bash
git add Assets/_Project/Scripts/Modules/Tarot/ITarotService.cs \
        Assets/_Project/Scripts/Modules/Tarot/TarotService.cs \
        Assets/_Project/Scripts/Modules/HubUI/Panels/TarotRevealController.cs
git commit -m "feat(tarot): add RequestSummaryAsync to ITarotService and wire controller"
```

---

### Task 6: 重构 TarotPanelStub — 移除 Reveal 逻辑，委托给 Controller

**Files:**
- Modify: `Assets/_Project/Scripts/Modules/HubUI/Panels/TarotPanelStub.cs`

- [ ] **Step 1: 移除 Reveal 相关序列化字段**

删除 `[Header("Reveal 阶段")]` 下的所有字段（第47-59行）：

```csharp
// 删除:
[Header("Reveal 阶段")]
[SerializeField] private GameObject? _revealRoot;
[SerializeField] private Image? _revealPastImage;
... (全部 reveal 相关字段)
[SerializeField] private Button? _finishButton;
```

替换为：

```csharp
[Header("Reveal")]
[SerializeField] private TarotRevealController? _revealController;
```

- [ ] **Step 2: 移除 Reveal 相关方法**

删除以下方法：
- `SetupRevealStage()` (第287-310行)
- `FireAllReadingsAsync()` (第312-331行)
- `GetReadingForSlot()` (第333-354行)
- `OnContinueReveal()` (第356-376行)
- `RevealSlotReadings()` (第378-400行)
- `ShowSlotLoading()` (第402-410行)
- `GetSlotBubbles()` (第412-418行)
- `ShowCardInRevealSlot()` (第420-428行)
- `HideAllBubbles()` (第430-438行)
- `OnFinish()` (第440-445行)

- [ ] **Step 3: 简化 EnterStage(Stage.Reveal)**

```csharp
case Stage.Reveal:
    if (_revealController != null && _session != null && _tarot != null)
        _revealController.BeginReveal(_session, _tarot);
    break;
```

- [ ] **Step 4: 在 Awake 中绑定 Controller 事件**

在 `Awake()` 末尾（`_arcLayout = ...` 之后）添加：

```csharp
// Awake 中绑定事件回调
_revealController.OnRevealComplete += () => {
    SaveToCollection(_session!);
    _session = _tarot!.CreateSession(_questionInput?.text);
    EnterStage(Stage.Select);
};
_revealController.OnOpenGuide += () => {
    SaveToCollection(_session!);
    SwitchTab(SubView.Guide);
};
```

同时修改 Logout 中移除的按钮绑定，删除对已移除按钮的引用：

```csharp
// 在 Awake 中删除:
// if (_continueButton != null) _continueButton.onClick.AddListener(OnContinueReveal);
// if (_finishButton != null) _finishButton.onClick.AddListener(OnFinish);

// 在 OnDestroy 中删除:
// if (_continueButton != null) _continueButton.onClick.RemoveAllListeners();
// if (_finishButton != null) _finishButton.onClick.RemoveAllListeners();
```

- [ ] **Step 5: 更新 SaveToCollection 为可复用方法签名**

确保 `SaveToCollection` 在 `OnRevealComplete` 和 `OnOpenGuide` 回调中都可调用（当前签名已是 `void SaveToCollection(TarotSession session)`，无需修改）。

- [ ] **Step 6: 编译验证**

Unity Editor 中 Refresh，确认无编译错误。

- [ ] **Step 7: Commit**

```bash
git add Assets/_Project/Scripts/Modules/HubUI/Panels/TarotPanelStub.cs
git commit -m "refactor(tarot): delegate Reveal logic to TarotRevealController"
```

---

### Task 7: Prefab 绑定 — 在 Unity Editor 中连线

**说明:** 以下步骤需要在 Unity Editor 中手动操作，不产生 C# 代码变更。

- [ ] **Step 1: 在 Reveal Prefab/Root 上挂载 TarotRevealController**

- 选中 Reveal 根节点 GameObject
- Add Component → `TarotRevealController`
- 依次拖入所有 Inspector 字段：
  - 底图 × 3（past/present/future BaseImage）
  - 占位图 × 4（past/present/future/summary Placeholder）
  - 卡面 × 3（从旧 Reveal 的 _revealPastImage 等字段迁移）
  - Angel/Devil 气泡 × 6
  - 总结 UI Text 组件（运势星级、4 个幸运字段、今日建议）
  - 按钮：继续、再抽一次、查看图鉴

- [ ] **Step 2: 更新 TarotPanelStub Prefab**

- 在 TarotPanelStub Inspector 中，删除已移除的 Reveal 字段引用
- 将 TarotRevealController 组件拖入 `_revealController` 字段
- 确认旧的 `_continueButton` / `_finishButton` 绑定已清除

- [ ] **Step 3: 运行验证**

- 进入 Play Mode
- 打开 Tarot 面板 → 抽 3 张牌 → 进入 Reveal
- 验证：占卜中显示占位图 → LLM 返回后占位图渐隐气泡渐显 → 继续按钮出现 → 点击进入下一槽位
- 验证总结轮：运势星级 + 幸运提示 + 建议 + 两个按钮
- 验证"再抽一次" → 回到 Select 阶段
- 验证"查看图鉴" → 切换到图鉴子视图

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Prefabs/  # 或其他 Prefab 路径
git commit -m "feat(tarot): wire TarotRevealController in prefab, update PanelStub bindings"
```
