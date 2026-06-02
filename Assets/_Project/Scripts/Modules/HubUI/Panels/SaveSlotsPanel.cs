#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core;
using GeminiLab.Core.SceneFlow;
using GeminiLab.Core.UI;
using GeminiLab.Modules.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 主菜单"存档"按钮打开的面板。
    /// Phase E 起走真实 <see cref="ISaveCoordinator"/>：List / Save / Load / Delete。
    /// 读档成功时跳转到 Apartment（通过 ISceneFlowService），同时 SaveCoordinator
    /// 会把 Settings / Inventory / Collection / Tarot 的状态恢复回当前 Service 实例。
    /// </summary>
    public sealed class SaveSlotsPanel : StubPanelBase
    {
        public override PanelId Id => PanelId.SaveSlots;

        [Header("槽位按钮（容器）")]
        [SerializeField] private Transform? _slotContainer;

        [Header("操作")]
        [SerializeField] private TMP_Text? _statusText;

        private readonly List<SlotRow> _rows = new();
        private ISaveCoordinator? _coordinator;

        private sealed class SlotRow
        {
            public string SlotId = string.Empty;
            public GameObject Root = null!;
            public TMP_Text? Summary;
            public Button? LoadButton;
            public Button? NewOrSaveButton;
            public Button? DeleteButton;
        }

        protected override void Awake()
        {
            base.Awake();
        }

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);
            EnsureCoordinator();
            BuildRowsIfNeeded();
            _ = RefreshAllAsync();
            SetStatus("");
        }

        private void EnsureCoordinator()
        {
            if (_coordinator == null)
            {
                ServiceLocator.TryResolve(out _coordinator);
            }
        }

        private void CloseSelf()
        {
            if (ServiceLocator.TryResolve(out IUIRouter? router) && router is not null)
            {
                router.Close(Id);
            }
        }

        private void BuildRowsIfNeeded()
        {
            if (_slotContainer == null) return;
            if (_coordinator == null) return;

            var defaultSlots = _coordinator.DefaultSlotIds;
            if (_rows.Count == defaultSlots.Count) return;

            for (int i = _slotContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_slotContainer.GetChild(i).gameObject);
            }
            _rows.Clear();

            foreach (var slotId in defaultSlots)
            {
                _rows.Add(BuildRow(slotId));
            }
        }

        private SlotRow BuildRow(string slotId)
        {
            int layer = _slotContainer != null ? _slotContainer.gameObject.layer : 5;
            var rowGo = new GameObject($"Slot_{slotId}");
            rowGo.transform.SetParent(_slotContainer, false);
            rowGo.layer = layer;
            var rt = rowGo.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 110);
            var bg = rowGo.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.06f);

            var summaryGo = new GameObject("Summary");
            summaryGo.transform.SetParent(rowGo.transform, false);
            summaryGo.layer = layer;
            var srt = summaryGo.AddComponent<RectTransform>();
            srt.anchorMin = Vector2.zero; srt.anchorMax = new Vector2(0.55f, 1);
            srt.offsetMin = new Vector2(16, 8); srt.offsetMax = new Vector2(-8, -8);
            var summary = summaryGo.AddComponent<TextMeshProUGUI>();
            summary.fontSize = 18;
            summary.color = Color.white;
            summary.alignment = TextAlignmentOptions.MidlineLeft;
            summaryGo.AddComponent<GeminiLab.Modules.UI.Catalogs.TMPFontBinder>();

            var loadBtn = BuildActionButton(rowGo, layer, "读取", new Vector2(0.55f, 0), new Vector2(0.70f, 1));
            var saveBtn = BuildActionButton(rowGo, layer, "新建 / 覆盖", new Vector2(0.70f, 0), new Vector2(0.88f, 1));
            var delBtn = BuildActionButton(rowGo, layer, "删除", new Vector2(0.88f, 0), new Vector2(1.0f, 1));

            var row = new SlotRow
            {
                SlotId = slotId,
                Root = rowGo,
                Summary = summary,
                LoadButton = loadBtn,
                NewOrSaveButton = saveBtn,
                DeleteButton = delBtn
            };

            loadBtn.onClick.AddListener(() => _ = OnLoadAsync(row));
            saveBtn.onClick.AddListener(() => _ = OnSaveAsync(row));
            delBtn.onClick.AddListener(() => _ = OnDeleteAsync(row));

            return row;
        }

        private static Button BuildActionButton(GameObject parent, int layer, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent.transform, false);
            go.layer = layer;
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(6, 20);
            rt.offsetMax = new Vector2(-6, -20);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.3f, 0.45f, 1f);
            var btn = go.AddComponent<Button>();

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.layer = layer;
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            labelGo.AddComponent<GeminiLab.Modules.UI.Catalogs.TMPFontBinder>();

            return btn;
        }

        private async System.Threading.Tasks.Task RefreshAllAsync()
        {
            if (_coordinator == null) return;
            var summaries = await _coordinator.ListSlotsAsync().ConfigureAwait(true);
            foreach (var summary in summaries)
            {
                var row = _rows.Find(r => r.SlotId == summary.SlotId);
                if (row != null) ApplySummary(row, summary);
            }
        }

        private static void ApplySummary(SlotRow row, SlotSummary summary)
        {
            if (row.Summary == null) return;
            if (summary.Exists)
            {
                row.Summary.text = $"{summary.SlotId}：{summary.LastSavedAtIso}";
                if (row.LoadButton != null) row.LoadButton.interactable = true;
                if (row.DeleteButton != null) row.DeleteButton.interactable = true;
            }
            else
            {
                row.Summary.text = $"{summary.SlotId}：空槽位";
                if (row.LoadButton != null) row.LoadButton.interactable = false;
                if (row.DeleteButton != null) row.DeleteButton.interactable = false;
            }
        }

        private async System.Threading.Tasks.Task OnLoadAsync(SlotRow row)
        {
            if (_coordinator == null) return;
            SetStatus($"正在读取 {row.SlotId} …");
            bool ok = await _coordinator.LoadAsync(row.SlotId).ConfigureAwait(true);
            if (!ok)
            {
                SetStatus($"{row.SlotId} 读取失败或槽位为空");
                return;
            }

            SetStatus($"{row.SlotId} 读取成功");
            // 读档完成后切回公寓
            if (ServiceLocator.TryResolve(out ISceneFlowService? sceneFlow) && sceneFlow is not null)
            {
                sceneFlow.LoadAsync(SceneId.Apartment);
            }
        }

        private async System.Threading.Tasks.Task OnSaveAsync(SlotRow row)
        {
            if (_coordinator == null) return;
            SetStatus($"正在写入 {row.SlotId} …");
            try
            {
                await _coordinator.SaveAsync(row.SlotId).ConfigureAwait(true);
                SetStatus($"{row.SlotId} 已保存");
            }
            catch (Exception ex)
            {
                SetStatus($"{row.SlotId} 写入失败：{ex.Message}");
            }

            await RefreshAllAsync().ConfigureAwait(true);
        }

        private async System.Threading.Tasks.Task OnDeleteAsync(SlotRow row)
        {
            if (_coordinator == null) return;
            try
            {
                await _coordinator.DeleteAsync(row.SlotId).ConfigureAwait(true);
                SetStatus($"{row.SlotId} 已删除");
            }
            catch (Exception ex)
            {
                SetStatus($"{row.SlotId} 删除失败：{ex.Message}");
            }

            await RefreshAllAsync().ConfigureAwait(true);
        }

        private void SetStatus(string msg)
        {
            if (_statusText != null) _statusText.text = msg;
        }
    }
}
