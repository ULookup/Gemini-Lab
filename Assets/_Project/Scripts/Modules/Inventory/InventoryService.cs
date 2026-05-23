#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using UnityEngine;

namespace GeminiLab.Modules.Inventory
{
    /// <summary>
    /// <see cref="IInventoryService"/> 默认实现。
    /// - 堆叠策略：每个 itemId 一格，满格再开新格（按 ItemDefSO.MaxPerStack）。
    /// - 当前持久化：内存；C1 阶段通过 <see cref="IPersistentService"/> 接入 SaveSystem。
    /// </summary>
    public sealed class InventoryService : IInventoryService, IPersistentService
    {
        private readonly ItemCatalogSO _catalog;
        private readonly EventBus? _eventBus;
        private readonly List<ItemStack> _stacks = new();

        public InventoryService(ItemCatalogSO catalog, EventBus? eventBus)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _eventBus = eventBus;
        }

        public string Key => "inventory";

        public IReadOnlyList<ItemStack> GetAllStacks()
        {
            return _stacks.AsReadOnly();
        }

        public int GetTotalCount(string itemId)
        {
            int total = 0;
            foreach (var s in _stacks)
            {
                if (s.ItemId == itemId) total += s.Count;
            }
            return total;
        }

        public int Add(string itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0) return 0;

            var def = _catalog.Get(itemId);
            if (def == null)
            {
                Debug.LogWarning($"[Inventory] 未登记的 itemId：{itemId}");
                return 0;
            }

            int remaining = count;
            int max = def.MaxPerStack;

            if (def.Stackable)
            {
                // 先填已存在的非满堆
                for (int i = 0; i < _stacks.Count && remaining > 0; i++)
                {
                    if (_stacks[i].ItemId != itemId) continue;
                    int space = max - _stacks[i].Count;
                    if (space <= 0) continue;
                    int put = Mathf.Min(space, remaining);
                    var s = _stacks[i];
                    s.Count += put;
                    _stacks[i] = s;
                    remaining -= put;
                }
            }

            // 再开新堆
            while (remaining > 0)
            {
                int put = Mathf.Min(max, remaining);
                _stacks.Add(new ItemStack(itemId, put));
                remaining -= put;
            }

            int added = count - remaining;
            if (added > 0)
            {
                _eventBus?.Publish(new InventoryChangedEvent(itemId, added));
            }
            return added;
        }

        public bool TryRemove(string itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0) return false;
            if (GetTotalCount(itemId) < count) return false;

            int remaining = count;
            for (int i = _stacks.Count - 1; i >= 0 && remaining > 0; i--)
            {
                if (_stacks[i].ItemId != itemId) continue;
                if (_stacks[i].Count > remaining)
                {
                    var s = _stacks[i];
                    s.Count -= remaining;
                    _stacks[i] = s;
                    remaining = 0;
                }
                else
                {
                    remaining -= _stacks[i].Count;
                    _stacks.RemoveAt(i);
                }
            }

            _eventBus?.Publish(new InventoryChangedEvent(itemId, -count));
            return true;
        }

        public void Clear()
        {
            _stacks.Clear();
            _eventBus?.Publish(new InventoryChangedEvent(string.Empty, 0));
        }

        // ---- IPersistentService ----
        [Serializable]
        private struct SavePayload
        {
            public int version;
            public ItemStack[] stacks;
        }

        public string CaptureJson()
        {
            return JsonUtility.ToJson(new SavePayload { version = 1, stacks = _stacks.ToArray() });
        }

        public bool RestoreJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                var payload = JsonUtility.FromJson<SavePayload>(json);
                _stacks.Clear();
                if (payload.stacks != null)
                {
                    _stacks.AddRange(payload.stacks);
                }
                _eventBus?.Publish(new InventoryChangedEvent(string.Empty, 0));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
