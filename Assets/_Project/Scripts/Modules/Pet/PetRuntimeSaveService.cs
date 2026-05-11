#nullable enable
using System;
using GeminiLab.Core.Persistence;
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// 让 Pet 的两只运行态数据随 SaveBundle 走。
    /// 实现 <see cref="IPersistentService"/>，由 PetRuntimeBootstrap 注册。
    /// </summary>
    public sealed class PetRuntimeSaveService : IPersistentService
    {
        private readonly IPetRoster _roster;

        public PetRuntimeSaveService(IPetRoster roster)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
        }

        public string Key => "pet_runtime";

        [Serializable]
        private struct Entry
        {
            public int petId;
            public float mood;
            public float energy;
            public float satiety;
            public float runtimeTime;
            public int travelCompletedCount;
            public string currentState;
            public string lastInteractionFurnitureId;
            public string lastInteractionSummary;
        }

        [Serializable]
        private struct SavePayload
        {
            public int version;
            public Entry[] entries;
        }

        public string CaptureJson()
        {
            var pets = _roster.RegisteredPets;
            var entries = new Entry[pets.Count];
            for (int i = 0; i < pets.Count; i++)
            {
                var id = pets[i];
                var d = _roster.TryGet(id);
                if (d == null)
                {
                    entries[i] = new Entry { petId = (int)id };
                    continue;
                }
                entries[i] = new Entry
                {
                    petId = (int)id,
                    mood = d.Mood,
                    energy = d.Energy,
                    satiety = d.Satiety,
                    runtimeTime = d.RuntimeTimeSeconds,
                    travelCompletedCount = d.TravelCompletedCount,
                    currentState = d.CurrentState,
                    lastInteractionFurnitureId = d.LastInteractionFurnitureId ?? string.Empty,
                    lastInteractionSummary = d.LastInteractionSummary ?? string.Empty
                };
            }
            return JsonUtility.ToJson(new SavePayload { version = 1, entries = entries });
        }

        public bool RestoreJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                var payload = JsonUtility.FromJson<SavePayload>(json);
                if (payload.entries == null) return true;

                foreach (var e in payload.entries)
                {
                    var petId = (PetId)e.petId;
                    var d = _roster.TryGet(petId);
                    if (d == null) continue;
                    d.Mood = e.mood;
                    d.Energy = e.energy;
                    d.Satiety = e.satiety;
                    d.RuntimeTimeSeconds = e.runtimeTime;
                    d.TravelCompletedCount = e.travelCompletedCount;
                    d.CurrentState = e.currentState ?? "None";
                    d.LastInteractionFurnitureId = e.lastInteractionFurnitureId ?? string.Empty;
                    d.LastInteractionSummary = e.lastInteractionSummary ?? string.Empty;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
