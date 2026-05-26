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
