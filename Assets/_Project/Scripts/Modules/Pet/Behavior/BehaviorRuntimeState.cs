#nullable enable
using System.Collections.Generic;

namespace GeminiLab.Modules.Pet.Behavior
{
    /// <summary>
    /// 行为运行态（数值规则文档 §22 BehaviorRuntimeState）：
    /// 当前行为、最近 3 个行为、冷却表。只存活于运行期，不随存档持久化。
    /// </summary>
    public sealed class BehaviorRuntimeState
    {
        /// <summary>重复判定窗口长度（§9 "最近三个行为"）。</summary>
        public const int RecentWindowSize = 3;

        private readonly Queue<string> _recentBehaviors = new();
        private readonly Dictionary<string, float> _cooldownUntilSeconds = new();

        /// <summary>正在执行的行为；空闲时为 Empty。</summary>
        public string CurrentBehaviorId = string.Empty;

        /// <summary>上一次完成的行为；没有时为 null。</summary>
        public string? LastCompletedBehaviorId { get; private set; }

        public IReadOnlyCollection<string> RecentBehaviors => _recentBehaviors;

        /// <summary>
        /// 重复倍率（§9），判定优先级 Cooldown &gt; LastAction &gt; Recent3 &gt; None。
        /// </summary>
        public float RepeatMultiplierFor(string behaviorId, float nowSeconds)
        {
            if (IsOnCooldown(behaviorId, nowSeconds))
            {
                return 0f;
            }

            if (LastCompletedBehaviorId == behaviorId)
            {
                return 0.2f;
            }

            return ContainsInRecent(behaviorId) ? 0.6f : 1f;
        }

        public bool IsOnCooldown(string behaviorId, float nowSeconds)
        {
            return _cooldownUntilSeconds.TryGetValue(behaviorId, out float until) && nowSeconds < until;
        }

        /// <summary>行为完成时记录：推入最近窗口并从行为结束时刻起算冷却（§10）。</summary>
        public void RecordCompletion(string behaviorId, float nowSeconds, float cooldownSeconds)
        {
            if (string.IsNullOrEmpty(behaviorId))
            {
                return;
            }

            LastCompletedBehaviorId = behaviorId;
            _recentBehaviors.Enqueue(behaviorId);
            while (_recentBehaviors.Count > RecentWindowSize)
            {
                _recentBehaviors.Dequeue();
            }

            if (cooldownSeconds > 0f)
            {
                _cooldownUntilSeconds[behaviorId] = nowSeconds + cooldownSeconds;
            }
            else
            {
                _cooldownUntilSeconds.Remove(behaviorId);
            }
        }

        public bool ContainsInRecent(string behaviorId)
        {
            return _recentBehaviors.Contains(behaviorId);
        }

        /// <summary>测试/调试用：冷却截止时刻，未记录返回 null。</summary>
        public float? GetCooldownUntil(string behaviorId)
        {
            return _cooldownUntilSeconds.TryGetValue(behaviorId, out float until) ? until : null;
        }

        public void Clear()
        {
            CurrentBehaviorId = string.Empty;
            LastCompletedBehaviorId = null;
            _recentBehaviors.Clear();
            _cooldownUntilSeconds.Clear();
        }
    }
}
