#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI
{
    /// <summary>
    /// 左侧可展开/收起的侧边栏。
    /// 仅负责发出"打开某个 Panel"的意图，面板本体由 UIRouter 管理。
    /// Apartment 与 WorldMap 场景都可以使用同一份 Prefab。
    /// </summary>
    public sealed class SidebarController : MonoBehaviour
    {
        [Header("展开/收起")]
        [SerializeField] private RectTransform? _panelRoot;
        [SerializeField] private Button? _toggleButton;
        [SerializeField] private float _expandedX = 0f;
        [SerializeField] private float _collapsedX = -240f;

        [Header("Tab 按钮")]
        [SerializeField] private Button? _tabPetStatus;
        [SerializeField] private Button? _tabTarot;
        [SerializeField] private Button? _tabCollection;
        [SerializeField] private Button? _tabInventory;

        private IUIRouter? _router;
        private bool _expanded = true;

        private void Awake()
        {
            ServiceLocator.TryResolve(out _router);

            if (_toggleButton is not null)
            {
                _toggleButton.onClick.AddListener(Toggle);
            }

            if (_tabPetStatus is not null)
            {
                _tabPetStatus.onClick.AddListener(() => OpenPanel(PanelId.PetStatus));
            }

            if (_tabTarot is not null)
            {
                _tabTarot.onClick.AddListener(() => OpenPanel(PanelId.Tarot));
            }

            if (_tabCollection is not null)
            {
                _tabCollection.onClick.AddListener(() => OpenPanel(PanelId.Collection));
            }

            if (_tabInventory is not null)
            {
                _tabInventory.onClick.AddListener(() => OpenPanel(PanelId.Inventory));
            }

            ApplyState(instant: true);
        }

        public void Toggle()
        {
            _expanded = !_expanded;
            ApplyState(instant: false);
        }

        private void OpenPanel(PanelId id)
        {
            if (_router is null && !ServiceLocator.TryResolve(out _router))
            {
                Debug.LogWarning($"[Sidebar] 未找到 IUIRouter，无法打开 {id}");
                return;
            }

            _router!.Open(id);
        }

        private void ApplyState(bool instant)
        {
            if (_panelRoot is null)
            {
                return;
            }

            float targetX = _expanded ? _expandedX : _collapsedX;
            Vector2 pos = _panelRoot.anchoredPosition;
            pos.x = targetX;
            _panelRoot.anchoredPosition = pos;
            _ = instant; // 动画暂未接入，占位保留签名
        }
    }
}
