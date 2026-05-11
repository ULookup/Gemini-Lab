#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using GeminiLab.Core;
using GeminiLab.Core.Time;
using GeminiLab.Core.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 主菜单"存档"按钮打开的面板。
    /// Phase D 骨架版：读写 Application.persistentDataPath/saves/slot_N.json。
    /// Phase E 存档整合后会改走 IPersistentService 扫描；当前只保留"槽位元数据"骨架。
    /// </summary>
    public sealed class SaveSlotsPanel : StubPanelBase
    {
        public override PanelId Id => PanelId.SaveSlots;

        private const int SlotCount = 3;

        [Header("槽位按钮（容器）")]
        [SerializeField] private Transform? _slotContainer;

        [Header("操作")]
        [SerializeField] private Button? _closeButton;
        [SerializeField] private TMP_Text? _statusText;

        private readonly List<SlotRow> _rows = new();

        [Serializable]
        private struct SlotMeta
        {
            public int SlotId;
            public string CreatedAtIso;
            public string LastPlayedIso;
            public float PlayTimeSeconds;
        }

        private sealed class SlotRow
        {
            public int SlotId;
            public GameObject Root = null!;
            public TMP_Text? Summary;
            public Button? LoadButton;
            public Button? NewOrSaveButton;
            public Button? DeleteButton;
        }

        protected override void Awake()
        {
            base.Awake();
            if (_closeButton != null) _closeButton.onClick.AddListener(CloseSelf);
        }

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);
            EnsureSlotsFolder();
            BuildRowsIfNeeded();
            RefreshAll();
            SetStatus("");
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
            if (_rows.Count == SlotCount) return;

            for (int i = _slotContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_slotContainer.GetChild(i).gameObject);
            }
            _rows.Clear();

            for (int i = 1; i <= SlotCount; i++)
            {
                _rows.Add(BuildRow(i));
            }
        }

        private SlotRow BuildRow(int slotId)
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

            loadBtn.onClick.AddListener(() => OnLoad(row));
            saveBtn.onClick.AddListener(() => OnNewOrOverwrite(row));
            delBtn.onClick.AddListener(() => OnDelete(row));

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

        private void RefreshAll()
        {
            foreach (var row in _rows)
            {
                RefreshRow(row);
            }
        }

        private void RefreshRow(SlotRow row)
        {
            if (row.Summary == null) return;

            string path = SlotPath(row.SlotId);
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var meta = JsonUtility.FromJson<SlotMeta>(json);
                    row.Summary.text = $"存档 {row.SlotId}：{meta.LastPlayedIso}";
                    if (row.LoadButton != null) row.LoadButton.interactable = true;
                    if (row.DeleteButton != null) row.DeleteButton.interactable = true;
                }
                catch
                {
                    row.Summary.text = $"存档 {row.SlotId}：损坏（建议删除）";
                    if (row.LoadButton != null) row.LoadButton.interactable = false;
                    if (row.DeleteButton != null) row.DeleteButton.interactable = true;
                }
            }
            else
            {
                row.Summary.text = $"存档 {row.SlotId}：空槽位";
                if (row.LoadButton != null) row.LoadButton.interactable = false;
                if (row.DeleteButton != null) row.DeleteButton.interactable = false;
            }
        }

        private void OnLoad(SlotRow row)
        {
            SetStatus($"（骨架版）读取槽位 {row.SlotId}：Phase E 接入 SaveSystem 后生效。");
        }

        private void OnNewOrOverwrite(SlotRow row)
        {
            EnsureSlotsFolder();

            string now;
            if (ServiceLocator.TryResolve(out IGameClock? clock) && clock is not null)
            {
                now = clock.Now.ToString("yyyy-MM-dd HH:mm");
            }
            else
            {
                now = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            }

            var meta = new SlotMeta
            {
                SlotId = row.SlotId,
                CreatedAtIso = now,
                LastPlayedIso = now,
                PlayTimeSeconds = 0f
            };

            try
            {
                File.WriteAllText(SlotPath(row.SlotId), JsonUtility.ToJson(meta, prettyPrint: true));
                SetStatus($"槽位 {row.SlotId} 已写入（mock）。");
            }
            catch (Exception ex)
            {
                SetStatus($"槽位 {row.SlotId} 写入失败：{ex.Message}");
            }

            RefreshRow(row);
        }

        private void OnDelete(SlotRow row)
        {
            string path = SlotPath(row.SlotId);
            try
            {
                if (File.Exists(path)) File.Delete(path);
                SetStatus($"槽位 {row.SlotId} 已删除。");
            }
            catch (Exception ex)
            {
                SetStatus($"槽位 {row.SlotId} 删除失败：{ex.Message}");
            }
            RefreshRow(row);
        }

        private static void EnsureSlotsFolder()
        {
            var dir = SlotsFolder;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        private static string SlotsFolder => Path.Combine(Application.persistentDataPath, "saves");
        private static string SlotPath(int slotId) => Path.Combine(SlotsFolder, $"slot_{slotId}.json");

        private void SetStatus(string msg)
        {
            if (_statusText != null) _statusText.text = msg;
        }
    }
}
