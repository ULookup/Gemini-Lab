#nullable enable

namespace GeminiLab.Core.Time
{
    /// <summary>
    /// 每日重置协调器。业务代码调用 <see cref="CheckAndReset"/> 或由场景切换 / 启动阶段触发；
    /// 跨天时服务会写回最后记录日期并通过 EventBus 广播 <see cref="NewDayStartedEvent"/>。
    /// 订阅者自行决定重置什么（塔罗 lastDrawDate / 花园阶段推进 / 每日奖励…）。
    /// </summary>
    public interface IDailyResetService
    {
        /// <summary>上次记录的日期 ISO（yyyy-MM-dd）；首次启动为空。</summary>
        string LastRecordedDateIso { get; }

        /// <summary>
        /// 与 <see cref="IGameClock.TodayIso"/> 对比；跨天则写回新日期 + 广播事件。
        /// 幂等：同一天内反复调用只在第一次触发。
        /// </summary>
        /// <returns>是否触发了新一天事件。</returns>
        bool CheckAndReset();
    }
}
