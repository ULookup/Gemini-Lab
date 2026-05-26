#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core.Persistence;
using UnityEngine;

namespace GeminiLab.Modules.Tarot
{
    public interface ITarotSessionRecordStore
    {
        void Add(TarotSessionRecord record);
        IReadOnlyList<TarotSessionRecord> GetAll();
        bool Remove(string sessionId);
    }

    public sealed class TarotSessionRecordStore : ITarotSessionRecordStore, IPersistentService
    {
        private readonly List<TarotSessionRecord> _records = new();

        public string Key => "tarot_history";

        public void Add(TarotSessionRecord record)
        {
            if (string.IsNullOrEmpty(record.SessionId)) return;
            int existing = _records.FindIndex(r => r.SessionId == record.SessionId);
            if (existing >= 0)
                _records[existing] = record;
            else
                _records.Add(record);
        }

        public IReadOnlyList<TarotSessionRecord> GetAll()
        {
            _records.Sort((a, b) => string.Compare(b.SessionDateIso, a.SessionDateIso, StringComparison.Ordinal));
            return _records;
        }

        public bool Remove(string sessionId)
        {
            int idx = _records.FindIndex(r => r.SessionId == sessionId);
            if (idx < 0) return false;
            _records.RemoveAt(idx);
            return true;
        }

        // ---- IPersistentService ----

        [Serializable]
        private struct SavePayload
        {
            public int version;
            public TarotSessionRecord[] records;
        }

        public string CaptureJson()
        {
            return JsonUtility.ToJson(new SavePayload { version = 1, records = _records.ToArray() });
        }

        public bool RestoreJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                var payload = JsonUtility.FromJson<SavePayload>(json);
                _records.Clear();
                if (payload.records != null) _records.AddRange(payload.records);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
