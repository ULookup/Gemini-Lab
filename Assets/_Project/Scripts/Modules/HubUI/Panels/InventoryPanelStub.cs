#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.UI;
using GeminiLab.Modules.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 物品栏面板：网格显示当前 Inventory；订阅 InventoryChangedEvent 自动刷新。
    /// 每格显示 icon + count + Tooltip（中文名 / 分类 / 说明）。
    /// </summary>
    public sealed class InventoryPanelStub : StubPanelBase
    {
        public override PanelId Id => PanelId.Inventory;

        [Header("格子")]
        [SerializeField] private Transform? _gridRoot;
        [SerializeField] private GameObject? _slotPrefab;

        [Header("Tooltip")]
        [SerializeField] private GameObject? _tooltipRoot;
        [SerializeField] private TMP_Text? _tooltipName;
        [SerializeField] private TMP_Text? _tooltipCategory;
        [SerializeField] private TMP_Text? _tooltipText;

        [Header("道具目录")]
        [SerializeField] private ItemCatalogSO? _catalog;

        private IInventoryService? _service;
        private EventBus? _eventBus;
        private IDisposable? _changedSub;
        private readonly List<GameObject> _spawned = new();

        protected override void Awake()
        {
            base.Awake();
            HideTooltip();
        }

        protected override void OnDestroy()
        {
            _changedSub?.Dispose();
            base.OnDestroy();
        }

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);
            EnsureServices();
            Refresh();

            if (_eventBus is not null && _changedSub is null)
            {
                _changedSub = _eventBus.Subscribe<InventoryChangedEvent>(_ => Refresh());
            }
        }

        public override void OnClose()
        {
            base.OnClose();
            _changedSub?.Dispose();
            _changedSub = null;
            HideTooltip();
        }

        private void EnsureServices()
        {
            if (_service == null) ServiceLocator.TryResolve(out _service);
            if (_eventBus == null) ServiceLocator.TryResolve(out _eventBus);
        }

        private void Refresh()
        {
            if (_gridRoot == null) return;

            foreach (var go in _spawned)
            {
                if (go != null) Destroy(go);
            }
            _spawned.Clear();

            if (_service == null) return;

            foreach (var stack in _service.GetAllStacks())
            {
                SpawnSlot(stack);
            }
        }

        private void SpawnSlot(ItemStack stack)
        {
            if (_gridRoot == null) return;

            GameObject slot;
            if (_slotPrefab != null)
            {
                slot = Instantiate(_slotPrefab, _gridRoot);
            }
            else
            {
                slot = BuildSlotDefault();
            }

            var icon = slot.transform.Find("Icon")?.GetComponent<Image>();
            var count = slot.transform.Find("Count")?.GetComponent<TMP_Text>();

            var def = _catalog != null ? _catalog.Get(stack.ItemId) : null;
            if (icon != null)
            {
                icon.sprite = def?.Icon;
                icon.color = def?.Icon != null ? Color.white : new Color(0.55f, 0.55f, 0.6f, 1f);
            }
            if (count != null)
            {
                count.text = stack.Count > 1 ? stack.Count.ToString() : string.Empty;
            }

            var trigger = slot.AddComponent<InventoryHoverTrigger>();
            trigger.Bind(this, stack, def);

            _spawned.Add(slot);
        }

        private GameObject BuildSlotDefault()
        {
            var go = new GameObject("Slot");
            go.transform.SetParent(_gridRoot, false);
            int layer = _gridRoot != null ? _gridRoot.gameObject.layer : 5;
            go.layer = layer;
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(96, 96);
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.08f);
            img.raycastTarget = true;

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            iconGo.layer = layer;
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(8, 8); iconRt.offsetMax = new Vector2(-8, -8);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.raycastTarget = false;
            iconImg.preserveAspect = true;

            var countGo = new GameObject("Count");
            countGo.transform.SetParent(go.transform, false);
            countGo.layer = layer;
            var crt = countGo.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(1, 0); crt.anchorMax = new Vector2(1, 0);
            crt.pivot = new Vector2(1, 0);
            crt.anchoredPosition = new Vector2(-4, 4);
            crt.sizeDelta = new Vector2(60, 24);
            var ct = countGo.AddComponent<TextMeshProUGUI>();
            ct.alignment = TextAlignmentOptions.BottomRight;
            ct.fontSize = 18;
            ct.color = Color.white;
            countGo.AddComponent<GeminiLab.Modules.UI.Catalogs.TMPFontBinder>();

            return go;
        }

        internal void ShowTooltip(ItemStack stack, ItemDefSO? def)
        {
            if (_tooltipRoot == null) return;
            _tooltipRoot.SetActive(true);

            if (_tooltipName != null) _tooltipName.text = def != null ? def.DisplayNameZh : stack.ItemId;
            if (_tooltipCategory != null) _tooltipCategory.text = def != null ? def.Category.ToString() : string.Empty;
            if (_tooltipText != null) _tooltipText.text = def != null ? def.Tooltip : string.Empty;
        }

        internal void HideTooltip()
        {
            if (_tooltipRoot != null) _tooltipRoot.SetActive(false);
        }
    }
}
