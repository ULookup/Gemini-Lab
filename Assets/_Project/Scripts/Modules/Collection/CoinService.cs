#nullable enable
using System;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using UnityEngine;

namespace GeminiLab.Modules.Collection
{
    public sealed class CoinService : ICoinService, IPersistentService
    {
        private readonly EventBus? _eventBus;

        public CoinService(EventBus? eventBus) { _eventBus = eventBus; Balance = 200; }

        public string Key => "coin";

        public int Balance { get; private set; }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            Balance += amount;
            _eventBus?.Publish(new CoinChangedEvent(Balance));
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0) return false;
            if (Balance < amount) return false;
            Balance -= amount;
            _eventBus?.Publish(new CoinChangedEvent(Balance));
            return true;
        }

        // ---- IPersistentService ----
        [Serializable]
        private struct SavePayload
        {
            public int version;
            public int balance;
        }

        public string CaptureJson()
        {
            return JsonUtility.ToJson(new SavePayload { version = 1, balance = Balance });
        }

        public bool RestoreJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                var payload = JsonUtility.FromJson<SavePayload>(json);
                Balance = payload.balance;
                _eventBus?.Publish(new CoinChangedEvent(Balance));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
