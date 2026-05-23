#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI
{
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
        [SerializeField] private Button? _tabGarden;

        [Header("高亮色")]
        [SerializeField] private Color _activeTabColor = new Color(1f, 0.85f, 0.3f, 0.45f);
        [SerializeField] private Color _inactiveTabColor = new Color(0.18f, 0.22f, 0.3f, 1f);

        private IUIRouter? _router;
        private EventBus? _eventBus;
        private IDisposable? _panelClosedSub;
        private bool _expanded = true;

        private PanelId? _activePanelId;
        private Dictionary<PanelId, Button> _tabMap = new();
        private Dictionary<Button, Image> _tabBgMap = new();

        private void Awake()
        {
            ServiceLocator.TryResolve(out _router);
            ServiceLocator.TryResolve(out _eventBus);

            if (_eventBus is not null)
            {
                _panelClosedSub = _eventBus.Subscribe<UIPanelClosedEvent>(OnPanelClosed);
            }

            BuildTabMap();

            if (_toggleButton is not null)
            {
                _toggleButton.onClick.AddListener(Toggle);
            }

            if (_tabPetStatus is not null)
                _tabPetStatus.onClick.AddListener(() => OpenPanel(PanelId.PetStatus));
            if (_tabTarot is not null)
                _tabTarot.onClick.AddListener(() => OpenPanel(PanelId.Tarot));
            if (_tabCollection is not null)
                _tabCollection.onClick.AddListener(() => OpenPanel(PanelId.Collection));
            if (_tabInventory is not null)
                _tabInventory.onClick.AddListener(() => OpenPanel(PanelId.Inventory));
            if (_tabGarden is not null)
                _tabGarden.onClick.AddListener(() => OpenPanel(PanelId.Garden));

            ApplyState(instant: true);
        }

        private void OnDestroy()
        {
            _panelClosedSub?.Dispose();
        }

        public void Toggle()
        {
            _expanded = !_expanded;
            ApplyState(instant: false);
        }

        private void BuildTabMap()
        {
            AddTab(PanelId.PetStatus, _tabPetStatus);
            AddTab(PanelId.Tarot, _tabTarot);
            AddTab(PanelId.Collection, _tabCollection);
            AddTab(PanelId.Inventory, _tabInventory);
            AddTab(PanelId.Garden, _tabGarden);
        }

        private void AddTab(PanelId id, Button? btn)
        {
            if (btn == null) return;
            _tabMap[id] = btn;
            var img = btn.GetComponent<Image>();
            if (img != null) _tabBgMap[btn] = img;
        }

        private void OpenPanel(PanelId id)
        {
            if (_router is null && !ServiceLocator.TryResolve(out _router))
            {
                Debug.LogWarning($"[Sidebar] 未找到 IUIRouter，无法打开 {id}");
                return;
            }

            if (_activePanelId == id)
            {
                _router!.Close(id);
                return;
            }

            if (_activePanelId is not null)
            {
                _router!.Close(_activePanelId.Value);
            }

            _router!.Open(id);
            _activePanelId = id;
            RefreshTabHighlight();
        }

        private void OnPanelClosed(UIPanelClosedEvent e)
        {
            if (_activePanelId == e.Id)
            {
                _activePanelId = null;
                RefreshTabHighlight();
            }
        }

        private void RefreshTabHighlight()
        {
            foreach (var (id, btn) in _tabMap)
            {
                if (_tabBgMap.TryGetValue(btn, out var img))
                {
                    img.color = id == _activePanelId ? _activeTabColor : _inactiveTabColor;
                }
            }
        }

        private void ApplyState(bool instant)
        {
            if (_panelRoot is null) return;
            float targetX = _expanded ? _expandedX : _collapsedX;
            Vector2 pos = _panelRoot.anchoredPosition;
            pos.x = targetX;
            _panelRoot.anchoredPosition = pos;
            _ = instant;
        }
    }
}
