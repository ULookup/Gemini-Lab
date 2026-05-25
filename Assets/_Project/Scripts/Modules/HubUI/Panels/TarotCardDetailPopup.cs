#nullable enable
using System.Collections;
using GeminiLab.Modules.Tarot;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 塔罗图鉴详情弹窗：展示卡面、中英文名、正位/逆位描述。
    /// </summary>
    public sealed class TarotCardDetailPopup : MonoBehaviour
    {
        [Header("卡面")]
        [SerializeField] private Image? _cardImage;
        [SerializeField] private TMP_Text? _nameZhText;
        [SerializeField] private TMP_Text? _nameEnText;

        [Header("描述")]
        [SerializeField] private TMP_Text? _uprightTitle;
        [SerializeField] private TMP_Text? _uprightDesc;
        [SerializeField] private TMP_Text? _reversedTitle;
        [SerializeField] private TMP_Text? _reversedDesc;

        [Header("操作")]
        [SerializeField] private Button? _closeButton;
        [SerializeField] private Button? _overlayButton;
        [SerializeField] private GameObject? _contentRoot;
        [SerializeField] private float _fadeDuration = 0.25f;

        private CanvasGroup? _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
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

        public void Show(TarotCardSO card)
        {
            if (_cardImage != null && card.Artwork != null)
            {
                _cardImage.sprite = card.Artwork;
                _cardImage.color = Color.white;
            }

            if (_nameZhText != null)
                _nameZhText.text = $"{card.MajorIndex}: {card.DisplayNameZh}";
            if (_nameEnText != null)
                _nameEnText.text = card.DisplayNameEn;

            if (_uprightTitle != null) _uprightTitle.text = "正位";
            if (_uprightDesc != null)
                _uprightDesc.text = !string.IsNullOrEmpty(card.UprightDescription)
                    ? card.UprightDescription
                    : string.Join("、", card.UprightKeywords);

            if (_reversedTitle != null) _reversedTitle.text = "逆位";
            if (_reversedDesc != null)
                _reversedDesc.text = !string.IsNullOrEmpty(card.ReversedDescription)
                    ? card.ReversedDescription
                    : string.Join("、", card.ReversedKeywords);

            gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(FadeIn());
        }

        public void Hide()
        {
            StopAllCoroutines();
            StartCoroutine(FadeOut());
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
