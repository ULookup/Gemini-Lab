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
