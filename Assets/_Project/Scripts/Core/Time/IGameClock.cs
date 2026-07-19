#nullable enable
using System;

namespace GeminiLab.Core.Time
{
    /// <summary>
    /// 全局统一时间源。
    /// 业务代码（塔罗每日限制、花园离线生长、宠物日夜判定 …）必须从这里取时间，
    /// 禁止直接调用 <see cref="System.DateTime.Now"/> / <see cref="System.DateTime.UtcNow"/>。
    /// 这样做的理由：
    /// 1. 方便单元测试（注入固定时钟）
    /// 2. 方便加 time-skip / debug 快进
    /// 3. 未来接服务端校准时间只改这里一处
    /// </summary>
    public interface IGameClock
    {
        /// <summary>本机本地时间（随各业务使用）。</summary>
        DateTime Now { get; }

        /// <summary>本机 UTC 时间（跨时区/存档时间戳用）。</summary>
        DateTime UtcNow { get; }

        /// <summary>今日日期字符串，格式 <c>yyyy-MM-dd</c>（本地时区）。</summary>
        string TodayIso { get; }

        /// <summary>
        /// 判断给定 ISO 日期串是不是"今天"。空串返回 false。
        /// 用于每日重置类逻辑（塔罗、每日签到）。
        /// </summary>
        bool IsToday(string isoDate);

        /// <summary>
        /// 计算给定 UTC 时间戳到现在经过的时间差；时间戳空/非法返回 <see cref="TimeSpan.Zero"/>。
        /// 花园离线推进、旅行 ETA 等用这个。
        /// </summary>
        TimeSpan ElapsedSinceUtc(DateTime utcWhen);

        /// <summary>
        /// [调试] 将时钟整体拨快 N 天，影响 Now / UtcNow / TodayIso。
        /// 所有每日重置类系统（情绪花园、塔罗等）会随之进入新的一天。
        /// 开发者模式下偏移持久化（跨 Play 会话连续），玩家模式永远真实时间。
        /// </summary>
        void DebugAdvanceDays(int days);

        /// <summary>[调试] 清零调试偏移，回到真实系统时间。</summary>
        void DebugResetClock();
    }
}
