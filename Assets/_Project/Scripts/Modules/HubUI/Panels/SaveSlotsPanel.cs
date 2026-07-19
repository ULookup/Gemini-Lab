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
    /// 主菜单"存档"按钮打开的面板。槽位行从场景中 SlotTemplate 模板克隆，
    /// 用户在模板上直接编辑美术资源，scene 视图与 play 视图完全一致。
    /// </summary>
    [ExecuteAlways]
    public sealed class SaveSlotsPanel : StubPanelBase
    {
        public override PanelId Id => PanelId.SaveSlots;

        [Header("槽位")]
        [SerializeField] private GameObject? _slotTemplate;
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
            if (Application.isPlaying)
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
                ServiceLocator.TryResolve(out _coordinator);
        }

        private void CloseSelf()
        {
            if (ServiceLocator.TryResolve(out IUIRouter? router) && router is not null)
                router.Close(Id);
        }

        private void BuildRowsIfNeeded()
        {
            if (_slotContainer == null || _slotTemplate == null) return;
            if (_coordinator == null) return;

            var defaultSlots = _coordinator.DefaultSlotIds;
            if (_rows.Count == defaultSlots.Count) return;

            for (int i = _slotContainer.childCount - 1; i >= 0; i--)
                Destroy(_slotContainer.GetChild(i).gameObject);
            _rows.Clear();

            foreach (var slotId in defaultSlots)
                _rows.Add(BuildRow(slotId));
        }

        private SlotRow BuildRow(string slotId)
        {
            var clone = Instantiate(_slotTemplate);
            clone.transform.SetParent(_slotContainer, false);
            clone.name = $"Slot_{slotId}";
            clone.SetActive(true);

            var summary = FindInChildren(clone.transform, "Summary")?.GetComponent<TMP_Text>();
            var loadBtn = FindInChildren(clone.transform, "Btn_读取")?.GetComponent<Button>();
            var saveBtn = FindInChildren(clone.transform, "Btn_保存")?.GetComponent<Button>();
            var delBtn = FindInChildren(clone.transform, "Btn_删除")?.GetComponent<Button>();

            var row = new SlotRow
            {
                SlotId = slotId,
                Root = clone,
                Summary = summary,
                LoadButton = loadBtn,
                NewOrSaveButton = saveBtn,
                DeleteButton = delBtn
            };

            if (loadBtn != null) loadBtn.onClick.AddListener(() => _ = OnLoadAsync(row));
            if (saveBtn != null) saveBtn.onClick.AddListener(() => _ = OnSaveAsync(row));
            if (delBtn != null) delBtn.onClick.AddListener(() => _ = OnDeleteAsync(row));

            return row;
        }

        private static Transform? FindInChildren(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var found = FindInChildren(child, name);
                if (found != null) return found;
            }
            return null;
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
            if (ServiceLocator.TryResolve(out ISceneFlowService? sceneFlow) && sceneFlow is not null)
                sceneFlow.LoadAsync(SceneId.Apartment);
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

#if UNITY_EDITOR
        private void OnEnable()
        {
            if (Application.isPlaying) return;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null || Application.isPlaying) return;
                BuildEditorPreview();
            };
        }

        private void BuildEditorPreview()
        {
            if (_slotContainer == null || _slotTemplate == null) return;

            var content = transform.Find("Content");
            if (content != null && !content.gameObject.activeSelf)
                content.gameObject.SetActive(true);

            // Only create if container is empty — never auto-destroy user's work
            if (_slotContainer.childCount > 0) return;

            string[] previewIds = { "slot_1", "slot_2", "slot_3" };
            foreach (var id in previewIds)
            {
                var row = BuildRow(id);
                if (row.Summary != null)
                    row.Summary.text = $"{id}：预览槽位（编辑器预览）";
            }
        }

        [UnityEditor.MenuItem("Tools/Gemini-Lab/Rebuild Save Slot Previews")]
        private static void RebuildPreviews()
        {
            var panel = FindFirstObjectByType<SaveSlotsPanel>();
            if (panel == null)
            {
                Debug.LogWarning("[SaveSlotsPanel] 当前场景未找到 Panel_SaveSlots。");
                return;
            }

            var container = panel._slotContainer;
            if (container == null) return;

            for (int i = container.childCount - 1; i >= 0; i--)
                DestroyImmediate(container.GetChild(i).gameObject);

            panel.BuildEditorPreview();
            Debug.Log("[SaveSlotsPanel] 预览已重建。");
        }
#endif
    }
}
