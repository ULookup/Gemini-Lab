#nullable enable
using System;
using System.Collections;
using GeminiLab.Modules.Tarot;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 可选取的塔罗牌 Prefab 脚本。
    /// 挂到卡牌 Prefab 上，绑定 Image + TMP_Text（牌名）。
    /// 支持 hover 放大/上浮、点击选中、飞行动画到槽位。
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
        private Vector2 _originalAnchoredPosition;
        private Quaternion _originalRotation;
        private bool _isSelected;
        private CanvasGroup? _canvasGroup;
        private RectTransform? _rectTransform;

        private RectTransform RectTransform =>
            _rectTransform ??= (transform as RectTransform)!;

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
            _originalScale = transform.localScale;
            _originalAnchoredPosition = RectTransform!.anchoredPosition;
            _originalRotation = transform.localRotation;
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>弧形布局排列完成后调用，重新记录当前弧位为原点。</summary>
        public void RecalibrateOrigin()
        {
            _originalScale = transform.localScale;
            _originalAnchoredPosition = RectTransform!.anchoredPosition;
            _originalRotation = transform.localRotation;
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
            RectTransform!.anchoredPosition = _originalAnchoredPosition;
            transform.localRotation = _originalRotation;
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
            float scale = layout?.LayoutConfig?.HoverScale ?? 1.18f;
            float lift = layout?.LayoutConfig?.HoverLift ?? 50f;
            transform.localScale = _originalScale * scale;
            // Pop outward along radial direction from arc origin
            Vector2 dir = layout != null
                ? layout.GetRadialDirection(RectTransform!.anchoredPosition)
                : Vector2.up;
            RectTransform!.anchoredPosition = _originalAnchoredPosition + dir * lift;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isSelected) return;
            transform.localScale = _originalScale;
            RectTransform!.anchoredPosition = _originalAnchoredPosition;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (CardData == null || _isSelected) return;
            _isSelected = true;
            OnClicked?.Invoke(CardData);
        }

        /// <summary>飞入目标槽位（RectTransform 位置），动画完成后回调。</summary>
        public IEnumerator FlyToSlot(RectTransform targetRt, float duration, Action? onComplete = null)
        {
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _canvasGroup.blocksRaycasts = false;

            // Render on top of siblings during flight
            transform.SetAsLastSibling();

            Vector3 startPos = RectTransform!.position;
            Vector3 startScale = transform.localScale;
            Quaternion startRot = transform.localRotation;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float e = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                RectTransform!.position = Vector3.Lerp(startPos, targetRt.position, e);
                transform.localScale = Vector3.Lerp(startScale, targetRt.localScale * 0.9f, e);
                transform.localRotation = Quaternion.Slerp(startRot, Quaternion.identity, e);
                yield return null;
            }

            RectTransform!.position = targetRt.position;
            onComplete?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
