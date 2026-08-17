#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using GeminiLab.Core.Time;
using UnityEngine;

namespace GeminiLab.Modules.Apple
{
    /// <summary>
    /// 苹果资源默认实现。
    /// - 新档初始 20 个苹果。
    /// - 每棵树每 6 小时生成 1 个，单树缓存上限 3 个。
    /// - 生成进度使用 IGameClock.UtcNow，并把时间戳和缓存一起写入存档。
    /// </summary>
    public sealed class AppleService : IAppleService
    {
        public const int DefaultInitialBalance = 20;
        public const int DefaultGenerationIntervalMinutes = 360;
        public const int DefaultMaxPendingPerTree = 3;

        private readonly IGameClock _clock;
        private readonly EventBus? _eventBus;
        private readonly Dictionary<string, AppleTreeState> _trees = new(StringComparer.Ordinal);

        public AppleService(
            IGameClock clock,
            EventBus? eventBus,
            int initialBalance = DefaultInitialBalance,
            int generationIntervalMinutes = DefaultGenerationIntervalMinutes,
            int maxPendingPerTree = DefaultMaxPendingPerTree)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _eventBus = eventBus;
            InitialBalance = Mathf.Max(0, initialBalance);
            Balance = InitialBalance;
            GenerationIntervalMinutes = Mathf.Max(1, generationIntervalMinutes);
            MaxPendingPerTree = Mathf.Max(1, maxPendingPerTree);
        }

        public string Key => "apple";
        public int Balance { get; private set; }
        public int InitialBalance { get; }
        public int GenerationIntervalMinutes { get; }
        public int MaxPendingPerTree { get; }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            Balance = SafeAdd(Balance, amount);
            _eventBus?.Publish(new AppleChangedEvent(Balance, amount));
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0 || Balance < amount) return false;
            Balance -= amount;
            _eventBus?.Publish(new AppleChangedEvent(Balance, -amount));
            return true;
        }

        public void EnsureTree(string treeId)
        {
            if (string.IsNullOrWhiteSpace(treeId)) return;
            string normalized = treeId.Trim();
            if (_trees.ContainsKey(normalized)) return;

            var state = new AppleTreeState
            {
                TreeId = normalized,
                LastGeneratedUtcTicks = _clock.UtcNow.Ticks,
                PendingCount = 0,
                TotalCollected = 0
            };
            _trees.Add(normalized, state);
            _eventBus?.Publish(new AppleTreeChangedEvent(state));
        }

        public int GetPendingCount(string treeId)
        {
            if (string.IsNullOrWhiteSpace(treeId)) return 0;
            EnsureTree(treeId);
            string normalized = treeId.Trim();
            var state = _trees[normalized];
            GeneratePending(ref state);
            _trees[normalized] = state;
            return state.PendingCount;
        }

        public int ShakeTree(string treeId)
        {
            if (string.IsNullOrWhiteSpace(treeId)) return 0;
            EnsureTree(treeId);
            string normalized = treeId.Trim();
            var state = _trees[normalized];
            GeneratePending(ref state);

            int collected = state.PendingCount;
            if (collected <= 0)
            {
                _trees[normalized] = state;
                return 0;
            }

            state.PendingCount = 0;
            state.TotalCollected = SafeAdd(state.TotalCollected, collected);
            _trees[normalized] = state;
            Add(collected);
            _eventBus?.Publish(new AppleTreeChangedEvent(state));
            _eventBus?.Publish(new AppleTreeShakenEvent(normalized, collected));
            return collected;
        }

        public IReadOnlyList<AppleTreeState> GetTreeStates()
        {
            var result = new List<AppleTreeState>(_trees.Count);
            foreach (string treeId in new List<string>(_trees.Keys))
            {
                GetPendingCount(treeId);
                result.Add(_trees[treeId]);
            }
            return result;
        }

        public string CaptureJson()
        {
            var states = new List<AppleTreeState>(_trees.Count);
            foreach (var state in _trees.Values)
            {
                states.Add(state);
            }

            return JsonUtility.ToJson(new SavePayload
            {
                Version = 1,
                Balance = Mathf.Max(0, Balance),
                Trees = states
            });
        }

        public bool RestoreJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                var payload = JsonUtility.FromJson<SavePayload>(json);
                if (payload == null) return false;

                Balance = Mathf.Max(0, payload.Balance);
                _trees.Clear();
                if (payload.Trees != null)
                {
                    foreach (var saved in payload.Trees)
                    {
                        if (string.IsNullOrWhiteSpace(saved.TreeId)) continue;
                        var state = saved;
                        state.TreeId = state.TreeId.Trim();
                        state.LastGeneratedUtcTicks = state.LastGeneratedUtcTicks > 0
                            ? state.LastGeneratedUtcTicks
                            : _clock.UtcNow.Ticks;
                        state.PendingCount = Mathf.Clamp(state.PendingCount, 0, MaxPendingPerTree);
                        state.TotalCollected = Mathf.Max(0, state.TotalCollected);
                        _trees[state.TreeId] = state;
                    }
                }

                _eventBus?.Publish(new AppleChangedEvent(Balance, 0));
                foreach (var state in _trees.Values)
                {
                    _eventBus?.Publish(new AppleTreeChangedEvent(state));
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AppleService] 存档恢复失败：{e.Message}");
                return false;
            }
        }

        private void GeneratePending(ref AppleTreeState state)
        {
            long nowTicks = _clock.UtcNow.Ticks;
            if (state.LastGeneratedUtcTicks <= 0)
            {
                state.LastGeneratedUtcTicks = nowTicks;
                return;
            }

            long elapsedTicks = nowTicks - state.LastGeneratedUtcTicks;
            long intervalTicks = TimeSpan.FromMinutes(GenerationIntervalMinutes).Ticks;
            if (elapsedTicks < intervalTicks || intervalTicks <= 0) return;

            long generatedIntervals = elapsedTicks / intervalTicks;
            state.LastGeneratedUtcTicks = SafeAddTicks(state.LastGeneratedUtcTicks, generatedIntervals * intervalTicks);
            state.PendingCount = Mathf.Clamp(
                SafeAdd(state.PendingCount, generatedIntervals > int.MaxValue ? int.MaxValue : (int)generatedIntervals),
                0,
                MaxPendingPerTree);
        }

        private static int SafeAdd(int left, int right)
        {
            long sum = (long)left + right;
            return sum > int.MaxValue ? int.MaxValue : (int)Mathf.Max(0, (float)sum);
        }

        private static long SafeAddTicks(long left, long right)
        {
            if (right > 0 && left > long.MaxValue - right) return long.MaxValue;
            return left + right;
        }

        [Serializable]
        private sealed class SavePayload
        {
            public int Version;
            public int Balance;
            public List<AppleTreeState> Trees = new();
        }
    }
}
