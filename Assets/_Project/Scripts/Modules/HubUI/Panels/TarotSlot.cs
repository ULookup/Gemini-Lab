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
