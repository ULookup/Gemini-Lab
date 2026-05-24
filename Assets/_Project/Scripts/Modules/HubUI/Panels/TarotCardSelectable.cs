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
