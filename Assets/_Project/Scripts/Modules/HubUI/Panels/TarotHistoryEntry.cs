#nullable enable
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 塔罗历史记录单条目的三区布局：
    /// 左区（日期 + 类型）、中区（3 张卡面缩略图）、右区（5 星运势）。
    /// </summary>
    public sealed class TarotHistoryEntry : MonoBehaviour
    {
        [Header("左区：基础信息")]
        [SerializeField] private TMP_Text? _dateText;
        [SerializeField] private TMP_Text? _typeText;

        [Header("中区：三张塔罗牌")]
        [SerializeField] private Image? _cardImage1;
        [SerializeField] private Image? _cardImage2;
        [SerializeField] private Image? _cardImage3;

        [Header("右区：五星评分")]
        [SerializeField] private TMP_Text? _starsText;

        [Header("分隔线")]
        [SerializeField] private GameObject? _separator;

        public void SetData(string date, string type,
            Sprite? card1, Sprite? card2, Sprite? card3, int fortuneLevel)
        {
            if (_dateText != null) _dateText.text = date;
            if (_typeText != null) _typeText.text = type;
            SetCardSprite(_cardImage1, card1);
            SetCardSprite(_cardImage2, card2);
            SetCardSprite(_cardImage3, card3);
            if (_starsText != null)
                _starsText.text = new string('★', fortuneLevel) + new string('☆', 5 - fortuneLevel);
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
