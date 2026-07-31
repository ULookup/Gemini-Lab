#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.UI;
using GeminiLab.Modules.EmotionGarden;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 每周培育面板：周一~周日 7 格展示指定周的情绪花，支持前后翻周。
    /// </summary>
    public sealed class WeeklyGardenPanelStub : StubPanelBase
    {
        public override PanelId Id => PanelId.WeeklyGardenView;

        private static readonly string[] DayLabels = { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };
        private static readonly Vector2[] DefaultCellPositions =
        {
            new(-510f, 0f),
            new(-340f, 0f),
            new(-170f, 0f),
            new(0f, 0f),
            new(170f, 0f),
            new(340f, 0f),
            new(510f, 0f),
        };
        private const string CellTemplateName = "CellTemplate";

        [Header("格子模板")]
        [SerializeField] private GameObject? _cellPrefab;

        [Header("网格容器")]
        [SerializeField] private Transform? _gridRoot;

        [Header("标题")]
        [SerializeField] private TMP_Text? _weekTitleText;

        [Header("翻周按钮")]
        [SerializeField] private Button? _nextWeekButton;

        [Header("精灵资源")]
        [SerializeField] private Sprite[]? _dayLabelSprites;

        [Header("瓶子精灵变体")]
        [Tooltip("顺序: angel_growing, angel_bloomed, demon_growing, demon_bloomed")]
        [SerializeField] private Sprite[]? _bottleSprites;
        [SerializeField] private Sprite[]? _growthSprites;

        private IEmotionGardenService? _service;
        private EventBus? _eventBus;
        private IDisposable? _submittedSub;
        private IDisposable? _bloomedSub;
        private IDisposable? _clearedSub;
        private readonly GameObject[] _cells = new GameObject[7];
        private readonly Sprite?[] _defaultBottleSprites = new Sprite?[7];
        private int _viewedWeekId;

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);
            _service ??= ServiceLocator.TryResolve(out IEmotionGardenService? service) ? service : null;
            _eventBus ??= ServiceLocator.TryResolve(out EventBus? eventBus) ? eventBus : null;
            if (_service == null) return;

            EnsureSubscriptions();
            HideCellTemplate();
            _viewedWeekId = _service.GetCurrentWeekId();
            Refresh();
        }

        public override void OnClose()
        {
            _submittedSub?.Dispose();
            _bloomedSub?.Dispose();
            _clearedSub?.Dispose();
            _submittedSub = null;
            _bloomedSub = null;
            _clearedSub = null;
            base.OnClose();
        }

        protected override void OnDestroy()
        {
            _submittedSub?.Dispose();
            _bloomedSub?.Dispose();
            _clearedSub?.Dispose();
            base.OnDestroy();
        }

        private void EnsureSubscriptions()
        {
            if (_eventBus == null) return;

            _submittedSub ??= _eventBus.Subscribe<EmotionFlowerSubmittedEvent>(_ => Refresh());
            _bloomedSub ??= _eventBus.Subscribe<EmotionFlowerBloomedEvent>(_ => Refresh());
            _clearedSub ??= _eventBus.Subscribe<EmotionGardenClearedEvent>(_ => Refresh());
        }

        public void ShowPrevWeek()
        {
            if (_service == null) return;
            _viewedWeekId = _service.OffsetWeekId(_viewedWeekId, -1);
            Refresh();
        }

        public void ShowNextWeek()
        {
            if (_service == null) return;
            if (_viewedWeekId >= _service.GetCurrentWeekId()) return;
            _viewedWeekId = _service.OffsetWeekId(_viewedWeekId, +1);
            Refresh();
        }

        private void Refresh()
        {
            if (_service == null) return;

            HideCellTemplate();
            var currentWeekId = _service.GetCurrentWeekId();
            if (_nextWeekButton != null) _nextWeekButton.interactable = _viewedWeekId < currentWeekId;

            if (_weekTitleText != null)
            {
                int year = _viewedWeekId / 100;
                int week = _viewedWeekId % 100;
                var monday = _service.GetWeekStartDate(_viewedWeekId);
                var sunday = monday.AddDays(6);
                var suffix = _viewedWeekId == currentWeekId ? "（本周）" : string.Empty;
                _weekTitleText.text = $"{year} 第 {week} 周 ({monday:MM-dd} ~ {sunday:MM-dd}){suffix}";
            }

            var flowers = _service.GetWeekFlowers(_viewedWeekId);
            EnsureCells();

            for (int i = 0; i < 7; i++)
            {
                var flower = flowers[i];
                var cell = _cells[i];
                if (cell == null) continue;

                var dayImg = cell.transform.Find("DayLabel/DaySprite")?.GetComponent<Image>();
                var dayTmp = cell.transform.Find("DayLabel/DayText")?.GetComponent<TextMeshProUGUI>();
                if (dayImg != null && _dayLabelSprites != null && i < _dayLabelSprites.Length && _dayLabelSprites[i] != null)
                {
                    dayImg.sprite = _dayLabelSprites[i];
                    dayImg.enabled = true;
                    if (dayTmp != null) dayTmp.enabled = false;
                }
                else if (dayTmp != null)
                {
                    dayTmp.text = DayLabels[i];
                    dayTmp.enabled = true;
                    if (dayImg != null) dayImg.enabled = false;
                }

                var bottle = cell.transform.Find("Bottle")?.GetComponent<Image>();
                if (bottle != null && _bottleSprites != null && _bottleSprites.Length == 4)
                {
                    if (flower.HasValue)
                    {
                        var f = flower.Value;
                        int spriteIndex = GetBottleSpriteIndex(f.Owner, f.State);
                        if (spriteIndex >= 0 && spriteIndex < _bottleSprites.Length && _bottleSprites[spriteIndex] != null)
                        {
                            bottle.sprite = _bottleSprites[spriteIndex];
                        }
                    }
                    else if (_defaultBottleSprites[i] != null)
                    {
                        bottle.sprite = _defaultBottleSprites[i];
                    }
                }

                var growth = cell.transform.Find("Growth")?.GetComponent<Image>();
                if (growth != null)
                {
                    if (flower.HasValue && _growthSprites != null && _growthSprites.Length >= 2)
                    {
                        growth.gameObject.SetActive(true);
                        growth.sprite = GetGrowthSprite(flower.Value.State);
                    }
                    else
                    {
                        growth.gameObject.SetActive(false);
                    }
                }

                var label = cell.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
                if (label == null) continue;

                if (flower.HasValue)
                {
                    var f = flower.Value;
                    var stateStr = f.State == GrowthState.Bloomed ? "已开花" : "培育中";
                    var flowerName = string.IsNullOrWhiteSpace(f.FlowerName)
                        ? EmotionFlowerCatalog.ResolveFlowerName(f.EmotionType, f.Owner)
                        : f.FlowerName;
                    var emotionName = EmotionFlowerCatalog.ResolveEmotionDisplayName(f.EmotionType);
                    label.text = $"{flowerName}\n{emotionName}\n{stateStr}";
                }
                else
                {
                    label.text = "—";
                }
            }
        }

        private void EnsureCells()
        {
            if (_gridRoot == null) return;

            HideCellTemplate();
            for (int i = 0; i < 7; i++)
            {
                if (_cells[i] != null) continue;

                var existing = _gridRoot.Find($"Day{i}");
                if (existing != null)
                {
                    existing.gameObject.SetActive(true);
                    EnsureCellStructure(existing, i);
                    _cells[i] = existing.gameObject;
                    CacheDefaultBottleSprite(i, existing.gameObject);
                    continue;
                }

                if (_cellPrefab != null)
                {
                    var cell = Instantiate(_cellPrefab, _gridRoot);
                    cell.name = $"Day{i}";
                    cell.SetActive(true);
                    var rt = cell.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchorMin = new Vector2(0f, 0f);
                        rt.anchorMax = new Vector2(0f, 0f);
                        rt.pivot = new Vector2(0.5f, 0.5f);
                        rt.anchoredPosition = GetDefaultCellPosition(i);
                    }

                    _cells[i] = cell;
                    CacheDefaultBottleSprite(i, cell);
                }
                else
                {
                    _cells[i] = CreateFallbackCell(i);
                }
            }
        }

        private void HideCellTemplate()
        {
            if (_gridRoot == null) return;

            var template = _gridRoot.Find(CellTemplateName);
            if (template != null && template.gameObject.activeSelf)
            {
                template.gameObject.SetActive(false);
            }
        }

        private static void EnsureCellStructure(Transform cell, int index)
        {
            var cellRt = cell.GetComponent<RectTransform>();
            if (cellRt != null)
            {
                cellRt.anchorMin = new Vector2(0f, 0f);
                cellRt.anchorMax = new Vector2(0f, 0f);
                cellRt.pivot = new Vector2(0.5f, 0.5f);
                cellRt.sizeDelta = new Vector2(160, 320);
            }

            var bottle = cell.Find("Bottle");
            if (bottle == null)
            {
                var go = new GameObject("Bottle");
                go.transform.SetParent(cell, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                var img = go.AddComponent<Image>();
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else if (bottle.GetComponent<Image>() == null)
            {
                var img = bottle.gameObject.AddComponent<Image>();
                img.preserveAspect = true;
                img.color = Color.white;
            }

            var growth = cell.Find("Growth");
            if (growth == null)
            {
                var go = new GameObject("Growth");
                go.transform.SetParent(cell, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(96, 96);
                rt.anchoredPosition = new Vector2(0f, -8f);
                var img = go.AddComponent<Image>();
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else if (growth.GetComponent<Image>() == null)
            {
                var img = growth.gameObject.AddComponent<Image>();
                img.preserveAspect = true;
                img.color = Color.white;
            }

            var dayLabel = cell.Find("DayLabel");
            if (dayLabel == null)
            {
                var go = new GameObject("DayLabel");
                go.transform.SetParent(cell, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0, -10);
                rt.sizeDelta = new Vector2(80, 32);
                dayLabel = go.transform;
            }

            var daySprite = dayLabel.Find("DaySprite");
            if (daySprite == null)
            {
                var go = new GameObject("DaySprite");
                go.transform.SetParent(dayLabel, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                var img = go.AddComponent<Image>();
                img.preserveAspect = true;
                img.color = Color.white;
            }

            var dayText = dayLabel.Find("DayText");
            if (dayText == null)
            {
                var go = new GameObject("DayText");
                go.transform.SetParent(dayLabel, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = 16;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.raycastTarget = false;
                tmp.text = DayLabels[index];
            }

            var labelT = cell.Find("Label");
            if (labelT == null)
            {
                var go = new GameObject("Label");
                go.transform.SetParent(cell, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(-12, 80);
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = 14;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(0.9f, 0.9f, 0.9f, 1f);
                tmp.raycastTarget = false;
                tmp.enableWordWrapping = true;
            }
        }

        private static int GetBottleSpriteIndex(string owner, GrowthState state)
        {
            int baseIndex = EmotionFlowerCatalog.NormalizeOwner(owner) == EmotionFlowerCatalog.OwnerAngel ? 0 : 2;
            return state == GrowthState.Bloomed ? baseIndex + 1 : baseIndex;
        }

        private void CacheDefaultBottleSprite(int index, GameObject cell)
        {
            if (index < 0 || index >= _defaultBottleSprites.Length)
            {
                return;
            }

            var bottle = cell.transform.Find("Bottle")?.GetComponent<Image>();
            if (bottle != null && _defaultBottleSprites[index] == null)
            {
                _defaultBottleSprites[index] = bottle.sprite;
            }
        }

        private GameObject CreateFallbackCell(int index)
        {
            var cell = new GameObject($"Day{index}");
            cell.transform.SetParent(_gridRoot, false);
            var rt = cell.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(160, 320);
            rt.anchoredPosition = GetDefaultCellPosition(index);

            var img = cell.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.35f, 1f);

            var growthGo = new GameObject("Growth");
            growthGo.transform.SetParent(cell.transform, false);
            var grt = growthGo.AddComponent<RectTransform>();
            grt.anchorMin = new Vector2(0.5f, 0.5f);
            grt.anchorMax = new Vector2(0.5f, 0.5f);
            grt.pivot = new Vector2(0.5f, 0.5f);
            grt.sizeDelta = new Vector2(96, 96);
            grt.anchoredPosition = new Vector2(0f, -8f);
            var gimg = growthGo.AddComponent<Image>();
            gimg.preserveAspect = true;
            gimg.color = Color.white;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(cell.transform, false);
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(6, 6);
            lrt.offsetMax = new Vector2(-6, -6);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return cell;
        }

        private Sprite? GetGrowthSprite(GrowthState state)
        {
            if (_growthSprites == null || _growthSprites.Length < 2)
            {
                return null;
            }

            return state == GrowthState.Bloomed ? _growthSprites[1] : _growthSprites[0];
        }

        private static Vector2 GetDefaultCellPosition(int index)
        {
            if (index >= 0 && index < DefaultCellPositions.Length)
            {
                return DefaultCellPositions[index];
            }

            return new Vector2(index * 170f, 0f);
        }
    }
}
