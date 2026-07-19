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
    /// 当前阶段：每格用方块占位，显示情绪标签+培育者+生长状态。
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
            if (_viewedWeekId >= _service.GetCurrentWeekId()) return; // 不看未来
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

                var label = cell.GetComponentInChildren<TMP_Text>();
                if (label == null) continue;

                if (flower.HasValue)
                {
                    var f = flower.Value;
                    var stateStr = f.State == GrowthState.Bloomed ? "已开花" : "培育中";
                    var ownerStr = f.Owner == "angel" ? "天使" : "恶魔";
                    label.text = $"{DayLabels[i]}\n{ownerStr}\n{f.EmotionType}\n{stateStr}";
                }
                else
                {
                    label.text = $"{DayLabels[i]}\n—";
                }
            }
        }

        private void EnsureCells()
        {
            if (_gridRoot == null) return;

            for (int i = 0; i < 7; i++)
            {
                if (_cells[i] != null) continue;

                GameObject cell;
                if (_cellPrefab != null)
                {
                    cell = Instantiate(_cellPrefab, _gridRoot);
                }
                else
                {
                    // 方块占位
                    cell = new GameObject($"Day{i}");
                    cell.transform.SetParent(_gridRoot, false);
                    var rt = cell.AddComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(200, 300);

                    var layout = cell.AddComponent<UnityEngine.UI.LayoutElement>();
                    layout.preferredWidth = 200;
                    layout.preferredHeight = 300;
                    layout.minWidth = 140;
                    layout.minHeight = 240;

                    var img = cell.AddComponent<UnityEngine.UI.Image>();
                    img.color = new Color(0.25f, 0.25f, 0.35f, 1f);

                    var labelGo = new GameObject("Label");
                    labelGo.transform.SetParent(cell.transform, false);
                    var lrt = labelGo.AddComponent<RectTransform>();
                    lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                    lrt.offsetMin = new Vector2(6, 6); lrt.offsetMax = new Vector2(-6, -6);
                    var tmp = labelGo.AddComponent<TextMeshProUGUI>();
                    tmp.fontSize = 24;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = Color.white;
                }

                _cells[i] = cell;
            }
        }
    }
}
