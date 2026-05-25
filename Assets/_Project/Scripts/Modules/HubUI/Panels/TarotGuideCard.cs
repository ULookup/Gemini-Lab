#nullable enable
using System;
using GeminiLab.Modules.Tarot;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 塔罗图鉴中单张卡牌组件。支持已解锁/未解锁双状态，点击已解锁卡牌弹出详情。
    /// </summary>
    public sealed class TarotGuideCard : MonoBehaviour
    {
        [SerializeField] private Image? _cardImage;
        [SerializeField] private TMP_Text? _nameText;
        [SerializeField] private GameObject? _unlockedState;
        [SerializeField] private GameObject? _lockedState;
        [SerializeField] private Sprite? _lockedPlaceholder;
        [SerializeField] private Button? _button;

        public event Action<TarotCardSO>? OnClicked;

        private TarotCardSO? _card;
        private bool _unlocked;

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

        public void SetData(TarotCardSO card, bool unlocked)
        {
            _card = card;
            _unlocked = unlocked;

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
                if (_button != null) _button.interactable = true;
            }
            else
            {
                if (_unlockedState != null) _unlockedState.SetActive(false);
                if (_lockedState != null) _lockedState.SetActive(true);
                if (_button != null) _button.interactable = false;
                if (_cardImage != null && _lockedPlaceholder != null)
                {
                    _cardImage.enabled = true;
                    _cardImage.sprite = _lockedPlaceholder;
                    _cardImage.color = Color.white;
                }
                if (_nameText != null) _nameText.text = "";
            }
        }

        private void HandleClick()
        {
            if (_unlocked && _card != null)
                OnClicked?.Invoke(_card);
        }
    }
}
