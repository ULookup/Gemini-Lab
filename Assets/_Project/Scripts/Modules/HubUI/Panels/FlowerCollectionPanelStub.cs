#nullable enable
using System.Collections.Generic;
using GeminiLab.Core;
using GeminiLab.Core.UI;
using GeminiLab.Modules.EmotionGarden;
using TMPro;
using UnityEngine;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 情绪花图鉴面板。按"情绪+培育者"展示累计数量和解锁进度。
    /// 当前阶段：方块 UI 占位，列表式展示。
    /// </summary>
    public sealed class FlowerCollectionPanelStub : StubPanelBase
    {
        public override PanelId Id => PanelId.EmotionCollection;

        [Header("列表容器")]
        [SerializeField] private Transform? _listRoot;

        [Header("条目模板")]
        [SerializeField] private GameObject? _entryPrefab;

        private IEmotionGardenService? _service;
        private readonly List<GameObject> _entries = new();

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);
            _service ??= ServiceLocator.TryResolve(out IEmotionGardenService? s) ? s : null;
            if (_service == null) return;

            Refresh();
        }

        private void Refresh()
        {
            if (_service == null || _listRoot == null) return;

            ClearEntries();

            var clusters = _service.GetAllClusters();
            foreach (var c in clusters)
            {
                CreateEntry(c);
            }

            if (clusters.Count == 0)
            {
                var emptyGo = new GameObject("Empty");
                emptyGo.transform.SetParent(_listRoot, false);
                var tmp = emptyGo.AddComponent<TextMeshProUGUI>();
                tmp.text = "还没有收集到情绪花";
                tmp.fontSize = 20;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                _entries.Add(emptyGo);
            }
        }

        private void CreateEntry(ClusterProgress cluster)
        {
            GameObject entry;
            if (_entryPrefab != null)
            {
                entry = Instantiate(_entryPrefab, _listRoot);
            }
            else
            {
                entry = new GameObject($"Entry_{cluster.EmotionType}_{cluster.Owner}");
                entry.transform.SetParent(_listRoot, false);
                var rt = entry.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(400, 60);

                var img = entry.AddComponent<UnityEngine.UI.Image>();
                img.color = new Color(0.2f, 0.25f, 0.35f, 1f);

                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(entry.transform, false);
                var lrt = labelGo.AddComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(8, 4); lrt.offsetMax = new Vector2(-8, -4);
                var tmp = labelGo.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = 16;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.color = Color.white;

                var ownerStr = cluster.Owner == "angel" ? "天使" : "恶魔";
                var stageStr = cluster.UnlockedStage switch
                {
                    3 => "已解锁小花丛",
                    1 => "已解锁单株花",
                    _ => $"还差 {1 - cluster.TotalCount} 朵解锁单株花"
                };
                if (cluster.UnlockedStage == 1 && cluster.TotalCount < 3)
                {
                    stageStr += $"，还差 {3 - cluster.TotalCount} 朵解锁小花丛";
                }

                tmp.text = $"{ownerStr}培育 · {cluster.EmotionType}花 × {cluster.TotalCount}\n{stageStr}";
            }

            _entries.Add(entry);
        }

        private void ClearEntries()
        {
            foreach (var e in _entries)
            {
                if (e != null) Destroy(e);
            }
            _entries.Clear();
        }
    }
}
