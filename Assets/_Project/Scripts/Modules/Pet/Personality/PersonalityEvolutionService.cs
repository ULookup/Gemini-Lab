#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using GeminiLab.Modules.Tarot;
using UnityEngine;

namespace GeminiLab.Modules.Pet.Personality
{
    /// <summary>
    /// 默认性格演化实现。
    /// - 对每只 PetId 维护一个运行态 PersonalityVector
    /// - 订阅 <see cref="CardDrawnEvent"/> + <see cref="PetInteractionCompletedEvent"/> 按 Rules SO 叠加
    /// - 实现 <see cref="IPersistentService"/>，`matrices` 以 SaveBundle 随存档走
    /// </summary>
    public sealed class PersonalityEvolutionService : IPersonalityEvolutionService, IPersistentService, IDisposable
    {
        private readonly PersonalityEvolutionRulesSO _rules;
        private readonly EventBus? _eventBus;
        private readonly Dictionary<PetId, PersonalityVector> _matrices = new();
        private IDisposable? _tarotSub;
        private IDisposable? _interactSub;
        private IDisposable? _petInitSub;

        public PersonalityEvolutionService(PersonalityEvolutionRulesSO rules, EventBus? eventBus)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _eventBus = eventBus;

            if (_eventBus is not null)
            {
                _tarotSub = _eventBus.Subscribe<CardDrawnEvent>(OnCardDrawn);
                _interactSub = _eventBus.Subscribe<PetInteractionCompletedEvent>(OnInteractionCompleted);
                _petInitSub = _eventBus.Subscribe<PetControllerInitializedEvent>(OnPetControllerInitialized);
            }
        }

        private void OnPetControllerInitialized(PetControllerInitializedEvent evt)
        {
            // 存档恢复若已写入则不覆盖
            if (_matrices.ContainsKey(evt.PetId)) return;
            SetInitialMatrix(evt.PetId, PersonalityVector.FromSO(evt.PersonalityMatrix));
        }

        public string Key => "personality";

        public event Action<PetId, PersonalityVector>? MatrixChanged;

        public PersonalityVector GetMatrix(PetId petId)
        {
            return _matrices.TryGetValue(petId, out var v) ? v : default;
        }

        public void SetInitialMatrix(PetId petId, PersonalityVector initial)
        {
            _matrices[petId] = initial.Clamp();
            MatrixChanged?.Invoke(petId, _matrices[petId]);
        }

        public bool SeedInitialMatrixIfAbsent(PetId petId, PersonalityMatrixSO? matrix)
        {
            // 存档恢复若已写入则不覆盖（与 OnPetControllerInitialized 的守卫一致）
            if (_matrices.ContainsKey(petId)) return false;
            SetInitialMatrix(petId, PersonalityVector.FromSO(matrix));
            return true;
        }

        public void Dispose()
        {
            _tarotSub?.Dispose();
            _interactSub?.Dispose();
            _petInitSub?.Dispose();
        }

        private void OnCardDrawn(CardDrawnEvent evt)
        {
            if (_rules.TarotRules == null) return;

            string cardId = evt.CardId;
            bool isUpright = evt.Orientation == TarotOrientation.Upright;

            foreach (var rule in _rules.TarotRules)
            {
                if (!string.IsNullOrEmpty(rule.CardId) && rule.CardId != cardId) continue;

                switch (rule.Filter)
                {
                    case PersonalityEvolutionRulesSO.OrientationFilter.UprightOnly when !isUpright: continue;
                    case PersonalityEvolutionRulesSO.OrientationFilter.ReversedOnly when isUpright: continue;
                }

                PetId target = ResolveTargetPet(rule.TargetPet, isUpright);
                ApplyDelta(target, ScaleDelta(rule.Delta, _rules.GlobalDeltaScale));
            }
        }

        private void OnInteractionCompleted(PetInteractionCompletedEvent evt)
        {
            if (_rules.FurnitureRules == null) return;

            foreach (var rule in _rules.FurnitureRules)
            {
                if (rule.Type != Furniture.FurnitureInteractionType.Unknown && rule.Type != evt.InteractionType)
                {
                    continue;
                }

                PetId target = rule.TargetPet switch
                {
                    PersonalityEvolutionRulesSO.PetIdFilter.Angel => PetId.Angel,
                    PersonalityEvolutionRulesSO.PetIdFilter.Devil => PetId.Devil,
                    _ => evt.PetId
                };
                ApplyDelta(target, ScaleDelta(rule.Delta, _rules.GlobalDeltaScale));
            }
        }

        private static PetId ResolveTargetPet(PersonalityEvolutionRulesSO.PetIdFilter filter, bool isUpright)
        {
            return filter switch
            {
                PersonalityEvolutionRulesSO.PetIdFilter.Angel => PetId.Angel,
                PersonalityEvolutionRulesSO.PetIdFilter.Devil => PetId.Devil,
                _ => isUpright ? PetId.Angel : PetId.Devil
            };
        }

        private static PersonalityVector ScaleDelta(PersonalityVector d, float scale)
        {
            return new PersonalityVector
            {
                Kindness = d.Kindness * scale,
                Evilness = d.Evilness * scale,
                Calmness = d.Calmness * scale,
                Bravery = d.Bravery * scale,
                Shyness = d.Shyness * scale,
                Integrity = d.Integrity * scale,
                Curiosity = d.Curiosity * scale
            };
        }

        private void ApplyDelta(PetId petId, PersonalityVector delta)
        {
            var current = _matrices.TryGetValue(petId, out var v) ? v : default;
            var next = (current + delta).Clamp();
            _matrices[petId] = next;
            MatrixChanged?.Invoke(petId, next);
        }

        // ---- IPersistentService ----
        [Serializable]
        private struct Entry
        {
            public int petId;
            public PersonalityVector vector;
        }

        [Serializable]
        private struct SavePayload
        {
            public int version;
            public Entry[] entries;
        }

        public string CaptureJson()
        {
            var entries = new Entry[_matrices.Count];
            int i = 0;
            foreach (var kv in _matrices)
            {
                entries[i++] = new Entry { petId = (int)kv.Key, vector = kv.Value };
            }
            return JsonUtility.ToJson(new SavePayload { version = 2, entries = entries });
        }

        public bool RestoreJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                var payload = JsonUtility.FromJson<SavePayload>(json);

                // v1 存档（初始矩阵接入修复前）是基于全 0 初始矩阵累计的：当时编辑器
                // Play 时序让 PetControllerInitializedEvent 被错过，初始矩阵从未写入，
                // 演化增量全部叠在 0 基数上，恢复它会覆盖启动补种写入的真实初始矩阵
                // （雷达又变全 0 正多边形）。v1 一律忽略，由启动补种/宠物初始化事件
                // 以真实 PersonalityMatrixSO 为基数重新建立。
                if (payload.version < 2)
                {
                    Debug.Log("[PersonalityEvolutionService] 忽略旧版性格存档(v1，0 基数累计，已由真实初始矩阵取代)");
                    return true;
                }

                _matrices.Clear();
                if (payload.entries != null)
                {
                    foreach (var e in payload.entries)
                    {
                        var petId = (PetId)e.petId;
                        _matrices[petId] = e.vector.Clamp();
                        MatrixChanged?.Invoke(petId, _matrices[petId]);
                    }
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
