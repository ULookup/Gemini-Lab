#nullable enable
using System;
using System.Collections.Generic;

namespace GeminiLab.Modules.Persistence
{
    /// <summary>
    /// 一个存档槽位的顶层容器。
    /// slot 文件落地为 `<slotId>.sav`（内容为 SaveEnvelope&lt;SaveBundle&gt; JSON）。
    ///
    /// Services 字段结构：key = IPersistentService.Key；value = 该服务自己的 JSON（已 capture）。
    /// 读档时按 key 路由回各服务的 Restore。
    /// </summary>
    [Serializable]
    public sealed class SaveBundle
    {
        /// <summary>槽位 id，例：slot_1 / slot_2 / slot_3。</summary>
        public string SlotId = string.Empty;

        /// <summary>首次创建时间。</summary>
        public string CreatedAtIso = string.Empty;

        /// <summary>最近一次保存时间。</summary>
        public string LastSavedAtIso = string.Empty;

        /// <summary>累计游玩秒数（Phase E 骨架还没接入实际 tick）。</summary>
        public float PlayTimeSeconds;

        /// <summary>
        /// 各服务自己生成的 JSON。
        /// Unity JsonUtility 不直接支持 Dictionary，这里用两个并行数组存。
        /// </summary>
        public List<string> ServiceKeys = new();
        public List<string> ServiceJsons = new();

        public void SetService(string key, string json)
        {
            if (string.IsNullOrEmpty(key)) return;
            int idx = ServiceKeys.IndexOf(key);
            if (idx >= 0)
            {
                ServiceJsons[idx] = json ?? string.Empty;
            }
            else
            {
                ServiceKeys.Add(key);
                ServiceJsons.Add(json ?? string.Empty);
            }
        }

        public string? GetService(string key)
        {
            int idx = ServiceKeys.IndexOf(key);
            return idx >= 0 ? ServiceJsons[idx] : null;
        }
    }

    /// <summary>列表 UI 用的槽位摘要。</summary>
    public readonly struct SlotSummary
    {
        public SlotSummary(string slotId, bool exists, string createdAtIso, string lastSavedAtIso, float playTimeSeconds)
        {
            SlotId = slotId;
            Exists = exists;
            CreatedAtIso = createdAtIso;
            LastSavedAtIso = lastSavedAtIso;
            PlayTimeSeconds = playTimeSeconds;
        }

        public string SlotId { get; }
        public bool Exists { get; }
        public string CreatedAtIso { get; }
        public string LastSavedAtIso { get; }
        public float PlayTimeSeconds { get; }
    }
}
