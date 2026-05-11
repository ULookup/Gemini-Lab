#nullable enable
using System;

namespace GeminiLab.Core.Time
{
    /// <summary>
    /// 默认 <see cref="IGameClock"/> 实现：直接代理系统时钟。
    /// 真实运行时由 <see cref="GameBootstrap"/> 注册；测试时可注入 <see cref="FakeGameClock"/>。
    /// </summary>
    public sealed class SystemGameClock : IGameClock
    {
        public DateTime Now => DateTime.Now;

        public DateTime UtcNow => DateTime.UtcNow;

        public string TodayIso => DateTime.Now.ToString("yyyy-MM-dd");

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

            DateTime now = UtcNow;
            return now > utcWhen ? now - utcWhen : TimeSpan.Zero;
        }
    }
}
