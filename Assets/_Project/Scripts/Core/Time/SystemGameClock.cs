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
        private const string OffsetPrefsKey = "GeminiLab.Debug.ClockOffsetDays";

        private TimeSpan _debugOffset = TimeSpan.Zero;

        public SystemGameClock()
        {
            // 仅开发者模式恢复调试偏移，保证虚拟日期跨 Play 会话连续；
            // 玩家模式（含打包版本）永远真实时间
            if (DevMode.Active)
            {
                int days = UnityEngine.PlayerPrefs.GetInt(OffsetPrefsKey, 0);
                if (days != 0)
                {
                    _debugOffset = TimeSpan.FromDays(days);
                    UnityEngine.Debug.Log($"[GameClock] 恢复调试偏移 {days} 天，当前游戏日期: {TodayIso}");
                }
            }
        }

        public DateTime Now => DateTime.Now + _debugOffset;

        public DateTime UtcNow => DateTime.UtcNow + _debugOffset;

        public string TodayIso => Now.ToString("yyyy-MM-dd");

        public void DebugAdvanceDays(int days)
        {
            _debugOffset += TimeSpan.FromDays(days);
            UnityEngine.PlayerPrefs.SetInt(OffsetPrefsKey, (int)_debugOffset.TotalDays);
            UnityEngine.PlayerPrefs.Save();
            UnityEngine.Debug.Log($"[GameClock] 调试快进 {days} 天，当前游戏日期: {TodayIso}");
        }

        public void DebugResetClock()
        {
            _debugOffset = TimeSpan.Zero;
            UnityEngine.PlayerPrefs.DeleteKey(OffsetPrefsKey);
            UnityEngine.PlayerPrefs.Save();
            UnityEngine.Debug.Log($"[GameClock] 调试偏移已清零，当前游戏日期: {TodayIso}");
        }

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
