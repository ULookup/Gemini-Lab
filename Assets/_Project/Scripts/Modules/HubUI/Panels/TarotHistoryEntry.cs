#nullable enable
using GeminiLab.Modules.Collection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 塔罗历史记录列表中单条目组件。
    /// 拖到 HistoryEntry prefab 上，绑定卡面图标 / 标题 / 日期文本。
    /// </summary>
    public sealed class TarotHistoryEntry : MonoBehaviour
    {
        [SerializeField] private Image? _cardIcon;
        [SerializeField] private TMP_Text? _titleText;
        [SerializeField] private TMP_Text? _dateText;

        public void SetData(CollectionEntry entry, Sprite? cardSprite)
        {
            if (_cardIcon != null && cardSprite != null)
            {
                _cardIcon.sprite = cardSprite;
                _cardIcon.color = Color.white;
            }
            if (_titleText != null) _titleText.text = entry.Title;
            if (_dateText != null) _dateText.text = entry.AcquiredDateIso;
        }
    }
}
