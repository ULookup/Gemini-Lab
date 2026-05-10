#nullable enable
using System.Collections.Generic;

namespace GeminiLab.Core.SceneFlow
{
    /// <summary>
    /// 场景跳转时携带的一次性数据包。
    /// 目标场景读完后自行消费，不保证跨多次跳转存活。
    /// </summary>
    public sealed class SceneTransitionPayload
    {
        private readonly Dictionary<string, object?> _values = new();

        public SceneId? SourceScene { get; set; }
        public string? EntrySpawnId { get; set; }

        public SceneTransitionPayload Set(string key, object? value)
        {
            _values[key] = value;
            return this;
        }

        public bool TryGet<T>(string key, out T? value)
        {
            if (_values.TryGetValue(key, out object? raw) && raw is T typed)
            {
                value = typed;
                return true;
            }

            value = default;
            return false;
        }
    }
}
