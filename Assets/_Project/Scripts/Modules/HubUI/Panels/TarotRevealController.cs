#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
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

            if (_continueButton != null) SetButtonAlpha(_continueButton, 0f);
            if (_redrawButton != null) SetButtonAlpha(_redrawButton, 0f);
            if (_openGuideButton != null) SetButtonAlpha(_openGuideButton, 0f);
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

            ShowCardFace(_pastCardImage, session.PastCard);
            ShowCardFace(_presentCardImage, session.PresentCard);
            ShowCardFace(_futureCardImage, session.FutureCard);

            HideAllBubbles();

            if (_summaryContentRoot != null) _summaryContentRoot.SetActive(false);
            if (_summaryPlaceholder != null) _summaryPlaceholder.gameObject.SetActive(false);

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _ = FireAllReadingsAsync(_cts.Token);

            EnterPhase(RevealPhase.Past);
        }

        // ======================== 阶段管理 ========================

        private void EnterPhase(RevealPhase phase)
        {
            _phase = phase;

            SetButtonAlpha(_continueButton, 0f);
            if (_continueButton != null) _continueButton.interactable = false;

            switch (phase)
            {
                case RevealPhase.Past:
                    ShowPlaceholder(TarotSlotPosition.Past);
                    StartCoroutine(WaitThenRevealSlot(TarotSlotPosition.Past));
                    break;
                case RevealPhase.Present:
                    ShowPlaceholder(TarotSlotPosition.Present);
                    StartCoroutine(WaitThenRevealSlot(TarotSlotPosition.Present));
                    break;
                case RevealPhase.Future:
                    ShowPlaceholder(TarotSlotPosition.Future);
                    StartCoroutine(WaitThenRevealSlot(TarotSlotPosition.Future));
                    break;
                case RevealPhase.Summary:
                    EnterSummaryPhase();
                    break;
            }
        }

        private void OnContinueClicked()
        {
            if (_continueButton != null) _continueButton.interactable = false;
            StartCoroutine(FadeButton(_continueButton, toAlpha: 0f));

            switch (_phase)
            {
                case RevealPhase.Past: EnterPhase(RevealPhase.Present); break;
                case RevealPhase.Present: EnterPhase(RevealPhase.Future); break;
                case RevealPhase.Future: EnterPhase(RevealPhase.Summary); break;
            }
        }

        // ======================== 槽位占卜 → 揭示 ========================

        private void ShowPlaceholder(TarotSlotPosition slot)
        {
            var placeholder = GetPlaceholder(slot);
            if (placeholder == null) return;
            placeholder.gameObject.SetActive(true);
            SetImageAlpha(placeholder, 1f);
            HideSlotBubbles(slot);
        }

        private IEnumerator WaitThenRevealSlot(TarotSlotPosition slot)
        {
            string angelKey = TarotSession.ReadingKey(slot, PetId.Angel);
            string devilKey = TarotSession.ReadingKey(slot, PetId.Devil);

            while (_session != null &&
                   (!_session.Readings.ContainsKey(angelKey) || !_session.Readings.ContainsKey(devilKey)))
            {
                yield return null;
            }

            var placeholder = GetPlaceholder(slot);
            if (placeholder != null)
            {
                yield return StartCoroutine(CrossfadeImage(placeholder, toAlpha: 0f, _fadeDuration));
                placeholder.gameObject.SetActive(false);
            }

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
                    StartCoroutine(FadeBubbleIn(angelBubble, _fadeDuration));
                }
                if (_session.Readings.TryGetValue(devilKey, out var devilReading))
                {
                    devilBubble?.Show($"恶魔 · {slotName}", devilReading.Text, isAngel: false);
                    StartCoroutine(FadeBubbleIn(devilBubble, _fadeDuration));
                }
            }

            if (_phase != RevealPhase.Summary)
            {
                yield return StartCoroutine(FadeButton(_continueButton, toAlpha: 1f));
                if (_continueButton != null) _continueButton.interactable = true;
            }
        }

        // ======================== 总结阶段 ========================

        private void EnterSummaryPhase()
        {
            if (_summaryPlaceholder != null)
            {
                _summaryPlaceholder.gameObject.SetActive(true);
                SetImageAlpha(_summaryPlaceholder, 1f);
            }
            if (_summaryContentRoot != null) _summaryContentRoot.SetActive(false);

            _ = RequestSummaryAsync();
        }

        private async Task RequestSummaryAsync()
        {
            if (_session == null || _tarot == null) return;

            TarotSummaryResult result;
            try
            {
                var past = _session.PastCard ?? default;
                var present = _session.PresentCard ?? default;
                var future = _session.FutureCard ?? default;

                result = await _tarot.RequestSummaryAsync(past, present, future,
                    _session.Question, _cts?.Token ?? CancellationToken.None)
                    .ConfigureAwait(true);
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TarotReveal] Summary failed: {ex.Message}");
                result = TarotSummaryResult.Default();
            }

            PopulateSummaryUI(result);
        }

        private void PopulateSummaryUI(TarotSummaryResult result)
        {
            if (_summaryPlaceholder != null)
            {
                StartCoroutine(CrossfadeImage(_summaryPlaceholder, toAlpha: 0f, _fadeDuration));
                _summaryPlaceholder.gameObject.SetActive(false);
            }

            if (_summaryContentRoot != null)
            {
                _summaryContentRoot.SetActive(true);
                SetGameObjectAlpha(_summaryContentRoot, 0f);
            }

            if (_fortuneStarsText != null)
                _fortuneStarsText.text = new string('★', result.fortuneLevel) + new string('☆', 5 - result.fortuneLevel);
            if (_luckyColorText != null) _luckyColorText.text = result.luckyHint?.color ?? "";
            if (_luckyNumberText != null) _luckyNumberText.text = result.luckyHint?.number ?? "";
            if (_luckyTimeText != null) _luckyTimeText.text = result.luckyHint?.time ?? "";
            if (_luckyActionText != null) _luckyActionText.text = result.luckyHint?.action ?? "";
            if (_adviceText != null) _adviceText.text = result.advice ?? "";

            StartCoroutine(FadeGameObjectIn(_summaryContentRoot, _fadeDuration));
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

            var tasks = new List<Task>();
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

        // ======================== 动画辅助 ========================

        private static void SetImageAlpha(Image? img, float alpha)
        {
            if (img == null) return;
            var c = img.color;
            c.a = alpha;
            img.color = c;
        }

        private static void SetButtonAlpha(Button? btn, float alpha)
        {
            if (btn == null) return;
            var cg = btn.GetComponent<CanvasGroup>();
            if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = alpha;
        }

        private static void SetGameObjectAlpha(GameObject go, float alpha)
        {
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            cg.alpha = alpha;
        }

        private IEnumerator CrossfadeImage(Image? img, float toAlpha, float duration)
        {
            if (img == null) yield break;
            var c = img.color;
            float startAlpha = c.a;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(startAlpha, toAlpha, Mathf.Clamp01(elapsed / duration));
                img.color = c;
                yield return null;
            }
            c.a = toAlpha;
            img.color = c;
        }

        private IEnumerator FadeButton(Button? btn, float toAlpha)
        {
            if (btn == null) yield break;
            var cg = btn.GetComponent<CanvasGroup>();
            if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
            float startAlpha = cg.alpha;
            float elapsed = 0f;
            while (elapsed < _buttonFadeDuration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(startAlpha, toAlpha, Mathf.Clamp01(elapsed / _buttonFadeDuration));
                yield return null;
            }
            cg.alpha = toAlpha;
        }

        private IEnumerator FadeBubbleIn(ReadingBubble? bubble, float duration)
        {
            if (bubble == null) yield break;
            var cg = bubble.GetComponent<CanvasGroup>();
            if (cg == null) cg = bubble.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(0f, 1f, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            cg.alpha = 1f;
        }

        private IEnumerator FadeGameObjectIn(GameObject? go, float duration)
        {
            if (go == null) yield break;
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(0f, 1f, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            cg.alpha = 1f;
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
