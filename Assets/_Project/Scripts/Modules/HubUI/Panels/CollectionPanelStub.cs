#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.UI;
using GeminiLab.Modules.Collection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 收藏面板：顶部按 CollectionCategory 切页签，中间 Grid 列条目。
    /// 订阅 CollectionChangedEvent 自动刷新。
    /// </summary>
    public sealed class CollectionPanelStub : StubPanelBase
    {
        public override PanelId Id => PanelId.Collection;

        [Header("页签按钮")]
        [SerializeField] private Button? _tabTravel;
        [SerializeField] private Button? _tabTarot;
        [SerializeField] private Button? _tabGarden;

        [Header("Grid 与空态")]
        [SerializeField] private Transform? _gridRoot;
        [SerializeField] private TMP_Text? _emptyHint;

        private ICollectionService? _service;
        private EventBus? _eventBus;
        private IDisposable? _changedSub;
        private CollectionCategory _currentCategory = CollectionCategory.Tarot;
        private readonly List<GameObject> _spawned = new();

        protected override void Awake()
        {
            base.Awake();

            if (_tabTravel != null) _tabTravel.onClick.AddListener(() => SwitchCategory(CollectionCategory.Travel));
            if (_tabTarot != null) _tabTarot.onClick.AddListener(() => SwitchCategory(CollectionCategory.Tarot));
            if (_tabGarden != null) _tabGarden.onClick.AddListener(() => SwitchCategory(CollectionCategory.GardenHarvest));
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
                _changedSub = _eventBus.Subscribe<CollectionChangedEvent>(_ => Refresh());
            }
        }

        public override void OnClose()
        {
            base.OnClose();
            _changedSub?.Dispose();
            _changedSub = null;
        }

        private void EnsureServices()
        {
            if (_service == null) ServiceLocator.TryResolve(out _service);
            if (_eventBus == null) ServiceLocator.TryResolve(out _eventBus);
        }

        private void SwitchCategory(CollectionCategory category)
        {
            _currentCategory = category;
            Refresh();
        }

        private void Refresh()
        {
            foreach (var go in _spawned)
            {
                if (go != null) Destroy(go);
            }
            _spawned.Clear();

            if (_service == null || _gridRoot == null) return;

            int count = 0;
            foreach (var entry in _service.GetByCategory(_currentCategory))
            {
                SpawnEntry(entry);
                count++;
            }

            if (_emptyHint != null)
            {
                _emptyHint.gameObject.SetActive(count == 0);
                _emptyHint.text = count == 0 ? "（此页签暂无收藏）" : string.Empty;
            }
        }

        private void SpawnEntry(CollectionEntry entry)
        {
            if (_gridRoot == null) return;

            var go = new GameObject($"Entry_{entry.Id}");
            go.transform.SetParent(_gridRoot, false);
            int layer = _gridRoot.gameObject.layer;
            go.layer = layer;
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(240, 96);
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.08f);

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(go.transform, false);
            titleGo.layer = layer;
            var trt = titleGo.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 1);
            trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 1);
            trt.anchoredPosition = new Vector2(0, -8);
            trt.sizeDelta = new Vector2(-16, 28);
            var tmp = titleGo.AddComponent<TextMeshProUGUI>();
            tmp.text = entry.Title;
            tmp.fontSize = 18;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Left;
            titleGo.AddComponent<GeminiLab.Modules.UI.Catalogs.TMPFontBinder>();

            var descGo = new GameObject("Description");
            descGo.transform.SetParent(go.transform, false);
            descGo.layer = layer;
            var drt = descGo.AddComponent<RectTransform>();
            drt.anchorMin = Vector2.zero;
            drt.anchorMax = new Vector2(1, 1);
            drt.offsetMin = new Vector2(8, 8);
            drt.offsetMax = new Vector2(-8, -40);
            var dtmp = descGo.AddComponent<TextMeshProUGUI>();
            dtmp.text = entry.Description;
            dtmp.fontSize = 14;
            dtmp.color = new Color(0.85f, 0.9f, 1f, 0.85f);
            dtmp.alignment = TextAlignmentOptions.TopLeft;
            dtmp.enableWordWrapping = true;
            descGo.AddComponent<GeminiLab.Modules.UI.Catalogs.TMPFontBinder>();

            _spawned.Add(go);
        }
    }
}
