#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.UI;
using GeminiLab.Modules.EmotionGarden;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 每周培育面板：周一~周日 7 格展示指定周的情绪花，支持前后翻周。
    /// 单元格使用 PSD 瓶子精灵 + 天数标签 + 情绪文字叠加。
    /// </summary>
    public sealed class WeeklyGardenPanelStub : StubPanelBase
    {
        public override PanelId Id => PanelId.WeeklyGardenView;

        private static readonly string[] DayLabels = { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };

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

        private IEmotionGardenService? _service;
        private readonly GameObject[] _cells = new GameObject[7];
        private int _viewedWeekId;

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);
            _service ??= ServiceLocator.TryResolve(out IEmotionGardenService? s) ? s : null;
            if (_service == null) return;

            _viewedWeekId = _service.GetCurrentWeekId();
            Refresh();
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

            var currentWeekId = _service.GetCurrentWeekId();
            if (_nextWeekButton != null) _nextWeekButton.interactable = _viewedWeekId < currentWeekId;

            if (_weekTitleText != null)
            {
                int year = _viewedWeekId / 100;
                int week = _viewedWeekId % 100;
                var monday = _service.GetWeekStartDate(_viewedWeekId);
                var sunday = monday.AddDays(6);
                var suffix = _viewedWeekId == currentWeekId ? "（本周）" : "";
                _weekTitleText.text = $"{year} 第 {week} 周 ({monday:MM-dd} ~ {sunday:MM-dd}){suffix}";
            }

            var flowers = _service.GetWeekFlowers(_viewedWeekId);
            EnsureCells();

            for (int i = 0; i < 7; i++)
            {
                var flower = flowers[i];
                var cell = _cells[i];
                if (cell == null) continue;

                // 天数精灵
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

                // 瓶子精灵
                var bottle = cell.transform.Find("Bottle")?.GetComponent<Image>();
                if (bottle != null && _bottleSprites != null && _bottleSprites.Length == 4)
                {
                    if (flower.HasValue)
                    {
                        var f = flower.Value;
                        int spriteIndex = GetBottleSpriteIndex(f.Owner, f.State);
                        if (spriteIndex >= 0 && spriteIndex < _bottleSprites.Length && _bottleSprites[spriteIndex] != null)
                            bottle.sprite = _bottleSprites[spriteIndex];
                    }
                }

                // 情绪文字
                var label = cell.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
                if (label != null)
                {
                    if (flower.HasValue)
                    {
                        var f = flower.Value;
                        var stateStr = f.State == GrowthState.Bloomed ? "已开花" : "培育中";
                        var ownerStr = f.Owner == "angel" ? "天使" : "恶魔";
                        label.text = $"{ownerStr}\n{f.EmotionType}\n{stateStr}";
                    }
                    else
                    {
                        label.text = "—";
                    }
                }
            }
        }

        private void EnsureCells()
        {
            if (_gridRoot == null) return;

            for (int i = 0; i < 7; i++)
            {
                if (_cells[i] != null) continue;

                // 优先使用场景中预置的格子（方便在 Scene 视图中编辑）
                var existing = _gridRoot.Find($"Day{i}");
                if (existing != null)
                {
                    existing.gameObject.SetActive(true);
                    EnsureCellStructure(existing, i);
                    _cells[i] = existing.gameObject;
                    continue;
                }

                // 回退：从模板 Instantiate
                if (_cellPrefab != null)
                {
                    var cell = Instantiate(_cellPrefab, _gridRoot);
                    cell.name = $"Day{i}";
                    cell.SetActive(true);
                    _cells[i] = cell;
                }
                else
                {
                    _cells[i] = CreateFallbackCell(i);
                }
            }
        }

        /// <summary>确保场景预置格子有完整的子对象结构（Bottle + DayLabel/DaySprite/DayText + Label）。</summary>
        private void EnsureCellStructure(Transform cell, int index)
        {
            // Bottle
            var bottle = cell.Find("Bottle");
            if (bottle == null)
            {
                var go = new GameObject("Bottle");
                go.transform.SetParent(cell, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
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

            // DayLabel
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

            // DayLabel/DaySprite
            var daySprite = dayLabel.Find("DaySprite");
            if (daySprite == null)
            {
                var go = new GameObject("DaySprite");
                go.transform.SetParent(dayLabel, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                var img = go.AddComponent<Image>();
                img.preserveAspect = true;
                img.color = Color.white;
            }

            // DayLabel/DayText
            var dayText = dayLabel.Find("DayText");
            if (dayText == null)
            {
                var go = new GameObject("DayText");
                go.transform.SetParent(dayLabel, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = 16;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.raycastTarget = false;
                tmp.text = DayLabels[index];
            }

            // Label
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

        /// <summary>根据花的所有者和生长状态返回瓶子精灵索引。</summary>
        private static int GetBottleSpriteIndex(string owner, GrowthState state)
        {
            int baseIndex = owner == "angel" ? 0 : 2;
            return state == GrowthState.Bloomed ? baseIndex + 1 : baseIndex;
        }

        private GameObject CreateFallbackCell(int index)
        {
            var cell = new GameObject($"Day{index}");
            cell.transform.SetParent(_gridRoot, false);
            var rt = cell.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 320);

            var layout = cell.AddComponent<LayoutElement>();
            layout.preferredWidth = 160;
            layout.preferredHeight = 320;

            var img = cell.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.35f, 1f);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(cell.transform, false);
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(6, 6); lrt.offsetMax = new Vector2(-6, -6);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return cell;
        }
    }
}
