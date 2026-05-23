#nullable enable

namespace GeminiLab.Core.Time
{
    /// <summary>跨天切换后由 DailyResetService 广播的事件。任何业务可订阅做每日刷新。</summary>
    public readonly struct NewDayStartedEvent
    {
        public NewDayStartedEvent(string previousDateIso, string currentDateIso)
        {
            PreviousDateIso = previousDateIso;
            CurrentDateIso = currentDateIso;
        }

        /// <summary>上次记录的日期；首次启动 / 存档缺失时为空串。</summary>
        public string PreviousDateIso { get; }

        /// <summary>当前日期（已写入记录）。</summary>
        public string CurrentDateIso { get; }
    }
}
