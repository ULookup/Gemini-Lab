#nullable enable
using UnityEngine;
using UnityEngine.EventSystems;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 每周种植面板瓶子的场景作者化交互。
    /// 运行时只切换已经存在的高亮节点和 RectTransform 缩放，不创建或替换视觉资源。
    /// </summary>
    public sealed class WeeklyGardenBottleInteraction : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler
    {
        [SerializeField] private WeeklyGardenPanelStub? _panel;
        [SerializeField] private int _dayIndex;
        [SerializeField] private RectTransform? _scaleTarget;
        [SerializeField] private GameObject? _selectedHighlight;
        [SerializeField] private Vector3 _normalScale = Vector3.one;
        [SerializeField] private Vector3 _hoverScale = new(1.06f, 1.06f, 1f);

        private bool _isHovered;
        private bool _isSelected;

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            ApplyVisualState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            ApplyVisualState();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _panel?.SelectDay(_dayIndex);
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            ApplyVisualState();
        }

        private void ApplyVisualState()
        {
            if (_scaleTarget != null)
            {
                // 选中态使用外圈高亮，保持瓶子本体的原始尺寸与颜色。
                _scaleTarget.localScale = _isSelected
                    ? _normalScale
                    : (_isHovered ? _hoverScale : _normalScale);
            }

            if (_selectedHighlight != null)
            {
                _selectedHighlight.SetActive(_isSelected);
            }
        }
    }
}
