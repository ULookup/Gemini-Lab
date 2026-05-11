#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using UnityEngine;

namespace GeminiLab.Modules.Collection
{
    /// <summary>
    /// 默认收藏服务。单例 List 存储 + 按 Id 去重。
    /// </summary>
    public sealed class CollectionService : ICollectionService, IPersistentService
    {
        private readonly List<CollectionEntry> _entries = new();
        private readonly EventBus? _eventBus;

        public CollectionService(EventBus? eventBus) { _eventBus = eventBus; }

        public string Key => "collection";

        public IReadOnlyList<CollectionEntry> All => _entries;

        public IEnumerable<CollectionEntry> GetByCategory(CollectionCategory category)
        {
            foreach (var e in _entries)
            {
                if (e.Category == category) yield return e;
            }
        }

        public void Add(CollectionEntry entry)
        {
            if (entry.IsEmpty) return;

            int existing = _entries.FindIndex(e => e.Id == entry.Id);
            if (existing >= 0)
            {
                _entries[existing] = entry;
            }
            else
            {
                _entries.Add(entry);
            }

            _eventBus?.Publish(new CollectionAddedEvent(entry));
            _eventBus?.Publish(new CollectionChangedEvent());
        }

        public bool TryRemove(string id)
        {
            int idx = _entries.FindIndex(e => e.Id == id);
            if (idx < 0) return false;
            _entries.RemoveAt(idx);
            _eventBus?.Publish(new CollectionChangedEvent());
            return true;
        }

        public void Clear()
        {
            _entries.Clear();
            _eventBus?.Publish(new CollectionChangedEvent());
        }

        // ---- IPersistentService ----
        [Serializable]
        private struct SavePayload
        {
            public int version;
            public CollectionEntry[] entries;
        }

        public string CaptureJson()
        {
            return JsonUtility.ToJson(new SavePayload { version = 1, entries = _entries.ToArray() });
        }

        public bool RestoreJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                var payload = JsonUtility.FromJson<SavePayload>(json);
                _entries.Clear();
                if (payload.entries != null) _entries.AddRange(payload.entries);
                _eventBus?.Publish(new CollectionChangedEvent());
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
