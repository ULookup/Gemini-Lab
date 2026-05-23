#nullable enable
using System;

namespace GeminiLab.Core.Time
{
    /// <summary>
    /// 测试 / debug 用的可控时钟。
    /// 不会被 <see cref="GameBootstrap"/> 注册；业务测试自行 new + 注入 ServiceLocator。
    /// </summary>
    public sealed class FakeGameClock : IGameClock
    {
        public DateTime Now { get; set; } = DateTime.Now;
        public DateTime UtcNow { get; set; } = DateTime.UtcNow;

        public string TodayIso => Now.ToString("yyyy-MM-dd");

        public bool IsToday(string isoDate)
        {
            if (string.IsNullOrEmpty(isoDate))
            {
                return false;
            }

            return string.Equals(isoDate, TodayIso, StringComparison.Ordinal);
        }

        public TimeSpan ElapsedSinceUtc(DateTime utcWhen)
        {
            if (utcWhen == default)
            {
                return TimeSpan.Zero;
            }

            return UtcNow > utcWhen ? UtcNow - utcWhen : TimeSpan.Zero;
        }

        public void Advance(TimeSpan delta)
        {
            Now += delta;
            UtcNow += delta;
        }

        public void SetLocal(DateTime local)
        {
            Now = local;
            UtcNow = local.ToUniversalTime();
        }
    }
}
