#nullable enable
using GeminiLab.Modules.Tarot;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 塔罗图鉴中单张卡牌组件。
    /// 拖到 GuideCard prefab 上，绑定卡面图片和名称文本。
    /// </summary>
    public sealed class TarotGuideCard : MonoBehaviour
    {
        [SerializeField] private Image? _cardImage;
        [SerializeField] private TMP_Text? _nameText;

        public void SetData(TarotCardSO card)
        {
            if (_cardImage != null && card.Artwork != null)
            {
                _cardImage.sprite = card.Artwork;
                _cardImage.color = Color.white;
            }
            if (_nameText != null) _nameText.text = $"{card.MajorIndex}: {card.DisplayNameZh}";
        }
    }
}
