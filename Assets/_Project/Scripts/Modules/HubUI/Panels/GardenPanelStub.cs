#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.UI;
using GeminiLab.Modules.Garden;
using GeminiLab.Modules.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 花园面板：3×3 地块网格 + 种子选择条。
    /// - 空地块 → 点击弹出种子选择（Inventory 中 Seed 类道具）
    /// - 成长中 → 显示剩余秒数 / 进度条
    /// - Ready → 点击收获
    /// 订阅 <see cref="GardenPlotChangedEvent"/> / <see cref="InventoryChangedEvent"/> 自动刷新。
    /// </summary>
    public sealed class GardenPanelStub : StubPanelBase
    {
        public override PanelId Id => PanelId.Garden;

        [Header("3×3 地块")]
        [SerializeField] private Transform? _gridRoot;

        [Header("种子选择条")]
        [SerializeField] private Transform? _seedBarRoot;
        [SerializeField] private TMP_Text? _seedHintText;

        [Header("道具目录（查 icon / 显示名）")]
        [SerializeField] private ItemCatalogSO? _itemCatalog;

        private IGardenService? _garden;
        private IInventoryService? _inventory;
        private EventBus? _eventBus;
        private IDisposable? _plotSub;
        private IDisposable? _inventorySub;

        private readonly List<GameObject> _plotCells = new();
        private readonly List<GameObject> _seedButtons = new();

        private int _selectedSeedIndex = -1;
        private readonly List<string> _availableSeedIds = new();

        protected override void OnDestroy()
        {
            _plotSub?.Dispose();
            _inventorySub?.Dispose();
            base.OnDestroy();
        }

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);
            EnsureServices();
            Refresh();

            if (_eventBus is not null)
            {
                _plotSub ??= _eventBus.Subscribe<GardenPlotChangedEvent>(_ => RefreshPlots());
                _inventorySub ??= _eventBus.Subscribe<InventoryChangedEvent>(_ => RefreshSeedBar());
            }
        }

        public override void OnClose()
        {
            base.OnClose();
            _plotSub?.Dispose(); _plotSub = null;
            _inventorySub?.Dispose(); _inventorySub = null;
        }

        private void Update()
        {
            // 每帧刷剩余秒数文本
            if (_garden == null) return;
            for (int i = 0; i < _plotCells.Count; i++)
            {
                var cell = _plotCells[i];
                if (cell == null) continue;
                var timer = cell.transform.Find("Timer")?.GetComponent<TMP_Text>();
                if (timer == null) continue;
                var p = _garden.Get(i);
                if (p.Stage == GardenStage.Seeded || p.Stage == GardenStage.Growing)
                {
                    int remain = _garden.GetRemainingSeconds(i);
                    timer.text = FormatRemain(remain);
                }
                else
                {
                    timer.text = string.Empty;
                }
            }
        }

        private void EnsureServices()
        {
            if (_garden == null) ServiceLocator.TryResolve(out _garden);
            if (_inventory == null) ServiceLocator.TryResolve(out _inventory);
            if (_eventBus == null) ServiceLocator.TryResolve(out _eventBus);
        }

        private void Refresh()
        {
            RefreshPlots();
            RefreshSeedBar();
        }

        private void RefreshPlots()
        {
            if (_gridRoot == null || _garden == null) return;

            EnsurePlotCells();

            for (int i = 0; i < _plotCells.Count; i++)
            {
                var p = _garden.Get(i);
                ApplyPlotVisual(_plotCells[i], p);
            }
        }

        private void EnsurePlotCells()
        {
            if (_gridRoot == null || _garden == null) return;
            int needed = _garden.PlotCount;
            while (_plotCells.Count < needed)
            {
                int index = _plotCells.Count;
                var cell = BuildPlotCell(index);
                _plotCells.Add(cell);
            }
        }

        private GameObject BuildPlotCell(int index)
        {
            var go = new GameObject($"Plot_{index}");
            go.transform.SetParent(_gridRoot, false);
            int layer = _gridRoot != null ? _gridRoot.gameObject.layer : 5;
            go.layer = layer;

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(96, 96);
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.3f, 0.25f, 0.15f, 0.5f);
            var btn = go.AddComponent<Button>();

            // Stage icon
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            iconGo.layer = layer;
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(48, 48);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.raycastTarget = false;
            iconImg.preserveAspect = true;

            // Timer
            var timerGo = new GameObject("Timer");
            timerGo.transform.SetParent(go.transform, false);
            timerGo.layer = layer;
            var tRt = timerGo.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 0);
            tRt.anchorMax = new Vector2(1, 0);
            tRt.pivot = new Vector2(0.5f, 0);
            tRt.sizeDelta = new Vector2(0, 20);
            var timerText = timerGo.AddComponent<TextMeshProUGUI>();
            timerText.fontSize = 14;
            timerText.alignment = TextAlignmentOptions.Bottom;
            timerText.color = new Color(1, 1, 1, 0.85f);
            timerGo.AddComponent<GeminiLab.Modules.UI.Catalogs.TMPFontBinder>();

            int captured = index;
            btn.onClick.AddListener(() => OnPlotClicked(captured));

            return go;
        }

        private void ApplyPlotVisual(GameObject cell, GardenPlot p)
        {
            if (cell == null) return;
            var icon = cell.transform.Find("Icon")?.GetComponent<Image>();
            if (icon == null) return;

            switch (p.Stage)
            {
                case GardenStage.Empty:
                    icon.enabled = false;
                    break;
                case GardenStage.Seeded:
                    icon.enabled = true;
                    icon.sprite = GetSpriteForItem(p.SeedItemId);
                    icon.color = icon.sprite != null ? new Color(1, 1, 1, 0.5f) : new Color(0.6f, 0.45f, 0.3f, 0.6f);
                    break;
                case GardenStage.Growing:
                    icon.enabled = true;
                    icon.sprite = GetSpriteForItem(p.SeedItemId);
                    icon.color = icon.sprite != null ? new Color(1, 1, 1, 0.75f) : new Color(0.5f, 0.75f, 0.4f, 0.8f);
                    break;
                case GardenStage.Ready:
                    icon.enabled = true;
                    icon.sprite = GetSpriteForItem(p.CropItemId);
                    icon.color = icon.sprite != null ? Color.white : new Color(0.95f, 0.85f, 0.25f, 1f);
                    break;
                default:
                    icon.enabled = false;
                    break;
            }
        }

        private Sprite? GetSpriteForItem(string? itemId)
        {
            if (string.IsNullOrEmpty(itemId) || _itemCatalog == null) return null;
            return _itemCatalog.Get(itemId)?.Icon;
        }

        private void OnPlotClicked(int index)
        {
            if (_garden == null) return;
            var p = _garden.Get(index);
            if (p.Stage == GardenStage.Ready)
            {
                _garden.Harvest(index);
                return;
            }
            if (p.Stage == GardenStage.Empty)
            {
                TryPlantSelected(index);
            }
        }

        private void TryPlantSelected(int index)
        {
            if (_garden == null) return;
            if (_selectedSeedIndex < 0 || _selectedSeedIndex >= _availableSeedIds.Count)
            {
                if (_seedHintText != null) _seedHintText.text = "先在下方选一种种子";
                return;
            }
            string seedId = _availableSeedIds[_selectedSeedIndex];
            bool ok = _garden.Plant(index, seedId);
            if (!ok && _seedHintText != null)
            {
                _seedHintText.text = $"种植失败：{seedId}";
            }
        }

        private void RefreshSeedBar()
        {
            if (_seedBarRoot == null) return;

            foreach (var go in _seedButtons) if (go != null) Destroy(go);
            _seedButtons.Clear();
            _availableSeedIds.Clear();

            if (_inventory == null || _itemCatalog == null) return;

            foreach (var stack in _inventory.GetAllStacks())
            {
                var def = _itemCatalog.Get(stack.ItemId);
                if (def == null || def.Category != ItemCategory.Seed) continue;
                if (_availableSeedIds.Contains(stack.ItemId)) continue;
                _availableSeedIds.Add(stack.ItemId);
                SpawnSeedButton(stack.ItemId, def, _inventory.GetTotalCount(stack.ItemId));
            }

            if (_selectedSeedIndex >= _availableSeedIds.Count) _selectedSeedIndex = -1;
            if (_selectedSeedIndex < 0 && _availableSeedIds.Count > 0) _selectedSeedIndex = 0;
            HighlightSelection();

            if (_seedHintText != null)
            {
                _seedHintText.text = _availableSeedIds.Count > 0
                    ? "点地块种下；成熟后点收获"
                    : "没有种子，试试通过其他系统获取";
            }
        }

        private void SpawnSeedButton(string seedId, ItemDefSO def, int count)
        {
            if (_seedBarRoot == null) return;
            var go = new GameObject($"Seed_{seedId}");
            go.transform.SetParent(_seedBarRoot, false);
            int layer = _seedBarRoot.gameObject.layer;
            go.layer = layer;

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(72, 72);
            var bg = go.AddComponent<Image>();
            bg.color = new Color(1, 1, 1, 0.12f);
            var btn = go.AddComponent<Button>();

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            iconGo.layer = layer;
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(8, 20); iconRt.offsetMax = new Vector2(-8, -8);
            var img = iconGo.AddComponent<Image>();
            img.sprite = def.Icon;
            img.raycastTarget = false;
            img.preserveAspect = true;
            img.color = def.Icon != null ? Color.white : new Color(0.55f, 0.55f, 0.6f, 1f);

            var countGo = new GameObject("Count");
            countGo.transform.SetParent(go.transform, false);
            countGo.layer = layer;
            var crt = countGo.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 0); crt.anchorMax = new Vector2(1, 0);
            crt.pivot = new Vector2(0.5f, 0); crt.sizeDelta = new Vector2(0, 18);
            var countText = countGo.AddComponent<TextMeshProUGUI>();
            countText.text = $"{def.DisplayNameZh}×{count}";
            countText.fontSize = 12;
            countText.alignment = TextAlignmentOptions.Center;
            countGo.AddComponent<GeminiLab.Modules.UI.Catalogs.TMPFontBinder>();

            int captured = _seedButtons.Count;
            btn.onClick.AddListener(() =>
            {
                _selectedSeedIndex = captured;
                HighlightSelection();
            });

            _seedButtons.Add(go);
        }

        private void HighlightSelection()
        {
            for (int i = 0; i < _seedButtons.Count; i++)
            {
                var img = _seedButtons[i].GetComponent<Image>();
                if (img == null) continue;
                img.color = i == _selectedSeedIndex
                    ? new Color(1f, 0.9f, 0.35f, 0.45f)
                    : new Color(1, 1, 1, 0.12f);
            }
        }

        private static string FormatRemain(int seconds)
        {
            if (seconds <= 0) return string.Empty;
            int m = seconds / 60;
            int s = seconds % 60;
            return $"{m:00}:{s:00}";
        }
    }
}
