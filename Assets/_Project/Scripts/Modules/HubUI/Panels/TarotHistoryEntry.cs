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
