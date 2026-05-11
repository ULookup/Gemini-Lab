#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using GeminiLab.Core.Time;
using UnityEngine;

namespace GeminiLab.Modules.Persistence
{
    /// <summary>事件：某槽位已写入完成。</summary>
    public readonly struct SaveSlotCommittedEvent
    {
        public SaveSlotCommittedEvent(string slotId) { SlotId = slotId; }
        public string SlotId { get; }
    }

    /// <summary>事件：某槽位已读取完成（所有 IPersistentService Restore 已跑完）。</summary>
    public readonly struct SaveSlotLoadedEvent
    {
        public SaveSlotLoadedEvent(string slotId) { SlotId = slotId; }
        public string SlotId { get; }
    }

    /// <summary>事件：某槽位已删除。</summary>
    public readonly struct SaveSlotDeletedEvent
    {
        public SaveSlotDeletedEvent(string slotId) { SlotId = slotId; }
        public string SlotId { get; }
    }

    /// <summary>
    /// <see cref="ISaveCoordinator"/> 默认实现。
    /// Save 流程：
    ///   遍历 Registry → Capture → 塞进 SaveBundle → SaveSystem.SaveAsync
    /// Load 流程：
    ///   SaveSystem.LoadAsync → 遍历 bundle.ServiceKeys → Registry.TryGet → Restore
    /// </summary>
    public sealed class SaveCoordinator : ISaveCoordinator
    {
        private static readonly string[] _defaultSlots = { "slot_1", "slot_2", "slot_3" };

        private readonly ISaveSystem _saveSystem;
        private readonly IPersistentServiceRegistry _registry;
        private readonly IGameClock _clock;
        private readonly EventBus? _eventBus;

        public SaveCoordinator(
            ISaveSystem saveSystem,
            IPersistentServiceRegistry registry,
            IGameClock clock,
            EventBus? eventBus)
        {
            _saveSystem = saveSystem ?? throw new ArgumentNullException(nameof(saveSystem));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _eventBus = eventBus;
        }

        public IReadOnlyList<string> DefaultSlotIds => _defaultSlots;

        public async Task<IReadOnlyList<SlotSummary>> ListSlotsAsync(CancellationToken cancellationToken = default)
        {
            var result = new List<SlotSummary>(_defaultSlots.Length);
            foreach (var slot in _defaultSlots)
            {
                var bundle = await _saveSystem.LoadAsync<SaveBundle>(slot, cancellationToken).ConfigureAwait(true);
                if (bundle is null)
                {
                    result.Add(new SlotSummary(slot, exists: false, "", "", 0f));
                }
                else
                {
                    result.Add(new SlotSummary(slot, exists: true, bundle.CreatedAtIso, bundle.LastSavedAtIso, bundle.PlayTimeSeconds));
                }
            }
            return result;
        }

        public async Task SaveAsync(string slotId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(slotId))
            {
                throw new ArgumentException("slotId cannot be empty", nameof(slotId));
            }

            // 先读老 bundle 以保留 CreatedAtIso；没有则新建
            var existing = await _saveSystem.LoadAsync<SaveBundle>(slotId, cancellationToken).ConfigureAwait(true);
            string nowIso = _clock.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var bundle = existing ?? new SaveBundle
            {
                SlotId = slotId,
                CreatedAtIso = nowIso
            };
            bundle.SlotId = slotId;
            bundle.LastSavedAtIso = nowIso;
            // PlayTimeSeconds 这一阶段先保持不变；待接真实 session 计时再累加

            // 重置并按当前 Registry 重新写入
            bundle.ServiceKeys.Clear();
            bundle.ServiceJsons.Clear();
            foreach (var svc in _registry.All)
            {
                try
                {
                    bundle.SetService(svc.Key, svc.CaptureJson() ?? string.Empty);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveCoordinator] Capture failed for '{svc.Key}': {ex.Message}");
                }
            }

            await _saveSystem.SaveAsync(slotId, bundle, cancellationToken).ConfigureAwait(true);
            _eventBus?.Publish(new SaveSlotCommittedEvent(slotId));
        }

        public async Task<bool> LoadAsync(string slotId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(slotId)) return false;

            var bundle = await _saveSystem.LoadAsync<SaveBundle>(slotId, cancellationToken).ConfigureAwait(true);
            if (bundle is null) return false;

            for (int i = 0; i < bundle.ServiceKeys.Count; i++)
            {
                string key = bundle.ServiceKeys[i];
                string json = i < bundle.ServiceJsons.Count ? bundle.ServiceJsons[i] : string.Empty;
                var svc = _registry.TryGet(key);
                if (svc is null)
                {
                    Debug.LogWarning($"[SaveCoordinator] 未找到 key='{key}' 对应的 IPersistentService，跳过");
                    continue;
                }
                try
                {
                    if (!svc.RestoreJson(json))
                    {
                        Debug.LogWarning($"[SaveCoordinator] Restore '{key}' 返回 false");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveCoordinator] Restore '{key}' 抛异常：{ex.Message}");
                }
            }

            _eventBus?.Publish(new SaveSlotLoadedEvent(slotId));
            return true;
        }

        public async Task DeleteAsync(string slotId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(slotId)) return;
            await _saveSystem.DeleteSlotAsync(slotId, cancellationToken).ConfigureAwait(true);
            _eventBus?.Publish(new SaveSlotDeletedEvent(slotId));
        }
    }
}
